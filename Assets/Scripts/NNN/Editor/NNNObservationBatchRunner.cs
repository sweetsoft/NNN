#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>
    /// Editorメニューとbatchmodeの両方から30日シミュレーションを実行する回帰検証入口。
    /// 企画上重要な順序制約をコード化し、優先度調整時の意図しない破綻を検出する。
    /// </summary>
    public static class NNNObservationBatchRunner
    {
        private static readonly int[] VerificationSeeds = { 7, 42, 20260904, 8675309 };

        /// <summary>基準Seed一件の全日ログをConsoleへ表示し、手動で傾向を確認する。</summary>
        [MenuItem("NNN/Observation/Simulate 30 Days")]
        public static void Simulate30Days()
        {
            int seed = 20260904;
            var results = new ObservationSimulator(SatoSuzuVisitObservationFactory.CreateRoute(), seed).Simulate30Days();
            Debug.Log("NNN Observation Seed " + seed + "\n" + ObservationDebugRunner.Format(results));
        }

        /// <summary>複数Seedについて再現性、日数、イベント順、後退と回復、最終到達状態を検証する。</summary>
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

        /// <summary>CIやコマンドラインから呼ぶ終了コード付き入口。</summary>
        // Unity -batchmode -executeMethod NNN.Editor.NNNObservationBatchRunner.RunBatchVerification
        public static void RunBatchVerification()
        {
            try { VerifyMultipleSeeds(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        /// <summary>RouteとSimulatorを毎回作り直し、別実行の乱数・履歴が混ざらない結果を返す。</summary>
        private static (List<DaySimulationResult> Results, ObservationSimulationState State) Run(int seed)
        {
            var simulator = new ObservationSimulator(SatoSuzuVisitObservationFactory.CreateRoute(), seed);
            return (simulator.Simulate30Days(), simulator.State);
        }

        /// <summary>
        /// 成功条件を例外として検査する。同じSeedの署名一致は通常行動を含む完全再現性を意味する。
        /// DayOfが返す値は1始まりなので、結果配列を参照するときだけ-1する。
        /// </summary>
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
