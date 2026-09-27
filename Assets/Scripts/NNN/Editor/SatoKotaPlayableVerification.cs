#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using K = NNN.SatoKotaObservationFactory;

namespace NNN.Editor
{
    /// <summary>
    /// 保存されたSceneをPlayModeで動かし、共有Controllerから最終日まで到達することを確認する。
    /// 自動送りは進行・接続の回帰確認用であり、読書速度や手動プレイのテンポ評価には使わない。
    /// </summary>
    [InitializeOnLoad]
    public static class SatoKotaPlayableVerification
    {
        // PlayModeへの移行でドメインがリロードされても、検証中であることをSessionStateに保持する。
        // staticコンストラクタがEditorの更新へ再接続し、Tick内でControllerの初期化を待つ。
        private const string Key = "NNN.KotaPlayableVerification";
        private static double deadline;
        private static readonly System.Collections.Generic.HashSet<Sprite> idleFrames = new System.Collections.Generic.HashSet<Sprite>();
        static SatoKotaPlayableVerification() { EditorApplication.update += Tick; }
        /// <summary>Scene生成は別の検証入口が担当する。ここでは保存済みSceneの参照切れも検出する。</summary>
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene(SatoKotaPlayableSceneBuilder.KotaScenePath);
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 180;
            try
            {
                Check(EditorApplication.timeSinceStartup < deadline, "Kota playable timeout");
                var c = UnityEngine.Object.FindFirstObjectByType<SatoHachiPlayableController>();
                if (c == null || c.Simulator == null) return;
                var sprite = c.Presentation.Actions.Cat.SpriteAnimation;
                Check(sprite != null && sprite.Renderer != null && sprite.Renderer.sprite != null, "Cat sprite references");
                if (sprite.CurrentClip == sprite.Idle) idleFrames.Add(sprite.Renderer.sprite);
                c.AutoAdvance = true;
                Check(c.Route.Cat.Id == "CAT_KOTA" && c.Definition.CatName == "コタ", "Wrong demo content");
                Check(c.Presentation.BackgroundId.StartsWith("HOME"), "Outdoor background");
                Check(string.IsNullOrEmpty(c.Error), c.Error);
                foreach (var prop in c.Presentation.WorldProps)
                {
                    bool allowed = c.Simulator.State.WorldFlags.Contains(prop.Flag) != prop.HideWhenSet;
                    // Worldは表示可能性を決める。Scene内のStored/Hiddenで一時非表示になるPropもある。
                    bool temporaryHidden = c.Presentation.Props.Any(x => x.gameObject == prop.Target &&
                        (x.State == ScenePropState.Stored || x.State == ScenePropState.Hidden));
                    Check(prop.Target.activeSelf == (allowed && !temporaryHidden), "World prop visibility");
                }
                if (!c.SliceComplete) return;
                Check(idleFrames.Count > 1, "Idle sprite frames must advance during Play Mode");
                var actor = c.Presentation.Actions.Cat;
                foreach (var pair in new[] {
                    new[] { "CAT_SIT", "ANIM_SIT" }, new[] { "CAT_REST", "ANIM_REST" },
                    new[] { "CAT_WALK", "ANIM_WALK" }, new[] { "CAT_APPROACH", "ANIM_WALK" },
                    new[] { "CAT_ENTER_HOME", "ANIM_WALK" }, new[] { "CAT_EXPLORE", "ANIM_WALK" },
                    new[] { "HARNESS_LOW_WALK", "ANIM_WALK" }, new[] { "CAT_RUN", "ANIM_RUN" },
                    new[] { "CAT_PAW", "ANIM_PAW" }, new[] { "CAT_PLAY", "ANIM_PAW" },
                    new[] { "CAT_RUB", "ANIM_RUB" }, new[] { "CAT_SLEEP", "ANIM_SLEEP" },
                    new[] { "CAT_JUMP", "ANIM_JUMP_UP" }, new[] { "CAT_JUMP_DOWN", "ANIM_JUMP_DOWN" },
                    new[] { "CAT_DRINK", "ANIM_DRINK" },
                    new[] { "UNKNOWN", "ANIM_IDLE" } })
                {
                    actor.PlayAction(ActorMotion.Sit, pair[0]);
                    Check(sprite.CurrentClip.name == pair[1] && sprite.Renderer.sprite.texture.name == pair[1], "Cat clip mapping " + pair[0]);
                    if (pair[1] != "ANIM_REST")
                    {
                        var first = sprite.Renderer.sprite;
                        sprite.Animator.Update(.25f);
                        Check(sprite.Renderer.sprite != first, "Sprite frames advance: " + pair[0]);
                    }
                }
                actor.Face(Vector3.right); Check(sprite.Renderer.flipX, "Sprite faces right");
                actor.Face(Vector3.left); Check(!sprite.Renderer.flipX, "Sprite faces left");
                actor.ResetPose(); Check(sprite.CurrentClip == sprite.Idle && !sprite.Renderer.flipX, "Sprite reset");
                Check(sprite.Actions.All(x => x.Clip.name.StartsWith("ANIM_")) && sprite.Idle.name == "ANIM_IDLE", "Confirmed clips only");
                foreach (string id in new[] { "CAT_JUMP", "CAT_JUMP_DOWN" })
                {
                    sprite.Play(id); sprite.Animator.Update(sprite.CurrentClip.length + .1f);
                    sprite.SendMessage("LateUpdate");
                    Check(sprite.CurrentClip == sprite.Idle, "One-shot jump returns to idle: " + id);
                }
                var target = new GameObject("Jump verification marker").transform;
                var jumpLog = new ObservationLogEntry { Actor = ObservationActor.Cat, ActionId = "CAT_JUMP" };
                target.position = actor.transform.position + Vector3.down;
                c.Presentation.Actions.Play(jumpLog, target);
                Check(sprite.CurrentClip.name == "ANIM_JUMP_DOWN", "Descending marker selects jump down");
                target.position = actor.transform.position + Vector3.up;
                c.Presentation.Actions.Play(jumpLog, target);
                Check(sprite.CurrentClip.name == "ANIM_JUMP_UP", "Ascending marker selects jump");
                actor.ResetPose(); UnityEngine.Object.Destroy(target.gameObject);
                Debug.Log("CAT SPRITES: PASS / all 11 clips including DRINK / animated frames / one-shot jumps / height selection / fallback / facing / reset.");
                Check(c.Measurements.Count == 11 && c.Reviews.Count == 11 && c.Measurements.All(x => x.Scenes > 0 && x.Seconds > 0), "Playable completion/telemetry");
                Check(c.Measurements.All(x => x.Clicks == x.Scenes + 5), "Shared input flow");
                // Viewを通さない日次実行と照合し、アニメーションやUIが物語の結果を変えていないか調べる。
                var baseline = new ObservationSimulator(K.CreateRoute(), c.Seed);
                for (int day = 1; day <= 11; day++)
                {
                    baseline.BeginDay(day); baseline.CompleteObservation(); baseline.CompleteCatReport(); baseline.ApplyNNNAction(K.GuidedActions[day - 1]);
                    var expected = baseline.EndDay(); var actual = c.DayResults[day - 1];
                    Check(expected.MajorEventId == actual.MajorEventId && expected.NormalActionIds.SequenceEqual(actual.NormalActionIds)
                        && expected.CatReport.Text == actual.CatReport.Text && expected.StateAfter.ToString() == actual.StateAfter.ToString(), "Presentation invariance DAY" + day);
                }
                foreach (var stage in c.Definition.LogStages)
                {
                    Check(c.Route.Events.Any(x => x.Id == stage.EventId && x.Logs.Count > stage.StepIndex), "Invalid log binding");
                    foreach (var marker in new[] { stage.CatMarker, stage.CatDestination, stage.HumanMarker, stage.PropDestination }.Where(x => !string.IsNullOrEmpty(x)))
                        Check(c.Presentation.Markers.Find(marker) != null, "Missing marker " + marker);
                    foreach (var step in stage.Steps)
                    {
                        if (!string.IsNullOrEmpty(step.TargetPointId)) Check(c.Presentation.Markers.Find(step.TargetPointId) != null, "Missing step point");
                        if (!string.IsNullOrEmpty(step.TargetPropId)) Check(c.Presentation.Props.Any(x => x.Id == step.TargetPropId), "Missing step prop");
                    }
                }
                // 終了位置は最後のSceneのSetup次第。永続位置ではなく演出回数とWorld由来状態を確認する。
                var pen = c.Presentation.Props.Single(x => x.Id == "PEN");
                Check(pen.Moves == 2, "Two pen drop sequences");
                Check(c.Presentation.Props.Single(x => x.Id == "FRAGILE").Moves == 1, "Human clearing presentation");
                var fragile = c.Presentation.Props.Single(x => x.Id == "FRAGILE");
                Check(fragile.State == ScenePropState.Stored && !fragile.gameObject.activeSelf && Vector3.Distance(fragile.transform.position, c.Presentation.Markers.Find("Object_Storage").position) < .05f, "World setup reconstructs storage");
                Check(c.Route.Events.SelectMany(x => x.Logs).All(x => c.Definition.Actions.Any(a => a.ActionId == x.ActionId)), "Animation mapping coverage");
                Debug.Log("KOTA PLAYABLE: PASS / DAY1-11 / pen drops x2 / human clearing / world props / action mapping / presentation invariance. CSV: " + c.CsvPath);
                SessionState.SetBool(Key, false);
                PresentationV02Verification.Run(c, () =>
                {
                    c.AutoAdvance = false; c.Restart();
                    Check(pen.Moves == 0 && fragile.Moves == 0 && fragile.State == ScenePropState.Normal && Vector3.Distance(fragile.transform.position, fragile.RestMarker.position) < .05f, "Replay prop reset");
                    SatoKotaVerification.Verify();
                    SatoHachiVerification.Verify(); SatoHachiInsightVerification.Verify();
                    NNNObservationBatchRunner.RunStressTest();
                });
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Key, false); EditorApplication.Exit(1); }
        }
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    }
}
#endif
