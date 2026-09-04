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
        public RelationshipState Relationship { get; } = new RelationshipState { HumanToCat = HumanToCatState.Avoid, CatWariness = CatWarinessState.High, Settlement = SettlementState.Unknown };
        public HashSet<RelationshipHistoryFlag> HistoryFlags { get; } = new HashSet<RelationshipHistoryFlag>();
        public HashSet<RelationshipMemory> MemoryFlags { get; } = new HashSet<RelationshipMemory>();
        public HashSet<string> OccurredEventIds { get; } = new HashSet<string>();
        public Queue<string> RecentNormalActionIds { get; } = new Queue<string>();
    }

    /// <summary>イベント期間、単発性、関係状態、履歴、記憶、人物・猫TraitをAND条件で評価する。</summary>
    public static class ObservationConditionEvaluator
    {
        /// <summary>イベントが今日の候補になれるかを、副作用なしで判定する。</summary>
        public static bool Evaluate(ObservationEventDefinition definition, ObservationRouteDefinition route, ObservationSimulationState state)
        {
            if (definition == null || state.CurrentDay < definition.EarliestDay || state.CurrentDay > definition.LatestDay) return false;
            if (!definition.Repeatable && state.OccurredEventIds.Contains(definition.Id)) return false;
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
        {
            // DAY1/DAY30は通常のクールダウンや確率に左右されない観察期間の境界イベント。
            var milestone = candidates.Where(x => x.Category == ObservationEventCategory.Milestone).OrderByDescending(x => x.BasePriority).FirstOrDefault();
            if (milestone != null) return milestone;
            // Major Eventの翌日は通常描写だけにし、関係変化を毎日連続させない。
            if (day - state.LastMajorEventDay <= 1 || candidates.Count == 0) return null;
            // 条件を満たしたまま発生期間を過ぎるイベントを取りこぼさないため、最終日は確率判定を省略する。
            var expiring = candidates.Where(x => x.LatestDay == day).OrderByDescending(x => x.BasePriority).ThenBy(x => x.Id).FirstOrDefault();
            if (expiring != null) return expiring;
            int gap = day - state.LastMajorEventDay;
            // 空白が長いほどMajor Eventを出しやすくするが、通常行動だけの日も残す。
            double chance = gap >= 4 ? 0.9 : gap == 3 ? 0.68 : 0.42;
            if (random.NextDouble() > chance) return null;
            // BasePriorityとフェーズ補正を支配的にし、0～20の乱数では大差のある物語順を逆転させない。
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

        /// <summary>成立中の通常候補から1～3件を選び、直近6件に含まれる行動へ減点して連続表示を避ける。</summary>
        public List<ObservationEventDefinition> SelectNormalActions(IList<ObservationEventDefinition> candidates, ObservationSimulationState state)
        {
            int count = random.Next(1, 4);
            return candidates.Select(x => new { Event = x, Score = x.BasePriority - (state.RecentNormalActionIds.Contains(x.Id) ? 35 : 0) + random.Next(0, 21) })
                .OrderByDescending(x => x.Score).Take(count).Select(x => x.Event).ToList();
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
        public ObservationSimulationState State { get; } = new ObservationSimulationState();
        /// <summary>同じRouteとSeedなら同じ乱数列を使い、30日結果を再現する。</summary>
        public ObservationSimulator(ObservationRouteDefinition route, int seed) { this.route = route ?? throw new ArgumentNullException(nameof(route)); director = new ObservationDirector(seed); }

        /// <summary>
        /// 指定日の候補を状態から生成し、通常行動とMajor Eventを選択して結果へ適用する。
        /// 乱数列と履歴の整合性を守るため、DAY1から一日ずつ昇順に一度だけ呼ぶ。
        /// </summary>
        public DaySimulationResult SimulateDay(int day)
        {
            if (day != State.CurrentDay + 1 || day < 1 || day > 30) throw new ArgumentOutOfRangeException(nameof(day), "Days must be simulated once, in order, from DAY1 to DAY30.");
            State.CurrentDay = day;
            // 状態更新前の同一スナップショットから全カテゴリの候補を作り、一日の途中で候補条件を変えない。
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
            // Normalは関係状態を進めない前提。Majorだけが履歴・記憶・関係状態を更新する。
            foreach (var action in selectedNormal) Apply(action, result);
            if (selectedMajor != null) Apply(selectedMajor, result);

            // イベントの選択・状態更新は従来のNormal→Major順を維持し、すべての適用が終わってから
            // 表示用ログだけをゲーム内時刻順へ並べる。これにより乱数消費や関係状態の因果へ影響を与えない。
            SortLogEntriesChronologically(result.LogEntries);
            result.StateAfter = State.Relationship.Clone();
            return result;
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
                State.RecentNormalActionIds.Enqueue(definition.Id);
                // 件数ベースの短い履歴で十分なため、日付付きの大規模な行動履歴は持たない。
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
            // 定義を直接UIへ渡さず実行結果へ複製し、表示とシミュレーションの依存を分離する。
            foreach (var log in definition.Logs)
                result.LogEntries.Add(new ObservationLogEntry { Time = log.Time, Actor = log.Actor, ActionId = log.ActionId, Text = log.Text, Importance = log.Importance });
        }
    }
}
