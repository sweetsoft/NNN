using System.Collections.Generic;
using UnityEngine;
namespace NNN
{
    /// <summary>Sceneが選ぶRouteとInsightの供給元。進行Controllerは猫のIDを判定しない。</summary>
    public abstract class ObservationPlayableContent : ScriptableObject
    {
        /// <summary>Restartごとに新しい実行用Routeを作る。表示側から前回の可変状態を再利用しない。</summary>
        public abstract ObservationRouteDefinition CreateRoute();
        /// <summary>同じRouteに対応する表示定義を供給する。比較履歴は別のInsightPresenterが持つ。</summary>
        public abstract List<ObservationInsightDefinition> CreateInsights();
    }
}
