#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using H = NNN.SatoHachiObservationFactory;

namespace NNN.Editor
{
    public static class SatoHachiInsightVerification
    {
        private static readonly string[] Plan = { "SKIP", "SKIP", "SKIP", H.InvestigateIndoor, H.InstallTower,
            H.InvestigatePast, H.PrepareGear, H.InvestigateHarness, H.InvestigateRoute, H.ShortTrip, "SKIP" };
        [MenuItem("NNN/Playable/Verify Observation Insights")]
        public static void Verify()
        {
            var report = new StringBuilder("# Observation Insight 表示検証\n\nSeed 42 Guided。Review時点（当日のAction実行前）の表示。\n\n| DAY | UPDATE | CHANGE | QUESTION |\n|---|---|---|---|\n");
            int cases = 0;
            foreach (int seed in Enumerable.Range(0, 32).Concat(new[] { 42 }))
            for (int policy = 0; policy < 3; policy++)
            {
                var route = H.CreateRoute(); var sim = new ObservationSimulator(route, seed);
                var baseline = new ObservationSimulator(H.CreateRoute(), seed);
                var presenter = new ObservationInsightPresenter(SatoHachiInsightFactory.Create());
                var random = new System.Random(seed);
                Check(route.Actions.Concat(NNNActionCatalog.All).Where(x => x.Kind != NNNActionKind.Skip).All(x => !string.IsNullOrWhiteSpace(x.IntentText)), "Intent coverage");
                for (int day = 1; day <= 11; day++)
                {
                    sim.BeginDay(day); sim.CompleteObservation(); baseline.BeginDay(day); baseline.CompleteObservation();
                    string before = Signature(sim);
                    var review = presenter.Build(sim, sim.GetObservationScenes());
                    Check(Signature(sim) == before, "Review mutated simulation");
                    Check(review.Updates.Count >= 1 && review.Updates.Count <= 3 && review.Changes.Count <= 2 && !string.IsNullOrWhiteSpace(review.CurrentQuestion), "Review bounds");
                    foreach (var entry in review.Updates.Concat(review.Changes))
                    {
                        Check(entry.Condition.TodayEvents.All(sim.DayContext.ExecutedEventIds.Contains), "Unseen event: " + entry.Id);
                        Check(entry.Condition.Knowledge.All(sim.State.PlayerKnowledgeFlags.Contains), "Unknown knowledge: " + entry.Id);
                        Check(entry.Condition.WorldFlags.All(sim.State.WorldFlags.Contains), "Absent world flag: " + entry.Id);
                    }
                    string allText = string.Join(" ", review.Updates.Concat(review.Changes).Select(x => x.Text)) + review.CurrentQuestion;
                    if (!sim.State.PlayerKnowledgeFlags.Contains(H.BusyRoad))
                        Check(!allText.Contains("大通り") && !allText.Contains("猫だけ"), "Route spoiler");
                    if (day == 4) Check(!allText.Contains("商店街") && !allText.Contains("愛着") && !allText.Contains("細道"), "DAY4 spoiler");
                    if (!sim.State.WorldFlags.Contains(H.Tower))
                        Check(!review.Changes.Any(x => x.Text.Contains("高い場所") || x.Text.Contains("探索場所")), "Premature tower change");
                    if (policy == 0 && day == 6) Check(review.Changes.Any(x => x.Condition.Comparison == InsightComparison.AfterOperation), "Tower operation comparison missing");
                    if (policy == 0 && day == 9) Check(!allText.Contains("大通り"), "Same-day research leaked into review");
                    if (policy == 0 && day == 10) Check(allText.Contains("大通り"), "Known route omitted");
                    if (seed == 42 && policy == 0)
                        report.AppendLine("| " + day + " | " + string.Join("<br>", review.Updates.Select(x => x.Text)) + " | "
                            + string.Join("<br>", review.Changes.Select(x => x.Text)) + " | " + review.CurrentQuestion + " |");
                    sim.CompleteCatReport(); baseline.CompleteCatReport();
                    var options = sim.GetNNNActionOptions();
                    Check(Options(sim) == Options(baseline), "Action options changed");
                    string action = policy == 0 ? Plan[day - 1] : policy == 1 ? "SKIP"
                        : options.Where(x => x.IsAvailable).OrderBy(x => random.Next()).First().Definition.Id;
                    if (route.Actions.Single(x => x.Id == action).Kind == NNNActionKind.Operation) presenter.RecordOperation(action, sim);
                    sim.ApplyNNNAction(action); baseline.ApplyNNNAction(action);
                    Check(Signature(sim) == Signature(baseline), "Simulation invariance");
                    var actual = sim.EndDay(); var expected = baseline.EndDay();
                    Check(actual.MajorEventId == expected.MajorEventId && actual.NormalActionIds.SequenceEqual(expected.NormalActionIds)
                        && actual.CatReport.Text == expected.CatReport.Text, "Event/report invariance");
                    cases++;
                }
            }
            VerifyLockedAndFallback();
            report.AppendLine("\nPASS: " + cases + " days / Guided, SKIP, Alternative / No Spoiler / bounds / evidence / comparisons / intents / simulation invariance.");
            Directory.CreateDirectory("outputs"); File.WriteAllText("outputs/sato-hachi-insight-displays.md", report.ToString());
            Debug.Log("OBSERVATION INSIGHTS: PASS / " + cases + " days / 33 seeds × 3 policies / locked intents / fallback / no spoiler / simulation invariance");
        }
        private static void VerifyLockedAndFallback()
        {
            var sim = new ObservationSimulator(H.CreateRoute(), 42); sim.BeginDay(1); sim.CompleteObservation();
            var review = new ObservationInsightPresenter(new System.Collections.Generic.List<ObservationInsightDefinition>()).Build(sim, sim.GetObservationScenes());
            Check(review.SelectedInsightId == "LOG_FALLBACK" && review.Updates.Count > 0, "Unknown event fallback");
            sim.State.PlayerKnowledgeFlags.UnionWith(new[] { H.Walkable, H.BusyRoad, H.CatOnlyPaths, H.WantsOutside, H.HarnessUncertain });
            sim.CompleteCatReport();
            var locked = sim.GetNNNActionOptions(true).Single(x => x.Definition.Id == H.ShortTrip);
            Check(locked.Visibility == NNNActionVisibility.VisibleLocked && !string.IsNullOrWhiteSpace(locked.Definition.IntentText), "Locked intent");
            Check(!locked.IsAvailable && !string.IsNullOrWhiteSpace(locked.UnavailableReason), "Locked requirements");
        }
        private static string Options(ObservationSimulator s) => string.Join(";", s.GetNNNActionOptions(true).Select(x => x.Definition.Id + ":" + x.Visibility + ":" + x.UnavailableReason));
        private static string Signature(ObservationSimulator s) => s.State.Relationship + "|" + s.DayContext.Phase + "|"
            + string.Join(",", s.State.PlayerKnowledgeFlags.OrderBy(x => x)) + "|" + string.Join(",", s.State.WorldFlags.OrderBy(x => x))
            + "|" + string.Join(",", s.DayContext.ExecutedEventIds.OrderBy(x => x)) + "|" + Options(s)
            + "|" + string.Join(",", s.State.PendingOperations.Select(x => x.OperationId + ":" + x.ActiveFromDay));
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        public static void RunBatch()
        {
            try { Verify(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
