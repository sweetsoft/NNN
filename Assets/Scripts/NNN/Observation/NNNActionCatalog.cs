using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NNN
{
    public enum NNNActionKind { Investigation, Operation, Skip }

    /// <summary>プレイヤーが発見した情報。人物のTraitや経験のMemoryとは別に案件内へ保存する。</summary>
    public static class KnowledgeTag
    {
        public const string CatBoundarySignal = "KNOW_CAT_BOUNDARY_SIGNAL";
        public const string HumanPlayOpportunity = "KNOW_HUMAN_PLAY_OPPORTUNITY";
        public const string HomeObjectRisk = "KNOW_HOME_OBJECT_RISK";
    }

    /// <summary>選択画面と実行処理で共有する変更不能なアクション定義。</summary>
    public sealed class NNNActionDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public NNNActionKind Kind { get; }
        public string Knowledge { get; }
        public string TargetEventId { get; }
        public int PriorityBonus { get; }
        public int DurationDays { get; }

        internal NNNActionDefinition(string id, string name, string description, NNNActionKind kind,
            string knowledge = null, string target = null, int bonus = 0, int duration = 0)
        {
            Id = id; DisplayName = name; Description = description; Kind = kind;
            Knowledge = knowledge; TargetEventId = target; PriorityBonus = bonus; DurationDays = duration;
        }
    }

    public sealed class NNNActionOption
    {
        public NNNActionDefinition Definition { get; }
        public bool IsAvailable => UnavailableReason == null;
        public string UnavailableReason { get; }
        internal NNNActionOption(NNNActionDefinition definition, string reason)
        { Definition = definition; UnavailableReason = reason; }
    }

    public static class NNNActionCatalog
    {
        public const string InvestigateCat = "INVESTIGATE_CAT_SIGNAL";
        public const string InvestigateHuman = "INVESTIGATE_HUMAN_ROUTINE";
        public const string InvestigateHome = "INVESTIGATE_HOME_LAYOUT";
        public const string HintSignal = "OPERATION_SIGNAL_HINT";
        public const string ArrangePlay = "OPERATION_PLAY_OPPORTUNITY";
        public const string HintHome = "OPERATION_HOME_SAFETY_HINT";
        public const string Skip = "SKIP";

        public static ReadOnlyCollection<NNNActionDefinition> All { get; } = new List<NNNActionDefinition>
        {
            new NNNActionDefinition(InvestigateCat, "猫の距離感を調べる", "猫が接触を嫌がる前に出すサインを調べる。", NNNActionKind.Investigation, KnowledgeTag.CatBoundarySignal),
            new NNNActionDefinition(InvestigateHuman, "人間の生活リズムを調べる", "猫と短く遊べそうな時間帯を探す。", NNNActionKind.Investigation, KnowledgeTag.HumanPlayOpportunity),
            new NNNActionDefinition(InvestigateHome, "部屋の配置を調べる", "猫の動線と、落としやすい小物の置き場所を調べる。", NNNActionKind.Investigation, KnowledgeTag.HomeObjectRisk),
            new NNNActionDefinition(HintSignal, "猫のサインを伝える", "人間が猫の拒否サインに気づくきっかけを作る。", NNNActionKind.Operation, KnowledgeTag.CatBoundarySignal, "REL_RESPECT_SIGNAL", 80, 3),
            new NNNActionDefinition(ArrangePlay, "遊ぶきっかけを作る", "生活の空き時間に、おもちゃを手に取るきっかけを作る。", NNNActionKind.Operation, KnowledgeTag.HumanPlayOpportunity, "REL_PLAY_TOGETHER", 60, 3),
            new NNNActionDefinition(HintHome, "片づけのヒントを届ける", "小物の置き場所を見直すきっかけを作る。", NNNActionKind.Operation, KnowledgeTag.HomeObjectRisk, "REL_HUMAN_ADAPT_ENVIRONMENT", 80, 3),
            new NNNActionDefinition(Skip, "SKIP", "今日は手を加えず、様子を見る。", NNNActionKind.Skip)
        }.AsReadOnly();

        public static NNNActionDefinition Find(string id) => All.FirstOrDefault(x => x.Id == id);
    }
}
