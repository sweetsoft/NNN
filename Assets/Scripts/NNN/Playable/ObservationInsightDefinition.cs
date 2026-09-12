using System;
using System.Collections.Generic;
using System.Linq;

namespace NNN
{
    public enum InsightComparison { None, NewWorldFlag, AfterOperation, PreviousEvent, CohabitationChanged, NewKnowledge }

    [Serializable]
    public sealed class InsightCondition
    {
        public string[] TodayEvents = Array.Empty<string>();
        public string[] Knowledge = Array.Empty<string>();
        public string[] WorldFlags = Array.Empty<string>();
        public string[] OccurredEvents = Array.Empty<string>();
        public InsightComparison Comparison;
        public string CompareId;
    }

    [Serializable]
    public sealed class InsightEntry
    {
        public string Id, Text;
        public InsightCondition Condition = new InsightCondition();
    }

    [Serializable]
    public sealed class ObservationInsightDefinition
    {
        public string Id, CurrentQuestion;
        public int Priority;
        public int UpdateLimit = 3;
        public InsightCondition Condition = new InsightCondition();
        public List<InsightEntry> Updates = new List<InsightEntry>();
        public List<InsightEntry> Changes = new List<InsightEntry>();
    }

    /// <summary>表示時点で複製する。翌日の更新や調査結果が前日の証拠を書き換えない。</summary>
    public sealed class ObservationInsightSnapshot
    {
        public RelationshipState Relationship;
        public HashSet<string> Events, Knowledge, WorldFlags, OccurredEvents;
        public static ObservationInsightSnapshot Capture(ObservationSimulator simulator) => new ObservationInsightSnapshot
        {
            Relationship = simulator.State.Relationship.Clone(),
            Events = new HashSet<string>(simulator.DayContext.ExecutedEventIds),
            Knowledge = new HashSet<string>(simulator.State.PlayerKnowledgeFlags),
            WorldFlags = new HashSet<string>(simulator.State.WorldFlags),
            OccurredEvents = new HashSet<string>(simulator.State.OccurredEventIds)
        };
    }

    public sealed class ObservationReview
    {
        public string SelectedInsightId, CurrentQuestionId, CurrentQuestion;
        public List<InsightEntry> Updates = new List<InsightEntry>();
        public List<InsightEntry> Changes = new List<InsightEntry>();
        public string Source(InsightEntry entry) => string.Join(", ", entry.Condition.TodayEvents
            .Concat(entry.Condition.Knowledge.Select(x => "Knowledge:" + x))
            .Concat(entry.Condition.WorldFlags.Select(x => "World:" + x))
            .Concat(entry.Condition.OccurredEvents.Select(x => "History:" + x)))
            + (entry.Condition.Comparison == InsightComparison.None ? "" : " / " + entry.Condition.Comparison + ":" + entry.Condition.CompareId);
    }

    /// <summary>Simulationを変更しない演出用セッション。日番号で物語を決めない。</summary>
    public sealed class ObservationInsightPresenter
    {
        private readonly List<ObservationInsightDefinition> definitions;
        private ObservationInsightSnapshot previous;
        private readonly Dictionary<string, ObservationInsightSnapshot> beforeOperations = new Dictionary<string, ObservationInsightSnapshot>();
        private HashSet<string> previousUpdates = new HashSet<string>();
        private string question = "ハチはどんな過ごし方をする？", questionId = "QUESTION_OBSERVE";
        public ObservationInsightPresenter(List<ObservationInsightDefinition> definitions) { this.definitions = definitions; }
        public void RecordOperation(string id, ObservationSimulator simulator)
            => beforeOperations[id] = ObservationInsightSnapshot.Capture(simulator);

        public bool Matches(InsightCondition c, ObservationInsightSnapshot now)
        {
            if (!c.TodayEvents.All(now.Events.Contains) || !c.Knowledge.All(now.Knowledge.Contains)
                || !c.WorldFlags.All(now.WorldFlags.Contains) || !c.OccurredEvents.All(now.OccurredEvents.Contains)) return false;
            switch (c.Comparison)
            {
                case InsightComparison.NewWorldFlag: return previous != null && now.WorldFlags.Contains(c.CompareId) && !previous.WorldFlags.Contains(c.CompareId);
                case InsightComparison.NewKnowledge: return previous != null && now.Knowledge.Contains(c.CompareId) && !previous.Knowledge.Contains(c.CompareId);
                case InsightComparison.PreviousEvent: return previous != null && previous.Events.Contains(c.CompareId);
                case InsightComparison.CohabitationChanged: return previous != null && now.Relationship.Cohabitation != previous.Relationship.Cohabitation;
                case InsightComparison.AfterOperation:
                    return beforeOperations.TryGetValue(c.CompareId, out var before)
                        && c.WorldFlags.Any(flag => now.WorldFlags.Contains(flag) && !before.WorldFlags.Contains(flag));
                default: return true;
            }
        }

        public ObservationReview Build(ObservationSimulator simulator, IEnumerable<ObservationScene> scenes)
        {
            var now = ObservationInsightSnapshot.Capture(simulator);
            var matched = definitions.Where(x => Matches(x.Condition, now)).OrderByDescending(x => x.Priority).ToList();
            var current = matched.FirstOrDefault(x => !string.IsNullOrEmpty(x.CurrentQuestion));
            if (current != null) { question = current.CurrentQuestion; questionId = current.Id + "/QUESTION"; }
            var facts = matched.SelectMany(x => x.Updates).Where(x => Matches(x.Condition, now)).GroupBy(x => x.Id).Select(x => x.First()).ToList();
            // 同じ要約の連日表示を避け、新しい観察を優先。何も新しくなければ今日の証拠を一件残す。
            var fresh = facts.Where(x => !previousUpdates.Contains(x.Id)).Take(3).ToList();
            var result = new ObservationReview { SelectedInsightId = matched.FirstOrDefault()?.Id ?? "LOG_FALLBACK",
                CurrentQuestion = question, CurrentQuestionId = questionId,
                Updates = fresh.Count > 0 ? fresh : facts.Take(1).ToList(),
                Changes = matched.SelectMany(x => x.Changes).Where(x => Matches(x.Condition, now)).GroupBy(x => x.Id).Select(x => x.First()).Take(2).ToList() };
            if (result.Updates.Count == 0)
                result.Updates = scenes.SelectMany(x => x.Logs).OrderByDescending(x => x.Importance)
                    .GroupBy(x => x.EventId).Select(x => x.First()).Take(2)
                    .Select(x => new InsightEntry { Id = "LOG/" + x.EventId, Text = x.Text,
                        Condition = new InsightCondition { TodayEvents = new[] { x.EventId } } }).ToList();
            previousUpdates = new HashSet<string>(result.Updates.Select(x => x.Id)); previous = now;
            // 表示密度だけを調整し、翌日の既存の重複判定には波及させない。
            result.Updates = result.Updates.Take(matched.FirstOrDefault()?.UpdateLimit ?? 3).ToList();
            return result;
        }
    }
}
