using System.Collections.Generic;
using UnityEngine;
namespace NNN
{
    /// <summary>Sceneが選ぶRouteとInsightの供給元。進行Controllerは猫のIDを判定しない。</summary>
    public abstract class ObservationPlayableContent : ScriptableObject
    {
        public abstract ObservationRouteDefinition CreateRoute();
        public abstract List<ObservationInsightDefinition> CreateInsights();
    }
}
