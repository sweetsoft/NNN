using UnityEngine;
namespace NNN
{
    /// <summary>毎SceneでWorld StateとSetupから再構成する一時的な小物演出。Simulationへ書き戻さない。</summary>
    public sealed class ObservationPropView : MonoBehaviour
    {
        public string Id;
        public Transform RestMarker;
        public int Moves { get; private set; }
        public ScenePropState State { get; private set; }
        private Transform target;
        private bool initialized;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private void Initialize()
        {
            if (initialized) return;
            initialPosition = transform.position; initialRotation = transform.rotation; initialized = true;
        }
        public void StopMotion() => target = null;
        /// <summary>位置の持ち越しはしない。Movesだけは表示状態と別のセッション累計。</summary>
        public void ResetProp(bool newSession = false)
        {
            Initialize(); StopMotion();
            transform.position = RestMarker != null ? RestMarker.position : initialPosition;
            transform.rotation = initialRotation; State = ScenePropState.Normal; gameObject.SetActive(true);
            if (newSession) Moves = 0;
        }
        public void ApplySetup(Transform point, ScenePropState state, bool visible)
        {
            StopMotion(); if (point != null) transform.position = point.position;
            State = state; gameObject.SetActive(visible && state != ScenePropState.Hidden && state != ScenePropState.Stored);
        }
        // Fallen/Tippedは演出上の意味。物理演算や永続フラグへ変換しない。
        public void MoveTo(Transform marker, ScenePropState state = ScenePropState.Moved)
        {
            State = state;
            if (marker != null) { target = marker; Moves++; }
            if (state == ScenePropState.Stored || state == ScenePropState.Hidden) gameObject.SetActive(false);
        }
        private void Update()
        { if (target != null) transform.position = Vector3.MoveTowards(transform.position, target.position, 4 * Time.unscaledDeltaTime); }
    }
}
