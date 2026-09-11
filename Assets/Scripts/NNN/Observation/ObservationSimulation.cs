using System;
using System.Collections.Generic;
using System.Linq;

namespace NNN
{
    /// <summary>
    /// 30日間を通して持ち越す可変状態。イベント定義自体は変更せず、発生事実だけをここへ蓄積する。
    /// HashSetは単発履歴の重複を防ぎ、RecentNormalActionIdsは日常描写の偏り抑制に利用する。
    /// </summary>
    public sealed class ObservationSimulationState
    {
        public int CurrentDay { get; internal set; }
        // DAY1のMilestoneを通常の間隔計算から妨げないよう、開始前は十分小さい日として扱う。
        public int LastMajorEventDay { get; internal set; } = -99;
        public RelationshipState Relationship { get; } = new RelationshipState { HumanToCat = HumanToCatState.Avoid, CatWariness = CatWarinessState.High };
        public HashSet<RelationshipHistoryFlag> HistoryFlags { get; } = new HashSet<RelationshipHistoryFlag>();
        public HashSet<RelationshipMemory> MemoryFlags { get; } = new HashSet<RelationshipMemory>();
        public HashSet<string> OccurredEventIds { get; } = new HashSet<string>();
        public Queue<string> RecentNormalActionIds { get; } = new Queue<string>();
        /// <summary>前日まで何日連続で選ばれたかを保持し、候補条件を変えずに表示の単調さだけを抑える。</summary>
        public Dictionary<string, int> NormalActionStreaks { get; } = new Dictionary<string, int>();
        public HashSet<string> PlayerKnowledgeFlags { get; } = new HashSet<string>();
        public List<ObservationSimulationModifier> Modifiers { get; } = new List<ObservationSimulationModifier>();
        public List<ObservationPlayerActionRecord> PlayerActionHistory { get; } = new List<ObservationPlayerActionRecord>();
    }

    /// <summary>イベント期間、単発性、関係状態、履歴、記憶、人物・猫TraitをAND条件で評価する。</summary>
    public static class ObservationConditionEvaluator
    {
        /// <summary>イベントが今日の候補になれるかを、副作用なしで判定する。</summary>
        public static bool Evaluate(ObservationEventDefinition definition, ObservationRouteDefinition route, ObservationSimulationState state)
        {
            if (definition == null || state.CurrentDay < definition.EarliestDay || state.CurrentDay > definition.LatestDay) return false;
            if (!definition.Repeatable && state.OccurredEventIds.Contains(definition.Id)) return false;
            if (definition.StateChange.SetCohabitation
                && definition.StateChange.Cohabitation == CohabitationState.LivingTogether
                && state.Relationship.Cohabitation != CohabitationState.LivingTogether
                && !state.Relationship.CanStartCohabitation) return false;
            // Conditionsが空なら期間と単発性だけで候補化する。複数条件はすべて満たす必要がある。
            return definition.Conditions.All(condition => Evaluate(condition, route, state));
        }

        private static bool Evaluate(ObservationEventCondition c, ObservationRouteDefinition route, ObservationSimulationState s)
        {
            switch (c.Type)
            {
                case ObservationConditionType.HumanStateAtLeast: return s.Relationship.HumanToCat >= c.HumanState;
                case ObservationConditionType.HumanStateAtMost: return s.Relationship.HumanToCat <= c.HumanState;
                // CatWarinessだけはHigh(0)→Relaxed(3)の順で「値が大きいほど警戒が低い」。
                // 企画上の「警戒がMedium以下」はMedium/Low/Relaxedなので、数値比較の向きが他の状態軸と逆になる。
                case ObservationConditionType.WarinessAtLeast: return s.Relationship.CatWariness <= c.Wariness;
                case ObservationConditionType.WarinessAtMost: return s.Relationship.CatWariness >= c.Wariness;
                case ObservationConditionType.CohabitationAtLeast: return s.Relationship.Cohabitation >= c.Cohabitation;
                case ObservationConditionType.CohabitationAtMost: return s.Relationship.Cohabitation <= c.Cohabitation;
                case ObservationConditionType.AcceptanceAtLeast: return s.Relationship.HumanAcceptance >= c.Acceptance;
                case ObservationConditionType.AcceptanceAtMost: return s.Relationship.HumanAcceptance <= c.Acceptance;
                case ObservationConditionType.AdaptationAtLeast: return s.Relationship.CatAdaptation >= c.Adaptation;
                case ObservationConditionType.AdaptationAtMost: return s.Relationship.CatAdaptation <= c.Adaptation;
                case ObservationConditionType.HasHomePreparation: return s.Relationship.HasPreparation(c.Preparation);
                case ObservationConditionType.MissingHomePreparation: return !s.Relationship.HasPreparation(c.Preparation);
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

    /// <summary>
    /// 候補の生成責務を持たず、候補から今日見せるものだけを選ぶ簡易Director。
    /// 設計上の優先度を主軸とし、Seed乱数は発生日と近い優先度同士の揺らぎに限定する。
    /// </summary>
    public sealed class ObservationDirector
    {
        private readonly Random random;
        public ObservationDirector(int seed) { random = new Random(seed); }

        /// <summary>
        /// Major Eventを一日最大一件選ぶ。Milestone、翌日抑制、期限補正、発生間隔、優先度の順に判断する。
        /// </summary>
        public ObservationEventDefinition SelectMajorEvent(int day, IList<ObservationEventDefinition> candidates, ObservationSimulationState state)
            => SelectMajorEvent(day, candidates, state, null);

        /// <summary>有効Modifierを候補スコアへ加える逐次実行用入口。Modifierなしでは従来入口と同じ乱数列を使う。</summary>
        public ObservationEventDefinition SelectMajorEvent(int day, IList<ObservationEventDefinition> candidates,
            ObservationSimulationState state, IList<ObservationSimulationModifier> modifiers)
        {
            // 導入と期間末のMilestoneは通常のクールダウンや確率に左右されない。
            var milestone = candidates.Where(x => x.Category == ObservationEventCategory.Milestone).OrderByDescending(x => x.BasePriority).FirstOrDefault();
            if (milestone != null) return milestone;
            if (candidates.Count == 0) return null;

            // 通常はMajor翌日の休止を守る。Coreの残り日数が1日以下の場合だけ例外とし、
            // Optionalが直前日に枠を使っても主軸イベントが期限切れになる事態を防ぐ。
            var urgentCore = candidates.Where(x => x.Role == ObservationEventRole.Core && x.LatestDay - day <= 2).ToList();
            bool cooldownCriticalCoreExists = urgentCore.Any(x => x.LatestDay - day <= 1);
            if (day - state.LastMajorEventDay <= 1 && !cooldownCriticalCoreExists) return null;
            int gap = day - state.LastMajorEventDay;
            int nearestCoreDeadline = candidates.Where(x => x.Role == ObservationEventRole.Core)
                .Select(x => x.LatestDay - day).DefaultIfEmpty(int.MaxValue).Min();
            // 空白が長いほどMajor Eventを出しやすくするが、通常行動だけの日も残す。
            double chance = gap >= 4 ? 0.9 : gap == 3 ? 0.68 : 0.42;
            // 残り2日から段階的に確率を上げ、最終日だけ確実に保護する。
            // 期限保護をDAY固定にせず、同時に特定日へ集中することも避ける。
            int nearestUrgentDeadline = urgentCore.Select(x => x.LatestDay - day).DefaultIfEmpty(int.MaxValue).Min();
            double effectiveChance = nearestUrgentDeadline <= 0 ? 1.0
                : nearestUrgentDeadline == 1 ? 0.72
                : nearestUrgentDeadline == 2 ? 0.45
                : chance;
            if (random.NextDouble() > effectiveChance) return null;

            // DeadlineBonusは発生可能期間の進捗に対して二次曲線で滑らかに増える。
            // CoreBonusも同じ進捗で増やし、序盤はOptionalが競合でき、終盤だけ主軸を優先する。
            return candidates.Select(x => new
                {
                    Event = x,
                    Score = x.BasePriority + PhaseBonus(day, x) + DeadlineBonus(day, x) + CoreBonus(day, x) +
                        ModifierBonus(day, x, modifiers) +
                        OptionalOpportunityBonus(x, nearestCoreDeadline) +
                        (gap >= 4 && x.Category == ObservationEventCategory.Relationship ? 25 : 0) + random.Next(0, 21)
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Event.Id)
                .First().Event;
        }

        private static int ModifierBonus(int day, ObservationEventDefinition definition, IList<ObservationSimulationModifier> modifiers)
            => modifiers == null ? 0 : modifiers.Where(x => x != null && x.IsActive(day, definition.Id)).Sum(x => x.PriorityBonus);

        /// <summary>成立中の通常候補から1～3件を選び、直近6件に含まれる行動へ減点して連続表示を避ける。</summary>
        public List<ObservationEventDefinition> SelectNormalActions(IList<ObservationEventDefinition> candidates, ObservationSimulationState state)
        {
            int count = random.Next(1, 4);
            return candidates
                // 3日続いた行動は一日だけ休ませる。状態条件を満たした候補集合に対して行うため、不適切な行動は復活しない。
                .Where(x => !state.NormalActionStreaks.TryGetValue(x.Id, out int streak) || streak < 3)
                .Select(x => new { Event = x, Score = x.BasePriority - RecentNormalPenalty(state, x.Id) + random.Next(0, 21) })
                .OrderByDescending(x => x.Score).Take(count).Select(x => x.Event).ToList();
        }

        private static int RecentNormalPenalty(ObservationSimulationState state, string id)
        {
            if (!state.NormalActionStreaks.TryGetValue(id, out int streak)) return state.RecentNormalActionIds.Contains(id) ? 20 : 0;
            return streak >= 2 ? 75 : 45;
        }

        private static int DeadlineBonus(int day, ObservationEventDefinition definition)
        {
            float progress = DeadlineProgress(day, definition);
            float maximum = definition.Role == ObservationEventRole.Core ? 120f : 60f;
            return (int)Math.Round(maximum * progress * progress, MidpointRounding.AwayFromZero);
        }

        private static int CoreBonus(int day, ObservationEventDefinition definition)
            => definition.Role == ObservationEventRole.Core
                ? (int)Math.Round(100f * DeadlineProgress(day, definition), MidpointRounding.AwayFromZero)
                : 0;

        /// <summary>Core期限に十分な余白がある日だけOptionalへ出番を与え、期限間際は補正を外す。</summary>
        private static int OptionalOpportunityBonus(ObservationEventDefinition definition, int nearestCoreDeadline)
            => definition.Role == ObservationEventRole.Optional && nearestCoreDeadline >= 3 ? 100 : 0;

        private static float DeadlineProgress(int day, ObservationEventDefinition definition)
        {
            int duration = definition.LatestDay - definition.EarliestDay;
            if (duration <= 0) return 1f;
            return Math.Max(0f, Math.Min(1f, (float)(day - definition.EarliestDay) / duration));
        }

        /// <summary>序盤・中盤・終盤で見せたい変化へ小さな補正を加える。特定DAYへの固定配置ではない。</summary>
        private static int PhaseBonus(int day, ObservationEventDefinition definition)
        {
            if (day <= 10) return definition.Id.Contains("ENTER_HOME") || definition.Id.Contains("DRINK") ? 20 : 0;
            if (day <= 20) return definition.Id.Contains("TOUCH") || definition.Category == ObservationEventCategory.Problem || definition.Id.Contains("PLAY") ? 20 : 0;
            return definition.Id.Contains("SIT_BESIDE") || definition.Id.Contains("GREETING") || definition.Id.Contains("ADAPT") ? 20 : 0;
        }
    }

    /// <summary>一つのRouteとSeedを受け取り、候補生成・選択・適用をDAY順に進める実行単位。</summary>
    public sealed class ObservationSimulator
    {
        private readonly ObservationRouteDefinition route;
        private readonly ObservationDirector director;
        private readonly List<ScheduledObservationEvent> pendingEvents = new List<ScheduledObservationEvent>();
        private readonly Dictionary<string, int> normalSelectionOrder = new Dictionary<string, int>();
        private DaySimulationResult currentResult;
        private bool generatedSinceLastAction;
        private int nextSelectionOrder;
        public ObservationSimulationState State { get; } = new ObservationSimulationState();
        public ObservationDayContext DayContext { get; private set; }
        public int PendingEventCount => pendingEvents.Count;
        /// <summary>同じRouteとSeedなら同じ乱数列を使い、30日結果を再現する。</summary>
        public ObservationSimulator(ObservationRouteDefinition route, int seed) { this.route = route ?? throw new ArgumentNullException(nameof(route)); director = new ObservationDirector(seed); }

        /// <summary>
        /// 指定日の候補を状態から生成し、通常行動とMajor Eventを選択して結果へ適用する。
        /// 乱数列と履歴の整合性を守るため、DAY1から一日ずつ昇順に一度だけ呼ぶ。
        /// </summary>
        public DaySimulationResult SimulateDay(int day)
        {
            BeginDay(day);
            while (GenerateNextEvent() != null) ExecuteNextEvent();
            return EndDay();
        }

        /// <summary>日次Runtimeを初期化する。未来候補の選択は最初のGenerateNextEventまで行わない。</summary>
        public void BeginDay(int day)
        {
            if (DayContext != null) throw new InvalidOperationException("The current observation day has not ended.");
            if (day != State.CurrentDay + 1 || day < 1 || day > 30)
                throw new ArgumentOutOfRangeException(nameof(day), "Days must be simulated once, in order, from DAY1 to DAY30.");
            State.CurrentDay = day;
            DayContext = new ObservationDayContext { Day = day, CurrentTime = 0f };
            currentResult = new DaySimulationResult { Day = day, StateBefore = State.Relationship.Clone() };
            pendingEvents.Clear();
            normalSelectionOrder.Clear();
            generatedSinceLastAction = false;
            nextSelectionOrder = 0;
        }

        /// <summary>
        /// 呼出時点の状態と時刻から次イベントを返す。選択済みの残りはAction時に破棄されるため、過去だけが確定する。
        /// </summary>
        public ObservationEventDefinition GenerateNextEvent()
        {
            EnsureDayActive();
            if (pendingEvents.Count > 0) return pendingEvents[0].Definition;
            if (generatedSinceLastAction) return null;
            generatedSinceLastAction = true;

            var eligible = route.Events.Where(x => ObservationConditionEvaluator.Evaluate(x, route, State))
                .Where(x => !DayContext.ExecutedEventIds.Contains(x.Id) && EventStartTime(x) >= DayContext.CurrentTime)
                .ToList();
            var normal = eligible.Where(x => x.Category == ObservationEventCategory.Normal).ToList();
            AddCandidates(currentResult.NormalCandidates, normal);
            AddCandidates(currentResult.RelationshipCandidates, eligible.Where(x => x.Category == ObservationEventCategory.Relationship));
            AddCandidates(currentResult.ProblemCandidates, eligible.Where(x => x.Category == ObservationEventCategory.Problem));

            // 呼出順は旧SimulateDayと同じMajor→Normalとし、Actionなしの乱数消費順を維持する。
            ObservationEventDefinition major = DayContext.HasMajorEventOccurred ? null : director.SelectMajorEvent(
                DayContext.Day, eligible.Where(x => x.Category != ObservationEventCategory.Normal).ToList(), State, State.Modifiers);
            List<ObservationEventDefinition> selectedNormal = director.SelectNormalActions(normal, State);
            foreach (ObservationEventDefinition selected in selectedNormal)
            {
                normalSelectionOrder[selected.Id] = nextSelectionOrder;
                pendingEvents.Add(new ScheduledObservationEvent(selected, nextSelectionOrder++));
            }
            if (major != null) pendingEvents.Add(new ScheduledObservationEvent(major, nextSelectionOrder++));
            pendingEvents.Sort((left, right) =>
            {
                int time = EventStartTime(left.Definition).CompareTo(EventStartTime(right.Definition));
                return time != 0 ? time : left.SelectionOrder.CompareTo(right.SelectionOrder);
            });
            return pendingEvents.Count == 0 ? null : pendingEvents[0].Definition;
        }

        /// <summary>GenerateNextEventで得た先頭イベント一件だけを確定し、現在時刻と既存状態へ反映する。</summary>
        public ObservationEventDefinition ExecuteNextEvent()
        {
            EnsureDayActive();
            if (pendingEvents.Count == 0 && GenerateNextEvent() == null) return null;
            ScheduledObservationEvent scheduled = pendingEvents[0];
            pendingEvents.RemoveAt(0);
            ObservationEventDefinition definition = scheduled.Definition;
            Apply(definition, currentResult);
            DayContext.ExecutedEventIds.Add(definition.Id);
            // CurrentTimeはイベント開始時刻のカーソル。イベント内の複数ログは一つの原子的な出来事として同時に確定する。
            DayContext.CurrentTime = Math.Max(DayContext.CurrentTime, EventStartTime(definition));
            if (definition.Category != ObservationEventCategory.Normal)
            {
                DayContext.HasMajorEventOccurred = true;
                currentResult.MajorEventId = definition.Id;
                // 同日Major上限を保証するため、まだ実行していないMajor候補は破棄する。
                pendingEvents.RemoveAll(x => x.Definition.Category != ObservationEventCategory.Normal);
            }
            return definition;
        }

        /// <summary>テスト用NNN ACTIONを現在時刻へ記録し、未実行イベントだけを破棄して次回生成時に再評価する。</summary>
        public void ApplyNNNAction(string actionId, float time)
        {
            EnsureDayActive();
            if (time < DayContext.CurrentTime || time > 24f) throw new ArgumentOutOfRangeException(nameof(time));
            var record = new ObservationPlayerActionRecord { Day = DayContext.Day, Time = time, ActionId = actionId };
            DayContext.CurrentTime = time;
            DayContext.PlayerActionRecords.Add(record);
            State.PlayerActionHistory.Add(record);
            if (actionId == "CAT_INVESTIGATION_TEST")
            {
                State.PlayerKnowledgeFlags.Add("KNOW_SUZU_RETURNS_HOME");
            }
            else if (actionId == "HUMAN_OPERATION_SIGNAL_HINT_TEST")
            {
                State.Modifiers.Add(new ObservationSimulationModifier
                {
                    Id = "HUMAN_SIGNAL_HINT", AppliedDay = DayContext.Day, AppliedTime = time,
                    ActiveFromDay = DayContext.Day + 1, ExpireDay = DayContext.Day + 3,
                    TargetEventId = "REL_RESPECT_SIGNAL", PriorityBonus = 80
                });
            }
            else throw new ArgumentException("Unknown NNN ACTION: " + actionId, nameof(actionId));

            pendingEvents.Clear();
            generatedSinceLastAction = false;
        }

        /// <summary>当日結果を確定し、表示ログと旧NormalActionIds順、翌日用Recent状態を整える。</summary>
        public DaySimulationResult EndDay()
        {
            EnsureDayActive();
            if (pendingEvents.Count > 0) throw new InvalidOperationException("Execute pending events before ending the day.");
            currentResult.NormalActionIds = currentResult.NormalActionIds.OrderBy(id => normalSelectionOrder[id]).ToList();
            UpdateNormalActionStreaks(currentResult.NormalActionIds);
            foreach (string id in currentResult.NormalActionIds)
            {
                State.RecentNormalActionIds.Enqueue(id);
                while (State.RecentNormalActionIds.Count > 6) State.RecentNormalActionIds.Dequeue();
            }
            SortLogEntriesChronologically(currentResult.LogEntries);
            currentResult.StateAfter = State.Relationship.Clone();
            DayContext.IsComplete = true;
            DaySimulationResult completed = currentResult;
            DayContext = null;
            currentResult = null;
            return completed;
        }

        /// <summary>今日選ばれた行動だけ連続数を増やし、選ばれなかった行動は0へ戻す。</summary>
        private void UpdateNormalActionStreaks(IList<string> selectedIdsInOrder)
        {
            var selectedIds = new HashSet<string>(selectedIdsInOrder);
            foreach (string id in State.NormalActionStreaks.Keys.ToList())
                if (!selectedIds.Contains(id)) State.NormalActionStreaks[id] = 0;
            foreach (string id in selectedIds)
                State.NormalActionStreaks[id] = State.NormalActionStreaks.TryGetValue(id, out int streak) ? streak + 1 : 1;
        }

        /// <summary>
        /// 完成した一日分のログをTime昇順へ並べる。同時刻では追記時のIndexを第2キーにするため、
        /// 一つのイベント内で定義された観測・反応の因果順や、従来のイベント適用順が維持される。
        /// </summary>
        public static void SortLogEntriesChronologically(List<ObservationLogEntry> entries)
        {
            if (entries == null || entries.Count < 2) return;
            var ordered = entries.Select((entry, index) => new { Entry = entry, OriginalIndex = index })
                .OrderBy(item => item.Entry.Time)
                .ThenBy(item => item.OriginalIndex)
                .Select(item => item.Entry)
                .ToList();
            entries.Clear();
            entries.AddRange(ordered);
        }

        /// <summary>新規SimulatorをDAY1からDAY30まで進め、UI・検証で利用できる全日結果を返す。</summary>
        public List<DaySimulationResult> Simulate30Days()
        {
            var results = new List<DaySimulationResult>(30);
            for (int day = 1; day <= 30; day++) results.Add(SimulateDay(day));
            return results;
        }

        /// <summary>選択済みイベントを状態とDay結果へ反映する。候補評価中には呼ばない。</summary>
        private void Apply(ObservationEventDefinition definition, DaySimulationResult result)
        {
            if (definition.Category == ObservationEventCategory.Normal)
            {
                result.NormalActionIds.Add(definition.Id);
            }
            else
            {
                // 遷移の検証を履歴追加より先に行い、不正な同居イベントを記録しない。
                definition.StateChange.Apply(State.Relationship);
                State.LastMajorEventDay = State.CurrentDay;
                State.OccurredEventIds.Add(definition.Id);
                foreach (var flag in definition.AddHistoryFlags) State.HistoryFlags.Add(flag);
                foreach (var memory in definition.AddMemories) State.MemoryFlags.Add(memory);
            }
            // 定義を直接UIへ渡さず実行結果へ複製し、表示とシミュレーションの依存を分離する。
            foreach (var log in definition.Logs)
                result.LogEntries.Add(new ObservationLogEntry { Time = log.Time, Actor = log.Actor, ActionId = log.ActionId, Text = log.Text, Importance = log.Importance });
        }

        private void EnsureDayActive()
        {
            if (DayContext == null) throw new InvalidOperationException("BeginDay must be called first.");
        }

        private static float EventStartTime(ObservationEventDefinition definition)
            => definition.Logs.Count == 0 ? 0f : definition.Logs.Min(x => x.Time);

        private static void AddCandidates(ICollection<string> target, IEnumerable<ObservationEventDefinition> definitions)
        {
            foreach (ObservationEventDefinition definition in definitions)
                if (!target.Contains(definition.Id)) target.Add(definition.Id);
        }

        private sealed class ScheduledObservationEvent
        {
            public readonly ObservationEventDefinition Definition;
            public readonly int SelectionOrder;
            public ScheduledObservationEvent(ObservationEventDefinition definition, int selectionOrder)
            {
                Definition = definition;
                SelectionOrder = selectionOrder;
            }
        }
    }
}
