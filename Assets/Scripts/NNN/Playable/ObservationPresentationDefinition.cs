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
    [Serializable] public sealed class LogStageBinding
    {
        public string EventId;
        public int LogIndex;
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
