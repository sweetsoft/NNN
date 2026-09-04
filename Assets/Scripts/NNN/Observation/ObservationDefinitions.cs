using System;
using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    public enum DispatchMethod { Visit, Placement, Introduced, Rescue }
    public enum HumanToCatState { Avoid, Watch, Approach, Care }
    public enum CatWarinessState { High, Medium, Low, Relaxed }
    public enum SettlementState { Unknown, Visiting, Territory, Home }
    public enum RelationshipHistoryFlag { Seen, Approached, Watered, Fed, EnteredHome, SniffedHuman, Touched, Played, SatBeside, Greeted, FollowedHuman }
    public enum RelationshipMemory { HumanWaited, SafeEntry, OverTouched, RespectedSignal, HumanAdaptedEnvironment }
    public enum ObservationEventCategory { Normal, Relationship, Problem, Milestone }
    public enum ObservationActor { Human, Cat, Environment }
    public enum ObservationLogImportance { Normal, Attention, Emergency }
    public enum ObservationConditionType
    {
        HumanStateAtLeast, HumanStateAtMost, WarinessAtLeast, WarinessAtMost,
        SettlementAtLeast, SettlementAtMost, HasHistory, MissingHistory,
        HasMemory, MissingMemory, EventOccurred, EventNotOccurred,
        DaysSinceLastMajorAtLeast, CatTrait, HumanTrait
    }

    [Serializable]
    public sealed class RelationshipState
    {
        public HumanToCatState HumanToCat;
        public CatWarinessState CatWariness;
        public SettlementState Settlement;
        public RelationshipState Clone() => new RelationshipState { HumanToCat = HumanToCat, CatWariness = CatWariness, Settlement = Settlement };
        public override string ToString() => HumanToCat + " / " + CatWariness + " / " + Settlement;
    }

    [Serializable]
    public sealed class RelationshipStateChange
    {
        public bool SetHumanToCat;
        public HumanToCatState HumanToCat;
        public bool SetCatWariness;
        public CatWarinessState CatWariness;
        public bool SetSettlement;
        public SettlementState Settlement;
        public void Apply(RelationshipState state)
        {
            if (SetHumanToCat) state.HumanToCat = HumanToCat;
            if (SetCatWariness) state.CatWariness = CatWariness;
            if (SetSettlement) state.Settlement = Settlement;
        }
    }

    [Serializable]
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
    public sealed class ObservationLogTemplate
    {
        [Range(0f, 24f)] public float Time;
        public ObservationActor Actor;
        public string ActionId;
        [TextArea] public string Text;
        public ObservationLogImportance Importance;
    }

    [CreateAssetMenu(menuName = "NNN/Observation/Event")]
    public sealed class ObservationEventDefinition : IdDefinition
    {
        public ObservationEventCategory Category;
        [Range(1, 30)] public int EarliestDay = 1;
        [Range(1, 30)] public int LatestDay = 30;
        public int BasePriority = 50;
        public bool Repeatable;
        public List<ObservationEventCondition> Conditions = new List<ObservationEventCondition>();
        public List<RelationshipHistoryFlag> AddHistoryFlags = new List<RelationshipHistoryFlag>();
        public List<RelationshipMemory> AddMemories = new List<RelationshipMemory>();
        public RelationshipStateChange StateChange = new RelationshipStateChange();
        public List<ObservationLogTemplate> Logs = new List<ObservationLogTemplate>();
        [TextArea] public string DebugDescription;
    }

    [CreateAssetMenu(menuName = "NNN/Observation/Route")]
    public sealed class ObservationRouteDefinition : ScriptableObject
    {
        public HumanBenchmarkDefinition Human;
        public CatDefinition Cat;
        public DispatchMethod DispatchMethod = DispatchMethod.Visit;
        public List<ObservationEventDefinition> Events = new List<ObservationEventDefinition>();
    }

    [Serializable]
    public sealed class ObservationLogEntry
    {
        public float Time;
        public ObservationActor Actor;
        public string ActionId;
        public string Text;
        public ObservationLogImportance Importance;
    }

    [Serializable]
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
