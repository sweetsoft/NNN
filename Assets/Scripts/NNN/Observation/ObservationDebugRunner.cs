using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NNN
{
    /// <summary>Inspector/Consoleで候補、選択、状態、履歴を追跡するv0.1用ランナー。</summary>
    public sealed class ObservationDebugRunner : MonoBehaviour
    {
        [SerializeField] private int randomSeed = 20260904;
        [SerializeField] private int currentDay;
        [SerializeField] private string relationshipState;
        [SerializeField, TextArea(2, 5)] private string historyFlags;
        [SerializeField, TextArea(2, 5)] private string memoryFlags;
        [SerializeField, TextArea(2, 8)] private string todayCandidates;
        [SerializeField] private string selectedMajorEvent;
        [SerializeField] private string selectedNormalActions;
        [SerializeField, TextArea(8, 30)] private string lastSimulation;

        public int RandomSeed { get => randomSeed; set => randomSeed = value; }
        public string LastSimulation => lastSimulation;

        /// <summary>
        /// Inspectorのコンテキストメニューから30日を一括実行し、最終状態とDAY30候補をシリアライズ欄へ保存する。
        /// 全日詳細はConsoleへ出し、ゲーム本編のUI・時間進行・アニメーションには干渉しない。
        /// </summary>
        [ContextMenu("Simulate 30 Days")]
        public void Simulate30Days()
        {
            var simulator = new ObservationSimulator(SatoSuzuVisitObservationFactory.CreateRoute(), randomSeed);
            var results = simulator.Simulate30Days();
            lastSimulation = Format(results);
            var last = results[results.Count - 1];
            currentDay = last.Day;
            relationshipState = last.StateAfter.ToString();
            historyFlags = string.Join(", ", simulator.State.HistoryFlags.OrderBy(x => x).Select(x => x.ToString()).ToArray());
            memoryFlags = string.Join(", ", simulator.State.MemoryFlags.OrderBy(x => x).Select(x => x.ToString()).ToArray());
            todayCandidates = "Normal: " + string.Join(", ", last.NormalCandidates.ToArray()) + "\nRelationship: " + string.Join(", ", last.RelationshipCandidates.ToArray()) + "\nProblem: " + string.Join(", ", last.ProblemCandidates.ToArray());
            selectedMajorEvent = string.IsNullOrEmpty(last.MajorEventId) ? "none" : last.MajorEventId;
            selectedNormalActions = string.Join(", ", last.NormalActionIds.ToArray());
            Debug.Log("NNN Observation / 佐藤美咲 × スズ × Visit / Seed " + randomSeed + "\n" + lastSimulation);
        }

        /// <summary>日ごとの通常行動、Major Event、状態を人間が比較しやすい固定形式へ整形する。</summary>
        public static string Format(IList<DaySimulationResult> results)
        {
            var text = new StringBuilder();
            foreach (var day in results)
            {
                text.AppendLine("DAY " + day.Day.ToString("00"));
                text.AppendLine("Normal: " + (day.NormalActionIds.Count == 0 ? "none" : string.Join(", ", day.NormalActionIds.ToArray())));
                text.AppendLine("Major: " + (string.IsNullOrEmpty(day.MajorEventId) ? "none" : day.MajorEventId));
                text.AppendLine("State: " + day.StateAfter);
            }
            return text.ToString();
        }
    }
}
