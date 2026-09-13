#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using K = NNN.SatoKotaObservationFactory;

namespace NNN.Editor
{
    [InitializeOnLoad]
    public static class SatoKotaPlayableVerification
    {
        private const string Key = "NNN.KotaPlayableVerification";
        private static double deadline;
        static SatoKotaPlayableVerification() { EditorApplication.update += Tick; }
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
                c.AutoAdvance = true;
                Check(c.Route.Cat.Id == "CAT_KOTA" && c.Definition.CatName == "コタ", "Wrong demo content");
                Check(c.Presentation.BackgroundId == "HOME", "Outdoor background");
                Check(string.IsNullOrEmpty(c.Error), c.Error);
                foreach (var prop in c.Presentation.WorldProps)
                    Check(prop.Target.activeSelf == (c.Simulator.State.WorldFlags.Contains(prop.Flag) != prop.HideWhenSet), "World prop visibility");
                if (!c.SliceComplete) return;
                Check(c.Measurements.Count == 11 && c.Reviews.Count == 11 && c.Measurements.All(x => x.Scenes > 0 && x.Seconds > 0), "Playable completion/telemetry");
                Check(c.Measurements.All(x => x.Clicks == x.Scenes + 5), "Shared input flow");
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
                    Check(c.Route.Events.Any(x => x.Id == stage.EventId && x.Logs.Count > stage.LogIndex), "Invalid log binding");
                    foreach (var marker in new[] { stage.CatMarker, stage.CatDestination, stage.HumanMarker, stage.PropDestination }.Where(x => !string.IsNullOrEmpty(x)))
                        Check(c.Presentation.Markers.Find(marker) != null, "Missing marker " + marker);
                }
                var pen = c.Presentation.Props.Single(x => x.Id == "PEN");
                Check(pen.Moves == 2 && Vector3.Distance(pen.transform.position, c.Presentation.Markers.Find("Pen_Floor").position) < .05f, "Two visible pen drops");
                Check(c.Presentation.Props.Single(x => x.Id == "FRAGILE").Moves == 1, "Human clearing presentation");
                var fragile = c.Presentation.Props.Single(x => x.Id == "FRAGILE");
                Check(Vector3.Distance(fragile.transform.position, c.Presentation.Markers.Find("Object_Storage").position) < .05f, "Stored object returned next day");
                Check(c.Route.Events.SelectMany(x => x.Logs).All(x => c.Definition.Actions.Any(a => a.ActionId == x.ActionId)), "Animation mapping coverage");
                Debug.Log("KOTA PLAYABLE: PASS / DAY1-11 / pen drops x2 / human clearing / world props / action mapping / presentation invariance. CSV: " + c.CsvPath);
                SessionState.SetBool(Key, false);
                c.AutoAdvance = false; c.Restart();
                Check(pen.Moves == 0 && fragile.Moves == 0 && Vector3.Distance(fragile.transform.position, fragile.RestMarker.position) < .05f, "Replay prop reset");
                SatoHachiVerification.Verify();
                SatoHachiInsightVerification.Verify();
                NNNObservationBatchRunner.RunStressTest();
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Key, false); EditorApplication.Exit(1); }
        }
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    }
}
#endif
