#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NNN.Editor
{
    /// <summary>舞台・モデル・カメラ設定を保持し、UI階層だけをuGUIへ移行する。</summary>
    [InitializeOnLoad]
    public static class PlayableUGUIMigration
    {
        private const string Key = "NNN.UGUIVerification";
        private static double next, deadline;
        static PlayableUGUIMigration() { EditorApplication.update += VerifyTick; }
        [MenuItem("NNN/Playable/Apply uGUI Layout")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (var path in new[] { SatoHachiPlayableSceneBuilder.ScenePath, SatoKotaPlayableSceneBuilder.KotaScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var ui = UnityEngine.Object.FindFirstObjectByType<SatoHachiPlayableUI>();
                if (ui.Canvas == null) PlayableUGUILayout.Build(ui);
                PlayableUGUILayout.EnsureDisplayCamera(ui);
                ui.Controller.DebugVisible = true;
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        public static void ApplyAndPlay() { Apply(); EditorApplication.isPlaying = true; }
        public static void RunBatch()
        {
            Apply(); SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        private static void VerifyTick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 180;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("uGUI verification timeout");
                var ui = UnityEngine.Object.FindFirstObjectByType<SatoHachiPlayableUI>();
                if (ui == null || ui.Controller.Simulator == null || string.IsNullOrEmpty(ui.DayText.text)) return;
                var c = ui.Controller;
                // 自動進行を停止し、uGUI ButtonのUnityEventだけでDAY1～11を操作する。
                c.enabled = false; c.AutoAdvance = true;
                if (ui.Canvas == null || ui.ObservationImage.texture == null || ui.ActionButtons.Length != 4)
                    throw new Exception("Missing uGUI references/render target");
                if (c.SliceComplete)
                {
                    SessionState.SetBool(Key, false);
                    if (c.Measurements.Count != 11 || ui.LogText.text.Contains("観察を始めると")) throw new Exception("Incomplete uGUI route/log");
                    Debug.Log("UGUI: PASS / DAY1-11 via NEXT and Action Button UnityEvents / render texture / action log / serialized Canvas");
                    SatoKotaVerification.Verify(); SatoHachiVerification.Verify(); SatoHachiInsightVerification.Verify();
                    NNNObservationBatchRunner.RunStressTest();
                    return;
                }
                if (EditorApplication.timeSinceStartup < next || c.Presentation.IsPlaying) return;
                next = EditorApplication.timeSinceStartup + .3;
                if (c.Phase == ObservationDayPhase.ActionSelection)
                {
                    var visible = c.Options.Where(x => x.Visibility != NNNActionVisibility.Hidden).ToList();
                    int index = visible.FindIndex(x => x.Definition.Id == c.Definition.GuidedActions[c.Day - 1]);
                    if (index < 0 || !ui.ActionButtons[index].interactable || !ui.ActionButtons[index].gameObject.activeSelf)
                        throw new Exception("Guided uGUI action unavailable");
                    ui.ActionButtons[index].onClick.Invoke();
                }
                else if (ui.NextButton.interactable && ui.NextButton.gameObject.activeSelf) ui.NextButton.onClick.Invoke();
            }
            catch (Exception error)
            {
                SessionState.SetBool(Key, false); Debug.LogException(error); EditorApplication.Exit(1);
            }
        }
    }
}
#endif
