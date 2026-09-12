#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NNN.Editor
{
    [InitializeOnLoad]
    public static class SatoHachiPlayableVerification
    {
        private const string Key = "NNN.PlayableVerification";
        private static double deadline;
        private static bool sawStreet;
        private static readonly System.Collections.Generic.HashSet<int> reviewedDays = new System.Collections.Generic.HashSet<int>();
        static SatoHachiPlayableVerification() { EditorApplication.update += Tick; }
        public static void RunBatch()
        {
            SatoHachiPlayableSceneBuilder.CreateScene();
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (!EditorApplication.isPlaying) return;
            if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 180;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Playable timeout.");
                var controller = UnityEngine.Object.FindFirstObjectByType<SatoHachiPlayableController>();
                if (controller == null || controller.Simulator == null) return;
                controller.AutoAdvance = true;
                if (controller.ReviewVisible)
                {
                    Check(controller.Phase == ObservationDayPhase.CatReport && controller.Review != null, "Review must use the existing report phase.");
                    reviewedDays.Add(controller.Day);
                }
                sawStreet |= controller.Presentation.BackgroundId == "SHOPPING_STREET";
                if (!string.IsNullOrEmpty(controller.Error)) throw new Exception(controller.Error);
                if (!controller.SliceComplete) return;
                Check(reviewedDays.Count == 11 && controller.Reviews.Count == 11, "Every day displays one review.");
                Check(controller.Measurements.All(x => x.Clicks == x.Scenes + 5), "Exactly one additional review input per day.");
                Check(controller.Measurements.Count == 11 && controller.Measurements.All(x => x.Seconds > 0 && x.Scenes > 0 && x.Automated), "Timing, mode and scene logging.");
                Check(sawStreet && controller.Presentation.BackgroundId == "HOME", "Street visit and home return presentation.");
                Check(UnityEngine.Object.FindFirstObjectByType<NNNDebugUI>() == null, "Legacy debug overlay must not cover playable.");
                var baseline = new ObservationSimulator(SatoHachiObservationFactory.CreateRoute(), controller.Seed);
                for (int day = 1; day <= 11; day++)
                {
                    baseline.BeginDay(day); baseline.CompleteObservation(); baseline.CompleteCatReport(); baseline.ApplyNNNAction(controller.Definition.GuidedActions[day - 1]);
                    var expected = baseline.EndDay(); var actual = controller.DayResults[day - 1];
                    Check(expected.MajorEventId == actual.MajorEventId && expected.NormalActionIds.SequenceEqual(actual.NormalActionIds)
                        && expected.StateAfter.ToString() == actual.StateAfter.ToString() && expected.CatReport.Id == actual.CatReport.Id, "Presentation changed simulation DAY " + day);
                }
                Check(controller.Presentation.Actions.Resolve("MISSING_ACTION") == ActorMotion.Idle
                    && controller.Presentation.Actions.Resolve("HARNESS_LOW_WALK") == ActorMotion.Walk, "Fallback mapping.");
                Debug.Log("PLAYABLE GAME VIEW CONTROLLER: PASS / 11 days / presentation invariance / backgrounds / telemetry. CSV: " + controller.CsvPath);
                SessionState.SetBool(Key, false); EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Key, false); EditorApplication.Exit(1); }
        }
        private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}
#endif
