#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using K = NNN.SatoKotaObservationFactory;

namespace NNN.Editor
{
    public static class SatoKotaVerification
    {
        private static readonly string[] Expected = { K.Contact, K.Entry, K.Living, K.DeskTrouble, K.Night, K.PlayResponse,
            K.VerticalInterest, K.VerticalResponse, K.Proximity, K.HumanAdaptation, K.SharedSpace };
        [MenuItem("NNN/Playable/Verify Sato Kota 32 Seeds")]
        public static void Verify()
        {
            var report = new StringBuilder("# SatoKota Guided 表示一覧\n\nSeed 42。各日のAction前の観察とInsight。\n\n| DAY | Event / Scenes | CAT REPORT | UPDATE | CHANGE | QUESTION | ACTION |\n|---|---|---|---|---|---|---|\n");
            int days = 0;
            foreach (int seed in Enumerable.Range(0, 32).Concat(new[] { 42 }))
            for (int policy = 0; policy < 3; policy++)
            {
                var route = K.CreateRoute(); var sim = new ObservationSimulator(route, seed); var baseline = new ObservationSimulator(K.CreateRoute(), seed);
                var insights = new ObservationInsightPresenter(SatoKotaInsightFactory.Create()); var random = new System.Random(seed);
                string traits = string.Join(",", route.Cat.Traits.Select(x => x.Id));
                Check(route.Cat.Activity == 88 && route.Cat.Sociability == 90 && route.Cat.Independence == 50, "Profile");
                Check(route.Actions.Where(x => x.Kind != NNNActionKind.Skip).All(x => !string.IsNullOrWhiteSpace(x.IntentText)), "Intent coverage");
                Check(!route.Events.SelectMany(x => x.Conditions).Any(x => x.Type == ObservationConditionType.DayAtLeast)
                    && route.Events.All(x => x.EarliestDay == 1 && x.LatestDay == 30), "Calendar-locked route");
                for (int day = 1; day <= 11; day++)
                {
                    sim.BeginDay(day); baseline.BeginDay(day); sim.CompleteObservation(); baseline.CompleteObservation();
                    var scenes = sim.GetObservationScenes(); string before = Signature(sim);
                    var review = insights.Build(sim, scenes); Check(Signature(sim) == before, "Insight mutated state");
                    Check(review.Updates.Count >= 1 && review.Updates.Count <= 3 && review.Changes.Count <= 2, "Insight bounds");
                    foreach (var fact in review.Updates.Concat(review.Changes))
                    {
                        Check(fact.Condition.TodayEvents.All(sim.DayContext.ExecutedEventIds.Contains), "Unobserved fact");
                        Check(fact.Condition.Knowledge.All(sim.State.PlayerKnowledgeFlags.Contains), "Unknown knowledge");
                        Check(fact.Condition.WorldFlags.All(sim.State.WorldFlags.Contains), "Absent environment");
                    }
                    string shown = string.Join(" ", review.Updates.Concat(review.Changes).Select(x => x.Text)) + review.CurrentQuestion;
                    string all = shown + string.Join(" ", route.Actions.Select(x => x.Id + x.DisplayName)) + string.Join(" ", sim.State.PlayerKnowledgeFlags);
                    Check(!new[] { "HARNESS", "SHOPPING", "ShortTrip", "商店街", "ハーネス", "キャリー", "大通り", "細道" }.Any(x => all.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0), "Outdoor contamination");
                    if (day == 2) Check(sim.State.Relationship.Cohabitation == CohabitationState.Visiting, "DAY2 Visiting");
                    if (day == 3) Check(sim.State.Relationship.Cohabitation == CohabitationState.LivingTogether
                        && sim.State.Relationship.HumanAcceptance == HumanAcceptanceState.Welcoming
                        && sim.State.Relationship.CatAdaptation == CatAdaptationState.Exploring, "Independent DAY3 state");
                    if (day == 4) Check(review.CurrentQuestion == "なぜコタは作業中の机へ何度も来る？"
                        && !shown.Contains("遊び不足") && !shown.Contains("高所欲求") && !shown.Contains("そばにいたい"), "DAY4 premature diagnosis");
                    if (policy == 0 && (day == 6 || day == 8)) Check(review.Changes.Count == 2, "Partial change comparison");
                    sim.CompleteCatReport(); baseline.CompleteCatReport();
                    var options = sim.GetNNNActionOptions(); Check(Options(sim) == Options(baseline), "Action invariance");
                    string action = policy == 0 ? K.GuidedActions[day - 1] : policy == 1 ? "SKIP" : options.Where(x => x.IsAvailable).OrderBy(x => random.Next()).First().Definition.Id;
                    Check(options.Any(x => x.IsAvailable && x.Definition.Id == action), "Guided option missing DAY" + day);
                    string flags = string.Join(",", sim.State.WorldFlags.OrderBy(x => x));
                    if (route.Actions.Single(x => x.Id == action).Kind == NNNActionKind.Operation) insights.RecordOperation(action, sim);
                    sim.ApplyNNNAction(action); baseline.ApplyNNNAction(action);
                    Check(flags == string.Join(",", sim.State.WorldFlags.OrderBy(x => x)), "Same-day operation effect");
                    Check(Signature(sim) == Signature(baseline), "Presentation invariance");
                    var actual = sim.EndDay(); var expected = baseline.EndDay();
                    Check(actual.MajorEventId == expected.MajorEventId && actual.NormalActionIds.SequenceEqual(expected.NormalActionIds)
                        && actual.LogEntries.Select(x => x.Text).SequenceEqual(expected.LogEntries.Select(x => x.Text)), "Seed reproducibility");
                    if (policy == 0)
                    {
                        Check(actual.MajorEventId == Expected[day - 1], "Guided event DAY" + day + " = " + actual.MajorEventId);
                        if (day >= 6) Check(!actual.NormalCandidates.Contains(K.NightRun) && actual.NormalCandidates.Contains(K.ShortPlay), "Play must change nighttime candidates");
                        if (day >= 6 && day <= 10) Check(actual.NormalCandidates.Contains(K.DeskVisit), "Play/tower must not erase desk behavior");
                        if (day == 11) Check(actual.NormalCandidates.Contains(K.TowerJump) && actual.NormalCandidates.Contains(K.PawToy)
                            && actual.NormalCandidates.Contains(K.ShortPlay) && actual.LogEntries.Any(x => x.ActionId == "CAT_PAW")
                            && sim.State.WorldFlags.Contains(K.DeskCleared) && sim.State.MemoryFlags.Contains(RelationshipMemory.HumanAdaptedEnvironment), "Personality/human adaptation");
                    }
                    if (policy == 1) Check(sim.State.WorldFlags.Count == 0 && !actual.LogEntries.Any(x => x.EventId == K.SharedSpace), "SKIP false resolution");
                    if (seed == 42 && policy == 0) report.AppendLine($"| {day} | {actual.MajorEventId} / {scenes.Count} | {actual.CatReport.Text} | {string.Join("<br>", review.Updates.Select(x => x.Text))} | {string.Join("<br>", review.Changes.Select(x => x.Text))} | {review.CurrentQuestion} | {action} |");
                    Check(traits == string.Join(",", route.Cat.Traits.Select(x => x.Id)) && route.Cat.Activity == 88, "Personality mutated"); days++;
                }
            }
            VerifyConditions();
            Directory.CreateDirectory("outputs"); File.WriteAllText("outputs/sato-kota-insight-displays.md", report + "\nPASS: " + days + " DAY / 33 Seed × Guided, SKIP, Alternative / no spoiler / invariance / partial experiments / trait ablation.\n");
            Debug.Log("SATO KOTA: PASS / " + days + " days / 33 seeds x 3 policies / no spoiler / partial experiments / independent state / unchanged personality / invariance");
        }
        private static void VerifyConditions()
        {
            var route = K.CreateRoute(); var simulator = new ObservationSimulator(route, 42); simulator.BeginDay(1); var state = simulator.State;
            state.OccurredEventIds.UnionWith(new[] { K.Living, K.DeskTrouble, K.Night, K.VerticalInterest });
            foreach (var pair in new[] { new[] { K.DeskTrouble, K.Curiosity }, new[] { K.DeskTrouble, K.Affinity }, new[] { K.NightRun, K.Activity }, new[] { K.PawToy, K.PlayDrive }, new[] { K.VerticalInterest, K.Exploration } })
            {
                var e = route.Events.Single(x => x.Id == pair[0]); state.OccurredEventIds.Remove(e.Id);
                Check(ObservationConditionEvaluator.Evaluate(e, route, state), "Trait fixture " + e.Id);
                var trait = route.Cat.Traits.Single(x => x.Id == pair[1]); route.Cat.Traits.Remove(trait);
                Check(!ObservationConditionEvaluator.Evaluate(e, route, state), "Trait ignored " + pair[1]); route.Cat.Traits.Add(trait); state.OccurredEventIds.Add(e.Id);
            }
            state.WorldFlags.UnionWith(new[] { K.Tower, K.VerticalRoute });
            Check(ObservationConditionEvaluator.Evaluate(route.Events.Single(x => x.Id == K.VerticalResponse), route, state), "Vertical experiment should not require play operation");
            Check(ObservationConditionEvaluator.Evaluate(route.Events.Single(x => x.Id == K.Proximity), route, state), "Human proximity survives vertical environment");
        }
        private static string Options(ObservationSimulator s) => string.Join(";", s.GetNNNActionOptions(true).Select(x => x.Definition.Id + ":" + x.Visibility));
        private static string Signature(ObservationSimulator s) => s.State.Relationship + "|" + string.Join(",", s.State.WorldFlags.OrderBy(x => x))
            + "|" + string.Join(",", s.State.PlayerKnowledgeFlags.OrderBy(x => x)) + "|" + Options(s) + "|" + s.DayContext.Phase;
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        public static void RunBatch()
        {
            try { Verify(); SatoKotaPlayableSceneBuilder.CreateKotaScene(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
#endif
