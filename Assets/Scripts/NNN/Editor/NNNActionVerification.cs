#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>調査から工作への導線、日次枠、効果期間と世界状態の独立性を検証する。</summary>
    public static class NNNActionVerification
    {
        public static void Verify()
        {
            Check(NNNActionCatalog.All.Count(x => x.Kind == NNNActionKind.Investigation) == 3
                && NNNActionCatalog.All.Count(x => x.Kind == NNNActionKind.Operation) == 3, "Expected three investigation/operation pairs.");
            foreach (var research in NNNActionCatalog.All.Where(x => x.Kind == NNNActionKind.Investigation))
                VerifyPair(research);
            VerifyObservationalAction(NNNActionCatalog.Skip);
            foreach (var research in NNNActionCatalog.All.Where(x => x.Kind == NNNActionKind.Investigation))
                VerifyObservationalAction(research.Id);
            Debug.Log("NNN ACTION catalog, unlock, daily budget, atomic rejection and observation invariance: PASS");
        }

        private static void VerifyPair(NNNActionDefinition research)
        {
            var operation = NNNActionCatalog.All.Single(x => x.Kind == NNNActionKind.Operation && x.Knowledge == research.Knowledge);
            var route = SatoSuzuVisitObservationFactory.CreateRoute();
            var target = route.Events.Single(x => x.Id == operation.TargetEventId);
            var simulator = new ObservationSimulator(route, 7);
            simulator.BeginDay(1);
            string initial = simulator.State.Relationship.ToString();
            Reject(() => simulator.ApplyNNNAction(operation.Id, 0), "Locked operation must reject.");
            Reject(() => simulator.ApplyNNNAction("INVALID", 0), "Unknown ID must reject.");
            Reject(() => simulator.ApplyNNNAction(research.Id, float.NaN), "NaN time must reject.");
            Check(simulator.State.PlayerActionHistory.Count == 0 && simulator.DayContext.CurrentTime == 0,
                "Rejected action must not consume a day or alter time.");
            simulator.ApplyNNNAction(research.Id, 0);
            Check(simulator.State.PlayerKnowledgeFlags.Contains(research.Knowledge), "Research must grant its knowledge.");
            Reject(() => simulator.ApplyNNNAction(NNNActionCatalog.Skip, 0), "Second daily action must reject.");
            Check(simulator.GetNNNActionOptions().All(x => !x.IsAvailable), "All choices must lock after use.");
            Check(initial == simulator.State.Relationship.ToString(), "Research must not change living state.");
            Finish(simulator);

            int actionDay = target.EarliestDay - 1;
            for (int day = 2; day < actionDay; day++) simulator.SimulateDay(day);
            simulator.BeginDay(actionDay);
            Check(!simulator.GetNNNActionOptions().Single(x => x.Definition.Id == research.Id).IsAvailable, "Known research must be marked completed.");
            Check(simulator.GetNNNActionOptions().Single(x => x.Definition.Id == operation.Id).IsAvailable, "Knowledge must unlock the corresponding operation in its usable period.");
            string before = simulator.State.Relationship.ToString();
            int history = simulator.State.HistoryFlags.Count, memory = simulator.State.MemoryFlags.Count;
            simulator.GenerateNextEvent();
            int pending = simulator.PendingEventCount;
            simulator.ApplyNNNAction(operation.Id, 0);
            Check(pending == simulator.PendingEventCount && before == simulator.State.Relationship.ToString()
                && history == simulator.State.HistoryFlags.Count && memory == simulator.State.MemoryFlags.Count,
                "Operation must preserve today's events and living state.");
            var modifier = simulator.State.Modifiers.Single();
            Check(!modifier.IsActive(actionDay, target.Id) && modifier.IsActive(actionDay + 1, target.Id)
                && modifier.IsActive(actionDay + 3, target.Id) && !modifier.IsActive(actionDay + 4, target.Id)
                && !modifier.IsActive(actionDay + 1, "UNRELATED"), "Effect must target exactly three future days.");
            Finish(simulator);
            simulator.BeginDay(actionDay + 1);
            Reject(() => simulator.ApplyNNNAction(operation.Id, 0), "Active operation must not stack.");
            simulator.ApplyNNNAction(NNNActionCatalog.Skip, 0);
            Reject(() => simulator.ApplyNNNAction(NNNActionCatalog.InvestigateHome, 0), "Skip must consume the daily budget.");
            Finish(simulator);
            for (int day = actionDay + 2; day <= 30; day++) simulator.SimulateDay(day);
            Check(simulator.State.HistoryFlags.Contains(RelationshipHistoryFlag.Greeted), "Intervention route must complete.");
            Check(simulator.State.PlayerActionHistory.Count == 3, "Action history must contain research, operation and skip.");

            // 優先度への接続を乱数列の等しい対照と比較。元イベント条件の未成立を工作で突破しない。
            var state = new ObservationSimulator(route, 1);
            for (int day = 1; day < actionDay + 1; day++) state.SimulateDay(day);
            state.BeginDay(actionDay + 1);
            bool eligibility = ObservationConditionEvaluator.Evaluate(target, route, state.State);
            state.State.Modifiers.Add(modifier);
            Check(eligibility == ObservationConditionEvaluator.Evaluate(target, route, state.State), "Priority must not bypass event prerequisites.");
            var rival = ScriptableObject.CreateInstance<ObservationEventDefinition>();
            var boosted = ScriptableObject.CreateInstance<ObservationEventDefinition>();
            try
            {
                // PhaseBonusも同条件にそろえ、工作の加点だけを比較する。
                rival.Id = target.Id + "_OTHER"; boosted.Id = target.Id;
                rival.Category = boosted.Category = ObservationEventCategory.Relationship;
                rival.EarliestDay = boosted.EarliestDay = 1; rival.LatestDay = boosted.LatestDay = 30;
                rival.BasePriority = boosted.BasePriority = 50;
                int naturalCount = 0, boostedCount = 0;
                var fresh = new ObservationSimulator(route, 1).State;
                for (int seed = 0; seed < 100; seed++)
                {
                    var candidates = new[] { rival, boosted };
                    if (new ObservationDirector(seed).SelectMajorEvent(actionDay + 1, candidates, fresh)?.Id == target.Id) naturalCount++;
                    if (new ObservationDirector(seed).SelectMajorEvent(actionDay + 1, candidates, fresh, new[] { modifier })?.Id == target.Id) boostedCount++;
                }
                Check(boostedCount > naturalCount, "Operation must influence selection priority.");
            }
            finally { UnityEngine.Object.DestroyImmediate(rival); UnityEngine.Object.DestroyImmediate(boosted); }
        }

        private static void VerifyObservationalAction(string actionId)
        {
            var route = SatoSuzuVisitObservationFactory.CreateRoute();
            var control = new ObservationSimulator(route, 42);
            var subject = new ObservationSimulator(route, 42);
            for (int day = 1; day <= 30; day++)
            {
                var expected = control.SimulateDay(day);
                subject.BeginDay(day);
                if (day == 1)
                {
                    subject.GenerateNextEvent();
                    subject.ApplyNNNAction(actionId, 0);
                }
                while (subject.GenerateNextEvent() != null) subject.ExecuteNextEvent();
                var actual = subject.EndDay();
                Check(expected.MajorEventId == actual.MajorEventId
                    && expected.NormalActionIds.SequenceEqual(actual.NormalActionIds)
                    && expected.StateAfter.ToString() == actual.StateAfter.ToString(), "Research/skip must not reroll future events.");
            }
        }

        private static void Finish(ObservationSimulator simulator)
        { while (simulator.GenerateNextEvent() != null) simulator.ExecuteNextEvent(); simulator.EndDay(); }
        private static void Reject(Action action, string message)
        { bool rejected = false; try { action(); } catch (ArgumentException) { rejected = true; } catch (InvalidOperationException) { rejected = true; } Check(rejected, message); }
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
