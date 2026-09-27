using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NNN
{
    /// <summary>uGUIによる表示専用View。再生済みの観察とControllerの結果だけを表示する。</summary>
    public sealed class SatoHachiPlayableUI : MonoBehaviour
    {
        public SatoHachiPlayableController Controller;
        public Camera WorldCamera;
        public bool CaptionsVisible = true;
        public Canvas Canvas;
        public RawImage ObservationImage;
        public Text DayText, TimeText, LocationText, CaptionText, PhaseText, BodyText, DetailText, LogText, DebugText, ErrorText;
        public Button NextButton, GuidedButton;
        public Button[] ActionButtons;
        public Toggle DebugToggle, AutoToggle, GuidedToggle, CaptionToggle;
        public GameObject DebugPanel;
        public ScrollRect LogScroll;
        private Font runtimeFont;
        private RenderTexture texture;
        private RenderTexture previousTarget;
        private Rect previousRect;
        private readonly Vector3[] corners = new Vector3[4];
        private string lastLog;

        private void Awake()
        {
            if (Canvas == null) PlayableUGUILayout.Build(this);
            runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Arial" }, 22);
            foreach (var label in Canvas.GetComponentsInChildren<Text>(true)) label.font = runtimeFont;
            NextButton.onClick.AddListener(() => { if (Controller.SliceComplete) Controller.Restart(); else Controller.Advance(); });
            GuidedButton.onClick.AddListener(Controller.SelectGuided);
            DebugToggle.onValueChanged.AddListener(value => Controller.DebugVisible = value);
            AutoToggle.onValueChanged.AddListener(value => Controller.AutoAdvance = value);
            GuidedToggle.onValueChanged.AddListener(value => Controller.GuidedMode = value);
            CaptionToggle.onValueChanged.AddListener(value => CaptionsVisible = value);
            for (int i = 0; i < ActionButtons.Length; i++)
            {
                int index = i;
                ActionButtons[i].onClick.AddListener(() => SelectOption(index));
            }
        }
        private void OnEnable()
        {
            if (WorldCamera == null) return;
            previousTarget = WorldCamera.targetTexture; previousRect = WorldCamera.rect;
        }
        private void OnDisable()
        {
            if (WorldCamera != null) { WorldCamera.targetTexture = previousTarget; WorldCamera.rect = previousRect; }
            if (ObservationImage != null) ObservationImage.texture = null;
            if (texture != null) { texture.Release(); Destroy(texture); texture = null; }
        }
        private void OnDestroy() { if (runtimeFont != null) Destroy(runtimeFont); }

        private void LateUpdate()
        {
            UpdateCamera();
            if (Controller == null || Controller.Simulator == null) return;
            DayText.text = "DAY " + Controller.Day.ToString("00") + " / " + Controller.Definition.CatName;
            LocationText.text = Controller.Presentation.BackgroundId == "SHOPPING_STREET" ? "商店街" : "佐藤宅";
            UpdateLog();
            CaptionText.text = CaptionsVisible && !Controller.ReviewVisible ? Controller.Presentation.Caption : "";
            DebugToggle.SetIsOnWithoutNotify(Controller.DebugVisible);
            AutoToggle.SetIsOnWithoutNotify(Controller.AutoAdvance);
            GuidedToggle.SetIsOnWithoutNotify(Controller.GuidedMode);
            CaptionToggle.SetIsOnWithoutNotify(CaptionsVisible);
            DebugPanel.SetActive(Controller.DebugVisible);
            UpdateDebug();
            ErrorText.text = Controller.Error ?? "";
            if (Controller.Phase != ObservationDayPhase.ActionSelection || Controller.SliceComplete)
                foreach (var button in ActionButtons) button.gameObject.SetActive(false);
            GuidedButton.gameObject.SetActive(Controller.GuidedMode && Controller.Phase == ObservationDayPhase.ActionSelection && !Controller.SliceComplete);
            BodyText.text = DetailText.text = "";
            NextButton.gameObject.SetActive(Controller.Phase != ObservationDayPhase.ActionSelection || Controller.SliceComplete);
            NextButton.interactable = Controller.CanAdvance || Controller.SliceComplete;
            string next = "NEXT";
            if (Controller.SliceComplete)
            {
                PhaseText.text = Controller.Definition.SliceTitle + " Complete";
                next = "もう一度プレイ";
            }
            else if (Controller.Phase == ObservationDayPhase.Observing)
            {
                PhaseText.text = Controller.SceneIndex < 0 ? "DAY START" : "OBSERVATION";
                BodyText.text = Controller.SceneIndex < 0 ? "今日の生活を観察します。" :
                    "場面 " + (Controller.SceneIndex + 1) + " / " + Controller.Scenes.Count + "   ·   " + Controller.Presentation.LogIndex + " / " + Controller.Presentation.LogCount;
                DetailText.text = "NEXT / Space / Enter で次へ";
                if (Controller.Presentation.IsPlaying) next = "再生中…";
            }
            else if (Controller.ReviewVisible)
            {
                PhaseText.text = "OBSERVATION UPDATE";
                BodyText.text = string.Join("\n", Controller.Review.Updates.Select(x => "・" + x.Text));
                DetailText.text = Controller.Review.Changes.Count == 0 ? "" : "CHANGE\n" + string.Join("\n", Controller.Review.Changes.Select(x => "・" + x.Text));
                next = "CAT REPORT →";
            }
            else if (Controller.Phase == ObservationDayPhase.CatReport)
            {
                PhaseText.text = "CAT REPORT / " + Controller.Definition.CatName;
                BodyText.text = "「" + Controller.Simulator.DayContext.CatReport.Text + "」";
                DetailText.text = Controller.Simulator.ObservedKnowledgeTags.Count == 0 ? "" : "観察で分かったこと\n" +
                    string.Join("\n", Controller.Simulator.ObservedKnowledgeTags.Select(Controller.Definition.InformationText));
                next = "NNN ACTION →";
            }
            else if (Controller.Phase == ObservationDayPhase.ActionSelection) ShowActions();
            else
            {
                ShowResult();
                next = Controller.Day == 11 ? "COMPLETE" : "NEXT DAY →";
            }
            NextButton.GetComponentInChildren<Text>().text = next;
        }

        // CameraはRawImageの中へ描画する。Size/位置はSceneのCamera設定をそのまま使う。
        private void UpdateCamera()
        {
            if (WorldCamera == null || ObservationImage == null || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            ObservationImage.rectTransform.GetWorldCorners(corners);
            int width = Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(corners[0], corners[3])), 16, 4096);
            int height = Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(corners[0], corners[1])), 16, 4096);
            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture != null) { WorldCamera.targetTexture = null; texture.Release(); Destroy(texture); }
                texture = new RenderTexture(width, height, 24) { name = "Observation View", antiAliasing = 1 };
                texture.Create(); ObservationImage.texture = texture;
            }
            WorldCamera.targetTexture = texture;
            WorldCamera.rect = new Rect(0, 0, 1, 1);
        }
        private void UpdateLog()
        {
            var body = new StringBuilder();
            float? time = null;
            // Simulatorは一日分を先に生成するため、その全ログを直接表示しない。
            for (int i = 0; i <= Controller.SceneIndex && i < Controller.Scenes.Count; i++)
            {
                var scene = Controller.Scenes[i];
                int count = i < Controller.SceneIndex ? scene.Logs.Count : Math.Min(Controller.Presentation.LogIndex, scene.Logs.Count);
                for (int j = 0; j < count; j++)
                {
                    var log = scene.Logs[j]; time = log.Time;
                    body.Append(Clock(log.Time)).Append("  ").Append(log.Actor == ObservationActor.Human ? Controller.Definition.HumanName : Controller.Definition.CatName)
                        .Append('\n').Append(log.Text).Append("\n\n");
                }
            }
            if (Controller.SelectedAction != null) body.Append("NNN ACTION\n").Append(Controller.SelectedAction.DisplayName);
            string value = body.Length == 0 ? "観察を始めると、ここに行動が記録されます。" : body.ToString();
            TimeText.text = "時間  " + (time.HasValue ? Clock(time.Value) : "--:--");
            if (value == lastLog) return;
            lastLog = value; LogText.text = value;
            UnityEngine.Canvas.ForceUpdateCanvases(); LogScroll.verticalNormalizedPosition = 0;
        }
        private static string Clock(float hours)
        {
            int minutes = Mathf.RoundToInt(hours * 60);
            return (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00");
        }
        private void SelectOption(int index)
        {
            if (Controller.Options == null) return;
            var options = Controller.Options.Where(x => x.Visibility != NNNActionVisibility.Hidden).ToList();
            if (index < options.Count && options[index].IsAvailable) Controller.SelectAction(options[index].Definition.Id);
        }
        private void ShowActions()
        {
            PhaseText.text = "NNN ACTION / 今日はひとつ";
            CaptionText.text = "CURRENT QUESTION   " + Controller.Review?.CurrentQuestion;
            var options = Controller.Options.Where(x => x.Visibility != NNNActionVisibility.Hidden).ToList();
            for (int i = 0; i < ActionButtons.Length; i++)
            {
                var button = ActionButtons[i];
                button.gameObject.SetActive(i < options.Count);
                if (i >= options.Count) continue;
                var option = options[i];
                button.gameObject.SetActive(true); button.interactable = option.IsAvailable;
                string kind = option.Definition.Kind == NNNActionKind.Investigation ? "INVESTIGATION / 調査" : option.Definition.Kind == NNNActionKind.Operation ? "OPERATION / 工作" : "SKIP";
                string value = kind + "\n" + option.Definition.DisplayName;
                if (option.Definition.Kind != NNNActionKind.Skip) value += "\n" + option.Definition.IntentText;
                if (!option.IsAvailable) value += "\nLOCKED · " + Friendly(option.UnavailableReason);
                button.GetComponentInChildren<Text>().text = value;
                button.image.color = option.Definition.Kind == NNNActionKind.Investigation ? new Color(.78f, .9f, .89f) : new Color(.96f, .89f, .73f);
            }
        }
        private void ShowResult()
        {
            if (Controller.Investigation != null)
            {
                var result = Controller.Investigation;
                PhaseText.text = "調査結果 / " + Controller.SelectedAction.DisplayName;
                BodyText.text = result.ResultText;
                var operations = result.NewlyDiscoveredOperationIds.Union(result.NewlyUnlockedOperationIds).Select(id => Controller.Route.Actions.Single(x => x.Id == id).DisplayName).ToList();
                DetailText.text = "NEW INFORMATION\n" + string.Join("\n", result.AddedKnowledgeTags.Select(x => "・" + Controller.Definition.InformationText(x))) +
                    "\n\nNEW OPERATION\n" + (operations.Count > 0 ? string.Join(" / ", operations) : "今回の新規発見はありません");
            }
            else
            {
                bool operation = Controller.SelectedAction.Kind == NNNActionKind.Operation;
                PhaseText.text = operation ? "NNN OPERATION" : "SKIP";
                BodyText.text = operation ? "次の工作を手配しました。\n「" + Controller.SelectedAction.DisplayName + "」\n効果は翌日以降に現れます。" : "今日は手を加えず、様子を見ます。";
            }
        }
        private string Friendly(string value)
        {
            foreach (var label in Controller.Definition.Information) value = value.Replace(label.Id, label.Label);
            return value;
        }
        private void UpdateDebug()
        {
            if (!Controller.DebugVisible) return;
            var state = Controller.Simulator.State;
            DebugText.text = "DAY " + Controller.Day + " / " + Controller.Phase + "\nEvent: " + Controller.Presentation.EventId +
                "\nScene: " + Controller.Presentation.SceneId + "\n\n" + state.Relationship +
                "\n\nKnowledge\n" + string.Join("\n", state.PlayerKnowledgeFlags) + "\n\nWorld Flags\n" + string.Join("\n", state.WorldFlags) +
                "\n\nDays played: " + Controller.Measurements.Count + "\nTOTAL: " + Controller.Measurements.Sum(x => x.Seconds).ToString("F1") + " sec" +
                (Controller.Review == null ? "" : "\n\nInsight: " + Controller.Review.SelectedInsightId + "\nQuestion: " + Controller.Review.CurrentQuestionId);
        }
    }
}
