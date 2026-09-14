using System;
using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    public enum ActorMotion { Idle, Walk, Look, Sit, Crouch, Phone, Place, Hold, Meow, Paw, Jump, Groom, Eat }
    [Serializable] public sealed class ActionMotionBinding { public string ActionId; public ActorMotion Motion; }
    [Serializable] public sealed class SceneStageBinding
    {
        public string EventId;
        public string SceneId;
        public string BackgroundId = "HOME";
        public string HumanMarker = "Human_Default";
        public string CatMarker = "Cat_Default";
        public string CatDestination;
    }
    [Serializable] public sealed class InformationLabel { public string Id; public string Label; }
    /// <summary>
    /// 場面の途中で必要になる配置・移動・小物演出。イベントの条件や効果とは独立した表示データ。
    /// 現在はEventIdと場面内の0始まりLogIndexで照合するため、SceneId別の同一番号を区別しない。
    /// コタは各イベントを一場面として使う。複数Sceneへ分割する場合は照合方法も確認する。
    /// </summary>
    [Serializable] public sealed class LogStageBinding
    {
        public string EventId;
        public int LogIndex;
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
        public List<string> GuidedActions = new List<string>();
        public string HumanName = "佐藤";
        public string CatName = "ハチ";
        public string SliceTitle = "SatoHachi Vertical Slice";
        public string InformationText(string id) => Information.Find(x => x.Id == id)?.Label ?? id;
    }
}
