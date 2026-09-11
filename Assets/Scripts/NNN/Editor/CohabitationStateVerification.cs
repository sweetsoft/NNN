#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>同居開始の条件と、生活・対人状態の独立性を検証する。</summary>
    public static class CohabitationStateVerification
    {
        public static void Verify()
        {
            foreach (CohabitationState from in Enum.GetValues(typeof(CohabitationState)))
                foreach (CohabitationState to in Enum.GetValues(typeof(CohabitationState)))
                {
                    var transitionState = new RelationshipState { Cohabitation = from,
                        HumanAcceptance = HumanAcceptanceState.Welcoming, HomeReadiness = HomePreparation.All };
                    string original = transitionState.ToString();
                    bool failed = false;
                    try { new RelationshipStateChange { SetCohabitation = true, Cohabitation = to,
                        SetHumanToCat = true, HumanToCat = HumanToCatState.Care }.Apply(transitionState); }
                    catch (InvalidOperationException) { failed = true; }
                    bool allowed = from == to || (from == CohabitationState.Outside && to == CohabitationState.Visiting)
                        || (from == CohabitationState.Visiting && to == CohabitationState.LivingTogether);
                    Check(failed != allowed && (!failed || transitionState.ToString() == original), "Transition matrix must reject atomically: " + from + " -> " + to);
                }
            var state = new RelationshipState { Cohabitation = CohabitationState.Visiting };
            var start = new RelationshipStateChange { SetCohabitation = true, Cohabitation = CohabitationState.LivingTogether };
            Check(!state.CanStartCohabitation, "Unprepared visit must not start cohabitation.");
            var before = state.Clone();
            bool rejected = false;
            try { start.Apply(state); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected && state.ToString() == before.ToString(), "Invalid transition must be atomic.");

            new RelationshipStateChange { SetHumanAcceptance = true, HumanAcceptance = HumanAcceptanceState.Welcoming }.Apply(state);
            Check(!state.CanStartCohabitation, "Acceptance alone is insufficient.");
            new RelationshipStateChange { AddHomePreparation = HomePreparation.All & ~HomePreparation.ToiletReady }.Apply(state);
            Check(state.ReadinessStage == HomeReadinessStage.Preparing && !state.CanStartCohabitation, "All four preparation items are required.");
            new RelationshipStateChange { AddHomePreparation = HomePreparation.ToiletReady }.Apply(state);
            Check(state.ReadinessStage == HomeReadinessStage.BasicReady && state.CanStartCohabitation, "Ready visit should be eligible.");
            Check(state.Cohabitation == CohabitationState.Visiting, "Eligibility must not auto-start cohabitation.");
            var ready = state.Clone();
            start.Apply(state);
            Check(state.HumanAcceptance == HumanAcceptanceState.Welcoming, "Cohabitation does not imply commitment.");
            Check(state.CatWariness == CatWarinessState.High && state.CatAdaptation == CatAdaptationState.Unfamiliar,
                "Cohabitation must allow a wary, unfamiliar cat.");
            new RelationshipStateChange { SetCatAdaptation = true, CatAdaptation = CatAdaptationState.AtEase }.Apply(state);
            Check(state.CatWariness == CatWarinessState.High, "Home adaptation must not reduce human wariness.");
            new RelationshipStateChange { RemoveHomePreparation = HomePreparation.BasicSafetyReady,
                SetHumanAcceptance = true, HumanAcceptance = HumanAcceptanceState.Tolerating }.Apply(state);
            Check(state.Cohabitation == CohabitationState.LivingTogether, "Lost preparation or acceptance must not auto-end cohabitation.");
            Check(ready.ReadinessStage == HomeReadinessStage.BasicReady && ready.Cohabitation == CohabitationState.Visiting,
                "Snapshots must remain independent.");

            var definition = ScriptableObject.CreateInstance<ObservationEventDefinition>();
            var route = ScriptableObject.CreateInstance<ObservationRouteDefinition>();
            try
            {
                definition.Id = "TEST_START";
                definition.StateChange = start;
                var simulator = new ObservationSimulator(route, 7);
                simulator.BeginDay(1);
                var simulation = simulator.State;
                Check(!ObservationConditionEvaluator.Evaluate(definition, route, simulation), "Invalid start must not become a candidate.");
                simulation.Relationship.Cohabitation = CohabitationState.Visiting;
                simulation.Relationship.HumanAcceptance = HumanAcceptanceState.Welcoming;
                simulation.Relationship.HomeReadiness = HomePreparation.All;
                Check(ObservationConditionEvaluator.Evaluate(definition, route, simulation), "Ready start must be a candidate.");
                definition.StateChange = new RelationshipStateChange();
                var conditions = new[]
                {
                    new ObservationEventCondition { Type = ObservationConditionType.CohabitationAtLeast, Cohabitation = CohabitationState.Visiting },
                    new ObservationEventCondition { Type = ObservationConditionType.CohabitationAtMost, Cohabitation = CohabitationState.Visiting },
                    new ObservationEventCondition { Type = ObservationConditionType.AcceptanceAtLeast, Acceptance = HumanAcceptanceState.Welcoming },
                    new ObservationEventCondition { Type = ObservationConditionType.AcceptanceAtMost, Acceptance = HumanAcceptanceState.Welcoming },
                    new ObservationEventCondition { Type = ObservationConditionType.AdaptationAtLeast, Adaptation = CatAdaptationState.Unfamiliar },
                    new ObservationEventCondition { Type = ObservationConditionType.AdaptationAtMost, Adaptation = CatAdaptationState.Unfamiliar },
                    new ObservationEventCondition { Type = ObservationConditionType.HasHomePreparation, Preparation = HomePreparation.All }
                };
                definition.Conditions.AddRange(conditions);
                Check(ObservationConditionEvaluator.Evaluate(definition, route, simulation), "Independent condition boundaries must match.");
                definition.Conditions.Clear();
                definition.Conditions.Add(new ObservationEventCondition { Type = ObservationConditionType.MissingHomePreparation, Preparation = HomePreparation.ToiletReady });
                Check(!ObservationConditionEvaluator.Evaluate(definition, route, simulation), "Ready toilet must not be missing.");
                simulation.Relationship.HomeReadiness &= ~HomePreparation.ToiletReady;
                Check(ObservationConditionEvaluator.Evaluate(definition, route, simulation), "Missing toilet must be detected.");
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); UnityEngine.Object.DestroyImmediate(route); }
            Debug.Log("NNN independent cohabitation state checks: PASS");
        }

        public static void VerifyRoute(IList<DaySimulationResult> days)
        {
            Check(days[0].StateAfter.Cohabitation == CohabitationState.Outside, "DAY1 contact is not an indoor visit.");
            Check(days[1].MajorEventId == "REL_ENTER_HOME" && days[1].StateAfter.Cohabitation == CohabitationState.Visiting,
                "DAY2 must establish a visit.");
            Check(days[2].MajorEventId == "REL_START_COHABITATION" && days[2].StateBefore.CanStartCohabitation
                && days[2].StateAfter.Cohabitation == CohabitationState.LivingTogether, "DAY3 must start a prepared cohabitation.");
            Check(days[2].StateAfter.CatAdaptation == CatAdaptationState.Unfamiliar
                && days[2].StateAfter.CatWariness == CatWarinessState.High, "Demo must leave adaptation and trust for later.");
            for (int i = 2; i < days.Count; i++)
                Check(days[i].StateAfter.Cohabitation == CohabitationState.LivingTogether, "Later setbacks must preserve cohabitation.");
            Check(days[29].StateAfter.CatAdaptation == CatAdaptationState.AtEase
                && days[29].StateAfter.HumanAcceptance == HumanAcceptanceState.Committed, "Post-cohabitation life must progress.");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
