using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    [Serializable] public sealed class WorldVisibilityBinding { public string Flag; public GameObject Target; public bool HideWhenSet; }
    /// <summary>複数Idから同じPlaceholderを参照してもよい。将来はStage Assetの背景へ差し替えられる。</summary>
    [Serializable] public sealed class BackgroundVariantBinding { public string Id; public GameObject Root; }

    /// <summary>独立した短い生活場面の再生。位置・姿勢・小物状態を前Sceneから継承しない。</summary>
    public sealed class ObservationScenePresenter : MonoBehaviour
    {
        public ObservationActionPresenter Actions;
        public Transform Markers;
        public GameObject Home;
        public GameObject ShoppingStreet;
        public List<BackgroundVariantBinding> Backgrounds = new List<BackgroundVariantBinding>();
        public List<WorldVisibilityBinding> WorldProps = new List<WorldVisibilityBinding>();
        public List<ObservationPropView> Props = new List<ObservationPropView>();
        // Simulatorのコレクションを保持せずコピーする。Presenterに書き戻し経路はない。
        private readonly HashSet<string> worldFlags = new HashSet<string>();
        public void SyncWorld(ISet<string> flags)
        {
            worldFlags.Clear(); if (flags != null) worldFlags.UnionWith(flags);
            foreach (var binding in WorldProps)
                if (binding.Target != null) binding.Target.SetActive(worldFlags.Contains(binding.Flag) != binding.HideWhenSet);
        }
        public string Caption { get; private set; }
        public string BackgroundId { get; private set; } = "HOME";
        public string EventId { get; private set; }
        public string SceneId { get; private set; }
        public int LogIndex { get; private set; }
        public int LogCount { get; private set; }
        public string CurrentEventCgId { get; private set; }
        /// <summary>指定Stepの演出終了時に通知するだけ。画像表示・進行停止・世界状態更新は行わない。</summary>
        public event Action<string> EventCgRequested;
        private Coroutine sequence;
        private SceneStageBinding currentStage;
        public bool IsPlaying => sequence != null;
        private Transform Marker(string id) => string.IsNullOrEmpty(id) || Markers == null ? null : Markers.Find(id);

        public void ResetDay()
        {
            Stop(); EventId = null; SceneId = null; LogIndex = 0; LogCount = 0; CurrentEventCgId = null;
            SetStage(new SceneStageBinding()); Caption = "今日は、どんな一日になるだろう。";
        }
        /// <summary>Coroutineだけでなく、途中のActor/Prop移動も打ち切る。</summary>
        public void Stop()
        {
            if (sequence != null) StopCoroutine(sequence); sequence = null;
            if (Actions != null) { Actions.Human.ResetPose(); Actions.Cat.ResetPose(); }
            foreach (var prop in Props) prop.StopMotion();
        }
        /// <summary>再生と独立したSetup入口。未定義SceneもDefault Markerから必ず組み立てる。</summary>
        public void SetupScene(ObservationScene scene)
        {
            Stop(); EventId = scene.EventId; SceneId = scene.SceneId;
            Caption = null; LogIndex = 0; LogCount = scene.Logs.Count; CurrentEventCgId = null;
            currentStage = Actions.Definition.Stages.Find(x => x.EventId == EventId &&
                (x.SceneId ?? "") == (SceneId ?? "")) ?? new SceneStageBinding();
            SetStage(currentStage);
        }
        public void PlayScene(ObservationScene scene, bool fast)
        {
            SetupScene(scene);
            if (scene.Logs.Count > 0) sequence = StartCoroutine(PlayLogs(scene.Logs, fast));
        }
        private void SetStage(SceneStageBinding binding)
        {
            // デフォルト -> World由来Setup -> Scene Setup -> World表示制約の順。
            // 前SceneでFallenやStoredになったことは、この再構成には影響しない。
            foreach (var prop in Props) prop.ResetProp();
            ApplySetups(Actions.Definition.WorldPropSetups);
            SetBackground(binding.BackgroundId);
            Actions.Human.ResetPose(); Actions.Cat.ResetPose();
            Actions.Human.SetVisible(true); Actions.Cat.SetVisible(true);
            Actions.Human.PlaceAt(Marker(binding.HumanStartPoint) ?? Marker("Human_Default"));
            Actions.Cat.PlaceAt(Marker(binding.CatStartPoint) ?? Marker("Cat_Default"));
            Face(Actions.Human, binding.HumanFacing); Face(Actions.Cat, binding.CatFacing);
            ApplySetups(binding.PropSetups);
            foreach (var world in WorldProps)
            {
                if (world.Target == null) continue;
                bool allowed = worldFlags.Contains(world.Flag) != world.HideWhenSet;
                // Scene側で非表示としたPropをWorld側から再表示しない。
                bool isProp = Props.Exists(x => x.gameObject == world.Target);
                world.Target.SetActive(allowed && (!isProp || world.Target.activeSelf));
            }
        }
        private void ApplySetups(List<ScenePropSetup> setups)
        {
            foreach (var setup in setups)
            {
                if (!string.IsNullOrEmpty(setup.RequiredWorldFlag) && !worldFlags.Contains(setup.RequiredWorldFlag)) continue;
                Props.Find(x => x.Id == setup.PropId)?.ApplySetup(Marker(setup.PointId), setup.State, setup.Visible);
            }
        }
        private static void Face(CharacterActorView actor, ActorFacing facing)
        {
            if (facing != ActorFacing.Default) actor.Face(facing == ActorFacing.Left ? Vector3.left : Vector3.right);
        }
        private void SetBackground(string id)
        {
            BackgroundId = string.IsNullOrEmpty(id) ? "HOME" : id;
            if (Home != null) Home.SetActive(false);
            if (ShoppingStreet != null) ShoppingStreet.SetActive(false);
            foreach (var background in Backgrounds) if (background.Root != null) background.Root.SetActive(false);
            var selected = Backgrounds.Find(x => x.Id == BackgroundId)?.Root;
            if (selected == null) selected = BackgroundId == "SHOPPING_STREET" ? ShoppingStreet : Home;
            if (selected != null) selected.SetActive(true);
        }
        private IEnumerator PlayLogs(List<ObservationLogEntry> logs, bool fast)
        {
            for (int i = 0; i < logs.Count; i++)
            {
                LogIndex = i + 1; Caption = logs[i].Text;
                var stage = Actions.Definition.FindStep(EventId, SceneId, i);
                if (stage != null)
                {
                    if (!string.IsNullOrEmpty(stage.CatMarker)) Actions.Cat.PlaceAt(Marker(stage.CatMarker));
                    if (!string.IsNullOrEmpty(stage.HumanMarker)) Actions.Human.PlaceAt(Marker(stage.HumanMarker));
                }
                if (stage != null && stage.Steps.Count > 0)
                {
                    foreach (var step in stage.Steps)
                    {
                        if (!string.IsNullOrEmpty(step.ActionId)) Actions.Play(step.Actor, step.ActionId, Marker(step.TargetPointId));
                        if (!string.IsNullOrEmpty(step.TargetPropId))
                            Props.Find(x => x.Id == step.TargetPropId)?.MoveTo(Marker(step.TargetPointId), step.PropState);
                        yield return new WaitForSecondsRealtime(fast ? .7f / logs.Count / stage.Steps.Count : Mathf.Max(.05f, step.DurationSeconds));
                    }
                }
                else
                {
                    // 旧データは元ログのActionへfallback。Captionはどちらの経路でも変更しない。
                    Actions.Play(logs[i], Marker(!string.IsNullOrEmpty(stage?.CatDestination) ? stage.CatDestination : currentStage.CatDestination));
                    if (!string.IsNullOrEmpty(stage?.PropId)) Props.Find(x => x.Id == stage.PropId)?.MoveTo(Marker(stage.PropDestination));
                    yield return new WaitForSecondsRealtime(fast ? .7f / logs.Count : 1.5f);
                }
                if (currentStage.TriggerStep == i && !string.IsNullOrEmpty(currentStage.EventCgId))
                {
                    CurrentEventCgId = currentStage.EventCgId;
                    EventCgRequested?.Invoke(CurrentEventCgId);
                }
            }
            sequence = null;
        }
    }
}
