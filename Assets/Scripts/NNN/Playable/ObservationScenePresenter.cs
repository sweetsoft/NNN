using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    [System.Serializable] public sealed class WorldVisibilityBinding { public string Flag; public GameObject Target; }
    /// <summary>一場面に含まれる複数ログを連続再生する。背景と配置はDefinitionを読む。</summary>
    public sealed class ObservationScenePresenter : MonoBehaviour
    {
        public ObservationActionPresenter Actions;
        public Transform Markers;
        public GameObject Home;
        public GameObject ShoppingStreet;
        public List<WorldVisibilityBinding> WorldProps = new List<WorldVisibilityBinding>();
        public void SyncWorld(ISet<string> flags)
        { foreach (var prop in WorldProps) if (prop.Target != null) prop.Target.SetActive(flags.Contains(prop.Flag)); }
        public string Caption { get; private set; }
        public string BackgroundId { get; private set; } = "HOME";
        public string EventId { get; private set; }
        public int LogIndex { get; private set; }
        public int LogCount { get; private set; }
        private Coroutine sequence;
        public bool IsPlaying => sequence != null;
        private Transform Marker(string id) => string.IsNullOrEmpty(id) ? null : Markers.Find(id);
        public void ResetDay()
        {
            Stop(); Caption = "今日は、どんな一日になるだろう。"; EventId = null;
            SetStage(new SceneStageBinding());
        }
        public void Stop() { if (sequence != null) StopCoroutine(sequence); sequence = null; }
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
                LogIndex = i + 1; Caption = logs[i].Text; Actions.Play(logs[i], target);
                yield return new WaitForSecondsRealtime(fast ? 0.7f / Mathf.Max(1, logs.Count) : 1.5f);
            }
            sequence = null;
        }
    }
}
