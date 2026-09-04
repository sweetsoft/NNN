using System;
using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    /// <summary>猫を人間の生活圏へ接続する導入方法。v0.1ではVisitのみを実行対象とする。</summary>
    public enum DispatchMethod { Visit, Placement, Introduced, Rescue }
    /// <summary>好感度ではなく、人間が猫に対して実際に取る接触行動の段階。</summary>
    public enum HumanToCatState { Avoid, Watch, Approach, Care }
    /// <summary>猫が許容できる対人距離の段階。宣言順はHighからRelaxedへ警戒が下がる。</summary>
    public enum CatWarinessState { High, Medium, Low, Relaxed }
    /// <summary>猫が対象住居を一時訪問先から生活拠点へ変えていく段階。</summary>
    public enum SettlementState { Unknown, Visiting, Territory, Home }
    /// <summary>一度起きた観測事実。後続イベントの前提条件として消去せず保持する。</summary>
    public enum RelationshipHistoryFlag { Seen, Approached, Watered, Fed, EnteredHome, SniffedHuman, Touched, Played, SatBeside, Greeted, FollowedHuman }
    /// <summary>単なる発生履歴ではなく、後の行動選択に意味を持つ経験・学習結果。</summary>
    public enum RelationshipMemory { HumanWaited, SafeEntry, OverTouched, RespectedSignal, HumanAdaptedEnvironment }
    /// <summary>通常描写と、一日最大一件のMajor Eventを分類する。</summary>
    public enum ObservationEventCategory { Normal, Relationship, Problem, Milestone }
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
        SettlementAtLeast, SettlementAtMost, HasHistory, MissingHistory,
        HasMemory, MissingMemory, EventOccurred, EventNotOccurred,
        DaysSinceLastMajorAtLeast, CatTrait, HumanTrait
    }

    [Serializable]
    /// <summary>
    /// その時点の関係を三つの離散状態で表す。数値的な親密度・成功率としてUI表示する用途ではない。
    /// 日次結果ではCloneを保存し、イベント適用前後を同じ参照にしない。
    /// </summary>
    public sealed class RelationshipState
    {
        public HumanToCatState HumanToCat;
        public CatWarinessState CatWariness;
        public SettlementState Settlement;
        /// <summary>DaySimulationResultに変更前後の独立したスナップショットを残す。</summary>
        public RelationshipState Clone() => new RelationshipState { HumanToCat = HumanToCat, CatWariness = CatWariness, Settlement = Settlement };
        public override string ToString() => HumanToCat + " / " + CatWariness + " / " + Settlement;
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
        public bool SetSettlement;
        public SettlementState Settlement;
        /// <summary>指定された軸だけを実行中の関係状態へ反映する。</summary>
        public void Apply(RelationshipState state)
        {
            if (SetHumanToCat) state.HumanToCat = HumanToCat;
            if (SetCatWariness) state.CatWariness = CatWariness;
            if (SetSettlement) state.Settlement = Settlement;
        }
    }

    [Serializable]
    /// <summary>
    /// ScriptableObject上で一条件を表現する共用コンテナ。
    /// Typeに応じてIntValue、StringValue、またはいずれかのenum値だけが評価される。
    /// </summary>
    public sealed class ObservationEventCondition
    {
        public ObservationConditionType Type;
        public int IntValue;
        public string StringValue;
        public RelationshipHistoryFlag History;
        public RelationshipMemory Memory;
        public HumanToCatState HumanState;
        public CatWarinessState Wariness;
        public SettlementState Settlement;
    }

    [Serializable]
    /// <summary>イベント定義に保存する観測ログの雛形。Textには推測を入れず画面で観測できる事実を書く。</summary>
    public sealed class ObservationLogTemplate
    {
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
        [Range(1, 30)] public int EarliestDay = 1;
        [Range(1, 30)] public int LatestDay = 30;
        public int BasePriority = 50;
        /// <summary>trueは日常描写用。falseのイベントIDは発生済み集合に入り、二度目を候補から除外する。</summary>
        public bool Repeatable;
        public List<ObservationEventCondition> Conditions = new List<ObservationEventCondition>();
        public List<RelationshipHistoryFlag> AddHistoryFlags = new List<RelationshipHistoryFlag>();
        public List<RelationshipMemory> AddMemories = new List<RelationshipMemory>();
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
    }

    [Serializable]
    /// <summary>UIへ渡す実行済みログ。テンプレートから複製されるため表示側はイベント定義を参照しなくてよい。</summary>
    public sealed class ObservationLogEntry
    {
        public float Time;
        public ObservationActor Actor;
        public string ActionId;
        public string Text;
        public ObservationLogImportance Importance;
    }

    [Serializable]
    /// <summary>
    /// 一日分の純粋な出力。通常行動、Major Event、ログ、状態前後をUIやテストがまとめて利用できる。
    /// 候補一覧は、なぜ選ばれた／選ばれなかったかをInspectorで追跡するために保持する。
    /// </summary>
    public sealed class DaySimulationResult
    {
        public int Day;
        public List<string> NormalActionIds = new List<string>();
        public string MajorEventId;
        public List<ObservationLogEntry> LogEntries = new List<ObservationLogEntry>();
        public RelationshipState StateBefore;
        public RelationshipState StateAfter;
        public List<string> NormalCandidates = new List<string>();
        public List<string> RelationshipCandidates = new List<string>();
        public List<string> ProblemCandidates = new List<string>();
    }
}
