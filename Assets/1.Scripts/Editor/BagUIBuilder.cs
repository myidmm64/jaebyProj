using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BagUIBuilder
{
    static Font font;
    static readonly Color Surface = new Color32(32, 34, 39, 255);

    public static void SetFont(Font value) => font = value;

    [MenuItem("Tools/Explosive Wizard/Add Prototype Bag UI")]
    public static void Build() => SequenceUIBuilder.Create(false);

    [MenuItem("Tools/Explosive Wizard/Upgrade Bag UI")]
    public static void Upgrade() => SequenceUIBuilder.Create(true);

    public static void RenderPreview() => SequenceUIBuilder.RenderPreview();
    public static Image Box(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public static Text Label(Transform parent, string name, string text, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        return label;
    }

    public static Button Button(Transform parent, string name, string text, Color accent, int size)
    {
        var image = Box(parent, name, Surface);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        // Tab controls the drawer rather than changing button focus.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var label = Label(image.transform, "Label", text, size, accent);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    public static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    public static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
