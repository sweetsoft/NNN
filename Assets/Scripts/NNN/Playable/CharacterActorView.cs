using UnityEngine;

namespace NNN
{
    /// <summary>素材を差し替え可能な仮キャラクター。モーションはView内で完結し、ゲーム状態を変更しない。</summary>
    public sealed class CharacterActorView : MonoBehaviour
    {
        public Transform Visual;
        public Transform Head;
        public Transform Gesture;
        private ActorMotion motion;
        private Vector3 baseScale;
        private Vector3 headPosition;
        private Vector3 gesturePosition;
        private Vector3 destination;
        private bool moving;
        private float started;
        private void Awake()
        {
            baseScale = Visual.localScale;
            if (Head != null) headPosition = Head.localPosition;
            if (Gesture != null) gesturePosition = Gesture.localPosition;
        }
        public void PlayAction(ActorMotion action) { ResetPose(); motion = action; started = Time.unscaledTime; }
        public void MoveTo(Transform marker) { if (marker != null) { destination = marker.position; moving = true; Face(destination - transform.position); } }
        public void PlaceAt(Transform marker) { if (marker != null) transform.position = marker.position; moving = false; }
        public void Face(Vector3 direction)
        { if (direction.sqrMagnitude > 0.001f) Visual.localRotation = Quaternion.Euler(0, direction.x < 0 ? -18 : 18, 0); }
        public void SetVisible(bool visible) => Visual.gameObject.SetActive(visible);
        public void ResetPose()
        {
            motion = ActorMotion.Idle; moving = false;
            Visual.localPosition = Vector3.zero; Visual.localScale = baseScale; Visual.localRotation = Quaternion.identity;
            if (Head != null) { Head.localPosition = headPosition; Head.localRotation = Quaternion.identity; }
            if (Gesture != null) { Gesture.localPosition = gesturePosition; Gesture.localRotation = Quaternion.identity; }
        }
        private void Update()
        {
            float t = Time.unscaledTime - started;
            if (moving) { transform.position = Vector3.MoveTowards(transform.position, destination, 2.0f * Time.unscaledDeltaTime); if (Vector3.Distance(transform.position, destination) < 0.01f) moving = false; }
            float wave = Mathf.Sin(t * 8);
            float height = motion == ActorMotion.Jump ? Mathf.Abs(Mathf.Sin(t * 3)) * 0.5f : motion == ActorMotion.Walk ? Mathf.Abs(wave) * 0.06f : 0;
            Visual.localPosition = Vector3.up * height;
            float squash = motion == ActorMotion.Sit || motion == ActorMotion.Crouch ? 0.64f : 1f;
            Visual.localScale = Vector3.Scale(baseScale, new Vector3(1, squash, 1));
            if (Head != null)
            {
                float nod = motion == ActorMotion.Eat || motion == ActorMotion.Groom ? 18 + wave * 12 : motion == ActorMotion.Meow ? wave * 8 : 0;
                Head.localRotation = Quaternion.Euler(nod, motion == ActorMotion.Look ? Mathf.Sin(t * 2) * 25 : 0, 0);
            }
            if (Gesture != null)
            {
                bool raised = motion == ActorMotion.Phone || motion == ActorMotion.Hold;
                Gesture.localPosition = gesturePosition + Vector3.up * (raised ? 0.25f : motion == ActorMotion.Place || motion == ActorMotion.Paw ? Mathf.Abs(wave) * 0.14f : 0);
                Gesture.localRotation = Quaternion.Euler(0, 0, raised ? 30 : motion == ActorMotion.Groom ? wave * 15 : 0);
            }
        }
    }
}
