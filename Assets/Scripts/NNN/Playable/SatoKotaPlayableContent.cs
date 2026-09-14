using System.Collections.Generic;
using UnityEngine;
namespace NNN
{
    /// <summary>
    /// コタSceneに保存するFactoryの接続点。CatIdを判定する分岐を共通Controllerへ追加せず、
    /// 別の猫を追加する場合も同じ供給インターフェースを実装できる。
    /// </summary>
    [CreateAssetMenu(menuName = "NNN/Playable/Sato Kota")]
    public sealed class SatoKotaPlayableContent : ObservationPlayableContent
    {
        public override ObservationRouteDefinition CreateRoute() => SatoKotaObservationFactory.CreateRoute();
        public override List<ObservationInsightDefinition> CreateInsights() => SatoKotaInsightFactory.Create();
    }
}
