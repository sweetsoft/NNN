using System.Collections.Generic;
using UnityEngine;
namespace NNN
{
    [CreateAssetMenu(menuName = "NNN/Playable/Sato Kota")]
    public sealed class SatoKotaPlayableContent : ObservationPlayableContent
    {
        public override ObservationRouteDefinition CreateRoute() => SatoKotaObservationFactory.CreateRoute();
        public override List<ObservationInsightDefinition> CreateInsights() => SatoKotaInsightFactory.Create();
    }
}
