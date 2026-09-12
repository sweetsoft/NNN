using System.Linq;
using UnityEngine;

namespace NNN
{
    /// <summary>Game View用の検証UI。Simulationの文章は変更せず、結果を一画面に配置する。</summary>
    public sealed class SatoHachiPlayableUI : MonoBehaviour
    {
        public SatoHachiPlayableController Controller;
        public Camera WorldCamera;
        public bool CaptionsVisible = true;
        private Font font;
        private GUIStyle text, small, title, button, card, toggle;
        private readonly Color ink = new Color(0.12f, 0.2f, 0.22f);
        private readonly Color paper = new Color(0.96f, 0.95f, 0.91f);
        private void InitStyles()
        {
            if (text != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Arial" }, 22);
            text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 22, wordWrap = true }; text.normal.textColor = ink;
            text.hover.textColor = text.active.textColor = text.focused.textColor = ink;
            text.onNormal.textColor = text.onHover.textColor = text.onActive.textColor = text.onFocused.textColor = ink;
            small = new GUIStyle(text) { fontSize = 17 };
            toggle = new GUIStyle(GUI.skin.toggle) { font = font, fontSize = 17 }; toggle.normal.textColor = ink; toggle.onNormal.textColor = ink;
            toggle.hover.textColor = toggle.active.textColor = toggle.focused.textColor = ink;
            toggle.onHover.textColor = toggle.onActive.textColor = toggle.onFocused.textColor = ink;
            title = new GUIStyle(text) { fontSize = 32, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 23, wordWrap = true };
            card = new GUIStyle(button) { alignment = TextAnchor.MiddleLeft, fontSize = 17, padding = new RectOffset(16, 16, 6, 6) };
        }
        private void OnGUI()
        {
            if (Controller.Simulator == null) return;
            InitStyles();
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Space || Event.current.keyCode == KeyCode.Return)) Event.current.Use();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 900f);
            float left = (Screen.width - 1280 * scale) / 2, top = (Screen.height - 900 * scale) / 2;
            // The world camera only clears its viewport. Clear the surrounding UI every frame.
            var previousColor = GUI.color; GUI.color = new Color(.08f, .12f, .14f);
            float worldLeft = left + 24 * scale, worldTop = top + 90 * scale;
            float worldRight = left + 1256 * scale, worldBottom = top + 420 * scale;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, worldTop), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, worldBottom, Screen.width, Screen.height - worldBottom), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, worldTop, worldLeft, worldBottom - worldTop), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(worldRight, worldTop, Screen.width - worldRight, worldBottom - worldTop), Texture2D.whiteTexture);
            GUI.color = previousColor;
            var old = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(left, top, 0), Quaternion.identity, Vector3.one * scale);
            WorldCamera.rect = new Rect((left + 24 * scale) / Screen.width, 1 - (top + 420 * scale) / Screen.height, 1232 * scale / Screen.width, 330 * scale / Screen.height);
            Panel(0, 0, 1280, 84); Label(28, 17, 560, 50, "DAY " + Controller.Day.ToString("00") + "  /  HACHI", title);
            Label(690, 28, 540, 36, "NNN   ·   OBSERVE / UNDERSTAND / ACT", small);
            Label(45, 100, 800, 38, Controller.Presentation.BackgroundId == "SHOPPING_STREET" ? "商店街  /  SHOPPING STREET" : "佐藤宅  /  HOME", small);
            Panel(24, 435, 1232, 94);
            if (Controller.Phase == ObservationDayPhase.ActionSelection && !Controller.SliceComplete)
            {
                Label(44, 441, 1190, 26, "CURRENT QUESTION", small);
                Label(44, 474, 1190, 48, Controller.Review?.CurrentQuestion, text);
            }
            else if (CaptionsVisible && !Controller.ReviewVisible) Label(44, 447, 1190, 74, Controller.Presentation.Caption, text);
            Panel(24, 542, 1232, 298);
            Panel(24, 847, 1232, 48);
            if (Controller.SliceComplete)
            {
                Label(70, 580, 1150, 80, "SatoHachi Vertical Slice Complete", title);
                if (GUI.Button(new Rect(900, 747, 300, 64), "もう一度プレイ", button)) Controller.Restart();
            }
            else if (Controller.Phase == ObservationDayPhase.Observing)
            {
                Label(50, 565, 850, 50, Controller.SceneIndex < 0 ? "DAY START" : "OBSERVATION", title);
                Label(50, 628, 800, 80, Controller.SceneIndex < 0 ? "今日の生活を観察します。" : "場面 " + (Controller.SceneIndex + 1) + " / " + Controller.Scenes.Count + "   ·   " + Controller.Presentation.LogIndex + " / " + Controller.Presentation.LogCount, text);
                Label(50, 770, 790, 40, "NEXT / Space / Enter で次へ", small); NextButton("NEXT");
            }
            else if (Controller.ReviewVisible) ReviewPanel();
            else if (Controller.Phase == ObservationDayPhase.CatReport)
            {
                Label(50, 565, 1100, 50, "CAT REPORT   /   " + Controller.Definition.CatName, title);
                Label(64, 635, 1050, 60, "「" + Controller.Simulator.DayContext.CatReport.Text + "」", title);
                if (Controller.Simulator.ObservedKnowledgeTags.Count > 0)
                    Label(64, 698, 780, 118, "観察で分かったこと\n" + string.Join("\n", Controller.Simulator.ObservedKnowledgeTags.Select(Controller.Definition.InformationText)), small);
                NextButton("NNN ACTION →");
            }
            else if (Controller.Phase == ObservationDayPhase.ActionSelection) ActionPanel();
            else ResultPanel();
            Controller.DebugVisible = GUI.Toggle(new Rect(30, 857, 190, 32), Controller.DebugVisible, " Debug [F1]", toggle);
            if (Controller.GuidedMode && Controller.Phase == ObservationDayPhase.ActionSelection && !Controller.SliceComplete)
                if (GUI.Button(new Rect(905, 851, 330, 40), "Guided選択 [G]", button)) Controller.SelectGuided();
            if (Controller.DebugVisible) DebugPanel();
            if (!string.IsNullOrEmpty(Controller.Error)) Label(300, 850, 600, 40, Controller.Error, small);
            GUI.matrix = old;
        }
        private void ActionPanel()
        {
            Label(50, 550, 1100, 42, "NNN ACTION   /   今日はひとつ", title);
            var visible = Controller.Options.Where(x => x.Visibility != NNNActionVisibility.Hidden).ToList();
            for (int i = 0; i < visible.Count; i++)
            {
                var option = visible[i];
                string kind = option.Definition.Kind == NNNActionKind.Investigation ? "INVESTIGATION / 調査" : option.Definition.Kind == NNNActionKind.Operation ? "OPERATION / 工作" : "SKIP";
                string body = kind + "\n" + option.Definition.DisplayName;
                if (option.Definition.Kind != NNNActionKind.Skip)
                    body += "\n" + (option.Definition.Kind == NNNActionKind.Investigation ? "確かめる：" : "試す：") + option.Definition.IntentText;
                if (!option.IsAvailable) body += "\nLOCKED · " + Friendly(option.UnavailableReason);
                GUI.enabled = option.IsAvailable;
                GUI.backgroundColor = option.Definition.Kind == NNNActionKind.Investigation ? new Color(.55f, .88f, .83f) : new Color(.96f, .81f, .52f);
                if (GUI.Button(new Rect(48 + (i % 2) * 600, 596 + (i / 2) * 120, 582, 114), body, card)) Controller.SelectAction(option.Definition.Id);
                GUI.enabled = true; GUI.backgroundColor = Color.white;
            }
        }
        private void ReviewPanel()
        {
            Label(50, 552, 1100, 36, "OBSERVATION UPDATE", text);
            Label(50, 594, 1160, 98, string.Join("\n", Controller.Review.Updates.Select(x => "・" + x.Text)), text);
            if (Controller.Review.Changes.Count > 0)
            {
                Label(50, 700, 1100, 30, "CHANGE", small);
                Label(50, 732, 1160, 94, string.Join("\n", Controller.Review.Changes.Select(x => x.Text)), text);
            }
            if (GUI.Button(new Rect(945, 851, 290, 40), "CAT REPORT →", button)) Controller.Advance();
        }
        private void ResultPanel()
        {
            if (Controller.Investigation != null)
            {
                var result = Controller.Investigation;
                Label(50, 550, 1170, 44, "調査結果  /  " + Controller.SelectedAction.DisplayName, text);
                Label(50, 603, 705, 190, result.ResultText, text);
                Label(795, 600, 425, 28, "NEW INFORMATION", small);
                Label(795, 632, 425, 94, string.Join("\n", result.AddedKnowledgeTags.Select(x => "・" + Controller.Definition.InformationText(x))), small);
                var discovered = result.NewlyDiscoveredOperationIds.Union(result.NewlyUnlockedOperationIds).Select(id => Controller.Route.Actions.Single(x => x.Id == id).DisplayName);
                Label(795, 726, 425, 76, "NEW OPERATION\n" + (discovered.Any() ? string.Join(" / ", discovered) : "今回の新規発見はありません"), small);
            }
            else
            {
                Label(50, 565, 1100, 45, Controller.SelectedAction.Kind == NNNActionKind.Operation ? "NNN OPERATION" : "SKIP", title);
                Label(50, 632, 1100, 108, Controller.SelectedAction.Kind == NNNActionKind.Operation
                    ? "次の工作を手配しました。\n「" + Controller.SelectedAction.DisplayName + "」\n効果は翌日以降に現れます。"
                    : "今日は手を加えず、様子を見ます。", text);
            }
            // 結果本文の読書領域を避けて、常に同じ位置へ置く。
            GUI.enabled = Controller.CanAdvance;
            if (GUI.Button(new Rect(945, 847, 290, 42), Controller.Day == 11 ? "COMPLETE" : "NEXT DAY →", button)) Controller.Advance();
            GUI.enabled = true;
        }
        private void NextButton(string label)
        {
            GUI.enabled = Controller.CanAdvance;
            if (GUI.Button(new Rect(900, 747, 300, 64), Controller.Presentation.IsPlaying ? "再生中…" : label, button)) Controller.Advance();
            GUI.enabled = true;
        }
        private string Friendly(string value)
        { foreach (var label in Controller.Definition.Information) value = value.Replace(label.Id, label.Label); return value; }
        private void DebugPanel()
        {
            Panel(460, 90, 790, 448);
            Controller.AutoAdvance = GUI.Toggle(new Rect(700, 106, 250, 28), Controller.AutoAdvance, "Auto Advance (debug)", toggle);
            Controller.GuidedMode = GUI.Toggle(new Rect(700, 140, 250, 28), Controller.GuidedMode, "Guided [G]", toggle);
            CaptionsVisible = GUI.Toggle(new Rect(990, 106, 240, 28), CaptionsVisible, "Caption", toggle);
            Label(700, 177, 520, 32, "DAY " + Controller.Day + " / " + Controller.Phase + " / " + Controller.Presentation.EventId, small);
            Label(700, 211, 520, 120, Controller.Simulator.State.Relationship + "\nKnowledge: " + Controller.Simulator.State.PlayerKnowledgeFlags.Count + " / World: " + string.Join(", ", Controller.Simulator.State.WorldFlags), new GUIStyle(small) { fontSize = 14 });
            Label(700, 337, 520, 68, "Days played: " + Controller.Measurements.Count + " / TOTAL " + Controller.Measurements.Sum(x => x.Seconds).ToString("F1") + " sec\nInvestigations: " + Controller.Simulator.State.PlayerActionHistory.Count(x => x.ActionId.StartsWith("INVESTIGATE")) + " / Operations: " + Controller.Simulator.State.PlayerActionHistory.Count(x => x.ActionId.StartsWith("OP_")), small);
            if (Controller.Review != null)
                Label(475, 409, 760, 128, "Insight: " + Controller.Review.SelectedInsightId + " / Question: " + Controller.Review.CurrentQuestionId
                    + "\nUpdate sources: " + string.Join("; ", Controller.Review.Updates.Select(Controller.Review.Source))
                    + "\nChange sources: " + string.Join("; ", Controller.Review.Changes.Select(Controller.Review.Source)), new GUIStyle(small) { fontSize = 12 });
        }
        private void Panel(float x, float y, float w, float h)
        { var old = GUI.color; GUI.color = paper; GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = old; }
        private void Label(float x, float y, float w, float h, string value, GUIStyle style) => GUI.Label(new Rect(x, y, w, h), value, style);
    }
}
