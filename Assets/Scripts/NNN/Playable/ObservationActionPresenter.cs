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
        {
            var actor = log.Actor == ObservationActor.Cat ? Cat : log.Actor == ObservationActor.Human ? Human : null;
            if (actor == null) return;
            var motion = Resolve(log.ActionId); actor.PlayAction(motion);
            if (actor == Cat && (motion == ActorMotion.Walk || motion == ActorMotion.Jump)) actor.MoveTo(catDestination);
        }
    }
}
