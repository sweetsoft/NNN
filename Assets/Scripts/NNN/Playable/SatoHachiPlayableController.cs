using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NNN
{
    [Serializable] public sealed class PlayableDayMeasurement
    {
        public int Day;
        public string StartUtc, EndUtc, SelectedActionId, CatReportId;
        public double Seconds;
        public int Scenes, Clicks;
        public bool Automated;
    }
    public sealed class SatoHachiPlayableController : MonoBehaviour
    {
        public ObservationScenePresenter Presentation;
        public ObservationPresentationDefinition Definition;
        /// <summary>Sceneが選ぶ猫別コンテンツ。未設定なら保存済みハチSceneとの互換性を維持する。</summary>
        public ObservationPlayableContent Content;
        public int Seed = 42;
        public bool AutoAdvance;
        public bool GuidedMode;
        public bool DebugVisible;
        public ObservationSimulator Simulator { get; private set; }
        public ObservationRouteDefinition Route { get; private set; }
        public ObservationDayPhase Phase => Simulator?.DayContext?.Phase ?? ObservationDayPhase.Completed;
        public int Day { get; private set; }
        public bool SliceComplete { get; private set; }
        public int SceneIndex { get; private set; } = -1;
        public List<ObservationScene> Scenes { get; private set; }
        public List<NNNActionOption> Options { get; private set; }
        public NNNActionDefinition SelectedAction { get; private set; }
        public InvestigationResult Investigation { get; private set; }
        public List<PlayableDayMeasurement> Measurements { get; } = new List<PlayableDayMeasurement>();
        public List<DaySimulationResult> DayResults { get; } = new List<DaySimulationResult>();
        public string CsvPath { get; private set; }
        public string Error { get; private set; }
        public ObservationReview Review { get; private set; }
        public bool ReviewVisible { get; private set; }
        public List<ObservationReview> Reviews { get; } = new List<ObservationReview>();
        private ObservationInsightPresenter insights;
        private PlayableDayMeasurement measurement;
        private double dayStart;
        private float nextAuto;
        public bool CanAdvance => !SliceComplete && Phase != ObservationDayPhase.ActionSelection && !Presentation.IsPlaying;
        private void Start() { Restart(); }
        /// <summary>
        /// Route・Simulator・Insight比較履歴・計測を新しいセッションへ切り替える。
        /// 小物は日次保持設定に関係なく初期配置へ戻し、前回の片付けを新しいプレイへ持ち越さない。
        /// </summary>
        public void Restart()
        {
            Presentation.Stop(); Measurements.Clear(); DayResults.Clear(); SliceComplete = false; Error = null;
            foreach (var prop in Presentation.Props) prop.ResetProp(true);
            Route = Content != null ? Content.CreateRoute() : SatoHachiObservationFactory.CreateRoute();
            Simulator = new ObservationSimulator(Route, Seed);
            insights = new ObservationInsightPresenter(Content != null ? Content.CreateInsights() : SatoHachiInsightFactory.Create()); Reviews.Clear();
            string folder = Path.Combine(Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..")) : Application.persistentDataPath, "Logs/Playable");
            Directory.CreateDirectory(folder);
            CsvPath = Path.Combine(folder, "play-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".csv");
            File.WriteAllText(CsvPath, "Day,StartUtc,EndUtc,DayPlaySeconds,SelectedActionId,ObservationSceneCount,CatReportId,Clicks,Mode\n");
            BeginDay(1);
        }
        private void BeginDay(int day)
        {
            Day = day; SceneIndex = -1; SelectedAction = null; Investigation = null; Options = null;
            Review = null; ReviewVisible = false;
            measurement = new PlayableDayMeasurement { Day = day, StartUtc = DateTime.UtcNow.ToString("O"), Automated = AutoAdvance };
            dayStart = Time.realtimeSinceStartupAsDouble;
            // 翌朝のOperationEffectを先に適用してから、そのWorldFlagsに設備表示を合わせる。
            // 当日の観察は一度だけ実行し、以降は確定したScenesを再生する。Viewで再抽選しない。
            Simulator.BeginDay(day);
            Presentation.SyncWorld(Simulator.State.WorldFlags);
            while (Simulator.GenerateNextEvent() != null) Simulator.ExecuteNextEvent();
            Scenes = Simulator.GetObservationScenes(); Presentation.ResetDay(); nextAuto = Time.unscaledTime + 0.9f;
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) DebugVisible = !DebugVisible;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Advance();
            if (Input.GetKeyDown(KeyCode.G) && GuidedMode) SelectGuided();
            if (AutoAdvance && !SliceComplete)
            {
                measurement.Automated = true;
                if (Time.unscaledTime >= nextAuto)
                {
                    if (Phase == ObservationDayPhase.ActionSelection) SelectGuided(); else Advance();
                    nextAuto = Time.unscaledTime + 0.9f;
                }
            }
        }
        public void Advance()
        {
            if (!CanAdvance) return;
            measurement.Clicks++;
            switch (Phase)
            {
                case ObservationDayPhase.Observing:
                    if (++SceneIndex < Scenes.Count) { measurement.Scenes++; Presentation.PlayScene(Scenes[SceneIndex], AutoAdvance); }
                    else
                    {
                        Presentation.Stop(); Simulator.CompleteObservation(); measurement.CatReportId = Simulator.DayContext.CatReport.Id;
                        Review = insights.Build(Simulator, Scenes); Reviews.Add(Review); ReviewVisible = true;
                    }
                    break;
                case ObservationDayPhase.CatReport:
                    // Reviewは表示上の一枚。Simulationのフェーズや進行APIは増やさない。
                    if (ReviewVisible) { ReviewVisible = false; break; }
                    Simulator.CompleteCatReport(); Options = Simulator.GetNNNActionOptions(); break;
                case ObservationDayPhase.Completed:
                    FinishDay();
                    if (Day == 11) SliceComplete = true; else BeginDay(Day + 1);
                    break;
            }
        }
        public void SelectGuided()
        {
            if (Phase != ObservationDayPhase.ActionSelection || SliceComplete) return;
            string id = Definition.GuidedActions[Day - 1];
            if (!Options.Any(x => x.Definition.Id == id && x.IsAvailable)) { Error = "Guided候補がありません: " + id; AutoAdvance = false; return; }
            SelectAction(id);
        }
        public void SelectAction(string id)
        {
            if (Phase != ObservationDayPhase.ActionSelection || !Options.Any(x => x.Definition.Id == id && x.IsAvailable)) return;
            SelectedAction = Route.Actions.Single(x => x.Id == id);
            // 比較元は工作適用前のスナップショット。翌日のInsightが実際の環境差を説明するために使う。
            if (SelectedAction.Kind == NNNActionKind.Operation) insights.RecordOperation(id, Simulator);
            Investigation = Simulator.ApplyNNNAction(id); measurement.SelectedActionId = id; measurement.Clicks++;
        }
        private void FinishDay()
        {
            DayResults.Add(Simulator.EndDay());
            measurement.EndUtc = DateTime.UtcNow.ToString("O"); measurement.Seconds = Time.realtimeSinceStartupAsDouble - dayStart;
            Measurements.Add(measurement);
            File.AppendAllText(CsvPath, string.Join(",", measurement.Day, measurement.StartUtc, measurement.EndUtc,
                measurement.Seconds.ToString("F3", CultureInfo.InvariantCulture), measurement.SelectedActionId,
                measurement.Scenes, measurement.CatReportId, measurement.Clicks, measurement.Automated ? "Auto" : "Manual") + "\n");
            Debug.Log("Playable DAY " + Day + ": " + measurement.Seconds.ToString("F2") + " sec / " + measurement.Scenes + " scenes / " + measurement.Clicks + " inputs / " + measurement.SelectedActionId);
            if (Day == 11) Debug.Log("Playable TOTAL: " + Measurements.Sum(x => x.Seconds).ToString("F2") + " sec. CSV: " + CsvPath);
        }
    }
}
