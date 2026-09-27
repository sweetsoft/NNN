using UnityEngine;
namespace NNN {
    /// <summary>ActionIdを素材側のモーションへ変換。未登録の動作はIdle。</summary>
    public sealed class ObservationActionPresenter : MonoBehaviour
    {
        public ObservationPresentationDefinition Definition;
        public CharacterActorView Human;
        public CharacterActorView Cat;
        public ActorMotion Resolve(string actionId) => Definition.Actions.Find(x => x.ActionId == actionId)?.Motion ?? ActorMotion.Idle;
        public void Play(ObservationLogEntry log, Transform catDestination)
            => Play(log.Actor, log.ActionId, log.Actor == ObservationActor.Cat ? catDestination : null);
        /// <summary>Presentation専用Actionにも対応する。渡されたLogを変更しない。</summary>
        public void Play(ObservationActor actorKind, string actionId, Transform destination)
        {
            var actor = actorKind == ObservationActor.Cat ? Cat : actorKind == ObservationActor.Human ? Human : null;
            if (actor == null) return;
            var motion = Resolve(actionId);
            // 同じジャンプ指示でも低いMarkerへ向かうときは降下素材を選ぶ。Simulationのログは変更しない。
            string visualAction = actor == Cat && motion == ActorMotion.Jump && destination != null &&
                destination.position.y < actor.transform.position.y - .01f ? "CAT_JUMP_DOWN" : actionId;
            actor.PlayAction(motion, visualAction);
            if (motion == ActorMotion.Walk || motion == ActorMotion.Jump) actor.MoveTo(destination);
        }
    }
}
