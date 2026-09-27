using UnityEngine;

namespace NNN
{
    /// <summary>素材を差し替え可能なキャラクター。モーションはView内で完結し、ゲーム状態を変更しない。</summary>
    public sealed class CharacterActorView : MonoBehaviour
    {
        // このコンポーネントのTransformは場面内の位置、Visualは見た目だけの変形を担当する。
        // 歩行の上下動などでルート座標を揺らさず、Scene Markerへの移動と仮モーションを合成する。
        /// <summary>必須の見た目ルート。モデル差し替え時もローカル位置0・回転identityを基準にする。</summary>
        public Transform Visual;
        /// <summary>任意の頭パーツ。未設定なら、うなずきや見回しだけを省略する。</summary>
        public Transform Head;
        /// <summary>任意の手／前足パーツ。人間と猫で共通のジェスチャー処理を使う。</summary>
        public Transform Gesture;
        /// <summary>任意のスプライト素材。設定時はパーツ変形を行わず、クリップ再生へ委譲する。</summary>
        public SpriteActorAnimation SpriteAnimation;
        /// <summary>任意の3Dモデル素材。MasterAnimatorへの再生を委譲し、仮パーツ変形を省略する。</summary>
        public ModelActorAnimation ModelAnimation;
        // 仮モーション種別はPresenterで解決する。生のActionIdは各素材Presenterへ渡す。
        private ActorMotion motion;
        // Awake時の見た目サイズと各パーツ位置。毎フレームの変形を累積させず、この基準へ戻して計算する。
        private Vector3 baseScale;
        private Vector3 headPosition;
        private Vector3 gesturePosition;
        // MoveToを呼んだ瞬間のワールド座標を保存する。Markerが後で動いても追従先は変わらない。
        private Vector3 destination;
        private bool moving;
        // 現在の動作を開始した実時間。Time.timeScaleとは独立した位相計算に使用する。
        private float started;
        /// <summary>
        /// Sceneに配置された素材の基準値を一度だけ保存する。
        /// VisualはAwakeより前に設定しておく。実行中にパーツを差し替える場合は基準値の再取得が必要。
        /// 初期回転は保存しないため、パーツの向きはidentityで正しく見える階層を前提とする。
        /// </summary>
        private void Awake()
        {
            baseScale = Visual.localScale;
            if (Head != null) headPosition = Head.localPosition;
            if (Gesture != null) gesturePosition = Gesture.localPosition;
        }
        /// <summary>
        /// 前の動作・移動を解除し、向きは保ったまま新しいモーションを先頭から始める。
        /// 移動も伴う場合は、この呼び出しのあとにMoveToを呼ぶ。逆順だと移動が解除される。
        /// 仮モーションは次の指示まで継続する。スプライトの一回再生は素材Presenterへ委譲する。
        /// </summary>
        public void PlayAction(ActorMotion action, string actionId = null)
        {
            var facing = Visual.localRotation;
            bool flip = SpriteAnimation != null && SpriteAnimation.Renderer.flipX;
            var modelFacing = ModelAnimation != null ? ModelAnimation.transform.localRotation : Quaternion.identity;
            ResetPose(); motion = action; started = Time.unscaledTime;
            Visual.localRotation = facing;
            if (SpriteAnimation != null) { SpriteAnimation.Play(actionId); SpriteAnimation.Renderer.flipX = flip; }
            if (ModelAnimation != null) { ModelAnimation.Play(actionId); ModelAnimation.transform.localRotation = modelFacing; }
        }
        /// <summary>
        /// Markerの位置への移動を予約し、進行方向へ見た目を向ける。実際の位置更新はUpdateで行う。
        /// 移動モーション自体は選ばないため、PlayActionと組み合わせて使用する。
        /// nullなら何も変更せず、すでに進行中の移動も止めない。
        /// </summary>
        public void MoveTo(Transform marker) { if (marker != null) { destination = marker.position; moving = true; Face(destination - transform.position); } }
        /// <summary>
        /// 場面開始や配置切替用の即時移動。歩行補間を行わず、進行中の移動を解除する。
        /// Markerがnullでも移動は停止するが、位置・向き・モーションはそのままにする。
        /// </summary>
        public void PlaceAt(Transform marker) { if (marker != null) transform.position = marker.position; moving = false; }
        /// <summary>
        /// スプライトは左右反転、3Dモデルは移動方向、それ以外はX符号でVisualをY軸に±18度回す。
        /// 見た目の向きだけを変更し、ルートや移動先は変更しない。
        /// ほぼゼロの方向では現在の向きを維持する。
        /// </summary>
        public void Face(Vector3 direction)
        {
            if (SpriteAnimation != null) SpriteAnimation.Face(direction);
            else if (ModelAnimation != null) ModelAnimation.Face(direction);
            else if (direction.sqrMagnitude > 0.001f) Visual.localRotation = Quaternion.Euler(0, direction.x < 0 ? -18 : 18, 0);
        }
        /// <summary>見た目の階層だけを表示／非表示にする。ルートのUpdateや移動処理は停止しない。</summary>
        public void SetVisible(bool visible) => Visual.gameObject.SetActive(visible);
        /// <summary>
        /// Idleへ戻し、移動を止めて見た目とパーツの変形を初期化する。場面内のルート位置は保持する。
        /// Visualの位置と各回転は0／identityへ、サイズとパーツ位置はAwakeで保存した値へ戻す。
        /// 経過時間の起点はここでは更新せず、PlayActionが新しい動作の開始時に設定する。
        /// </summary>
        public void ResetPose()
        {
            motion = ActorMotion.Idle; moving = false;
            if (SpriteAnimation != null) SpriteAnimation.ResetPose();
            if (ModelAnimation != null) ModelAnimation.ResetPose();
            Visual.localPosition = Vector3.zero; Visual.localScale = baseScale; Visual.localRotation = Quaternion.identity;
            if (Head != null) { Head.localPosition = headPosition; Head.localRotation = Quaternion.identity; }
            if (Gesture != null) { Gesture.localPosition = gesturePosition; Gesture.localRotation = Quaternion.identity; }
        }
        private void Update()
        {
            // 待ち時間を実時間で扱うScene Presenterに合わせ、TimeScale=0でも仮演出を進める。
            float t = Time.unscaledTime - started;
            // ワールド座標を毎秒2 Unity単位で目標へ近づける。距離0.01未満で停止するため、
            // 到達判定時に目標へ厳密にスナップする仕様ではない。経路探索や衝突判定も行わない。
            if (moving)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, 2.0f * Time.unscaledDeltaTime);
                if (Vector3.Distance(transform.position, destination) < .01f)
                { moving = false; if (ModelAnimation != null) ModelAnimation.MovementCompleted(); }
            }
            // 素材付きActorはクリップの姿勢を使用する。仮モデルの縮小や上下動を重ねない。
            if (SpriteAnimation != null || ModelAnimation != null) return;
            // 各パーツで共有する周期波。8はラジアン/秒であり、1秒に8往復する指定ではない。
            float wave = Mathf.Sin(t * 8);
            // Jumpは最大0.5、Walkは最大0.06の上下動をVisualにだけ加える。
            // Jumpは放物運動や一回きりの跳躍ではなく、動作切替まで繰り返す仮表現。
            // 位置移動とは独立しているため、移動終了後もWalkの上下動は継続する。
            float height = motion == ActorMotion.Jump ? Mathf.Abs(Mathf.Sin(t * 3)) * 0.5f : motion == ActorMotion.Walk ? Mathf.Abs(wave) * 0.06f : 0;
            Visual.localPosition = Vector3.up * height;
            // 座る／しゃがむ姿勢を縦方向の縮小で代用する。元サイズを基準にするため縮小は累積しない。
            float squash = motion == ActorMotion.Sit || motion == ActorMotion.Crouch ? 0.64f : 1f;
            Visual.localScale = Vector3.Scale(baseScale, new Vector3(1, squash, 1));
            if (Head != null)
            {
                // 食事・毛づくろいは下向き18度を中心に±12度、鳴く動作は±8度のうなずき。
                // LookだけはY軸に±25度の見回しを加える。ルート全体の向きとは別に合成される。
                float nod = motion == ActorMotion.Eat || motion == ActorMotion.Groom ? 18 + wave * 12 : motion == ActorMotion.Meow ? wave * 8 : 0;
                Head.localRotation = Quaternion.Euler(nod, motion == ActorMotion.Look ? Mathf.Sin(t * 2) * 25 : 0, 0);
            }
            if (Gesture != null)
            {
                // Phone/Holdは手を0.25持ち上げて30度傾ける。Place/Pawは高さ0〜0.14の反復動作。
                // GroomはZ軸回転だけを揺らす。物を実際につかむ処理や物理的な接触判定は含まない。
                // ペンなどの小物移動はScene Presenterが別途指示し、このViewは状態を変更しない。
                bool raised = motion == ActorMotion.Phone || motion == ActorMotion.Hold;
                Gesture.localPosition = gesturePosition + Vector3.up * (raised ? 0.25f : motion == ActorMotion.Place || motion == ActorMotion.Paw ? Mathf.Abs(wave) * 0.14f : 0);
                Gesture.localRotation = Quaternion.Euler(0, 0, raised ? 30 : motion == ActorMotion.Groom ? wave * 15 : 0);
            }
        }
    }
}
