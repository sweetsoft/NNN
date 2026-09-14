using UnityEngine;
namespace NNN
{
    /// <summary>小物の移動先はScene Markerで指定する。ゲーム状態へは書き込まない。</summary>
    public sealed class ObservationPropView : MonoBehaviour
    {
        /// <summary>LogStageBinding.PropIdから参照する、Scene内で一意の小物ID。</summary>
        public string Id;
        /// <summary>再プレイ時の初期配置。座標をViewへ埋め込まず、Sceneで調整する。</summary>
        public Transform RestMarker;
        /// <summary>ペンは毎朝戻し、収納した小物はfalseにして前日の移動先を保持する。</summary>
        public bool ResetEachDay = true;
        /// <summary>物理衝突回数ではなく演出指示の受信回数。PlayMode検証で使用する。</summary>
        public int Moves { get; private set; }
        private Transform target;
        /// <summary>
        /// 日次リセットはResetEachDayに従う。新しいプレイでは必ず初期位置と計測を戻す。
        /// 日跨ぎで保持する小物はtargetも保持し、移動途中でも収納先へ向かい続ける。
        /// </summary>
        public void ResetProp(bool newSession = false)
        { if (!newSession && !ResetEachDay) return; target = null; if (RestMarker != null) transform.position = RestMarker.position; if (newSession) Moves = 0; }
        // 素材やMarkerが未配置なら何もしない。落下結果からKnowledgeやWorldFlagは変更しない。
        public void MoveTo(Transform marker) { if (marker != null) { target = marker; Moves++; } }
        // 物理演算ではなく一定速度の仮演出。UI待ちやTimeScaleに左右されない実時間を使う。
        private void Update()
        { if (target != null) transform.position = Vector3.MoveTowards(transform.position, target.position, 4 * Time.unscaledDeltaTime); }
    }
}
