using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NNN
{
    /// <summary>1920×1080の参照レイアウト。生成後はSceneのRectTransformを直接調整できる。</summary>
    public static class PlayableUGUILayout
    {
        private static readonly Color Ink = new Color(.12f, .2f, .22f);
        private static readonly Color Paper = new Color(.96f, .96f, .93f);
        public static void Build(SatoHachiPlayableUI ui)
        {
            var root = new GameObject("Playable Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(ui.transform, false);
            ui.Canvas = root.GetComponent<Canvas>(); ui.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            EnsureDisplayCamera(ui);
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            // 観察はRenderTexture経由なので背景Panelで全面を覆ってもCameraを隠さない。
            var background = Panel(root.transform, "Background", 0, 0, 1920, 1080, Color.white);
            background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one; background.offsetMin = background.offsetMax = Vector2.zero;
            var layout = Rect(root.transform, "Layout 1920x1080", 0, 0, 1920, 1080);
            layout.anchorMin = layout.anchorMax = new Vector2(.5f, .5f); layout.pivot = new Vector2(.5f, .5f); layout.anchoredPosition = Vector2.zero;
            ui.DayText = Label(layout, "Day", 40, 65, 330, 44, 30);
            ui.TimeText = Label(layout, "Time", 40, 126, 330, 40, 26);
            ui.DebugToggle = Toggle(layout, "Debug [F1]", 20, 202, 330);
            Panel(layout, "Debug Background", 10, 242, 355, 794, Paper);
            var debug = Rect(layout, "Debug Information", 10, 242, 355, 794); ui.DebugPanel = debug.gameObject;
            ui.AutoToggle = Toggle(debug, "Auto Advance", 16, 10, 320);
            ui.GuidedToggle = Toggle(debug, "Guided [G]", 16, 46, 320);
            ui.CaptionToggle = Toggle(debug, "Caption", 16, 82, 320);
            Scroll(debug, "Debug Scroll", 14, 126, 327, 650, out ui.DebugText, 17);
            Panel(layout, "Observation Border", 397, 78, 1094, 690, Ink);
            var observation = Rect(layout, "Observation View", 400, 81, 1088, 684);
            ui.ObservationImage = observation.gameObject.AddComponent<RawImage>(); ui.ObservationImage.raycastTarget = false;
            ui.LocationText = Label(layout, "Location", 415, 90, 900, 28, 19);
            Panel(layout, "Log Panel", 1524, 82, 368, 684, Paper);
            Label(layout, "Log Header", 1544, 100, 328, 35, 26).text = "行動ログ";
            ui.LogScroll = Scroll(layout, "Action Log", 1544, 154, 328, 592, out ui.LogText, 21);
            ui.CaptionText = Label(layout, "Caption or Question", 400, 778, 1090, 48, 22);
            ui.PhaseText = Label(layout, "Phase", 400, 835, 1220, 36, 26);
            ui.BodyText = Label(layout, "Report or Result", 400, 881, 720, 183, 21);
            ui.DetailText = Label(layout, "Additional Information", 1140, 881, 475, 183, 19);
            ui.BodyText.resizeTextForBestFit = ui.DetailText.resizeTextForBestFit = true;
            ui.BodyText.resizeTextMinSize = ui.DetailText.resizeTextMinSize = 15;
            ui.BodyText.resizeTextMaxSize = 21; ui.DetailText.resizeTextMaxSize = 19;
            ui.ActionButtons = new Button[4];
            for (int i = 0; i < 4; i++)
                ui.ActionButtons[i] = Button(layout, "Action " + (i + 1), 400 + i * 306, 879, 296, 183, "", 18);
            ui.NextButton = Button(layout, "Next", 1680, 977, 214, 77, "NEXT", 28);
            ui.GuidedButton = Button(layout, "Guided Selection", 1640, 885, 250, 58, "Guided選択 [G]", 22);
            ui.ErrorText = Label(layout, "Error", 400, 44, 1090, 30, 18); ui.ErrorText.color = new Color(.75f, .16f, .12f);
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            // Space/EnterはControllerが処理する。uGUI Submitとの二重進行を防ぐ。
            Object.FindFirstObjectByType<EventSystem>().sendNavigationEvents = false;
        }
        public static void EnsureDisplayCamera(SatoHachiPlayableUI ui)
        {
            // WorldCameraはオフスクリーン出力。Display側にもCameraを置き、Game Viewの
            // "No cameras rendering"表示を防ぎつつ、背景色だけをクリアする。
            if (ui.transform.Find("UI Display Camera") != null) return;
            var camera = new GameObject("UI Display Camera").AddComponent<Camera>();
            camera.transform.SetParent(ui.transform, false);
            camera.cullingMask = 0; camera.depth = -100; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white; camera.orthographic = true;
            camera.allowHDR = false; camera.allowMSAA = false;
        }
        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static RectTransform Panel(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var rect = Rect(parent, name, x, y, width, height); var image = rect.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return rect;
        }
        private static Text Label(Transform parent, string name, float x, float y, float width, float height, int size)
        {
            var rect = Rect(parent, name, x, y, width, height); var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.color = Ink;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false; text.supportRichText = false; return text;
        }
        private static Button Button(Transform parent, string name, float x, float y, float width, float height, string title, int size)
        {
            var rect = Panel(parent, name, x, y, width, height, new Color(.83f, .9f, .88f));
            var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var text = Label(rect, "Label", 10, 8, width - 20, height - 16, size); text.text = title;
            text.alignment = name.StartsWith("Action") ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = size;
            return button;
        }
        private static Toggle Toggle(Transform parent, string title, float x, float y, float width)
        {
            var rect = Rect(parent, title, x, y, width, 32); var toggle = rect.gameObject.AddComponent<Toggle>();
            var box = Panel(rect, "Box", 0, 5, 22, 22, new Color(.7f, .75f, .73f)); box.GetComponent<Image>().raycastTarget = true;
            var check = Panel(box, "Check", 4, 4, 14, 14, Ink);
            toggle.targetGraphic = box.GetComponent<Image>(); toggle.graphic = check.GetComponent<Image>();
            Label(rect, "Label", 32, 0, width - 32, 32, 20).text = title; return toggle;
        }
        private static ScrollRect Scroll(Transform parent, string name, float x, float y, float width, float height, out Text label, int size)
        {
            var rect = Rect(parent, name, x, y, width, height); var scroll = rect.gameObject.AddComponent<ScrollRect>();
            var viewport = Panel(rect, "Viewport", 0, 0, width, height, new Color(1, 1, 1, .01f));
            viewport.GetComponent<Image>().raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
            label = Label(viewport, "Content", 0, 0, width - 8, height, size);
            var fit = label.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = label.rectTransform; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            return scroll;
        }
    }
}
