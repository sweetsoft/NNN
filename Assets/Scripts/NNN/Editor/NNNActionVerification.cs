#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;

namespace NNN.Editor
{
    public static class NNNActionVerification
    {
        public static void Verify()
        {
            VerifyLoop();
            VerifyEffects();
            foreach (var action in NNNActionCatalog.All.Where(x => x.Kind != NNNActionKind.Operation)) VerifyObservationInvariant(action.Id);
            Debug.Log("NNN ACTION phases, visibility, multi-knowledge, world effects, CAT REPORT and invariance: PASS");
        }

        private static void VerifyLoop()
        {
            var sim = new ObservationSimulator(SatoSuzuVisitObservationFactory.CreateRoute(), 7);
            sim.BeginDay(1);
            Reject(() => sim.ApplyNNNAction(NNNActionCatalog.InvestigateCat), "Action during observation must reject.");
            Reject(() => sim.CompleteCatReport(), "Report cannot be dismissed before observation.");
            Check(Option(sim, NNNActionCatalog.ArrangePlay).Visibility == NNNActionVisibility.Hidden, "Unknown operation must be hidden.");
            var report = sim.CompleteObservation();
            Check(ReferenceEquals(report, sim.CompleteObservation()) && sim.DayContext.PlayerActionRecords.Count == 0,
                "One report per day, independent of ACTION.");
            Reject(() => sim.ApplyNNNAction(NNNActionCatalog.Skip), "Report precedes action.");
            sim.CompleteCatReport();
            Reject(() => sim.ApplyNNNAction("INVALID"), "Unknown ID must reject.");
            Reject(() => sim.ApplyNNNAction(NNNActionCatalog.Skip, float.NaN), "NaN must reject.");
            Reject(() => sim.ApplyNNNAction(NNNActionCatalog.ArrangePlay), "Hidden operation must reject.");
            Check(sim.GetNNNActionOptions().Count <= 4 && sim.State.PlayerActionHistory.Count == 0, "Bounded options and atomic rejection.");
            var research = sim.ApplyNNNAction(NNNActionCatalog.InvestigateHuman);
            Check(research.AddedKnowledgeTags.SequenceEqual(new[] { KnowledgeTag.HumanPlayOpportunity })
                && research.NewlyDiscoveredOperationIds.Contains(NNNActionCatalog.ArrangePlay)
                && !research.NewlyUnlockedOperationIds.Contains(NNNActionCatalog.ArrangePlay), "Discovery differs from full unlock.");
            Check(sim.State.WorldFlags.Count == 0, "Knowledge must not alter the world.");
            Reject(() => sim.ApplyNNNAction(NNNActionCatalog.Skip), "Second action must reject.");
            Check(sim.GenerateNextEvent() == null && sim.ExecuteNextEvent() == null, "No observation after action.");
            var day = sim.EndDay();
            Check(ReferenceEquals(day.CatReport, report) && ReferenceEquals(day.Investigation, research), "Day preserves UI results.");
            sim.BeginDay(2); Prepare(sim);
            var locked = Option(sim, NNNActionCatalog.ArrangePlay);
            Check(locked.Visibility == NNNActionVisibility.VisibleLocked && locked.UnavailableReason.Contains(KnowledgeTag.CatBoundarySignal), "Partial knowledge must show missing tags.");
            Reject(() => sim.ApplyNNNAction(NNNActionCatalog.ArrangePlay), "All knowledge is required.");
            research = sim.ApplyNNNAction(NNNActionCatalog.InvestigateCat);
            Check(research.NewlyUnlockedOperationIds.Contains(NNNActionCatalog.ArrangePlay), "Multiple AND knowledge unlocks operation.");
            sim.EndDay();
            sim.BeginDay(3); Prepare(sim);
            Check(Option(sim, NNNActionCatalog.ArrangePlay).IsAvailable, "Discovered complete operation must be available.");
            string state = sim.State.Relationship.ToString();
            int events = sim.State.OccurredEventIds.Count;
            sim.ApplyNNNAction(NNNActionCatalog.HintSignal);
            Check(sim.State.WorldFlags.Count == 0 && sim.State.Relationship.ToString() == state
                && sim.State.OccurredEventIds.Count == events && sim.PendingEventCount == 0, "Operation cannot immediately change state or emit events.");
            sim.EndDay();
            sim.BeginDay(4);
            Check(sim.State.WorldFlags.Contains(WorldFlag.HumanKnowsCatBoundarySignal) && sim.State.PendingOperations.Count == 0,
                "Operation changes world next morning.");
            Prepare(sim);
            Reject(() => sim.ApplyNNNAction(NNNActionCatalog.HintSignal), "Satisfied operation must reject.");
            sim.ApplyNNNAction(NNNActionCatalog.Skip);
            Reject(() => sim.ApplyNNNAction(NNNActionCatalog.InvestigateHome), "Skip consumes budget.");
            sim.EndDay();
        }

        private static void VerifyEffects()
        {
            var route = SatoSuzuVisitObservationFactory.CreateRoute();
            var sim = new ObservationSimulator(route, 42);
            sim.BeginDay(1); Prepare(sim); sim.ApplyNNNAction(NNNActionCatalog.InvestigateCat); sim.EndDay();
            sim.BeginDay(2); Prepare(sim);
            var candidates = route.Events.Where(x => x.Id == "NORMAL_SIGNAL_NOTICED" || x.Id == "NORMAL_SIGNAL_MISSED").ToList();
            Check(candidates.Count == 2 && candidates.All(x => !ObservationConditionEvaluator.Evaluate(x, route, sim.State)), "Two responses initially unavailable.");
            sim.ApplyNNNAction(NNNActionCatalog.HintSignal);
            Check(candidates.All(x => !ObservationConditionEvaluator.Evaluate(x, route, sim.State)), "No same-day candidate change.");
            sim.EndDay(); sim.BeginDay(3);
            Check(candidates.All(x => ObservationConditionEvaluator.Evaluate(x, route, sim.State)), "World flag enables multiple responses next day.");
            Check(!sim.State.PlayerKnowledgeFlags.Contains(WorldFlag.HumanKnowsCatBoundarySignal), "World flags are not player knowledge.");
            sim.EndDay();

            route.Actions.Add(new NNNActionDefinition("TEST_EFFECT", "test", "test", NNNActionKind.Operation,
                effect: new OperationEffect(new[] { "NEW_WORLD" }, new[] { WorldFlag.HumanKnowsCatBoundarySignal }, new[] { "NEW_KNOWLEDGE" },
                    HomePreparation.None, HomePreparation.ToiletReady)));
            sim.BeginDay(4); Prepare(sim); sim.ApplyNNNAction("TEST_EFFECT"); sim.EndDay(); sim.BeginDay(5);
            Check(sim.State.WorldFlags.Contains("NEW_WORLD") && !sim.State.WorldFlags.Contains(WorldFlag.HumanKnowsCatBoundarySignal)
                && sim.State.PlayerKnowledgeFlags.Contains("NEW_KNOWLEDGE") && !sim.State.Relationship.HasPreparation(HomePreparation.ToiletReady)
                && sim.State.Relationship.Cohabitation == CohabitationState.LivingTogether, "Effect additions/removals preserve cohabitation.");
            sim.EndDay();
            Reject(() => new OperationEffect(new[] { "X" }, new[] { "X" }), "Conflicting effect must reject.");
        }

        private static void VerifyObservationInvariant(string actionId)
        {
            var route = SatoSuzuVisitObservationFactory.CreateRoute();
            var control = new ObservationSimulator(route, 42);
            var subject = new ObservationSimulator(route, 42);
            for (int d = 1; d <= 30; d++)
            {
                var expected = control.SimulateDay(d);
                subject.BeginDay(d); Prepare(subject);
                subject.ApplyNNNAction(d == 1 ? actionId : NNNActionCatalog.Skip);
                var actual = subject.EndDay();
                Check(expected.MajorEventId == actual.MajorEventId && expected.NormalActionIds.SequenceEqual(actual.NormalActionIds)
                    && expected.StateAfter.ToString() == actual.StateAfter.ToString() && actual.CatReport != null,
                    "Research/skip must preserve observation and automatic daily report.");
            }
        }
        private static NNNActionOption Option(ObservationSimulator sim, string id)
            => sim.GetNNNActionOptions(true).Single(x => x.Definition.Id == id);
        private static void Prepare(ObservationSimulator sim) { sim.CompleteObservation(); sim.CompleteCatReport(); }
        private static void Reject(Action action, string message)
        { bool rejected = false; try { action(); } catch (ArgumentException) { rejected = true; } catch (InvalidOperationException) { rejected = true; } Check(rejected, message); }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
