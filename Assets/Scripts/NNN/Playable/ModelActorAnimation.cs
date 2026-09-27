using System;
using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    /// <summary>SteppedAnimatorのMasterのステートへActionを接続する。移動とSimulationは担当しない。</summary>
    public sealed class ModelActorAnimation : MonoBehaviour
    {
        [Serializable] public sealed class ActionState { public string ActionId; public string State; }
        public SteppedAnimator Stepped;
        public string IdleState = "HUMAN_IDLE_STAND";
        public Vector3 DefaultEuler = new Vector3(0, 180, 0);
        public List<ActionState> Actions = new List<ActionState>();
        public Animator MasterAnimator => Stepped != null ? Stepped.MasterAnimator : null;
        public string CurrentState { get; private set; }

        public void Play(string actionId)
        {
            var master = MasterAnimator;
            if (master == null) return;
            string state = Actions.Find(x => x.ActionId == actionId)?.State ?? actionId;
            if (string.IsNullOrEmpty(state) || !master.HasState(0, Animator.StringToHash(state))) state = IdleState;
            CurrentState = state;
            // MasterだけにPlayを送ると服や髪が旧ステートに残る。既存APIでMasterを含む全パーツへ送る。
            if (Stepped.isActiveAndEnabled) Stepped.Play(state);
        }
        public void ResetPose()
        {
            transform.localRotation = Quaternion.Euler(DefaultEuler);
            Play(IdleState);
        }
        public void Face(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
        public void MovementCompleted()
        {
            if (CurrentState == "HUMAN_WALK") Play(IdleState);
        }
    }
}
