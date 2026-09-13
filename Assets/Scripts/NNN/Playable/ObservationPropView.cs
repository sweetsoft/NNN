using UnityEngine;
namespace NNN
{
    /// <summary>小物の移動先はScene Markerで指定する。ゲーム状態へは書き込まない。</summary>
    public sealed class ObservationPropView : MonoBehaviour
    {
        public string Id;
        public Transform RestMarker;
        public bool ResetEachDay = true;
        public int Moves { get; private set; }
        private Transform target;
        public void ResetProp(bool newSession = false)
        { if (!newSession && !ResetEachDay) return; target = null; if (RestMarker != null) transform.position = RestMarker.position; if (newSession) Moves = 0; }
        public void MoveTo(Transform marker) { if (marker != null) { target = marker; Moves++; } }
        private void Update()
        { if (target != null) transform.position = Vector3.MoveTowards(transform.position, target.position, 4 * Time.unscaledDeltaTime); }
    }
}
