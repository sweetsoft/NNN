using System;
using System.Collections.Generic;
using System.Linq;

namespace NNN
{
    public sealed class ObservationSimulationState
    {
        public int CurrentDay { get; internal set; }
        public int LastMajorEventDay { get; internal set; } = -99;
        public RelationshipState Relationship { get; } = new RelationshipState { HumanToCat = HumanToCatState.Avoid, CatWariness = CatWarinessState.High, Settlement = SettlementState.Unknown };
        public HashSet<RelationshipHistoryFlag> HistoryFlags { get; } = new HashSet<RelationshipHistoryFlag>();
        public HashSet<RelationshipMemory> MemoryFlags { get; } = new HashSet<RelationshipMemory>();
        public HashSet<string> OccurredEventIds { get; } = new HashSet<string>();
        public Queue<string> RecentNormalActionIds { get; } = new Queue<string>();
    }

    public static class ObservationConditionEvaluator
    {
        public static bool Evaluate(ObservationEventDefinition definition, ObservationRouteDefinition route, ObservationSimulationState state)
        {
            if (definition == null || state.CurrentDay < definition.EarliestDay || state.CurrentDay > definition.LatestDay) return false;
            if (!definition.Repeatable && state.OccurredEventIds.Contains(definition.Id)) return false;
            return definition.Conditions.All(condition => Evaluate(condition, route, state));
        }

        private static bool Evaluate(ObservationEventCondition c, ObservationRouteDefinition route, ObservationSimulationState s)
        {
            switch (c.Type)
            {
                case ObservationConditionType.HumanStateAtLeast: return s.Relationship.HumanToCat >= c.HumanState;
                case ObservationConditionType.HumanStateAtMost: return s.Relationship.HumanToCat <= c.HumanState;
                // Enum order moves from High to Relaxed; "at most Medium" therefore means Medium/Low/Relaxed.
                case ObservationConditionType.WarinessAtLeast: return s.Relationship.CatWariness <= c.Wariness;
                case ObservationConditionType.WarinessAtMost: return s.Relationship.CatWariness >= c.Wariness;
                case ObservationConditionType.SettlementAtLeast: return s.Relationship.Settlement >= c.Settlement;
                case ObservationConditionType.SettlementAtMost: return s.Relationship.Settlement <= c.Settlement;
                case ObservationConditionType.HasHistory: return s.HistoryFlags.Contains(c.History);
                case ObservationConditionType.MissingHistory: return !s.HistoryFlags.Contains(c.History);
                case ObservationConditionType.HasMemory: return s.MemoryFlags.Contains(c.Memory);
                case ObservationConditionType.MissingMemory: return !s.MemoryFlags.Contains(c.Memory);
                case ObservationConditionType.EventOccurred: return s.OccurredEventIds.Contains(c.StringValue);
                case ObservationConditionType.EventNotOccurred: return !s.OccurredEventIds.Contains(c.StringValue);
                case ObservationConditionType.DaysSinceLastMajorAtLeast: return s.CurrentDay - s.LastMajorEventDay >= c.IntValue;
                case ObservationConditionType.CatTrait: return route.Cat != null && route.Cat.HasTrait(c.StringValue);
                case ObservationConditionType.HumanTrait: return route.Human != null && route.Human.Archetype != null && route.Human.Archetype.PreferredTraits.Exists(t => t != null && t.Id == c.StringValue);
                default: return false;
            }
        }
    }

    public sealed class ObservationDirector
    {
        private readonly Random random;
        public ObservationDirector(int seed) { random = new Random(seed); }

        public ObservationEventDefinition SelectMajorEvent(int day, IList<ObservationEventDefinition> candidates, ObservationSimulationState state)
        {
            var milestone = candidates.Where(x => x.Category == ObservationEventCategory.Milestone).OrderByDescending(x => x.BasePriority).FirstOrDefault();
            if (milestone != null) return milestone;
            if (day - state.LastMajorEventDay <= 1 || candidates.Count == 0) return null;
            var expiring = candidates.Where(x => x.LatestDay == day).OrderByDescending(x => x.BasePriority).ThenBy(x => x.Id).FirstOrDefault();
            if (expiring != null) return expiring;
            int gap = day - state.LastMajorEventDay;
            double chance = gap >= 4 ? 0.9 : gap == 3 ? 0.68 : 0.42;
            if (random.NextDouble() > chance) return null;
            // Priority is the dominant director signal; seed only moves close candidates and event days.
            return candidates.Select(x => new
                {
                    Event = x,
                    Score = x.BasePriority + PhaseBonus(day, x) +
                        (gap >= 4 && x.Category == ObservationEventCategory.Relationship ? 25 : 0) + random.Next(0, 21)
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Event.Id)
                .First().Event;
        }

        public List<ObservationEventDefinition> SelectNormalActions(IList<ObservationEventDefinition> candidates, ObservationSimulationState state)
        {
            int count = random.Next(1, 4);
            return candidates.Select(x => new { Event = x, Score = x.BasePriority - (state.RecentNormalActionIds.Contains(x.Id) ? 35 : 0) + random.Next(0, 21) })
                .OrderByDescending(x => x.Score).Take(count).Select(x => x.Event).ToList();
        }

        private static int PhaseBonus(int day, ObservationEventDefinition definition)
        {
            if (day <= 10) return definition.Id.Contains("ENTER_HOME") || definition.Id.Contains("DRINK") ? 20 : 0;
            if (day <= 20) return definition.Id.Contains("TOUCH") || definition.Category == ObservationEventCategory.Problem || definition.Id.Contains("PLAY") ? 20 : 0;
            return definition.Id.Contains("SIT_BESIDE") || definition.Id.Contains("GREETING") || definition.Id.Contains("ADAPT") ? 20 : 0;
        }
    }

    public sealed class ObservationSimulator
    {
        private readonly ObservationRouteDefinition route;
        private readonly ObservationDirector director;
        public ObservationSimulationState State { get; } = new ObservationSimulationState();
        public ObservationSimulator(ObservationRouteDefinition route, int seed) { this.route = route ?? throw new ArgumentNullException(nameof(route)); director = new ObservationDirector(seed); }

        public DaySimulationResult SimulateDay(int day)
        {
            if (day != State.CurrentDay + 1 || day < 1 || day > 30) throw new ArgumentOutOfRangeException(nameof(day), "Days must be simulated once, in order, from DAY1 to DAY30.");
            State.CurrentDay = day;
            var eligible = route.Events.Where(x => ObservationConditionEvaluator.Evaluate(x, route, State)).ToList();
            var normal = eligible.Where(x => x.Category == ObservationEventCategory.Normal).ToList();
            var selectedMajor = director.SelectMajorEvent(day, eligible.Where(x => x.Category != ObservationEventCategory.Normal).ToList(), State);
            var selectedNormal = director.SelectNormalActions(normal, State);
            var result = new DaySimulationResult
            {
                Day = day, MajorEventId = selectedMajor != null ? selectedMajor.Id : null, StateBefore = State.Relationship.Clone(),
                NormalCandidates = normal.Select(x => x.Id).ToList(),
                RelationshipCandidates = eligible.Where(x => x.Category == ObservationEventCategory.Relationship).Select(x => x.Id).ToList(),
                ProblemCandidates = eligible.Where(x => x.Category == ObservationEventCategory.Problem).Select(x => x.Id).ToList()
            };
            foreach (var action in selectedNormal) Apply(action, result);
            if (selectedMajor != null) Apply(selectedMajor, result);
            result.StateAfter = State.Relationship.Clone();
            return result;
        }

        public List<DaySimulationResult> Simulate30Days()
        {
            var results = new List<DaySimulationResult>(30);
            for (int day = 1; day <= 30; day++) results.Add(SimulateDay(day));
            return results;
        }

        private void Apply(ObservationEventDefinition definition, DaySimulationResult result)
        {
            if (definition.Category == ObservationEventCategory.Normal)
            {
                result.NormalActionIds.Add(definition.Id);
                State.RecentNormalActionIds.Enqueue(definition.Id);
                while (State.RecentNormalActionIds.Count > 6) State.RecentNormalActionIds.Dequeue();
            }
            else
            {
                State.LastMajorEventDay = State.CurrentDay;
                State.OccurredEventIds.Add(definition.Id);
                foreach (var flag in definition.AddHistoryFlags) State.HistoryFlags.Add(flag);
                foreach (var memory in definition.AddMemories) State.MemoryFlags.Add(memory);
                definition.StateChange.Apply(State.Relationship);
            }
            foreach (var log in definition.Logs)
                result.LogEntries.Add(new ObservationLogEntry { Time = log.Time, Actor = log.Actor, ActionId = log.ActionId, Text = log.Text, Importance = log.Importance });
        }
    }
}
