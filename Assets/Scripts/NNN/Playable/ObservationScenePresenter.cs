using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    /// <summary>Flag成立で表示する設備、またはHideWhenSetで非表示にする収納済み小物を指定する。</summary>
    [System.Serializable] public sealed class WorldVisibilityBinding { public string Flag; public GameObject Target; public bool HideWhenSet; }
    /// <summary>一場面に含まれる複数ログを連続再生する。背景と配置はDefinitionを読む。</summary>
    public sealed class ObservationScenePresenter : MonoBehaviour
    {
        public ObservationActionPresenter Actions;
        public Transform Markers;
        public GameObject Home;
        public GameObject ShoppingStreet;
        public List<WorldVisibilityBinding> WorldProps = new List<WorldVisibilityBinding>();
        public List<ObservationPropView> Props = new List<ObservationPropView>();
        /// <summary>
        /// BeginDayで翌朝の工作効果が適用されたあとに呼ぶ。受け取った状態を描画へ反映するだけで、
        /// 観察を再生したことを理由に設備を増やしたり、Simulator側のフラグを書き換えたりしない。
        /// </summary>
        public void SyncWorld(ISet<string> flags)
        { foreach (var prop in WorldProps) if (prop.Target != null) prop.Target.SetActive(flags.Contains(prop.Flag) != prop.HideWhenSet); }
        public string Caption { get; private set; }
        public string BackgroundId { get; private set; } = "HOME";
        public string EventId { get; private set; }
        public int LogIndex { get; private set; }
        public int LogCount { get; private set; }
        private Coroutine sequence;
        public bool IsPlaying => sequence != null;
        private Transform Marker(string id) => string.IsNullOrEmpty(id) ? null : Markers.Find(id);
        /// <summary>場面・Caption・初期姿勢を戻す。収納した小物などの日跨ぎ保持は各Propの設定に従う。</summary>
        public void ResetDay()
        {
            Stop(); Caption = "今日は、どんな一日になるだろう。"; EventId = null;
            foreach (var prop in Props) prop.ResetProp();
            SetStage(new SceneStageBinding());
        }
        public void Stop() { if (sequence != null) StopCoroutine(sequence); sequence = null; }
        /// <summary>
        /// 同じイベントでもSceneIdが異なる場面は別の背景・配置を選べる。
        /// 未定義の場面はHOMEの標準配置を使い、ログ自体の再生は止めない。
        /// </summary>
        public void PlayScene(ObservationScene scene, bool fast)
        {
            Stop(); EventId = scene.EventId;
            var binding = Actions.Definition.Stages.Find(x => x.EventId == scene.EventId && (x.SceneId ?? "") == (scene.SceneId ?? "")) ?? new SceneStageBinding();
            SetStage(binding);
            sequence = StartCoroutine(PlayLogs(scene.Logs, Marker(binding.CatDestination), fast));
        }
        private void SetStage(SceneStageBinding binding)
        {
            BackgroundId = binding.BackgroundId; Home.SetActive(BackgroundId != "SHOPPING_STREET"); ShoppingStreet.SetActive(BackgroundId == "SHOPPING_STREET");
            Actions.Human.ResetPose(); Actions.Cat.ResetPose();
            Actions.Human.SetVisible(true); Actions.Cat.SetVisible(true);
            Actions.Human.PlaceAt(Marker(binding.HumanMarker)); Actions.Cat.PlaceAt(Marker(binding.CatMarker));
        }
        private IEnumerator PlayLogs(List<ObservationLogEntry> logs, Transform target, bool fast)
        {
            LogCount = logs.Count;
            for (int i = 0; i < logs.Count; i++)
            {
                // UIの進捗は1始まり、PresentationDefinitionのログ番号は0始まり。
                // ログ番号はこのSceneのLogs内の添字なので、場面を分割したらBindingも見直す。
                LogIndex = i + 1; Caption = logs[i].Text;
                var stage = Actions.Definition.LogStages.Find(x => x.EventId == EventId && x.LogIndex == i);
                if (stage != null)
                {
                    if (!string.IsNullOrEmpty(stage.CatMarker)) Actions.Cat.PlaceAt(Marker(stage.CatMarker));
                    if (!string.IsNullOrEmpty(stage.HumanMarker)) Actions.Human.PlaceAt(Marker(stage.HumanMarker));
                }
                // 即時配置を先に行い、その地点からActionの移動を開始する。
                // ログ固有の移動先がなければ場面共通の移動先を使う。小物はActorと独立して動く。
                Actions.Play(logs[i], stage != null && !string.IsNullOrEmpty(stage.CatDestination) ? Marker(stage.CatDestination) : target);
                if (stage != null && !string.IsNullOrEmpty(stage.PropId)) Props.Find(x => x.Id == stage.PropId)?.MoveTo(Marker(stage.PropDestination));
                // 手動では各ログを読む時間を取り、自動検証では一場面を約0.7秒へ短縮する。
                // fastは進行確認用で、歩行や小物が各ログの待ち時間内に到着する保証ではない。
                yield return new WaitForSecondsRealtime(fast ? 0.7f / Mathf.Max(1, logs.Count) : 1.5f);
            }
            sequence = null;
        }
    }
}
