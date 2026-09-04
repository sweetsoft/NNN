using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NNN
{
    /// <summary>v0.1で対象とする佐藤美咲×スズ×訪問のデータ駆動ルート。</summary>
    public static class SatoSuzuVisitObservationFactory
    {
        /// <summary>
        /// 既存テストプロファイルのH02（佐藤美咲）と、観察用のスズ、通常・Majorイベントを接続する。
        /// 現段階では調整値をコードで組み立てるが、返却型はScriptableObjectなので将来の.asset化でも実行側は共通化できる。
        /// </summary>
        public static ObservationRouteDefinition CreateRoute()
        {
            var profile = NNNTestDataFactory.CreateRuntimeProfile();
            var route = ScriptableObject.CreateInstance<ObservationRouteDefinition>();
            route.name = "SatoSuzuVisitRoute";
            route.Human = profile.HumanBenchmarks.First(x => x.BenchmarkId == "H02");
            route.Cat = CreateSuzu();
            route.DispatchMethod = DispatchMethod.Visit;
            AddNormalEvents(route.Events);
            AddMajorEvents(route.Events);
            return route;
        }

        /// <summary>
        /// 候補猫一覧へ影響を与えず、今回の観察ルートだけで使うスズを作る。
        /// cautious_contactは猫パンチ、exploratoryは室内探索・物落としの成立条件に利用する。
        /// </summary>
        private static CatDefinition CreateSuzu()
        {
            var cat = ScriptableObject.CreateInstance<CatDefinition>();
            cat.name = "CAT_SUZU_スズ";
            cat.Id = "CAT_SUZU";
            cat.DisplayName = "スズ";
            cat.Age = 4;
            cat.Activity = 62;
            cat.Sociability = 38;
            cat.Independence = 72;
            cat.Adaptability = 58;
            cat.Traits.Add(Trait("cautious_contact", "#接触許容低め"));
            cat.Traits.Add(Trait("exploratory", "#探索好き"));
            return cat;
        }

        /// <summary>既存CatDefinition.HasTraitで照合できる最小Trait定義を生成する。</summary>
        private static TraitDefinition Trait(string id, string name)
        {
            var value = ScriptableObject.CreateInstance<TraitDefinition>();
            value.name = "Trait_" + id;
            value.Id = id;
            value.DisplayName = name;
            return value;
        }

        /// <summary>
        /// 関係を直接進展させず、現在状態を画面に表す日常行動を登録する。
        /// 遠距離／近距離や屋内行動の条件を分け、DAY1とDAY30で選ばれる行動の質を変える。
        /// </summary>
        private static void AddNormalEvents(List<ObservationEventDefinition> events)
        {
            events.Add(Normal("NORMAL_CAT_WATCH_HUMAN", 70, Log(8.2f, ObservationActor.Cat, "CAT_WATCH_HUMAN", "スズが離れた場所から佐藤を見ている"), WarinessAtLeast(CatWarinessState.Medium)));
            events.Add(Normal("NORMAL_HUMAN_WATCH_CAT", 60, Log(18.1f, ObservationActor.Human, "HUMAN_WATCH_CAT", "佐藤がスズのいる方を見る")));
            events.Add(Normal("NORMAL_CAT_REST_FAR", 75, Log(13.0f, ObservationActor.Cat, "CAT_REST_FAR", "スズが佐藤から離れた場所で伏せる"), WarinessAtLeast(CatWarinessState.Medium)));
            events.Add(Normal("NORMAL_CAT_REST_NEAR", 72, Log(14.0f, ObservationActor.Cat, "CAT_REST_NEAR", "スズが佐藤の近くで伏せる"), WarinessAtMost(CatWarinessState.Low), SettlementAtLeast(SettlementState.Territory)));
            events.Add(Normal("NORMAL_CAT_GROOMING", 55, Log(15.3f, ObservationActor.Cat, "CAT_GROOMING", "スズが前足を舐めて毛づくろいする"), WarinessAtMost(CatWarinessState.Low)));
            events.Add(Normal("NORMAL_CAT_EXPLORE_ROOM", 68, Log(10.4f, ObservationActor.Cat, "CAT_EXPLORE_ROOM", "スズが部屋の壁沿いを歩く"), SettlementAtLeast(SettlementState.Territory), CatTrait("exploratory")));
            events.Add(Normal("NORMAL_CAT_LOOK_WINDOW", 52, Log(11.2f, ObservationActor.Cat, "CAT_LOOK_WINDOW", "スズが窓の外を見る"), SettlementAtLeast(SettlementState.Territory)));
            events.Add(Normal("NORMAL_HUMAN_SMARTPHONE", 45, Log(20.0f, ObservationActor.Human, "HUMAN_SMARTPHONE", "佐藤が椅子に座ってスマートフォンを見る")));
            events.Add(Normal("NORMAL_HUMAN_MEAL", 48, Log(19.1f, ObservationActor.Human, "HUMAN_MEAL", "佐藤がテーブルで食事をする")));
            events.Add(Normal("NORMAL_SHARED_ROOM", 70, Log(21.0f, ObservationActor.Environment, "SHARED_ROOM", "佐藤とスズが同じ部屋にいる"), WarinessAtMost(CatWarinessState.Low), SettlementAtLeast(SettlementState.Territory)));
        }

        /// <summary>
        /// DAY1の訪問からDAY30の日常化までを、期間と状態・履歴条件で接続する。
        /// 日付は候補化できる幅であり固定発生日ではない。実際の日はDirectorの間隔判断とSeedで決まる。
        /// </summary>
        private static void AddMajorEvents(List<ObservationEventDefinition> events)
        {
            // 訪問で起きた観測事実だけを初期履歴にし、「訪問だから警戒度が下がる」といった直接補正は行わない。
            events.Add(Event("VISIT_FIRST_CONTACT", ObservationEventCategory.Milestone, 1, 1, 1000,
                Change(HumanToCatState.Watch, CatWarinessState.High, SettlementState.Visiting),
                History(RelationshipHistoryFlag.Seen, RelationshipHistoryFlag.Approached, RelationshipHistoryFlag.Watered),
                Memories(RelationshipMemory.HumanWaited), null,
                Log(18.0f, ObservationActor.Cat, "CAT_WAIT_ENTRANCE", "スズが玄関前にいる"),
                Log(18.1f, ObservationActor.Human, "HUMAN_APPROACH", "佐藤がスズへ一歩近づく"),
                Log(18.2f, ObservationActor.Cat, "CAT_STEP_BACK", "スズが一歩下がる"),
                Log(18.3f, ObservationActor.Human, "HUMAN_STOP", "佐藤がその場で止まる"),
                Log(20.0f, ObservationActor.Human, "HUMAN_PLACE_WATER", "佐藤が水皿を置いて離れる")));

            events.Add(Event("REL_CAT_RETURNS", ObservationEventCategory.Relationship, 3, 5, 80,
                Change(null, CatWarinessState.Medium, null), null, null,
                new[] { HistoryHas(RelationshipHistoryFlag.Watered) },
                Log(17.8f, ObservationActor.Cat, "CAT_RETURN", "スズが再び玄関付近に来る")));
            events.Add(Event("REL_DRINK_IN_FRONT_OF_HUMAN", ObservationEventCategory.Relationship, 4, 7, 90,
                Change(null, CatWarinessState.Low, null), null, null,
                new[] { HistoryHas(RelationshipHistoryFlag.Watered), WarinessAtMost(CatWarinessState.Medium) },
                Log(18.4f, ObservationActor.Cat, "CAT_DRINK", "スズが佐藤のいる場所で水を飲む")));
            events.Add(Event("REL_ENTER_HOME", ObservationEventCategory.Relationship, 7, 11, 95,
                Change(null, null, SettlementState.Territory), History(RelationshipHistoryFlag.EnteredHome), Memories(RelationshipMemory.SafeEntry),
                new[] { SettlementAtMost(SettlementState.Visiting), WarinessAtMost(CatWarinessState.Medium) },
                Log(18.6f, ObservationActor.Cat, "CAT_ENTER_HOME", "スズが玄関から室内へ入る")));
            events.Add(Event("REL_SNIFF_HUMAN", ObservationEventCategory.Relationship, 10, 14, 92,
                Change(null, null, null), History(RelationshipHistoryFlag.SniffedHuman), null,
                new[] { HistoryHas(RelationshipHistoryFlag.EnteredHome), WarinessAtMost(CatWarinessState.Medium) },
                Log(19.0f, ObservationActor.Cat, "CAT_SNIFF_HUMAN", "スズが佐藤の手元に鼻を近づける")));
            events.Add(Event("REL_FIRST_TOUCH", ObservationEventCategory.Relationship, 12, 16, 95,
                Change(HumanToCatState.Approach, CatWarinessState.Low, null), History(RelationshipHistoryFlag.Touched), null,
                new[] { HistoryHas(RelationshipHistoryFlag.SniffedHuman), WarinessAtMost(CatWarinessState.Low) },
                Log(19.2f, ObservationActor.Human, "HUMAN_TOUCH", "佐藤の指先がスズの額に触れる"),
                Log(19.3f, ObservationActor.Cat, "CAT_REMAIN", "スズがその場に残る")));
            // 触れられる関係になったことを前提に発生する問題であり、関係不成立を示すFailureではない。
            events.Add(Event("PROBLEM_OVERTOUCH_CAT_PUNCH", ObservationEventCategory.Problem, 14, 19, 110,
                Change(HumanToCatState.Watch, CatWarinessState.Medium, null), null, Memories(RelationshipMemory.OverTouched),
                new[] { HistoryHas(RelationshipHistoryFlag.Touched), HumanAtLeast(HumanToCatState.Approach), CatTrait("cautious_contact") },
                AttentionLog(19.4f, ObservationActor.Cat, "CAT_TAIL_STRONG", "スズが尻尾を強く動かす"),
                AttentionLog(19.5f, ObservationActor.Human, "HUMAN_KEEP_TOUCH", "佐藤がスズの背中を撫で続ける"),
                AttentionLog(19.6f, ObservationActor.Cat, "CAT_PUNCH", "スズが佐藤の手を前足で払う"),
                AttentionLog(19.7f, ObservationActor.Cat, "CAT_MOVE_AWAY", "スズが佐藤から少し離れる")));
            // 猫パンチの記憶を後退で終わらせず、人間が拒否サインを学ぶ回収イベントへ接続する。
            events.Add(Event("REL_RESPECT_SIGNAL", ObservationEventCategory.Relationship, 17, 22, 120,
                Change(HumanToCatState.Approach, CatWarinessState.Low, null), null, Memories(RelationshipMemory.RespectedSignal),
                new[] { MemoryHas(RelationshipMemory.OverTouched), MemoryMissing(RelationshipMemory.RespectedSignal), WarinessAtMost(CatWarinessState.Medium) },
                Log(19.1f, ObservationActor.Human, "HUMAN_TOUCH", "佐藤がスズの背中に手を置く"),
                Log(19.2f, ObservationActor.Cat, "CAT_TAIL_SIGNAL", "スズが尻尾を二度動かす"),
                Log(19.3f, ObservationActor.Human, "HUMAN_STOP_TOUCH", "佐藤が手を止める"),
                Log(19.4f, ObservationActor.Cat, "CAT_REMAIN", "スズがその場に残る")));
            events.Add(Event("REL_PLAY_TOGETHER", ObservationEventCategory.Relationship, 18, 25, 100,
                Change(null, null, SettlementState.Home), History(RelationshipHistoryFlag.Played), null,
                new[] { HistoryHas(RelationshipHistoryFlag.Touched), MemoryHas(RelationshipMemory.RespectedSignal), WarinessAtMost(CatWarinessState.Low) },
                Log(20.0f, ObservationActor.Human, "HUMAN_MOVE_TOY", "佐藤が紐のおもちゃを床で動かす"),
                Log(20.1f, ObservationActor.Cat, "CAT_PLAY", "スズが紐を前足で押さえる")));
            // 状態を大きく悪化させない生活上の問題。後続の環境適応イベントが発生する入口になる。
            events.Add(Event("PROBLEM_OBJECT_DROP", ObservationEventCategory.Problem, 22, 29, 65,
                Change(null, null, null), null, null,
                new[] { SettlementAtLeast(SettlementState.Territory), WarinessAtMost(CatWarinessState.Low), CatTrait("exploratory") },
                AttentionLog(11.5f, ObservationActor.Cat, "CAT_OBJECT_DROP", "スズがテーブルの小物を前足で床へ落とす")));
            events.Add(Event("REL_HUMAN_ADAPT_ENVIRONMENT", ObservationEventCategory.Relationship, 24, 28, 85,
                Change(null, null, null), null, Memories(RelationshipMemory.HumanAdaptedEnvironment),
                new[] { EventOccurred("PROBLEM_OBJECT_DROP") },
                Log(9.0f, ObservationActor.Human, "HUMAN_CLEAR_TABLE", "佐藤がテーブルの小物を箱へ移す")));
            events.Add(Event("REL_SIT_BESIDE", ObservationEventCategory.Relationship, 22, 27, 105,
                Change(null, CatWarinessState.Relaxed, null), History(RelationshipHistoryFlag.SatBeside), null,
                new[] { SettlementAtLeast(SettlementState.Home), WarinessAtMost(CatWarinessState.Low), HistoryHas(RelationshipHistoryFlag.Played), MemoryHas(RelationshipMemory.RespectedSignal) },
                Log(21.1f, ObservationActor.Cat, "CAT_SIT_BESIDE", "スズが佐藤の隣に座る")));
            events.Add(Event("REL_GREETING", ObservationEventCategory.Relationship, 25, 29, 110,
                Change(HumanToCatState.Care, CatWarinessState.Relaxed, null), History(RelationshipHistoryFlag.Greeted, RelationshipHistoryFlag.FollowedHuman), null,
                new[] { SettlementAtLeast(SettlementState.Home), WarinessAtMost(CatWarinessState.Low), HistoryHas(RelationshipHistoryFlag.SatBeside) },
                Log(18.0f, ObservationActor.Human, "HUMAN_RETURN_HOME", "佐藤が玄関を開ける"),
                Log(18.1f, ObservationActor.Cat, "CAT_GREETING", "スズが玄関まで歩いて来る")));
            events.Add(Event("VISIT_DAY30_ROUTINE", ObservationEventCategory.Milestone, 30, 30, 1000,
                Change(null, null, null), null, null, null,
                Log(18.0f, ObservationActor.Human, "HUMAN_RETURN_HOME", "佐藤が帰宅して水皿を確認する"),
                Log(18.1f, ObservationActor.Cat, "CAT_ROUTINE", "スズが佐藤と同じ部屋へ移動する")));
        }

        /// <summary>繰り返し可能で状態変更を持たないNormalイベントを簡潔に構築する。</summary>
        private static ObservationEventDefinition Normal(string id, int priority, ObservationLogTemplate log, params ObservationEventCondition[] conditions)
            => Event(id, ObservationEventCategory.Normal, ObservationEventRole.Optional, 1, 30, priority, Change(null, null, null), null, null, conditions, new[] { log }, true);

        /// <summary>一度だけ発生するMajor Event用の構築入口。</summary>
        private static ObservationEventDefinition Event(string id, ObservationEventCategory category, int earliest, int latest, int priority,
            RelationshipStateChange change, IEnumerable<RelationshipHistoryFlag> history, IEnumerable<RelationshipMemory> memories,
            IEnumerable<ObservationEventCondition> conditions, params ObservationLogTemplate[] logs)
            => Event(id, category, IsOptionalEvent(id) ? ObservationEventRole.Optional : ObservationEventRole.Core,
                earliest, latest, priority, change, history, memories, conditions, logs, false);

        /// <summary>
        /// イベント定義の共通項目を設定する。nullの履歴・記憶・条件は「追加／制約なし」として扱う。
        /// Factory内の全イベントが同じ初期化規則を通ることで設定漏れを局所化する。
        /// </summary>
        private static ObservationEventDefinition Event(string id, ObservationEventCategory category, ObservationEventRole role, int earliest, int latest, int priority,
            RelationshipStateChange change, IEnumerable<RelationshipHistoryFlag> history, IEnumerable<RelationshipMemory> memories,
            IEnumerable<ObservationEventCondition> conditions, ObservationLogTemplate[] logs, bool repeatable)
        {
            var value = ScriptableObject.CreateInstance<ObservationEventDefinition>();
            value.name = id;
            value.Id = id;
            value.DisplayName = id;
            value.Category = category;
            value.Role = role;
            value.EarliestDay = earliest;
            value.LatestDay = latest;
            value.BasePriority = priority;
            value.Repeatable = repeatable;
            value.StateChange = change;
            if (history != null) value.AddHistoryFlags.AddRange(history);
            if (memories != null) value.AddMemories.AddRange(memories);
            if (conditions != null) value.Conditions.AddRange(conditions);
            value.Logs.AddRange(logs);
            return value;
        }

        /// <summary>
        /// Vertical Sliceの主軸から外れる生活上の枝イベントだけをOptionalとする。
        /// Problem/Relationshipという表示カテゴリから役割を推測せず、猫パンチなど必須ProblemをCoreに保つ。
        /// </summary>
        private static bool IsOptionalEvent(string id)
            => id == "PROBLEM_OBJECT_DROP" || id == "REL_HUMAN_ADAPT_ENVIRONMENT";

        /// <summary>nullable引数をSetフラグへ変換し、変更なしとenumの先頭値を区別する。</summary>
        private static RelationshipStateChange Change(HumanToCatState? human, CatWarinessState? cat, SettlementState? settlement)
            => new RelationshipStateChange { SetHumanToCat = human.HasValue, HumanToCat = human ?? default(HumanToCatState), SetCatWariness = cat.HasValue, CatWariness = cat ?? default(CatWarinessState), SetSettlement = settlement.HasValue, Settlement = settlement ?? default(SettlementState) };
        private static IEnumerable<RelationshipHistoryFlag> History(params RelationshipHistoryFlag[] values) => values;
        private static IEnumerable<RelationshipMemory> Memories(params RelationshipMemory[] values) => values;
        private static ObservationEventCondition HistoryHas(RelationshipHistoryFlag value) => new ObservationEventCondition { Type = ObservationConditionType.HasHistory, History = value };
        private static ObservationEventCondition MemoryHas(RelationshipMemory value) => new ObservationEventCondition { Type = ObservationConditionType.HasMemory, Memory = value };
        private static ObservationEventCondition MemoryMissing(RelationshipMemory value) => new ObservationEventCondition { Type = ObservationConditionType.MissingMemory, Memory = value };
        private static ObservationEventCondition HumanAtLeast(HumanToCatState value) => new ObservationEventCondition { Type = ObservationConditionType.HumanStateAtLeast, HumanState = value };
        private static ObservationEventCondition WarinessAtLeast(CatWarinessState value) => new ObservationEventCondition { Type = ObservationConditionType.WarinessAtLeast, Wariness = value };
        private static ObservationEventCondition WarinessAtMost(CatWarinessState value) => new ObservationEventCondition { Type = ObservationConditionType.WarinessAtMost, Wariness = value };
        private static ObservationEventCondition SettlementAtLeast(SettlementState value) => new ObservationEventCondition { Type = ObservationConditionType.SettlementAtLeast, Settlement = value };
        private static ObservationEventCondition SettlementAtMost(SettlementState value) => new ObservationEventCondition { Type = ObservationConditionType.SettlementAtMost, Settlement = value };
        private static ObservationEventCondition CatTrait(string id) => new ObservationEventCondition { Type = ObservationConditionType.CatTrait, StringValue = id };
        private static ObservationEventCondition EventOccurred(string id) => new ObservationEventCondition { Type = ObservationConditionType.EventOccurred, StringValue = id };
        private static ObservationLogTemplate Log(float time, ObservationActor actor, string action, string text) => new ObservationLogTemplate { Time = time, Actor = actor, ActionId = action, Text = text, Importance = ObservationLogImportance.Normal };
        private static ObservationLogTemplate AttentionLog(float time, ObservationActor actor, string action, string text) => new ObservationLogTemplate { Time = time, Actor = actor, ActionId = action, Text = text, Importance = ObservationLogImportance.Attention };
    }
}
