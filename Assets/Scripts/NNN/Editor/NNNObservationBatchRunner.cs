#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NNN.Editor
{
    public static class NNNObservationBatchRunner
    {
        private static readonly int[] VerificationSeeds = { 7, 42, 20260904, 8675309 };

        [MenuItem("NNN/Observation/Simulate 30 Days")]
        public static void Simulate30Days()
        {
            int seed = 20260904;
            var results = new ObservationSimulator(SatoSuzuVisitObservationFactory.CreateRoute(), seed).Simulate30Days();
            Debug.Log("NNN Observation Seed " + seed + "\n" + ObservationDebugRunner.Format(results));
        }

        [MenuItem("NNN/Observation/Verify Multiple Seeds")]
        public static void VerifyMultipleSeeds()
        {
            foreach (int seed in VerificationSeeds)
            {
                var first = Run(seed);
                var repeat = Run(seed);
                Debug.Log(Summary(seed, first.Results, first.State));
                Validate(seed, first.Results, first.State, repeat.Results);
            }
            Debug.Log("NNN Observation: all seed checks passed.");
        }

        // Unity -batchmode -executeMethod NNN.Editor.NNNObservationBatchRunner.RunBatchVerification
        public static void RunBatchVerification()
        {
            try { VerifyMultipleSeeds(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static (List<DaySimulationResult> Results, ObservationSimulationState State) Run(int seed)
        {
            var simulator = new ObservationSimulator(SatoSuzuVisitObservationFactory.CreateRoute(), seed);
            return (simulator.Simulate30Days(), simulator.State);
        }

        private static void Validate(int seed, IList<DaySimulationResult> results, ObservationSimulationState state, IList<DaySimulationResult> repeated)
        {
            string signature = Signature(results);
            if (signature != Signature(repeated)) throw new InvalidOperationException("Seed " + seed + ": reproducibility failed.");
            if (results.Count != 30) throw new InvalidOperationException("Seed " + seed + ": not 30 days.");
            if (results.Any(x => x.NormalActionIds.Count < 1 || x.NormalActionIds.Count > 3)) throw new InvalidOperationException("Seed " + seed + ": invalid normal action count.");
            if (!results.Any(x => string.IsNullOrEmpty(x.MajorEventId))) throw new InvalidOperationException("Seed " + seed + ": no normal-only day.");
            var majors = results.Where(x => !string.IsNullOrEmpty(x.MajorEventId)).ToList();
            if (majors.GroupBy(x => x.MajorEventId).Any(g => g.Count() > 1)) throw new InvalidOperationException("Seed " + seed + ": one-shot event repeated.");
            int touch = DayOf(results, "REL_FIRST_TOUCH");
            int punch = DayOf(results, "PROBLEM_OVERTOUCH_CAT_PUNCH");
            int entry = DayOf(results, "REL_ENTER_HOME");
            int beside = DayOf(results, "REL_SIT_BESIDE");
            int respect = DayOf(results, "REL_RESPECT_SIGNAL");
            if (touch < 0 || punch <= touch) throw new InvalidOperationException("Seed " + seed + ": cat punch ordering failed.");
            if (entry < 0 || beside <= entry) throw new InvalidOperationException("Seed " + seed + ": sit-beside ordering failed.");
            if (respect <= punch) throw new InvalidOperationException("Seed " + seed + ": recovery ordering failed.");
            var punchResult = results[punch - 1];
            if (punchResult.StateBefore.HumanToCat != HumanToCatState.Approach || punchResult.StateAfter.HumanToCat != HumanToCatState.Watch || punchResult.StateAfter.CatWariness != CatWarinessState.Medium)
                throw new InvalidOperationException("Seed " + seed + ": temporary regression failed.");
            if (!state.MemoryFlags.Contains(RelationshipMemory.RespectedSignal) || !state.HistoryFlags.Contains(RelationshipHistoryFlag.Greeted))
                throw new InvalidOperationException("Seed " + seed + ": route did not complete.");
        }

        private static string Summary(int seed, IList<DaySimulationResult> results, ObservationSimulationState state)
            => "Seed " + seed + " major trend: " + string.Join(" -> ", results.Where(x => !string.IsNullOrEmpty(x.MajorEventId)).Select(x => "D" + x.Day + ":" + x.MajorEventId).ToArray()) + " | Final " + state.Relationship;
        private static string Signature(IEnumerable<DaySimulationResult> results)
            => string.Join("|", results.Select(x => x.Day + ":" + x.MajorEventId + ":" + string.Join(",", x.NormalActionIds.ToArray())).ToArray());
        private static int DayOf(IEnumerable<DaySimulationResult> results, string id)
        {
            var found = results.FirstOrDefault(x => x.MajorEventId == id);
            return found != null ? found.Day : -1;
        }
    }
}
#endif
