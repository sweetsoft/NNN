#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>Route共通の手動検証画面。シーンを変更せず観察→REPORT→ACTION→翌日を実行する。</summary>
    public sealed class ObservationSliceWindow : EditorWindow
    {
        private ObservationRouteDefinition route;
        private ObservationSimulator simulator;
        private int seed = 42;
        private Vector2 scroll;
        private readonly List<ObservationScene> scenes = new List<ObservationScene>();
        private readonly Queue<ObservationScene> pendingScenes = new Queue<ObservationScene>();
        private InvestigationResult investigation;
        private string actionText;
        private bool showDebug;
        private const int LastDay = 11;

        [MenuItem("NNN/Observation/Play Sato Hachi DAY1-11")]
        public static void Open() { GetWindow<ObservationSliceWindow>("Sato Hachi DAY1-11").Show(); }

        private void OnGUI()
        {
            seed = EditorGUILayout.IntField("Seed", seed);
            if (GUILayout.Button("DAY1から開始"))
            {
                route = SatoHachiObservationFactory.CreateRoute(); simulator = new ObservationSimulator(route, seed);
                StartDay(1);
            }
            if (simulator == null) return;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("DAY " + simulator.DayContext.Day + " / " + simulator.DayContext.Phase, EditorStyles.boldLabel);
            foreach (var scene in scenes)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var log in scene.Logs) EditorGUILayout.LabelField(log.Text, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }
            if (simulator.DayContext.Phase == ObservationDayPhase.Observing)
            {
                if (GUILayout.Button("次の観察シーン"))
                {
                    if (pendingScenes.Count > 0) scenes.Add(pendingScenes.Dequeue());
                    if (pendingScenes.Count == 0) simulator.CompleteObservation();
                }
            }
            else
            {
                EditorGUILayout.LabelField("CAT REPORT", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(simulator.DayContext.CatReport.Text, EditorStyles.wordWrappedLabel);
                if (simulator.ObservedKnowledgeTags.Count > 0)
                    EditorGUILayout.LabelField("観察から得た情報: " + string.Join(", ", simulator.ObservedKnowledgeTags), EditorStyles.wordWrappedLabel);
            }
            if (simulator.DayContext.Phase == ObservationDayPhase.CatReport && GUILayout.Button("NNN ACTIONへ")) simulator.CompleteCatReport();
            if (simulator.DayContext.Phase == ObservationDayPhase.ActionSelection)
                foreach (var option in simulator.GetNNNActionOptions())
                {
                    EditorGUILayout.LabelField(option.Definition.Description, EditorStyles.wordWrappedLabel);
                    using (new EditorGUI.DisabledScope(!option.IsAvailable))
                        if (GUILayout.Button(option.Definition.DisplayName))
                        {
                            investigation = simulator.ApplyNNNAction(option.Definition.Id);
                            actionText = option.Definition.Kind == NNNActionKind.Operation ? "工作を準備しました。条件は翌日から変化します。"
                                : option.Definition.Kind == NNNActionKind.Skip ? "今日は様子を見ます。" : "調査結果";
                        }
                    if (!option.IsAvailable) EditorGUILayout.LabelField(option.UnavailableReason, EditorStyles.wordWrappedLabel);
                }
            if (simulator.DayContext.Phase == ObservationDayPhase.Completed)
            {
                EditorGUILayout.LabelField(actionText, EditorStyles.boldLabel);
                if (investigation != null)
                {
                    EditorGUILayout.LabelField(investigation.ResultText, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField("新しい情報: " + string.Join(", ", investigation.AddedKnowledgeTags), EditorStyles.wordWrappedLabel);
                    foreach (string id in investigation.NewlyDiscoveredOperationIds.Union(investigation.NewlyUnlockedOperationIds))
                    {
                        var operation = route.Actions.Single(x => x.Id == id);
                        EditorGUILayout.LabelField((investigation.NewlyUnlockedOperationIds.Contains(id) ? "解禁: " : "発見: ") + operation.DisplayName, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(operation.Description, EditorStyles.wordWrappedLabel);
                    }
                }
                if (simulator.DayContext.Day < LastDay)
                {
                    if (GUILayout.Button("NEXT DAY")) { int next = simulator.DayContext.Day + 1; simulator.EndDay(); StartDay(next); }
                }
                else EditorGUILayout.HelpBox("DAY1〜11の検証範囲はここまでです。ここから先の巡回方法や関係調整は、まだ決まっていません。", MessageType.Info);
            }
            showDebug = EditorGUILayout.Foldout(showDebug, "検証用状態（世界の条件を含む）");
            if (showDebug)
            {
                EditorGUILayout.LabelField(simulator.State.Relationship.ToString(), EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("World: " + string.Join(", ", simulator.State.WorldFlags), EditorStyles.wordWrappedLabel);
                foreach (var option in simulator.GetNNNActionOptions(true))
                    EditorGUILayout.LabelField(option.Visibility + " / " + option.Definition.DisplayName, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        }
        private void StartDay(int day)
        {
            scenes.Clear(); pendingScenes.Clear(); investigation = null; actionText = null;
            simulator.BeginDay(day);
            // 表示用の抜粋を確定してから、一場面ずつ開示する。ACTIONは全場面とREPORTの後のみ。
            while (simulator.GenerateNextEvent() != null) simulator.ExecuteNextEvent();
            foreach (var scene in simulator.GetObservationScenes()) pendingScenes.Enqueue(scene);
        }
    }
}
#endif
