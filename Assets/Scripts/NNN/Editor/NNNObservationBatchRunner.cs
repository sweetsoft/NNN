#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace NNN.Editor
{
    /// <summary>
    /// Editorメニューとbatchmodeの両方から30日シミュレーションを実行する回帰検証入口。
    /// 企画上重要な順序制約をコード化し、優先度調整時の意図しない破綻を検出する。
    /// </summary>
    public static class NNNObservationBatchRunner
    {
        private static readonly int[] VerificationSeeds = { 7, 42, 12345, 20260904, 8675309 };
        private const int DefaultStressSeedCount = 1000;

        // 企画書で発生日集計を要求されているイベント。配列順をレポート順としても利用する。
        private static readonly string[] DistributionEventIds =
        {
            "REL_DRINK_IN_FRONT_OF_HUMAN", "REL_ENTER_HOME", "REL_SNIFF_HUMAN",
            "REL_FIRST_TOUCH", "PROBLEM_OVERTOUCH_CAT_PUNCH", "REL_RESPECT_SIGNAL",
            "REL_PLAY_TOGETHER", "REL_SIT_BESIDE", "REL_GREETING"
        };

        // 現行Vertical Sliceで全Seed到達を期待するイベント。未到達は警告ではなく進行不能として扱う。
        private static readonly string[] RequiredEventIds =
        {
            "VISIT_FIRST_CONTACT", "REL_ENTER_HOME", "REL_START_COHABITATION", "REL_SNIFF_HUMAN", "REL_FIRST_TOUCH",
            "PROBLEM_OVERTOUCH_CAT_PUNCH", "REL_RESPECT_SIGNAL", "REL_PLAY_TOGETHER",
            "REL_SIT_BESIDE", "REL_GREETING", "VISIT_DAY30_ROUTINE"
        };

        private static readonly string[] NormalEventIds =
        {
            "NORMAL_CAT_WATCH_HUMAN", "NORMAL_HUMAN_WATCH_CAT", "NORMAL_CAT_REST_FAR",
            "NORMAL_CAT_REST_NEAR", "NORMAL_CAT_GROOMING", "NORMAL_CAT_EXPLORE_ROOM",
            "NORMAL_CAT_LOOK_WINDOW", "NORMAL_HUMAN_SMARTPHONE", "NORMAL_HUMAN_MEAL",
            "NORMAL_SHARED_ROOM"
        };
        private static readonly string[] OptionalEventIds = { "PROBLEM_OBJECT_DROP", "REL_HUMAN_ADAPT_ENVIRONMENT" };

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
            CohabitationStateVerification.Verify();
            ValidateStableEqualTimeOrdering();
            foreach (int seed in VerificationSeeds)
            {
                var first = Run(seed);
                CohabitationStateVerification.VerifyRoute(first.Results);
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

        /// <summary>
        /// Seed 0～999を連続実行し、到達性・因果順・状態遷移・密度・通常行動分布を一括診断する。
        /// Consoleには集計と代表的な異常Seedだけを出し、全Seedの30日ログによるEditor停止を避ける。
        /// </summary>
        [MenuItem("NNN/Observation/Stress Test 1000 Seeds")]
        public static void StressTest1000Seeds()
        {
            StressTestReport report = ExecuteStressTest(DefaultStressSeedCount);
            UnityEngine.Debug.Log(report.Text);
            if (report.FailedCount > 0)
                UnityEngine.Debug.LogError("NNN Observation Stress Test failed seeds: " + report.FailedCount);
        }

        /// <summary>
        /// batchmode用入口。検証FAILが一件でもあれば終了コード1を返し、CIが見落とさないようにする。
        /// WARNINGだけの場合は診断情報を残しつつ終了コード0とする。
        /// </summary>
        // Unity -batchmode -executeMethod NNN.Editor.NNNObservationBatchRunner.RunStressTest
        public static void RunStressTest()
        {
            try
            {
                StressTestReport report = ExecuteStressTest(DefaultStressSeedCount);
                UnityEngine.Debug.Log(report.Text);
                EditorApplication.Exit(report.FailedCount == 0 ? 0 : 1);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// 将来100/1000/10000件へ拡張できるよう件数を引数化した本体。
        /// Seedごとの例外も捕捉して次Seedへ進み、低確率障害が一件で集計全体を中断しないようにする。
        /// </summary>
        private static StressTestReport ExecuteStressTest(int seedCount)
        {
            CohabitationStateVerification.Verify();
            if (seedCount < 1) throw new ArgumentOutOfRangeException(nameof(seedCount));
            ValidateStableEqualTimeOrdering();
            ValidateIncrementalApi();
            var aggregate = new StressAggregate(seedCount);
            var timer = Stopwatch.StartNew();

            for (int seed = 0; seed < seedCount; seed++)
            {
                try
                {
                    var run = Run(seed);
                    CohabitationStateVerification.VerifyRoute(run.Results);
                    ValidateStressSeed(seed, run.Results, run.State, aggregate);
                }
                catch (Exception exception)
                {
                    aggregate.AddFailure(seed, "例外: " + exception.GetType().Name + " - " + exception.Message, null, null);
                }
            }

            // 全件二重実行は避け、範囲全体へ均等に散らした20 Seedで乱数列と最終状態を照合する。
            int reproducibilityCount = Math.Min(20, seedCount);
            var reproducibilitySeeds = VerificationSeeds
                .Concat(Enumerable.Range(0, reproducibilityCount).Select(index =>
                    reproducibilityCount == 1 ? 0 : index * (seedCount - 1) / (reproducibilityCount - 1)))
                .Distinct().Take(reproducibilityCount).ToArray();
            foreach (int seed in reproducibilitySeeds)
            {
                var first = Run(seed);
                var second = Run(seed);
                if (Signature(first.Results) != Signature(second.Results) || StateSignature(first.State) != StateSignature(second.State))
                    aggregate.AddFailure(seed, "同一Seedの再実行結果が一致しない", first.Results, first.State);
            }

            timer.Stop();
            return aggregate.BuildReport(timer.Elapsed);
        }

        /// <summary>代表SeedのLegacy等価性と、猫パンチ直後に行う二種類のテストActionを検証する。</summary>
        private static void ValidateIncrementalApi()
        {
            foreach (int seed in VerificationSeeds)
            {
                var legacy = new ObservationSimulator(SatoSuzuVisitObservationFactory.CreateRoute(), seed);
                var incremental = new ObservationSimulator(SatoSuzuVisitObservationFactory.CreateRoute(), seed);
                var legacyResults = legacy.Simulate30Days();
                var incrementalResults = new List<DaySimulationResult>();
                for (int day = 1; day <= 30; day++)
                {
                    incremental.BeginDay(day);
                    while (incremental.GenerateNextEvent() != null) incremental.ExecuteNextEvent();
                    incrementalResults.Add(incremental.EndDay());
                }
                if (FullSignature(legacyResults, legacy.State) != FullSignature(incrementalResults, incremental.State))
                    throw new InvalidOperationException("Seed " + seed + ": incremental API differs from SimulateDay wrapper.");
                Debug.Log("NNN Incremental Legacy Equivalence Seed " + seed + ": PASS");
            }

            ValidateIntervention("CAT_INVESTIGATION_TEST");
            ValidateIntervention("HUMAN_OPERATION_SIGNAL_HINT_TEST");
        }

        private static void ValidateIntervention(string actionId)
        {
            const int seed = 7;
            var simulator = new ObservationSimulator(SatoSuzuVisitObservationFactory.CreateRoute(), seed);
            var results = new List<DaySimulationResult>();
            bool applied = false;
            string pastSignature = null;
            string relationshipBefore = null;
            string historyBefore = null;
            string memoryBefore = null;
            int actionDay = -1;
            float actionTime = -1f;
            bool pendingFutureWasInvalidated = false;

            for (int day = 1; day <= 30; day++)
            {
                simulator.BeginDay(day);
                while (simulator.GenerateNextEvent() != null)
                {
                    ObservationEventDefinition executed = simulator.ExecuteNextEvent();
                    if (!applied && executed.Id == "PROBLEM_OVERTOUCH_CAT_PUNCH")
                    {
                        actionDay = day;
                        pastSignature = Signature(results) + "|TODAY:" + string.Join(",", simulator.DayContext.ExecutedEventIds.OrderBy(x => x).ToArray());
                        relationshipBefore = simulator.State.Relationship.ToString();
                        historyBefore = string.Join(",", simulator.State.HistoryFlags.OrderBy(x => x).ToArray());
                        memoryBefore = string.Join(",", simulator.State.MemoryFlags.OrderBy(x => x).ToArray());
                        int pendingBeforeAction = simulator.PendingEventCount;
                        actionTime = Math.Max(20f, simulator.DayContext.CurrentTime);
                        simulator.ApplyNNNAction(actionId, actionTime);
                        pendingFutureWasInvalidated = pendingBeforeAction == 0 || simulator.PendingEventCount == 0;
                        applied = true;

                        if (relationshipBefore != simulator.State.Relationship.ToString() ||
                            historyBefore != string.Join(",", simulator.State.HistoryFlags.OrderBy(x => x).ToArray()) ||
                            memoryBefore != string.Join(",", simulator.State.MemoryFlags.OrderBy(x => x).ToArray()))
                            throw new InvalidOperationException(actionId + ": action changed relationship/history/memory immediately.");
                    }
                }
                results.Add(simulator.EndDay());
            }

            if (!applied || results.Count != 30) throw new InvalidOperationException(actionId + ": intervention route did not complete.");
            if (!pendingFutureWasInvalidated) throw new InvalidOperationException(actionId + ": pending future events were not invalidated.");
            if (pastSignature == null || !results[actionDay - 1].MajorEventId.Equals("PROBLEM_OVERTOUCH_CAT_PUNCH"))
                throw new InvalidOperationException(actionId + ": confirmed past event was replaced.");
            if (DayOf(results, "REL_RESPECT_SIGNAL") <= actionDay)
                throw new InvalidOperationException(actionId + ": RespectSignal occurred on the punch day.");
            if (DayOf(results, "REL_GREETING") < 0) throw new InvalidOperationException(actionId + ": route did not reach Greeting.");

            if (actionId == "CAT_INVESTIGATION_TEST" && !simulator.State.PlayerKnowledgeFlags.Contains("KNOW_SUZU_RETURNS_HOME"))
                throw new InvalidOperationException("CAT investigation did not add player knowledge.");
            if (actionId == "HUMAN_OPERATION_SIGNAL_HINT_TEST" && !simulator.State.Modifiers.Any(x =>
                    x.Id == "HUMAN_SIGNAL_HINT" && x.ActiveFromDay == actionDay + 1 && x.PriorityBonus > 0))
                throw new InvalidOperationException("Human operation did not add an active future modifier.");
            if (simulator.State.PlayerActionHistory.Count != 1) throw new InvalidOperationException(actionId + ": action history was not recorded once.");

            var natural = Run(seed);
            int naturalRespectDay = DayOf(natural.Results, "REL_RESPECT_SIGNAL");
            int actionRespectDay = DayOf(results, "REL_RESPECT_SIGNAL");
            Debug.Log("NNN Incremental Action " + actionId + ": PASS | DAY" + actionDay + " " +
                      actionTime.ToString("0.00", CultureInfo.InvariantCulture) + " | Respect natural/action=" +
                      naturalRespectDay + "/" + actionRespectDay + " | FutureChanged=" +
                      (Signature(natural.Results) != Signature(results)));
        }

        /// <summary>一つのSeedを検査し、FAIL/WARNINGを例外化せず集計器へ蓄積する。</summary>
        private static void ValidateStressSeed(int seed, IList<DaySimulationResult> results, ObservationSimulationState state, StressAggregate aggregate)
        {
            var failures = new List<string>();
            var warnings = new List<string>();
            if (results == null || results.Count != 30)
            {
                aggregate.AddFailure(seed, "30日分の結果が返らない", results, state);
                return;
            }

            if (results.Where((day, index) => day == null || day.Day != index + 1).Any()) failures.Add("DAY1～DAY30が昇順で揃っていない");
            if (results.Any(day => day.NormalActionIds == null || day.NormalActionIds.Count < 1 || day.NormalActionIds.Count > 3)) failures.Add("Normal Action数が1～3の範囲外");
            var majors = results.Where(day => !string.IsNullOrEmpty(day.MajorEventId)).ToList();
            if (majors.GroupBy(day => day.MajorEventId).Any(group => group.Count() > 1)) failures.Add("単発Major Eventが重複した");

            var days = majors.ToDictionary(day => day.MajorEventId, day => day.Day);
            foreach (string required in RequiredEventIds)
                if (!days.ContainsKey(required)) failures.Add(required + " が未発生");

            RequireDay(days, "VISIT_FIRST_CONTACT", 1, failures);
            RequireAfter(days, "REL_ENTER_HOME", "VISIT_FIRST_CONTACT", failures);
            RequireAfter(days, "REL_SNIFF_HUMAN", "REL_ENTER_HOME", failures);
            RequireAfter(days, "REL_FIRST_TOUCH", "REL_SNIFF_HUMAN", failures);
            RequireAfter(days, "PROBLEM_OVERTOUCH_CAT_PUNCH", "REL_FIRST_TOUCH", failures);
            RequireAfter(days, "REL_RESPECT_SIGNAL", "PROBLEM_OVERTOUCH_CAT_PUNCH", failures);
            RequireAfter(days, "REL_PLAY_TOGETHER", "REL_RESPECT_SIGNAL", failures);
            RequireAfter(days, "REL_SIT_BESIDE", "REL_PLAY_TOGETHER", failures);
            RequireAfter(days, "REL_GREETING", "REL_SIT_BESIDE", failures);
            RequireDay(days, "VISIT_DAY30_ROUTINE", 30, failures);

            ValidateRegressionAndRecovery(results, days, failures);
            if (!state.MemoryFlags.Contains(RelationshipMemory.RespectedSignal) || !state.HistoryFlags.Contains(RelationshipHistoryFlag.Greeted))
                failures.Add("DAY30時点で関係進行が完了していない");

            int maxMajorStreak = MaxConsecutive(results.Select(day => !string.IsNullOrEmpty(day.MajorEventId)));
            int maxNoMajorStreak = MaxConsecutive(results.Select(day => string.IsNullOrEmpty(day.MajorEventId)));
            // DAY1〜3の導入Milestoneは連続を仕様とする。通常期間の密度警告は残す。
            int postIntroStreak = MaxConsecutive(results.Where(day => day.Day > 3).Select(day => !string.IsNullOrEmpty(day.MajorEventId)));
            if (postIntroStreak >= 2) warnings.Add("導入後のMajor Eventが" + postIntroStreak + "日連続");
            if (maxNoMajorStreak >= 10) failures.Add("Major Eventなしが" + maxNoMajorStreak + "日連続");
            if (days.TryGetValue("REL_PLAY_TOGETHER", out int playDay) && playDay <= 10) failures.Add("後半イベントをDAY10以前に消化");

            int logOrderingViolations = ValidateLogOrdering(seed, results, failures);
            ValidateNormalActions(results, warnings, failures);
            aggregate.Record(seed, results, state, days, majors.Count, maxMajorStreak, maxNoMajorStreak,
                logOrderingViolations, failures, warnings);
        }

        /// <summary>
        /// 全隣接ログを比較し、降順になった箇所をFAILへ追加する。
        /// エラーにはSeed、DAY、前後ログを含め、単一Seedだけで再現調査できる形にする。
        /// </summary>
        private static int ValidateLogOrdering(int seed, IEnumerable<DaySimulationResult> results, IList<string> failures)
        {
            int violations = 0;
            foreach (DaySimulationResult day in results)
            {
                for (int index = 1; index < day.LogEntries.Count; index++)
                {
                    ObservationLogEntry previous = day.LogEntries[index - 1];
                    ObservationLogEntry current = day.LogEntries[index];
                    if (previous.Time <= current.Time) continue;
                    violations++;
                    failures.Add("ログ時刻逆転 Seed=" + seed + " DAY=" + day.Day +
                                 " Previous=[" + FormatLog(previous) + "] Current=[" + FormatLog(current) + "]");
                }
            }
            return violations;
        }

        /// <summary>
        /// 同一Timeの二件を人工的に並べ、ソート後も元の因果順が変わらないことを直接検査する。
        /// 実データに偶然同時刻がない場合でも、ソート契約自体の退行を検出できる。
        /// </summary>
        private static void ValidateStableEqualTimeOrdering()
        {
            var first = new ObservationLogEntry { Time = 19.42f, ActionId = "FIRST" };
            var second = new ObservationLogEntry { Time = 19.42f, ActionId = "SECOND" };
            var earlier = new ObservationLogEntry { Time = 18.30f, ActionId = "EARLIER" };
            var logs = new List<ObservationLogEntry> { first, second, earlier };
            ObservationSimulator.SortLogEntriesChronologically(logs);
            if (!ReferenceEquals(logs[0], earlier) || !ReferenceEquals(logs[1], first) || !ReferenceEquals(logs[2], second))
                throw new InvalidOperationException("Equal-time observation log ordering is not stable.");
        }

        private static string FormatLog(ObservationLogEntry log)
            => log.Time.ToString("0.00", CultureInfo.InvariantCulture) + " " + log.ActionId + " " + log.Text;

        private static void ValidateRegressionAndRecovery(IList<DaySimulationResult> results, IDictionary<string, int> days, IList<string> failures)
        {
            if (days.TryGetValue("PROBLEM_OVERTOUCH_CAT_PUNCH", out int punchDay))
            {
                DaySimulationResult punch = results[punchDay - 1];
                if (punch.StateBefore.HumanToCat != HumanToCatState.Approach) failures.Add("猫パンチ直前のHumanToCatがApproachでない");
                if (punch.StateAfter.HumanToCat != HumanToCatState.Watch || punch.StateAfter.CatWariness != CatWarinessState.Medium)
                    failures.Add("猫パンチ後にWatch / Mediumへ後退しない");
            }
            if (days.TryGetValue("REL_RESPECT_SIGNAL", out int respectDay))
            {
                DaySimulationResult respect = results[respectDay - 1];
                if (respect.StateAfter.HumanToCat != HumanToCatState.Approach || respect.StateAfter.CatWariness != CatWarinessState.Low)
                    failures.Add("RespectedSignal後にApproach / Lowへ回復しない");
            }
        }

        private static void ValidateNormalActions(IList<DaySimulationResult> results, IList<string> warnings, IList<string> failures)
        {
            foreach (DaySimulationResult day in results)
                foreach (string action in day.NormalActionIds)
                    if (!day.NormalCandidates.Contains(action)) failures.Add("DAY" + day.Day + "で状態条件外のNormal Action: " + action);

            foreach (string id in NormalEventIds)
            {
                int streak = 0;
                int maximum = 0;
                foreach (DaySimulationResult day in results)
                {
                    streak = day.NormalActionIds.Contains(id) ? streak + 1 : 0;
                    maximum = Math.Max(maximum, streak);
                }
                if (maximum >= 4) warnings.Add(id + " が" + maximum + "日連続");
            }
        }

        private static void RequireAfter(IDictionary<string, int> days, string later, string earlier, IList<string> failures)
        {
            if (days.TryGetValue(later, out int laterDay) && days.TryGetValue(earlier, out int earlierDay) && laterDay <= earlierDay)
                failures.Add(later + " が " + earlier + " より後でない");
        }

        private static void RequireDay(IDictionary<string, int> days, string id, int expected, IList<string> failures)
        {
            if (days.TryGetValue(id, out int actual) && actual != expected) failures.Add(id + " がDAY" + expected + "でない (DAY" + actual + ")");
        }

        private static int MaxConsecutive(IEnumerable<bool> values)
        {
            int current = 0;
            int maximum = 0;
            foreach (bool value in values)
            {
                current = value ? current + 1 : 0;
                maximum = Math.Max(maximum, current);
            }
            return maximum;
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
            var logFailures = new List<string>();
            if (ValidateLogOrdering(seed, results, logFailures) > 0)
                throw new InvalidOperationException(logFailures[0]);
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
        private static string StateSignature(ObservationSimulationState state)
            => state.Relationship + "|H:" + string.Join(",", state.HistoryFlags.OrderBy(x => x).Select(x => x.ToString()).ToArray()) +
               "|M:" + string.Join(",", state.MemoryFlags.OrderBy(x => x).Select(x => x.ToString()).ToArray());
        private static string FullSignature(IEnumerable<DaySimulationResult> results, ObservationSimulationState state)
            => string.Join("|", results.Select(day => day.Day + ":" + day.MajorEventId + ":" +
                string.Join(",", day.NormalActionIds.ToArray()) + ":" +
                string.Join(",", day.LogEntries.Select(log => FormatLog(log)).ToArray()) + ":" + day.StateAfter).ToArray()) +
               "|" + StateSignature(state);
        private static int DayOf(IEnumerable<DaySimulationResult> results, string id)
        {
            var found = results.FirstOrDefault(x => x.MajorEventId == id);
            return found != null ? found.Day : -1;
        }

        private sealed class StressTestReport
        {
            public int FailedCount;
            public string Text;
        }

        /// <summary>
        /// 1000 Seed分の生ログではなく統計に必要な値だけを保持する集計器。
        /// 異常詳細は集計中だけ保持し、出力時はFAIL優先の10件に制限する。Seed番号一覧自体は省略しない。
        /// </summary>
        private sealed class StressAggregate
        {
            private readonly int seedCount;
            private readonly Dictionary<string, List<int>> eventDays = DistributionEventIds.ToDictionary(id => id, id => new List<int>());
            private readonly Dictionary<string, int> normalCounts = NormalEventIds.ToDictionary(id => id, id => 0);
            private readonly Dictionary<string, int> normalMaxStreaks = NormalEventIds.ToDictionary(id => id, id => 0);
            private readonly Dictionary<string, int> optionalEventCounts = OptionalEventIds.ToDictionary(id => id, id => 0);
            private readonly Dictionary<int, List<string>> failures = new Dictionary<int, List<string>>();
            private readonly Dictionary<int, List<string>> warnings = new Dictionary<int, List<string>>();
            private readonly Dictionary<int, string> abnormalDetails = new Dictionary<int, string>();
            private int completedSeedCount;
            private int maximumMajorStreak;
            private int maximumNoMajorStreak;
            private int totalMajorEvents;
            private int logOrderingViolations;

            public StressAggregate(int seedCount) { this.seedCount = seedCount; }

            public void Record(int seed, IList<DaySimulationResult> results, ObservationSimulationState state,
                IDictionary<string, int> days, int majorCount, int majorStreak, int noMajorStreak,
                int seedLogOrderingViolations, IList<string> seedFailures, IList<string> seedWarnings)
            {
                completedSeedCount++;
                totalMajorEvents += majorCount;
                maximumMajorStreak = Math.Max(maximumMajorStreak, majorStreak);
                maximumNoMajorStreak = Math.Max(maximumNoMajorStreak, noMajorStreak);
                logOrderingViolations += seedLogOrderingViolations;
                foreach (string id in DistributionEventIds)
                    if (days.TryGetValue(id, out int day)) eventDays[id].Add(day);
                foreach (string id in OptionalEventIds)
                    if (days.ContainsKey(id)) optionalEventCounts[id]++;

                foreach (string id in NormalEventIds)
                {
                    normalCounts[id] += results.Sum(day => day.NormalActionIds.Count(action => action == id));
                    normalMaxStreaks[id] = Math.Max(normalMaxStreaks[id], MaxNormalStreak(results, id));
                }

                if (seedFailures.Count > 0) AddIssues(failures, seed, seedFailures);
                if (seedWarnings.Count > 0) AddIssues(warnings, seed, seedWarnings);
                // 詳細文字列は集計中だけ保持し、出力時にFAILを優先して最大10件へ絞る。
                // 先に現れたWARNINGだけで表示枠が埋まり、重要なFAILの原因が隠れることを防ぐ。
                if (seedFailures.Count > 0 || seedWarnings.Count > 0)
                    abnormalDetails[seed] = BuildSeedDetail(seed, results, state, days, seedFailures, seedWarnings);
            }

            public void AddFailure(int seed, string message, IList<DaySimulationResult> results, ObservationSimulationState state)
            {
                AddIssues(failures, seed, new[] { message });
                if (!abnormalDetails.ContainsKey(seed))
                    abnormalDetails[seed] = BuildSeedDetail(seed, results, state, null, new[] { message }, new string[0]);
            }

            public StressTestReport BuildReport(TimeSpan elapsed)
            {
                int failed = failures.Count;
                int warnedOnly = warnings.Keys.Count(seed => !failures.ContainsKey(seed));
                int passed = seedCount - failed - warnedOnly;
                var text = new StringBuilder();
                text.AppendLine("NNN Observation Stress Test");
                text.AppendLine();
                text.AppendLine("Seeds: " + seedCount);
                text.AppendLine("Total simulated days: " + (completedSeedCount * 30));
                text.AppendLine("Passed: " + passed);
                text.AppendLine("Warnings: " + warnedOnly);
                text.AppendLine("Failed: " + failed);
                text.AppendLine("Elapsed: " + elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms");
                text.AppendLine("Average per 30-day simulation: " + (elapsed.TotalMilliseconds / seedCount).ToString("0.000", CultureInfo.InvariantCulture) + " ms");
                text.AppendLine("Reproducibility checks: " + Math.Min(20, seedCount) + " representative seeds (mismatches are counted as FAIL)");
                text.AppendLine("Log ordering violations: " + logOrderingViolations);
                text.AppendLine("Average Major Events: " + (completedSeedCount == 0 ? "0" : ((double)totalMajorEvents / completedSeedCount).ToString("0.00", CultureInfo.InvariantCulture)));
                text.AppendLine("Maximum consecutive Major days: " + maximumMajorStreak);
                text.AppendLine("Maximum no-Major gap: " + maximumNoMajorStreak);
                text.AppendLine();
                text.AppendLine("Event day distribution:");
                foreach (string id in DistributionEventIds) AppendEventDistribution(text, id, eventDays[id], seedCount);
                text.AppendLine("Optional Event occurrence:");
                foreach (string id in OptionalEventIds) text.AppendLine(id + ": " + optionalEventCounts[id] + " / " + seedCount);
                text.AppendLine();
                text.AppendLine("Normal Action distribution:");
                foreach (string id in NormalEventIds)
                {
                    double rate = (double)normalCounts[id] / (seedCount * 30);
                    text.AppendLine(id + ": Total=" + normalCounts[id] + ", PerDayRate=" + rate.ToString("P2", CultureInfo.InvariantCulture) +
                                    ", MaxConsecutiveDays=" + normalMaxStreaks[id]);
                }
                text.AppendLine();
                text.AppendLine("Failed seeds: " + FormatSeedList(failures.Keys));
                text.AppendLine("Warning seeds: " + FormatSeedList(warnings.Keys));
                if (abnormalDetails.Count > 0)
                {
                    text.AppendLine();
                    text.AppendLine("Representative abnormal seeds (max 10):");
                    int[] representativeSeeds = failures.Keys.OrderBy(seed => seed)
                        .Concat(warnings.Keys.Where(seed => !failures.ContainsKey(seed)).OrderBy(seed => seed))
                        .Distinct().Take(10).ToArray();
                    foreach (int seed in representativeSeeds) text.AppendLine(abnormalDetails[seed]);
                }
                text.AppendLine();
                text.AppendLine("Result: " + (failed > 0 ? "FAIL" : warnedOnly > 0 ? "PASS WITH WARNINGS" : "PASS"));
                return new StressTestReport { FailedCount = failed, Text = text.ToString() };
            }

            private static void AppendEventDistribution(StringBuilder text, string id, IList<int> days, int seeds)
            {
                text.AppendLine();
                text.AppendLine(id);
                if (days.Count == 0)
                {
                    text.AppendLine("Min: -\nMax: -\nAverage: -\nMedian: -\nMissing: " + seeds + " / " + seeds);
                    return;
                }
                var sorted = days.OrderBy(day => day).ToList();
                double median = sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;
                text.AppendLine("Min: " + sorted.First());
                text.AppendLine("Max: " + sorted.Last());
                text.AppendLine("Average: " + sorted.Average().ToString("0.00", CultureInfo.InvariantCulture));
                text.AppendLine("Median: " + median.ToString("0.0", CultureInfo.InvariantCulture));
                text.AppendLine("Missing: " + (seeds - sorted.Count) + " / " + seeds);
                text.AppendLine("Days: " + string.Join(", ", sorted.GroupBy(day => day).Select(group => "D" + group.Key + "=" + group.Count()).ToArray()));
            }

            private static int MaxNormalStreak(IList<DaySimulationResult> results, string id)
                => MaxConsecutive(results.Select(day => day.NormalActionIds.Contains(id)));

            private static void AddIssues(IDictionary<int, List<string>> target, int seed, IEnumerable<string> issues)
            {
                if (!target.TryGetValue(seed, out List<string> existing)) target[seed] = existing = new List<string>();
                foreach (string issue in issues)
                    if (!existing.Contains(issue)) existing.Add(issue);
            }

            private static string BuildSeedDetail(int seed, IList<DaySimulationResult> results, ObservationSimulationState state,
                IDictionary<string, int> knownDays, IEnumerable<string> seedFailures, IEnumerable<string> seedWarnings)
            {
                var days = knownDays ?? (results == null
                    ? new Dictionary<string, int>()
                    : results.Where(day => !string.IsNullOrEmpty(day.MajorEventId)).ToDictionary(day => day.MajorEventId, day => day.Day));
                return "Seed " + seed +
                       " FAIL=[" + string.Join("; ", seedFailures.ToArray()) + "] WARNING=[" + string.Join("; ", seedWarnings.ToArray()) +
                       "] Events=[" + string.Join(", ", days.OrderBy(x => x.Value).Select(x => x.Key + "=D" + x.Value).ToArray()) +
                       "] Final=[" + (state == null ? "-" : state.Relationship.ToString()) + "]";
            }

            private static string FormatSeedList(IEnumerable<int> seeds)
            {
                int[] values = seeds.Distinct().OrderBy(seed => seed).ToArray();
                return values.Length == 0 ? "none" : string.Join(",", values.Select(seed => seed.ToString()).ToArray());
            }
        }
    }
}
#endif
