#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using H = NNN.SatoHachiObservationFactory;

namespace NNN.Editor
{
    public static class SatoHachiVerification
    {
        private static readonly string[] Plan = { NNNActionCatalog.Skip, NNNActionCatalog.Skip, NNNActionCatalog.Skip,
            H.InvestigateIndoor, H.InstallTower, H.InvestigatePast, H.PrepareGear, H.InvestigateHarness, H.InvestigateRoute,
            H.ShortTrip, NNNActionCatalog.Skip };

        [MenuItem("NNN/Observation/Verify Sato Hachi 32 Seeds")]
        public static void Verify()
        {
            var report = new StringBuilder("# SatoHachi DAY1–11 verification\n\n");
            int maxHiddenAvailableResearch = 0;
            for (int seed = 0; seed < 32; seed++)
            {
                var first = Run(seed, 0, report, ref maxHiddenAvailableResearch);
                var repeated = Run(seed, 0, null, ref maxHiddenAvailableResearch);
                Check(first == repeated, "Seed reproducibility: " + seed);
                Run(seed, 1, null, ref maxHiddenAvailableResearch); // SKIP control
                Run(seed, 2, null, ref maxHiddenAvailableResearch); // player chooses from presented options
            }
            VerifyWorldGate();
            VerifyFairness();
            VerifyDelayedImprovement();
            VerifyTripConditions();
            NNNObservationBatchRunner.VerifyMultipleSeeds();
            report.AppendLine("\n32 seeds × guided / skip / alternative policies: PASS (1056 days, plus 352 repeat days).");
            report.AppendLine("Guided Short Trip on DAY11: 32/32. No same-day trip, four scenes, return home, two observed knowledge tags and unresolved outdoor request: PASS.");
            report.AppendLine("Maximum consecutive days available Investigation was omitted: " + maxHiddenAvailableResearch);
            report.AppendLine("Synthetic contention (4 Operations + 3 Investigations, no fixed kind slots): all appear within 3 days; reads are stable: PASS.");
            report.AppendLine("World-only carrier gate, delayed Short Trip, multiple route knowledge, CAT REPORT budget and SatoSuzu regression: PASS.");
            string path = Path.Combine(Application.dataPath, "../outputs/sato-hachi-verification.md");
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, report.ToString());
            Debug.Log("SatoHachi 32 seeds / 3 policies / reproducibility / fairness / SatoSuzu: PASS\n" + report);
        }
        public static void RunBatch()
        {
            try { Verify(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static string Run(int seed, int policy, StringBuilder report, ref int maxOmitted)
        {
            var route = H.CreateRoute(); var sim = new ObservationSimulator(route, seed);
            var results = new List<DaySimulationResult>(); var signature = new StringBuilder();
            var omitted = new Dictionary<string, int>(); var random = new System.Random(seed);
            if (seed == 0 && policy == 0 && report != null) report.AppendLine("## Guided seed 0\n");
            for (int day = 1; day <= 11; day++)
            {
                sim.BeginDay(day);
                if (policy == 0 && day == 11)
                    Check(sim.State.WorldFlags.Contains(H.TripPrepared)
                        && ObservationConditionEvaluator.Evaluate(route.Events.Single(x => x.Id == H.ShortTripObservation), route, sim.State), "Prepared trip becomes eligible on DAY11 BeginDay.");
                string[] previousKnowledge = sim.State.PlayerKnowledgeFlags.ToArray();
                var catReport = sim.CompleteObservation();
                Check(ReferenceEquals(catReport, sim.CompleteObservation()) && sim.DayContext.PlayerActionRecords.Count == 0, "One free report per day.");
                sim.CompleteCatReport();
                var options = sim.GetNNNActionOptions();
                Check(options.Count <= 4 && options.All(x => x.Visibility != NNNActionVisibility.Hidden), "Candidate cap and visibility.");
                Check(options.Select(x => x.Definition.Id).SequenceEqual(sim.GetNNNActionOptions().Select(x => x.Definition.Id)), "Stable UI reads.");
                foreach (var research in sim.GetNNNActionOptions(true).Where(x => x.IsAvailable && x.Definition.Kind == NNNActionKind.Investigation))
                {
                    int streak = options.Any(x => x.Definition.Id == research.Definition.Id) ? 0 : (omitted.TryGetValue(research.Definition.Id, out int n) ? n : 0) + 1;
                    omitted[research.Definition.Id] = streak; maxOmitted = Math.Max(maxOmitted, streak);
                    Check(streak <= 2, "Available investigation starved: " + research.Definition.Id);
                }
                string action = policy == 0 ? Plan[day - 1] : policy == 1 ? NNNActionCatalog.Skip
                    : options.Where(x => x.IsAvailable).OrderBy(x => random.Next()).First().Definition.Id;
                Check(options.Any(x => x.IsAvailable && x.Definition.Id == action), "Guided action not presented D" + day + ": " + action);
                if (policy == 0 && day < 9) Check(Option(sim, H.ShortTrip).Visibility == NNNActionVisibility.Hidden, "Trip must not leak before route investigation.");
                string before = sim.State.Relationship.ToString(); string[] worldBefore = sim.State.WorldFlags.OrderBy(x => x).ToArray();
                var result = sim.ApplyNNNAction(action);
                Check(before == sim.State.Relationship.ToString() && worldBefore.SequenceEqual(sim.State.WorldFlags.OrderBy(x => x)), "No immediate world result.");
                Check(sim.GenerateNextEvent() == null && sim.DayContext.PlayerActionRecords.Count == 1, "No post-action events; daily budget.");
                Reject(() => sim.ApplyNNNAction(NNNActionCatalog.Skip), "Double daily action.");
                var completed = sim.EndDay(); results.Add(completed);
                Check(completed.NormalActionIds.Count >= 1 && completed.NormalActionIds.Count <= 3, "Normal count.");
                Check(completed.LogEntries.Zip(completed.LogEntries.Skip(1), (a, b) => a.Time <= b.Time).All(x => x), "Chronological logs.");
                Check(completed.CatReport.Text.Length <= 30 && !completed.CatReport.Text.Contains("佐藤"), "Short report, no unknown name.");
                Check(completed.StateAfter.Cohabitation == (day == 1 ? CohabitationState.Outside : day == 2 ? CohabitationState.Visiting : CohabitationState.LivingTogether), "Early cohabitation persists.");
                if (day == 3) Check(completed.StateAfter.CatAdaptation != CatAdaptationState.AtEase && completed.StateAfter.CatWariness != CatWarinessState.Relaxed
                    && completed.StateAfter.HumanAcceptance == HumanAcceptanceState.Welcoming, "Independent states at day 3.");
                if (policy == 0 && day == 6)
                {
                    Check(completed.MajorEventId == H.TowerResponse && sim.State.WorldFlags.Contains(H.Tower), "Tower used next day.");
                    Check(completed.NormalCandidates.Contains("NORMAL_USE_TOWER") && completed.NormalCandidates.Contains("NORMAL_VERTICAL_PATROL")
                        && completed.NormalCandidates.Contains("NORMAL_OUTSIDE_REQUEST"), "Indoor choices increase; outdoor request persists.");
                }
                if (policy == 0 && day == 8) Check(completed.MajorEventId == H.FirstHarness && result.AddedKnowledgeTags.Contains(H.HarnessUncertain), "Harness uncertainty.");
                Check(!sim.State.WorldFlags.Contains("CAT_HATES_HARNESS"), "No definitive harness dislike.");
                if (policy == 0 && day == 9)
                {
                    Check(completed.MajorEventId == H.HarnessSteps, "Second indoor harness attempt.");
                    Check(result.AddedKnowledgeTags.Count == 3 && result.AddedKnowledgeTags.Contains(H.BusyRoad) && result.AddedKnowledgeTags.Contains(H.CatOnlyPaths), "Multi-knowledge route result.");
                    Check(result.NewlyDiscoveredOperationIds.Contains(H.ShortTrip) && result.NewlyUnlockedOperationIds.Contains(H.ShortTrip), "Route discovers and unlocks Short Trip.");
                }
                if (policy == 0 && day == 10)
                    Check(string.IsNullOrEmpty(completed.MajorEventId) && !sim.State.OccurredEventIds.Contains(H.ShortTripObservation)
                        && !sim.State.WorldFlags.Contains(H.TripPrepared)
                        && sim.State.PendingOperations.Any(x => x.OperationId == H.ShortTrip && x.ActiveFromDay == 11)
                        && completed.CatReport.Text == "外、行きたい。", "DAY10 only queues the preparation.");
                if (policy == 0 && day == 11)
                {
                    Check(completed.MajorEventId == H.ShortTripObservation && completed.MajorEventCategory == ObservationEventCategory.Problem
                        && completed.MajorEventRole == ObservationEventRole.Core, "Short Trip must own DAY11 major slot.");
                    var scenes = completed.GetPresentationScenes();
                    Check(scenes.Count == 4 && scenes.All(x => x.EventId == H.ShortTripObservation)
                        && scenes.Select(x => x.SceneId).SequenceEqual(new[] { "DEPARTURE", "ARRIVAL", "MISMATCH", "RETURN" }), "Four trip scenes in order.");
                    Check(scenes[0].Logs.Any(x => x.Text.Contains("大通り")) && scenes[1].Logs.Any(x => x.Text.Contains("商店街"))
                        && scenes[2].Logs.Any(x => x.ActionId == "HUMAN_STOP") && scenes[3].Logs.Any(x => x.ActionId == "CAT_REST"), "Carrier road, arrival, inability to follow and safe home rest.");
                    Check(completed.AddedKnowledgeTags.SequenceEqual(new[] { H.TripPartiallyWorks, H.RouteMismatch })
                        && sim.State.PlayerKnowledgeFlags.IsSupersetOf(previousKnowledge), "Two observed facts, without deleting prior knowledge.");
                    Check(completed.NormalCandidates.Contains("NORMAL_OUTSIDE_REQUEST") && sim.State.PlayerKnowledgeFlags.Contains(H.WantsOutside)
                        && sim.State.PlayerKnowledgeFlags.Contains(H.FamiliarStreets)
                        && completed.StateAfter.ToString() == completed.StateBefore.ToString()
                        && !sim.State.WorldFlags.Any(x => x.Contains("SOLVED") || x.Contains("SUCCESS")), "Trip is not automatic resolution.");
                    Check(completed.CatReport.Text == "あそこ、行けなかった。", "DAY11 report.");
                    Check(completed.LogEntries.Count > scenes.Sum(x => x.Logs.Count), "Presentation keeps raw normal logs available.");
                }
                if (policy == 1) Check(!sim.State.WorldFlags.Contains(H.Tower) && !sim.State.WorldFlags.Contains(H.Harness), "Skip does not auto-complete operations.");
                if (policy == 1 && day == 11) Check(completed.MajorEventId != H.ShortTripObservation && completed.AddedKnowledgeTags.Count == 0, "No unprepared trip or unearned knowledge.");
                signature.Append(day + ":" + completed.MajorEventId + ":" + string.Join(",", completed.NormalActionIds) + ":" + catReport.Text + ":" + before + "|");
                if (seed == 0 && policy == 0 && report != null)
                {
                    report.AppendLine("### DAY " + day + "\n");
                    foreach (var scene in completed.GetPresentationScenes())
                    {
                        report.AppendLine("Scene: " + (string.IsNullOrEmpty(scene.SceneId) ? scene.EventId : scene.SceneId) + "\n");
                        foreach (var log in scene.Logs) report.AppendLine("- " + log.ActionId + ": " + log.Text);
                        report.AppendLine();
                    }
                    report.AppendLine("\nCAT REPORT: 「" + catReport.Text + "」\n\nACTION: " + action + "\n");
                    if (result != null) report.AppendLine(result.ResultText + "\n");
                    if (completed.AddedKnowledgeTags.Count > 0) report.AppendLine("Observed Knowledge: " + string.Join(", ", completed.AddedKnowledgeTags) + "\n");
                }
            }
            return signature.ToString();
        }

        private static void VerifyTripConditions()
        {
            var route = H.CreateRoute(); var sim = new ObservationSimulator(route, 7);
            for (int day = 1; day <= 10; day++)
            { sim.BeginDay(day); sim.CompleteObservation(); sim.CompleteCatReport(); sim.ApplyNNNAction(Plan[day - 1]); sim.EndDay(); }
            sim.BeginDay(11);
            var trip = route.Events.Single(x => x.Id == H.ShortTripObservation);
            foreach (string flag in new[] { H.TripPrepared, H.Carrier, H.Harness })
            {
                sim.State.WorldFlags.Remove(flag);
                Check(!ObservationConditionEvaluator.Evaluate(trip, route, sim.State), "Missing world prerequisite: " + flag);
                sim.State.WorldFlags.Add(flag);
            }
            sim.State.Relationship.Cohabitation = CohabitationState.Visiting;
            Check(!ObservationConditionEvaluator.Evaluate(trip, route, sim.State), "Visiting is not sufficient for trip.");
            sim.State.Relationship.Cohabitation = CohabitationState.LivingTogether;
            sim.State.PlayerKnowledgeFlags.Remove(H.BusyRoad);
            Check(!ObservationConditionEvaluator.Evaluate(trip, route, sim.State), "Road knowledge is independent of ownership.");
            sim.State.PlayerKnowledgeFlags.Add(H.BusyRoad);
            sim.CompleteObservation();
            Check(!ObservationConditionEvaluator.Evaluate(trip, route, sim.State), "Trip is one shot.");
            Check(sim.ObservedKnowledgeTags.Count == 2, "Observation grants facts before ACTION.");
        }

        private static void VerifyWorldGate()
        {
            var sim = new ObservationSimulator(H.CreateRoute(), 42);
            for (int day = 1; day <= 9; day++)
            {
                sim.BeginDay(day); sim.CompleteObservation(); sim.CompleteCatReport();
                if (day == 9) sim.State.WorldFlags.Remove(H.Carrier); // fixture: knowledge is identical, ownership differs
                var result = sim.ApplyNNNAction(Plan[day - 1]);
                if (day == 9) Check(result.NewlyDiscoveredOperationIds.Contains(H.ShortTrip) && !result.NewlyUnlockedOperationIds.Contains(H.ShortTrip), "Unlock receipt must include world requirements.");
                sim.EndDay();
            }
            sim.BeginDay(10); sim.CompleteObservation(); sim.CompleteCatReport();
            Check(Option(sim, H.ShortTrip).Visibility == NNNActionVisibility.VisibleLocked && Option(sim, H.ShortTrip).UnavailableReason.Contains("キャリー"), "World state gate stays locked.");
            Reject(() => sim.ApplyNNNAction(H.ShortTrip), "World condition enforced on execution.");
            sim.State.WorldFlags.Add(H.Carrier);
            Check(Option(sim, H.ShortTrip).IsAvailable && !sim.State.PlayerKnowledgeFlags.Contains(H.Carrier), "Ownership distinct from knowledge.");
        }
        private static void VerifyFairness()
        {
            var route = ScriptableObject.CreateInstance<ObservationRouteDefinition>(); route.Actions.Clear();
            for (int i = 0; i < 4; i++) route.Actions.Add(new NNNActionDefinition("OP_" + i, "work", "work", NNNActionKind.Operation, effect: new OperationEffect(new[] { "FLAG_" + i })));
            for (int i = 0; i < 3; i++) route.Actions.Add(new NNNActionDefinition("INV_" + i, "research", "research", NNNActionKind.Investigation, addedKnowledgeTags: new[] { "KNOW_" + i }));
            route.Actions.Add(NNNActionCatalog.Find(NNNActionCatalog.Skip));
            var sim = new ObservationSimulator(route, 1); var shown = new HashSet<string>();
            for (int day = 1; day <= 3; day++)
            {
                sim.BeginDay(day); sim.CompleteObservation(); sim.CompleteCatReport();
                shown.UnionWith(sim.GetNNNActionOptions().Select(x => x.Definition.Id)); sim.EndDay();
            }
            Check(shown.Count == 8, "Unchosen operations must not hide investigations forever.");
            UnityEngine.Object.DestroyImmediate(route);
        }
        private static void VerifyDelayedImprovement()
        {
            var sim = new ObservationSimulator(H.CreateRoute(), 7);
            var choices = new[] { NNNActionCatalog.Skip, NNNActionCatalog.Skip, NNNActionCatalog.Skip,
                H.InvestigateIndoor, NNNActionCatalog.Skip, H.InstallTower, H.InvestigatePast, H.PrepareGear, H.InvestigateHarness };
            for (int day = 1; day <= 9; day++)
            {
                sim.BeginDay(day); sim.CompleteObservation(); sim.CompleteCatReport();
                Check(sim.GetNNNActionOptions().Any(x => x.IsAvailable && x.Definition.Id == choices[day - 1]), "Delayed improvement must not deadlock gear discovery D" + day);
                sim.ApplyNNNAction(choices[day - 1]); sim.EndDay();
            }
            Check(sim.State.OccurredEventIds.Contains(H.FirstHarness), "Late tower still allows harness within the slice.");
        }
        private static NNNActionOption Option(ObservationSimulator sim, string id) => sim.GetNNNActionOptions(true).Single(x => x.Definition.Id == id);
        private static void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); }
        private static void Reject(Action action, string text)
        { bool rejected = false; try { action(); } catch (InvalidOperationException) { rejected = true; } Check(rejected, text); }
    }
}
#endif
