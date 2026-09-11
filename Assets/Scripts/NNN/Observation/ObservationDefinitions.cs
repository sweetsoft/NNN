using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NNN
{
    /// <summary>猫を人間の生活圏へ接続する導入方法。v0.1ではVisitのみを実行対象とする。</summary>
    public enum DispatchMethod { Visit, Placement, Introduced, Rescue }
    /// <summary>好感度ではなく、人間が猫に対して実際に取る接触行動の段階。</summary>
    public enum HumanToCatState { Avoid, Watch, Approach, Care }
    /// <summary>猫が許容できる対人距離の段階。宣言順はHighからRelaxedへ警戒が下がる。</summary>
    public enum CatWarinessState { High, Medium, Low, Relaxed }
    /// <summary>生活拠点としての同居事実。外出や対人警戒では後退しない。</summary>
    public enum CohabitationState { Outside, Visiting, LivingTogether }
    /// <summary>人間が猫との生活と継続的な世話を受け入れる姿勢。</summary>
    public enum HumanAcceptanceState { Reluctant, Tolerating, Welcoming, Committed }
    /// <summary>家と生活リズムへの適応。対人警戒とは独立する。</summary>
    public enum CatAdaptationState { Unfamiliar, Exploring, Settling, AtEase }
    /// <summary>準備項目から導出する表示段階。</summary>
    public enum HomeReadinessStage { Unprepared, Preparing, BasicReady }
    /// <summary>工作の効果やイベント条件でも個別に照合する最低限の住居準備。</summary>
    [Flags]
    public enum HomePreparation
    {
        None = 0, FoodAndWaterReady = 1, ToiletReady = 2,
        RestingPlaceReady = 4, BasicSafetyReady = 8,
        All = FoodAndWaterReady | ToiletReady | RestingPlaceReady | BasicSafetyReady
    }
    /// <summary>一度起きた観測事実。後続イベントの前提条件として消去せず保持する。</summary>
    public enum RelationshipHistoryFlag { Seen, Approached, Watered, Fed, EnteredHome, SniffedHuman, Touched, Played, SatBeside, Greeted, FollowedHuman, CohabitationStarted }
    /// <summary>単なる発生履歴ではなく、後の行動選択に意味を持つ経験・学習結果。</summary>
    public enum RelationshipMemory { HumanWaited, SafeEntry, OverTouched, RespectedSignal, HumanAdaptedEnvironment }
    /// <summary>通常描写と、一日最大一件のMajor Eventを分類する。</summary>
    public enum ObservationEventCategory { Normal, Relationship, Problem, Milestone }
    /// <summary>カテゴリとは独立した物語上の役割。Coreは期限切れで関係進行を止めてはいけないイベントを表す。</summary>
    public enum ObservationEventRole { Core, Optional }
    /// <summary>観察ログ上で動作主体として表示する対象。</summary>
    public enum ObservationActor { Human, Cat, Environment }
    /// <summary>UI側が強調や停止を判断するための重要度。ロジック自身は時間を停止しない。</summary>
    public enum ObservationLogImportance { Normal, Attention, Emergency }
    /// <summary>
    /// v0.1で必要な比較だけを列挙した条件種別。
    /// 汎用式パーサーを持たせず、値は<see cref="ObservationEventCondition"/>の対応フィールドから読む。
    /// </summary>
    public enum ObservationConditionType
    {
        HumanStateAtLeast, HumanStateAtMost, WarinessAtLeast, WarinessAtMost,
        // 旧Settlement条件のシリアライズ値4・5は再利用しない。
        HasHistory = 6, MissingHistory,
        HasMemory, MissingMemory, EventOccurred, EventNotOccurred,
        DaysSinceLastMajorAtLeast, CatTrait, HumanTrait,
        CohabitationAtLeast, CohabitationAtMost, AcceptanceAtLeast, AcceptanceAtMost,
        AdaptationAtLeast, AdaptationAtMost, HasHomePreparation, MissingHomePreparation,
        HasWorldFlag, MissingWorldFlag, HasKnowledgeTag, DayAtLeast
    }

    [Serializable]
    /// <summary>
    /// 同居・受容・住居準備・適応・対人警戒を独立して保持する。HumanToCatは行動選択専用。
    /// 日次結果ではCloneを保存し、イベント適用前後を同じ参照にしない。
    /// </summary>
    public sealed class RelationshipState
    {
        public HumanToCatState HumanToCat;
        public CatWarinessState CatWariness;
        public CohabitationState Cohabitation;
        public HumanAcceptanceState HumanAcceptance;
        public HomePreparation HomeReadiness;
        public CatAdaptationState CatAdaptation;
        public bool HasPreparation(HomePreparation items) => (HomeReadiness & items) == items;
        public HomeReadinessStage ReadinessStage => HasPreparation(HomePreparation.All)
            ? HomeReadinessStage.BasicReady
            : HomeReadiness == HomePreparation.None ? HomeReadinessStage.Unprepared : HomeReadinessStage.Preparing;
        /// <summary>条件成立だけで同居へ遷移しない。開始イベントが別途必要。</summary>
        public bool CanStartCohabitation => Cohabitation == CohabitationState.Visiting
            && HumanAcceptance >= HumanAcceptanceState.Welcoming && HasPreparation(HomePreparation.All);
        public bool CanTransitionTo(CohabitationState target) => target == Cohabitation
            || (Cohabitation == CohabitationState.Outside && target == CohabitationState.Visiting)
            || (target == CohabitationState.LivingTogether && CanStartCohabitation);
        /// <summary>DaySimulationResultに変更前後の独立したスナップショットを残す。</summary>
        public RelationshipState Clone() => (RelationshipState)MemberwiseClone();
        public override string ToString() => "Cohabitation=" + Cohabitation + " / Acceptance=" + HumanAcceptance
            + " / Home=" + ReadinessStage + " (" + HomeReadiness + ") / Adaptation=" + CatAdaptation
            + " / Wariness=" + CatWariness + " / HumanAction=" + HumanToCat;
    }

    [Serializable]
    /// <summary>
    /// イベントが変更する項目だけを保持する差分。Setフラグがfalseの状態軸は現状を維持する。
    /// enumのdefault値と「変更なし」を区別するため、各値とSetフラグを対で持つ。
    /// </summary>
    public sealed class RelationshipStateChange
    {
        public bool SetHumanToCat;
        public HumanToCatState HumanToCat;
        public bool SetCatWariness;
        public CatWarinessState CatWariness;
        public bool SetCohabitation;
        public CohabitationState Cohabitation;
        public bool SetHumanAcceptance;
        public HumanAcceptanceState HumanAcceptance;
        public bool SetCatAdaptation;
        public CatAdaptationState CatAdaptation;
        public HomePreparation AddHomePreparation;
        public HomePreparation RemoveHomePreparation;
        /// <summary>指定された軸だけを実行中の関係状態へ反映する。</summary>
        public void Apply(RelationshipState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if ((AddHomePreparation & RemoveHomePreparation) != HomePreparation.None)
                throw new InvalidOperationException("The same preparation cannot be added and removed together.");
            // 遷移前の条件を検証し、失敗時は他の状態軸も変更しない。
            if (SetCohabitation && !state.CanTransitionTo(Cohabitation))
                throw new InvalidOperationException("Normal events only advance one stage; living together requires prior acceptance and preparation. Dissolution requires a separate explicit API.");
            if (SetHumanToCat) state.HumanToCat = HumanToCat;
            if (SetCatWariness) state.CatWariness = CatWariness;
            if (SetCohabitation) state.Cohabitation = Cohabitation;
            if (SetHumanAcceptance) state.HumanAcceptance = HumanAcceptance;
            if (SetCatAdaptation) state.CatAdaptation = CatAdaptation;
            state.HomeReadiness = (state.HomeReadiness | AddHomePreparation) & ~RemoveHomePreparation;
        }
    }

    [Serializable]
    /// <summary>
    /// ScriptableObject上で一条件を表現する共用コンテナ。
    /// Typeに応じてIntValue、StringValue、またはいずれかのenum値だけが評価される。
    /// </summary>
    public sealed class ObservationEventCondition
    {
        public string Description;
        public ObservationConditionType Type;
        public int IntValue;
        public string StringValue;
        public RelationshipHistoryFlag History;
        public RelationshipMemory Memory;
        public HumanToCatState HumanState;
        public CatWarinessState Wariness;
        public CohabitationState Cohabitation;
        public HumanAcceptanceState Acceptance;
        public CatAdaptationState Adaptation;
        public HomePreparation Preparation;
    }

    [Serializable]
    /// <summary>イベント定義に保存する観測ログの雛形。Textには推測を入れず画面で観測できる事実を書く。</summary>
    public sealed class ObservationLogTemplate
    {
        /// <summary>同じイベント内の表示場面。空ならイベント全体を一場面にする。</summary>
        public string SceneId;
        [Range(0f, 24f)] public float Time;
        public ObservationActor Actor;
        public string ActionId;
        [TextArea] public string Text;
        public ObservationLogImportance Importance;
    }

    [CreateAssetMenu(menuName = "NNN/Observation/Event")]
    /// <summary>
    /// 発生期間、優先度、条件、結果、表示ログを一体で保持するイベントマスター。
    /// 条件判定と実行処理をデータから分離し、将来.assetへ移行してもSimulatorを変更しない構成にする。
    /// </summary>
    public sealed class ObservationEventDefinition : IdDefinition
    {
        public ObservationEventCategory Category;
        public ObservationEventRole Role = ObservationEventRole.Optional;
        [Range(1, 30)] public int EarliestDay = 1;
        [Range(1, 30)] public int LatestDay = 30;
        public int BasePriority = 50;
        /// <summary>trueは日常描写用。falseのイベントIDは発生済み集合に入り、二度目を候補から除外する。</summary>
        public bool Repeatable;
        public List<ObservationEventCondition> Conditions = new List<ObservationEventCondition>();
        public List<RelationshipHistoryFlag> AddHistoryFlags = new List<RelationshipHistoryFlag>();
        public List<RelationshipMemory> AddMemories = new List<RelationshipMemory>();
        public List<string> AddKnowledgeTags = new List<string>();
        public RelationshipStateChange StateChange = new RelationshipStateChange();
        public List<ObservationLogTemplate> Logs = new List<ObservationLogTemplate>();
        [TextArea] public string DebugDescription;
    }

    [CreateAssetMenu(menuName = "NNN/Observation/Route")]
    /// <summary>人間・猫・派遣方法と、その組み合わせで利用するイベント集合を結ぶ観察ルート。</summary>
    public sealed class ObservationRouteDefinition : ScriptableObject
    {
        public HumanBenchmarkDefinition Human;
        public CatDefinition Cat;
        public DispatchMethod DispatchMethod = DispatchMethod.Visit;
        public List<ObservationEventDefinition> Events = new List<ObservationEventDefinition>();
        public List<NNNActionDefinition> Actions = new List<NNNActionDefinition>(NNNActionCatalog.All);
        public List<CatReportDefinition> CatReports = new List<CatReportDefinition>();
    }

    [Serializable]
    /// <summary>UIへ渡す実行済みログ。テンプレートから複製されるため表示側はイベント定義を参照しなくてよい。</summary>
    public sealed class ObservationLogEntry
    {
        public string EventId;
        public string SceneId;
        public float Time;
        public ObservationActor Actor;
        public string ActionId;
        public string Text;
        public ObservationLogImportance Importance;
    }

    [Serializable]
    /// <summary>後からSaveDataへ移せる、プレイヤーが実行したNNN ACTIONの最小記録。</summary>
    public sealed class ObservationPlayerActionRecord
    {
        public int Day;
        public float Time;
        public string ActionId;
    }

    /// <summary>逐次実行中の一日だけ存在し、EndDay後は破棄されるRuntime情報。</summary>
    public sealed class ObservationDayContext
    {
        public ObservationDayPhase Phase { get; internal set; } = ObservationDayPhase.Observing;
        public CatReportResult CatReport { get; internal set; }
        public int Day { get; internal set; }
        public float CurrentTime { get; internal set; }
        public bool HasMajorEventOccurred { get; internal set; }
        public bool IsComplete { get; internal set; }
        public HashSet<string> ExecutedEventIds { get; } = new HashSet<string>();
        public List<ObservationPlayerActionRecord> PlayerActionRecords { get; } = new List<ObservationPlayerActionRecord>();
    }

    [Serializable]
    /// <summary>
    /// 一日分の純粋な出力。通常行動、Major Event、ログ、状態前後をUIやテストがまとめて利用できる。
    /// 候補一覧は、なぜ選ばれた／選ばれなかったかをInspectorで追跡するために保持する。
    /// </summary>
    public sealed class DaySimulationResult
    {
        public List<string> AddedKnowledgeTags = new List<string>();
        public CatReportResult CatReport;
        public InvestigationResult Investigation;
        public int Day;
        public List<string> NormalActionIds = new List<string>();
        public string MajorEventId;
        public ObservationEventRole MajorEventRole;
        public ObservationEventCategory MajorEventCategory;
        public List<ObservationLogEntry> LogEntries = new List<ObservationLogEntry>();
        public RelationshipState StateBefore;
        public RelationshipState StateAfter;
        public List<string> NormalCandidates = new List<string>();
        public List<string> RelationshipCandidates = new List<string>();
        public List<string> ProblemCandidates = new List<string>();

        /// <summary>重要な出来事を優先し、表示場面数をイベント数から分離する。生ログは保持する。</summary>
        public List<ObservationScene> GetPresentationScenes(int maximum = 4)
        {
            if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
            return LogEntries.GroupBy(x => new { x.EventId, Scene = x.SceneId ?? "" })
                .Select(group => new ObservationScene { EventId = group.Key.EventId, SceneId = group.Key.Scene,
                    Logs = group.OrderBy(x => x.Time).ToList() })
                .OrderByDescending(scene => scene.EventId == MajorEventId)
                .ThenBy(scene => scene.Logs[0].Time).Take(maximum)
                .OrderBy(scene => scene.Logs[0].Time).ToList();
        }
    }

    /// <summary>アニメーションの素材とは独立した、UIへ渡す一場面の観察ログ。</summary>
    public sealed class ObservationScene
    {
        public string EventId;
        public string SceneId;
        public List<ObservationLogEntry> Logs;
    }
}
