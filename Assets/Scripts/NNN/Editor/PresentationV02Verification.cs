#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K = NNN.SatoKotaObservationFactory;

namespace NNN.Editor
{
    /// <summary>Simulationを停止したままPresentationだけを動かし、Scene独立性と逆流の不在を検証する。</summary>
    public static class PresentationV02Verification
    {
        public static void RunBatch()
        {
            try { SatoKotaPlayableSceneBuilder.CreateKotaScene(); SatoKotaPlayableVerification.RunBatch(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void Run(SatoHachiPlayableController controller, Action completed)
        {
            controller.AutoAdvance = false; controller.enabled = false;
            controller.StartCoroutine(Guard(Verify(controller), () => { controller.enabled = true; completed(); }));
        }
        private static IEnumerator Guard(IEnumerator test, Action completed)
        {
            double deadline = EditorApplication.timeSinceStartup + 35;
            while (true)
            {
                object next;
                try
                {
                    Check(EditorApplication.timeSinceStartup < deadline, "Presentation test timeout");
                    if (!test.MoveNext()) { completed(); yield break; }
                    next = test.Current;
                }
                catch (Exception e) { (test as IDisposable)?.Dispose(); Debug.LogException(e); EditorApplication.Exit(1); yield break; }
                yield return next;
            }
        }
        private static IEnumerator Verify(SatoHachiPlayableController c)
        {
            var p = c.Presentation; var cat = p.Actions.Cat; var human = p.Actions.Human;
            var original = p.Actions.Definition;
            var definition = UnityEngine.Object.Instantiate(original); p.Actions.Definition = definition;
            string before = Signature(c);
            var pen = p.Props.Single(x => x.Id == "PEN"); var fragile = p.Props.Single(x => x.Id == "FRAGILE");
            Action<string> onCg = null;
            try
            {
                var flags = new HashSet<string>(c.Simulator.State.WorldFlags);
                Check(flags.Contains(K.Tower) && flags.Contains(K.DeskSpot) && flags.Contains(K.DeskCleared), "World fixture");
                p.SyncWorld(flags); flags.Clear(); // 受け取り元の変更が表示スナップショットへ逆流しない。
                p.SetupScene(Scene(K.DeskTrouble));
                Check(p.BackgroundId == "HOME_EVENING" && At(cat.transform, p, "Cat_DeskFloor"), "DAY4 setup");
                Check(pen.State == ScenePropState.Normal && At(pen.transform, p, "Pen_Desk"), "DAY4 pen setup");
                p.SetupScene(Scene(K.SharedSpace));
                Check(fragile.State == ScenePropState.Stored && !fragile.gameObject.activeSelf && At(fragile.transform, p, "Object_Storage"), "DAY11 stored from world");
                Check(p.Props.Single(x => x.Id == "CAT_CUSHION").gameObject.activeSelf, "DAY11 cushion from world");
                Check(pen.State == ScenePropState.Normal && At(pen.transform, p, "Pen_Desk"), "DAY11 pen reconstructed");

                definition.Stages.Add(new SceneStageBinding { EventId = "ISOLATION", SceneId = "A", CatStartPoint = "Cat_Bed" });
                definition.Stages.Add(new SceneStageBinding { EventId = "ISOLATION", SceneId = "B", CatStartPoint = "Cat_DeskFloor", CatFacing = ActorFacing.Right,
                    EventCgId = "TEST_CG", TriggerStep = 0, PropSetups = new List<ScenePropSetup> {
                        new ScenePropSetup { PropId = "PEN", PointId = "Pen_Desk" } } });
                definition.LogStages.Add(new LogStageBinding { EventId = "ISOLATION", SceneId = "A", StepIndex = 0, Steps = new List<PresentationStep> {
                    new PresentationStep { Actor = ObservationActor.Cat, ActionId = "CAT_WALK", TargetPointId = "Cat_Desk", DurationSeconds = .1f },
                    new PresentationStep { TargetPropId = "PEN", TargetPointId = "Pen_Floor", PropState = ScenePropState.Fallen, DurationSeconds = 2 } } });
                definition.LogStages.Add(new LogStageBinding { EventId = "ISOLATION", SceneId = "B", StepIndex = 0, Steps = new List<PresentationStep> {
                    new PresentationStep { Actor = ObservationActor.Cat, ActionId = "CAT_SIT", DurationSeconds = .2f } } });
                var a = Scene("ISOLATION", "A"); var b = Scene("ISOLATION", "B");
                // ログのActionと演出Actionを意図的に変え、Caption・元Actionが不変なことも確認。
                a.Logs.Add(new ObservationLogEntry { Actor = ObservationActor.Cat, ActionId = "CAT_LOOK", Text = "観察事実A" });
                b.Logs.Add(new ObservationLogEntry { Actor = ObservationActor.Cat, ActionId = "CAT_LOOK", Text = "観察事実B" });
                p.PlayScene(a, false); yield return new WaitForSecondsRealtime(.6f);
                Check(pen.State == ScenePropState.Fallen && At(pen.transform, p, "Pen_Floor"), "Prop fell in A");
                p.SetupScene(b); // Aの移動途中でも、Bが自分のSetupから開始する。
                Check(At(cat.transform, p, "Cat_DeskFloor") && cat.SpriteAnimation.Renderer.flipX, "Independent actor placement/facing");
                Check(pen.State == ScenePropState.Normal && At(pen.transform, p, "Pen_Desk"), "Prop reset across scenes");
                yield return new WaitForSecondsRealtime(.3f);
                Check(At(cat.transform, p, "Cat_DeskFloor") && At(pen.transform, p, "Pen_Desk"), "Old motion cancelled");
                int cgCount = 0; onCg = id => { Check(id == "TEST_CG", "CG id"); cgCount++; }; p.EventCgRequested += onCg;
                p.PlayScene(b, false);
                Check(cat.SpriteAnimation.CurrentClip.name == "ANIM_SIT" && p.Caption == "観察事実B", "SceneId isolation and caption separation");
                while (p.IsPlaying) yield return null;
                Check(cgCount == 1 && p.CurrentEventCgId == "TEST_CG", "CG trigger exactly once");
                p.SetupScene(Scene("UNDEFINED", "C"));
                Check(At(cat.transform, p, "Cat_Default") && At(human.transform, p, "Human_Default"), "Legacy/default stage fallback");
                Check(p.CurrentEventCgId == null && p.WorldProps.Single(x => x.Flag == K.Tower).Target.activeSelf, "CG reset and world persistence");
                Check(fragile.State == ScenePropState.Stored, "World prop reset survives unrelated scene");
                Check(a.Logs[0].ActionId == "CAT_LOOK" && b.Logs[0].ActionId == "CAT_LOOK", "Logs not mutated");

                // 実データのDAY9を通常速度で検証。人間が先に移動し、猫が机から降りてから追う。
                var follow = Scene(K.Proximity);
                follow.Logs.AddRange(c.Route.Events.Single(x => x.Id == K.Proximity).Logs.Select(x =>
                    new ObservationLogEntry { Actor = x.Actor, ActionId = x.ActionId, Text = x.Text }));
                p.PlayScene(follow, false);
                while (p.LogIndex < 3) yield return null;
                yield return new WaitForSecondsRealtime(.8f);
                Check(!At(human.transform, p, "Human_Default") && At(cat.transform, p, "Cat_Desk"), "Human moves before cat");
                while (p.LogIndex < 4) yield return null;
                Check(At(human.transform, p, "Human_OtherSide"), "Human destination reached");
                yield return new WaitForSecondsRealtime(.85f);
                Check(cat.transform.position.y < .05f, "Cat descends before following");
                while (p.IsPlaying) yield return null;
                Check(At(cat.transform, p, "Cat_OtherSide"), "Cat follows human");
                p.SetupScene(Scene(K.DeskTrouble));
                Check(At(cat.transform, p, "Cat_DeskFloor"), "Following endpoint not inherited");
                Check(Signature(c) == before, "Presentation changed Simulation state");
                Debug.Log("PRESENTATION V0.2: PASS / Scene independence / prop reset / world snapshot / isolation / SceneId / caption / DAY4 DAY11 setup / DAY9 follow / CG hook / legacy fallback.");
            }
            finally
            {
                if (onCg != null) p.EventCgRequested -= onCg;
                p.Stop(); p.Actions.Definition = original; UnityEngine.Object.Destroy(definition);
            }
        }
        private static ObservationScene Scene(string eventId, string sceneId = "") => new ObservationScene { EventId = eventId, SceneId = sceneId, Logs = new List<ObservationLogEntry>() };
        private static bool At(Transform value, ObservationScenePresenter p, string point) => Vector3.Distance(value.position, p.Markers.Find(point).position) < .05f;
        private static string Signature(SatoHachiPlayableController c) => c.Simulator.State.Relationship.ToString() + "|" +
            string.Join(",", c.Simulator.State.WorldFlags.OrderBy(x => x)) + "|" + string.Join(",", c.Simulator.State.PlayerKnowledgeFlags.OrderBy(x => x)) + "|" +
            string.Join(",", c.Simulator.State.HistoryFlags.OrderBy(x => x)) + "|" + string.Join(",", c.Simulator.State.MemoryFlags.OrderBy(x => x));
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    }
}
#endif
