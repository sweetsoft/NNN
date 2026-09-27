using System;
using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    /// <summary>スプライト差し替えクリップの再生だけを担当する。物語の状態や移動先は変更しない。</summary>
    public sealed class SpriteActorAnimation : MonoBehaviour
    {
        [Serializable]
        public sealed class ActionClip
        {
            public string ActionId;
            public AnimationClip Clip;
            public bool Loop = true;
        }

        public SpriteRenderer Renderer;
        public Animator Animator;
        public Camera FacingCamera;
        public AnimationClip Idle;
        public List<ActionClip> Actions = new List<ActionClip>();
        public AnimationClip CurrentClip { get; private set; }
        private bool loop = true;

        /// <summary>
        /// 未登録Actionは確定済みの待機素材へ戻す。クリップはm_Spriteのみを持ち、
        /// 空パスがこのGameObjectのSpriteRendererを指すことをEditor側で検証する。
        /// 元クリップのLoop設定を変更せず、BindingのLoop指定で繰り返し／一回再生を選ぶ。
        /// </summary>
        public void Play(string actionId)
        {
            var binding = Actions.Find(x => x.ActionId == actionId);
            CurrentClip = binding?.Clip ?? Idle;
            loop = binding?.Loop ?? true;
            if (Animator != null && CurrentClip != null)
            {
                Animator.Play(CurrentClip.name, 0, 0);
                Animator.Update(0);
            }
        }

        public void ResetPose()
        {
            Renderer.flipX = false;
            Play(null);
        }

        // 提供素材は左向き。上下だけの移動では、直前の左右方向を維持する。
        public void Face(Vector3 direction)
        {
            if (Mathf.Abs(direction.x) > .001f) Renderer.flipX = direction.x > 0;
        }

        private void OnEnable() => Play(null);
        private void LateUpdate()
        {
            if (Animator != null && CurrentClip != null && Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
            {
                if (loop) Animator.Play(CurrentClip.name, 0, 0);
                else Play(null); // 跳躍は一度だけ再生し、次の指示まで待機する。
            }
            // 固定カメラの俯角に合わせ、2Dの猫が薄く見えないよう板を正対させる。
            var camera = FacingCamera != null ? FacingCamera : Camera.main;
            if (camera != null) transform.rotation = camera.transform.rotation;
        }

    }
}
