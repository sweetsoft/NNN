using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NNN
{
    public enum ActorMotion { Idle, Walk, Look, Sit, Crouch, Phone, Place, Hold, Meow, Paw, Jump, Groom, Eat }
    [Serializable] public sealed class ActionMotionBinding { public string ActionId; public ActorMotion Motion; }
    public enum ActorFacing { Default, Left, Right }
    public enum ScenePropState { Normal, Moved, Fallen, Tipped, Stored, Hidden }
    /// <summary>Scene内だけの小物状態。RequiredWorldFlagは読み取り条件で、フラグを作成しない。</summary>
    [Serializable] public sealed class ScenePropSetup
    {
        public string PropId;
        public string PointId;
        public ScenePropState State;
        public bool Visible = true;
        public string RequiredWorldFlag;
    }
    /// <summary>一つの観察Captionを見せている間の演出。SimulationのログやActionを上書きしない。</summary>
    [Serializable] public sealed class PresentationStep
    {
        public ObservationActor Actor;
        public string ActionId;
        public string TargetPointId;
        public string TargetPropId;
        public ScenePropState PropState = ScenePropState.Moved;
        public float DurationSeconds = .75f;
    }
    [Serializable] public sealed class SceneStageBinding
    {
        public string EventId;
        public string SceneId;
        public string BackgroundId = "HOME";
        [FormerlySerializedAs("HumanMarker")] public string HumanStartPoint = "Human_Default";
        public ActorFacing HumanFacing;
        [FormerlySerializedAs("CatMarker")] public string CatStartPoint = "Cat_Default";
        public ActorFacing CatFacing;
        public string CatDestination;
        public List<ScenePropSetup> PropSetups = new List<ScenePropSetup>();
        public string EventCgId;
        public int TriggerStep = -1;
    }
    [Serializable] public sealed class InformationLabel { public string Id; public string Label; }
    /// <summary>
    /// 場面の途中で必要になる配置・移動・小物演出。イベントの条件や効果とは独立した表示データ。
    /// EventId + SceneId + 0始まりStepIndexで照合する。空SceneIdは空SceneIdにだけ一致する。
    /// Stepsが空なら従来のログActionを再生する。Captionは常に元ログのTextを使う。
    /// </summary>
    [Serializable] public sealed class LogStageBinding
    {
        public string EventId;
        public string SceneId;
        [FormerlySerializedAs("LogIndex")] public int StepIndex;
        public List<PresentationStep> Steps = new List<PresentationStep>();
        // Markerは即時配置、Destinationは動作に伴う移動先。空欄はその項目を上書きしない。
        // PropIdはObservationPropView.Id、各Marker名はObservationScenePresenter.Markers配下を指す。
        public string CatMarker, CatDestination, HumanMarker, PropId, PropDestination;
    }
    [CreateAssetMenu(menuName = "NNN/Observation/Presentation")]
    public sealed class ObservationPresentationDefinition : ScriptableObject
    {
        public List<ActionMotionBinding> Actions = new List<ActionMotionBinding>();
        public List<SceneStageBinding> Stages = new List<SceneStageBinding>();
        public List<InformationLabel> Information = new List<InformationLabel>();
        public List<LogStageBinding> LogStages = new List<LogStageBinding>();
        public List<ScenePropSetup> WorldPropSetups = new List<ScenePropSetup>();
        public List<string> GuidedActions = new List<string>();
        public string HumanName = "佐藤";
        public string CatName = "ハチ";
        public string SliceTitle = "SatoHachi Vertical Slice";
        public string InformationText(string id) => Information.Find(x => x.Id == id)?.Label ?? id;
        public LogStageBinding FindStep(string eventId, string sceneId, int index) => LogStages.Find(x =>
            x.EventId == eventId && (x.SceneId ?? "") == (sceneId ?? "") && x.StepIndex == index);
    }
}
