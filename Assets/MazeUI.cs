using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Shared native Unity controls, built at runtime to keep the existing scene assets.
public static class MazeUI
{
    public static readonly Color Panel = new Color(.12f, .14f, .16f, 1f);
    public static readonly Color Tile = new Color(.85f, .87f, .88f);
    public static readonly Color Ink = new Color(.08f, .10f, .12f);
    public static readonly Color Green = new Color(.18f, .60f, .37f);
    public static readonly Color Muted = new Color(.70f, .76f, .80f);

    public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 insetMin = default, Vector2 insetMax = default)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = insetMin; rect.offsetMax = insetMax;
        return rect;
    }

    public static RectTransform Box(string name, Transform parent, Vector2 min, Vector2 max, Color color, Vector2 insetMin = default, Vector2 insetMax = default)
    {
        var rect = Rect(name, parent, min, max, insetMin, insetMax);
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    public static Text Text(string name, Transform parent, string value, int size = 16, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var text = Rect(name, parent, Vector2.zero, Vector2.one).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size; text.text = value; text.color = Color.white;
        text.alignment = alignment; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        return text;
    }

    public static Button Button(string name, Transform parent, string label, Vector2 min, Vector2 max, UnityAction click, Color? color = null)
    {
        var button = ButtonFrame(name, parent, min, max, color ?? Tile, 3);
        var text = Text("Label", button.transform, label, 16, TextAnchor.MiddleCenter);
        text.color = color.HasValue ? Color.white : Ink;
        if (click != null) button.onClick.AddListener(click);
        return button;
    }

    public static Button ButtonFrame(string name, Transform parent, Vector2 min, Vector2 max, Color color, float inset = 0)
    {
        var rect = Box(name, parent, min, max, color, Vector2.one * inset, -Vector2.one * inset);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        return button;
    }

    public static Canvas Canvas(string name, int order)
    {
        EnsureEventSystem();
        var canvas = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
