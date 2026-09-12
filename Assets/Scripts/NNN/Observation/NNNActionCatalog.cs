using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NNN
{
    public enum NNNActionKind { Investigation, Operation, Skip }
    public enum NNNActionVisibility { Available, VisibleLocked, Hidden }
    public enum ObservationDayPhase { Observing, CatReport, ActionSelection, Completed }

    public static class KnowledgeTag
    {
        public const string CatBoundarySignal = "KNOW_CAT_BOUNDARY_SIGNAL";
        public const string HumanPlayOpportunity = "KNOW_HUMAN_PLAY_OPPORTUNITY";
        public const string HomeObjectRisk = "KNOW_HOME_OBJECT_RISK";
    }

    public static class WorldFlag
    {
        public const string HumanKnowsCatBoundarySignal = "HUMAN_KNOWS_CAT_BOUNDARY_SIGNAL";
        public const string PlayOpportunityPrepared = "PLAY_OPPORTUNITY_PREPARED";
        public const string HumanKnowsHomeObjectRisk = "HUMAN_KNOWS_HOME_OBJECT_RISK";
    }

    /// <summary>工作は翌朝に条件を変更する。特定の出来事や結果を指定しない。</summary>
    public sealed class OperationEffect
    {
        public ReadOnlyCollection<string> AddWorldFlags { get; }
        public ReadOnlyCollection<string> RemoveWorldFlags { get; }
        public ReadOnlyCollection<string> AddKnowledgeFlags { get; }
        public HomePreparation AddHomePreparation { get; }
        public HomePreparation RemoveHomePreparation { get; }

        public OperationEffect(IEnumerable<string> addWorldFlags = null, IEnumerable<string> removeWorldFlags = null,
            IEnumerable<string> addKnowledgeFlags = null, HomePreparation addHomePreparation = HomePreparation.None,
            HomePreparation removeHomePreparation = HomePreparation.None)
        {
            AddWorldFlags = Copy(addWorldFlags); RemoveWorldFlags = Copy(removeWorldFlags);
            AddKnowledgeFlags = Copy(addKnowledgeFlags);
            AddHomePreparation = addHomePreparation; RemoveHomePreparation = removeHomePreparation;
            if (AddWorldFlags.Intersect(RemoveWorldFlags).Any() || (addHomePreparation & removeHomePreparation) != 0)
                throw new ArgumentException("An effect cannot add and remove the same condition.");
        }
        internal static ReadOnlyCollection<string> Copy(IEnumerable<string> values)
            => (values ?? Enumerable.Empty<string>()).Distinct().ToList().AsReadOnly();
        internal void Apply(ObservationSimulationState state)
        {
            state.WorldFlags.ExceptWith(RemoveWorldFlags);
            state.WorldFlags.UnionWith(AddWorldFlags);
            state.PlayerKnowledgeFlags.UnionWith(AddKnowledgeFlags);
            state.Relationship.HomeReadiness = (state.Relationship.HomeReadiness | AddHomePreparation) & ~RemoveHomePreparation;
        }
    }

    public sealed class PendingOperationEffect
    {
        public string OperationId { get; }
        public int ActiveFromDay { get; }
        public OperationEffect Effect { get; }
        internal PendingOperationEffect(string id, int day, OperationEffect effect)
        { OperationId = id; ActiveFromDay = day; Effect = effect; }
    }

    public sealed class InvestigationResult
    {
        public string InvestigationId { get; internal set; }
        public string ResultText { get; internal set; }
        public ReadOnlyCollection<string> AddedKnowledgeTags { get; internal set; }
        // 発見と全条件成立を区別する。Lockedの新規工作もUIに伝える。
        public ReadOnlyCollection<string> NewlyDiscoveredOperationIds { get; internal set; }
        public ReadOnlyCollection<string> NewlyUnlockedOperationIds { get; internal set; }
    }

    public sealed class CatReportDefinition
    {
        public string Id;
        public List<ObservationEventCondition> Conditions = new List<ObservationEventCondition>();
        public string RequiredTodayEventId;
        public int Priority;
        public string Text;
    }

    public sealed class CatReportResult
    {
        public int Day { get; internal set; }
        public string Id { get; internal set; }
        public string Text { get; internal set; }
    }

    public sealed class NNNActionDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        /// <summary>表示専用。原因の確認や条件変更の意図であり、結果の保証ではない。</summary>
        public string IntentText { get; }
        public NNNActionKind Kind { get; }
        public ReadOnlyCollection<string> RequiredKnowledgeTags { get; }
        public ReadOnlyCollection<string> DiscoveryKnowledgeTags { get; }
        public ReadOnlyCollection<string> AddedKnowledgeTags { get; }
        public ReadOnlyCollection<ObservationEventCondition> Conditions { get; }
        public ReadOnlyCollection<ObservationEventCondition> DiscoveryConditions { get; }
        public string ResultText { get; }
        public OperationEffect Effect { get; }
        public NNNActionDefinition(string id, string name, string description, NNNActionKind kind,
            IEnumerable<string> requiredKnowledgeTags = null, IEnumerable<string> discoveryKnowledgeTags = null,
            IEnumerable<string> addedKnowledgeTags = null, string resultText = null, OperationEffect effect = null,
            IEnumerable<ObservationEventCondition> conditions = null, IEnumerable<ObservationEventCondition> discoveryConditions = null,
            string intentText = null)
        {
            if (kind == NNNActionKind.Operation && effect == null) throw new ArgumentNullException(nameof(effect));
            Id = id; DisplayName = name; Description = description; Kind = kind;
            IntentText = kind == NNNActionKind.Skip ? "" : intentText ?? description;
            RequiredKnowledgeTags = OperationEffect.Copy(requiredKnowledgeTags);
            DiscoveryKnowledgeTags = OperationEffect.Copy(discoveryKnowledgeTags);
            AddedKnowledgeTags = OperationEffect.Copy(addedKnowledgeTags);
            ResultText = resultText; Effect = effect;
            Conditions = (conditions ?? Enumerable.Empty<ObservationEventCondition>()).ToList().AsReadOnly();
            DiscoveryConditions = (discoveryConditions ?? Enumerable.Empty<ObservationEventCondition>()).ToList().AsReadOnly();
        }
    }

    public sealed class NNNActionOption
    {
        public NNNActionDefinition Definition { get; }
        public NNNActionVisibility Visibility { get; }
        public bool IsAvailable => Visibility == NNNActionVisibility.Available;
        public string UnavailableReason { get; }
        internal NNNActionOption(NNNActionDefinition definition, NNNActionVisibility visibility, string reason)
        { Definition = definition; Visibility = visibility; UnavailableReason = reason; }
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
            Research(InvestigateCat, "猫の距離感を調べる", KnowledgeTag.CatBoundarySignal, "接触が長くなると、尾や耳の動きが変わる。", "接触の仕方と猫の反応に関係があるか"),
            Research(InvestigateHuman, "人間の生活リズムを調べる", KnowledgeTag.HumanPlayOpportunity, "夕食のあとに短い空き時間がある。", "猫と過ごせる時間がいつあるか"),
            Research(InvestigateHome, "部屋の配置を調べる", KnowledgeTag.HomeObjectRisk, "猫の通り道の近くに、落としやすい小物がある。", "部屋の配置が猫の行動に関係しているか"),
            Operation(HintSignal, "猫のサインを伝える", new[] { KnowledgeTag.CatBoundarySignal }, KnowledgeTag.CatBoundarySignal, WorldFlag.HumanKnowsCatBoundarySignal),
            Operation(ArrangePlay, "遊ぶきっかけを作る", new[] { KnowledgeTag.HumanPlayOpportunity, KnowledgeTag.CatBoundarySignal }, KnowledgeTag.HumanPlayOpportunity, WorldFlag.PlayOpportunityPrepared),
            Operation(HintHome, "片づけのヒントを届ける", new[] { KnowledgeTag.HomeObjectRisk }, KnowledgeTag.HomeObjectRisk, WorldFlag.HumanKnowsHomeObjectRisk),
            new NNNActionDefinition(Skip, "SKIP", "今日は様子を見る。", NNNActionKind.Skip)
        }.AsReadOnly();
        private static NNNActionDefinition Research(string id, string name, string tag, string result, string intent)
            => new NNNActionDefinition(id, name, name, NNNActionKind.Investigation, addedKnowledgeTags: new[] { tag }, resultText: result, intentText: intent);
        private static NNNActionDefinition Operation(string id, string name, string[] required, string discovery, string flag)
            => new NNNActionDefinition(id, name, name, NNNActionKind.Operation, required, new[] { discovery }, effect: new OperationEffect(new[] { flag }),
                intentText: id == HintSignal ? "猫のサインを伝えると、接触の仕方が変わるか"
                    : id == ArrangePlay ? "遊ぶきっかけを用意すると、一緒に過ごす時間が変わるか"
                    : "配置のヒントを伝えると、部屋の使い方が変わるか");
        public static NNNActionDefinition Find(string id) => All.FirstOrDefault(x => x.Id == id);
    }
}
