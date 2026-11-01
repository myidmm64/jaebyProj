using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static BagUIBuilder;

public static class SequenceUIBuilder
{
    const string ScenePath = "Assets/Scenes/TestStage.unity";
    const string PrefabPath = "Assets/3.Prefabs/UI/BagCanvas.prefab";
    static readonly Color Ink = new Color32(18, 19, 23, 255);
    static readonly Color Surface = new Color32(32, 34, 39, 255);
    static readonly Color Line = new Color32(75, 78, 81, 255);
    static readonly Color Lime = new Color32(215, 246, 80, 255);
    static readonly Color Paper = new Color32(242, 240, 225, 255);
    static readonly Color Muted = new Color32(156, 158, 158, 255);
    static Font bold;

    public static void Create(bool upgrade)
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var existing = UnityEngine.Object.FindFirstObjectByType<BagPanelUI>();
        if (!upgrade && (existing != null || File.Exists(PrefabPath))) throw new InvalidOperationException("UI already exists; use Upgrade Bag UI.");
        if (upgrade && existing == null) throw new InvalidOperationException("No existing UI to upgrade.");
        var regular = AssetDatabase.LoadAssetAtPath<Font>("Assets/4.UI/Fonts/NanumGothic-Regular.ttf");
        bold = AssetDatabase.LoadAssetAtPath<Font>("Assets/4.UI/Fonts/NanumGothic-Bold.ttf");
        if (regular == null || bold == null || !regular.HasCharacter('가')) throw new InvalidOperationException("Korean fonts missing.");
        SetFont(regular);
        var root = existing != null ? existing.gameObject : new GameObject("Sequence Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(BagPanelUI));
        if (PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        root.name = "Sequence Canvas";
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 20;
        root.GetComponent<Canvas>().pixelPerfect = true;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0f;

        var toggle = MakeButton(root.transform, "Sequence Toggle", "TAB   시퀀스", Lime, Ink, 18);
        Place(toggle.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(186, 48));
        var panel = Box(root.transform, "Sequence Drawer", Ink);
        Place(panel.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(-88, -640), new Vector2(-224, 624));
        var group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;
        group.interactable = group.blocksRaycasts = false;
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = Line;
        outline.effectDistance = new Vector2(2, -2);
        var header = Box(panel.transform, "Paper Header", Paper);
        Stretch(header.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -64), Vector2.zero);
        At(header.transform, "Eyebrow", "LOADOUT / 01", 14, Ink, 24, -3, 280, 20, true);
        At(header.transform, "Title", "마법 시퀀스", 28, Ink, 22, -23, 470, 36, true);
        At(header.transform, "Mode", "배치 순서가 연속 마법을 만듭니다", 16, Ink, 490, -23, 390, 32);
        var close = MakeButton(header.transform, "Close", "닫기  X", Ink, Paper, 16);
        Place(close.GetComponent<RectTransform>(), Vector2.one, Vector2.one, Vector2.one, new Vector2(-16, -10), new Vector2(100, 44));
        var status = Box(panel.transform, "Sequence Status", Lime);
        Stretch(status.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -98), new Vector2(0, -64));
        At(status.transform, "Status", "SEQUENCE LINK   /   12 SLOTS", 16, Ink, 24, -3, 480, 28, true);
        At(status.transform, "Route", "상단 > 하단   |   순서 연결 미리보기", 14, Ink, 610, -3, 420, 28).alignment = TextAnchor.MiddleRight;

        var board = new GameObject("Sequence Slots", typeof(RectTransform));
        board.transform.SetParent(panel.transform, false);
        Place(board.GetComponent<RectTransform>(), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -110), new Vector2(800, 216));
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 6; col++)
            {
                int order = row == 0 ? col + 1 : 12 - col;
                Slot(board.transform, order, col * 138, -row * 116);
                if (col < 5) At(board.transform, $"Link {row}-{col}", row == 0 ? ">" : "<", 24, Lime, col * 138 + 108, -row * 116 - 34, 20, 32, true).alignment = TextAnchor.MiddleCenter;
            }
        At(board.transform, "Row Link", "v", 14, Lime, 728, -99, 24, 18, true).alignment = TextAnchor.MiddleCenter;
        OwnedBag(root, panel.transform);
        var footer = Box(panel.transform, "Footer", Surface);
        Stretch(footer.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 42));
        At(footer.transform, "Instruction", "보유 카드 / 실행 순서는 서로 구분됩니다", 16, Paper, 24, -5, 660, 32);
        At(footer.transform, "Key Hint", "[ TAB ]  전장으로 돌아가기", 15, Lime, 690, -5, 340, 32).alignment = TextAnchor.MiddleRight;
        var data = new SerializedObject(root.GetComponent<BagPanelUI>());
        Set(data, "panel", panel.rectTransform);
        Set(data, "panelGroup", group);
        Set(data, "toggleButton", toggle);
        Set(data, "closeButton", close);
        data.ApplyModifiedPropertiesWithoutUndo();
        PassiveHUD(root);
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        Directory.CreateDirectory("Assets/3.Prefabs/UI");
        AssetDatabase.Refresh();
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(root.scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("SEQUENCE_UI_BUILD_OK: large sequence drawer, acquired-passive HUD, bundled Korean fonts.");
    }

    static void Slot(Transform parent, int index, float x, float y)
    {
        var slot = Box(parent, $"Slot {index:00}", index == 1 ? Lime : Line);
        Place(slot.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, y), new Vector2(100, 100));
        var inner = Box(slot.transform, "Inset", Surface);
        Stretch(inner.rectTransform, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
        var tag = Box(slot.transform, "Order Tag", index == 1 ? Lime : Paper);
        Place(tag.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(42, 28));
        At(tag.transform, "Number", index.ToString("00"), 18, Ink, 0, 0, 42, 28, true).alignment = TextAnchor.MiddleCenter;
        At(slot.transform, "Empty Symbol", "+", 28, new Color32(86, 88, 90, 255), 0, -27, 100, 36).alignment = TextAnchor.MiddleCenter;
        At(slot.transform, "Empty Label", "빈 슬롯", 14, Muted, 0, -70, 100, 25).alignment = TextAnchor.MiddleCenter;
    }

    static void OwnedBag(GameObject root, Transform parent)
    {
        var bag = root.GetComponent<OwnedCardBagUI>() ?? root.AddComponent<OwnedCardBagUI>();
        var box = Box(parent, "Owned Card Bag", Surface);
        Place(box.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1),
            new Vector2(0, -342), new Vector2(-48, 232));
        At(box.transform, "Title", "보유 카드 가방", 20, Paper, 16, -8, 400, 32, true);
        At(box.transform, "Hint", "획득한 카드 보관", 14, Muted, 220, -10, 420, 28);
        var count = At(box.transform, "Count", "00 장", 17, Lime, 0, 0, 120, 30, true);
        Place(count.rectTransform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-16, -10), new Vector2(120, 30));
        count.alignment = TextAnchor.MiddleRight;
        var viewport = Box(box.transform, "Viewport", Ink);
        Stretch(viewport.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -48));
        viewport.gameObject.AddComponent<RectMask2D>();
        var empty = Label(viewport.transform, "Empty Hint", "보유한 카드가 없습니다. 획득한 카드가 이곳에 추가됩니다.", 17, Muted);
        Stretch(empty.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
        empty.alignment = TextAnchor.MiddleCenter;
        var content = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        Place(contentRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(0, 166));
        var layout = content.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = box.gameObject.AddComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewport.rectTransform;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30;
        var template = Box(content.transform, "Card Template", Surface);
        template.rectTransform.sizeDelta = new Vector2(156, 166);
        var element = template.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = 156;
        element.preferredHeight = 166;
        var view = template.gameObject.AddComponent<CardUIView>();
        var title = At(template.transform, "Name", "카드 이름", 16, Paper, 8, -5, 140, 22, true);
        var type = At(template.transform, "Classification", "일반 / 실행", 12, Lime, 8, -28, 140, 18);
        var picture = Box(template.transform, "Artwork Back", Ink);
        Place(picture.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -50), new Vector2(56, 56));
        var art = Box(picture.transform, "Artwork", Color.white);
        Stretch(art.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
        art.preserveAspect = true;
        var fallback = Label(picture.transform, "Fallback", "+", 26, Lime);
        Stretch(fallback.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fallback.alignment = TextAnchor.MiddleCenter;
        At(template.transform, "Range Label", "범위", 12, Muted, 96, -48, 48, 18).alignment = TextAnchor.MiddleCenter;
        var range = new Image[9];
        for (int i = 0; i < range.Length; i++)
        {
            range[i] = Box(template.transform, $"Range {i}", Line);
            Place(range[i].rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(100 + i % 3 * 12, -69 - i / 3 * 12), new Vector2(10, 10));
        }
        var description = At(template.transform, "Effect", "카드 효과", 13, Paper, 8, -110, 140, 34);
        description.alignment = TextAnchor.UpperLeft;
        var life = At(template.transform, "Remaining Turns", "", 12, Muted, 8, -148, 140, 18);
        var viewData = new SerializedObject(view);
        Set(viewData, "title", title);
        Set(viewData, "classification", type);
        Set(viewData, "description", description);
        Set(viewData, "remainingTurns", life);
        Set(viewData, "artwork", art);
        Set(viewData, "fallback", fallback);
        var rangeArray = viewData.FindProperty("rangeCells");
        rangeArray.arraySize = range.Length;
        for (int i = 0; i < range.Length; i++) rangeArray.GetArrayElementAtIndex(i).objectReferenceValue = range[i];
        viewData.ApplyModifiedPropertiesWithoutUndo();
        template.gameObject.SetActive(false);
        var bagData = new SerializedObject(bag);
        Set(bagData, "content", contentRect);
        Set(bagData, "cardTemplate", view);
        Set(bagData, "scroll", scroll);
        Set(bagData, "countLabel", count);
        Set(bagData, "emptyHint", empty.gameObject);
        bagData.ApplyModifiedPropertiesWithoutUndo();
    }

    static void PassiveHUD(GameObject root)
    {
        var hud = root.GetComponent<PassiveItemHUD>() ?? root.AddComponent<PassiveItemHUD>();
        var box = Box(root.transform, "Acquired Passives", Color.clear);
        Place(box.rectTransform, new Vector2(1, 0), Vector2.one, new Vector2(1, .5f), new Vector2(-24, 0), new Vector2(156, -48));
        var heading = Box(box.transform, "Header Back", Ink);
        Stretch(heading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -72), Vector2.zero);
        var accent = Box(box.transform, "Accent", Lime);
        Stretch(accent.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -3), Vector2.zero);
        At(box.transform, "Title", "획득 아이템", 17, Paper, 12, -14, 132, 30, true);
        var count = At(box.transform, "Count", "00", 14, Lime, 12, -46, 132, 22);
        var empty = At(box.transform, "Empty Hint", "획득한 아이템이\n여기에 쌓입니다", 14, Muted, 12, -94, 132, 72);
        empty.alignment = TextAnchor.UpperCenter;
        var viewport = Box(box.transform, "Viewport", Color.clear);
        Stretch(viewport.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 12), new Vector2(-10, -80));
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = new GameObject("Items", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        Place(contentRect, new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), Vector2.zero, Vector2.zero);
        var layout = content.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(40, 40);
        layout.spacing = new Vector2(6, 8);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = box.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport.rectTransform;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28;
        var template = Box(content.transform, "Item Template", Line);
        var view = template.gameObject.AddComponent<PassiveItemIcon>();
        var icon = Box(template.transform, "Icon", Color.white);
        Stretch(icon.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var fallback = Label(template.transform, "Fallback", "유", 22, Lime);
        fallback.font = bold;
        Stretch(fallback.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fallback.alignment = TextAnchor.MiddleCenter;
        var itemData = new SerializedObject(view);
        Set(itemData, "icon", icon);
        Set(itemData, "fallback", fallback);
        itemData.ApplyModifiedPropertiesWithoutUndo();
        template.gameObject.SetActive(false);
        var tooltip = Box(root.transform, "Item Tooltip", Surface);
        Place(tooltip.rectTransform, Vector2.one, Vector2.one, Vector2.one, new Vector2(-196, -24), new Vector2(280, 126));
        tooltip.raycastTarget = false;
        var title = At(tooltip.transform, "Name", "", 18, Lime, 16, -12, 248, 30, true);
        var description = At(tooltip.transform, "Description", "", 15, Paper, 16, -49, 248, 65);
        description.alignment = TextAnchor.UpperLeft;
        tooltip.gameObject.SetActive(false);
        var data = new SerializedObject(hud);
        Set(data, "content", contentRect);
        Set(data, "itemTemplate", view);
        Set(data, "scroll", scroll);
        Set(data, "countLabel", count);
        Set(data, "emptyHint", empty.gameObject);
        Set(data, "tooltip", tooltip.gameObject);
        Set(data, "tooltipTitle", title);
        Set(data, "tooltipDescription", description);
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void RenderPreview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var ui = UnityEngine.Object.FindFirstObjectByType<BagPanelUI>();
        var data = new SerializedObject(ui);
        var panel = (RectTransform)data.FindProperty("panel").objectReferenceValue;
        var group = (CanvasGroup)data.FindProperty("panelGroup").objectReferenceValue;
        var camera = Camera.main;
        var canvas = ui.GetComponent<Canvas>();
        var target = new RenderTexture(1280, 720, 24);
        camera.targetTexture = target;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, 16);
        group.alpha = 1;
        group.interactable = group.blocksRaycasts = true;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        Directory.CreateDirectory("Docs");
        File.WriteAllBytes("Docs/BagUI-open.png", image.EncodeToPNG());
        // Extra preview only: the saved prefab and scene still start with no acquired items.
        var hud = ui.GetComponent<PassiveItemHUD>();
        hud.AddItem(null, "불씨 유물", "획득 목록 표시용 예시입니다.");
        hud.AddItem(null, "마법 장식", "아이템은 획득한 순서대로 추가됩니다.");
        hud.AddItem(null, "부적", "패시브는 시퀀스를 닫아도 표시됩니다.");
        hud.AddItem(null, "재의 결정", "아이콘이 없는 경우 글자로 표시합니다.");
        hud.AddItem(null, "불씨 유물", "같은 아이템을 다시 먹어도 추가됩니다.");
        Canvas.ForceUpdateCanvases();
        camera.Render();
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        File.WriteAllBytes("Docs/BagUI-passives.png", image.EncodeToPNG());
        var owned = ui.GetComponent<OwnedCardBagUI>();
        owned.AddCard(new CardUIData { name = "불씨 축적", description = "카드 효과 표시 예시" });
        owned.AddCard(new CardUIData { name = "폭렬 마법", description = "범위와 효과를 표시", rarity = "희귀",
            range = new[] { new Vector2Int(1, -1), Vector2Int.right, new Vector2Int(1, 1) } });
        owned.AddCard(new CardUIData { name = "잔류 불꽃", description = "남은 턴 표시 예시", remainingTurns = 3 });
        Canvas.ForceUpdateCanvases();
        camera.Render();
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        File.WriteAllBytes("Docs/BagUI-cards.png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previous;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("SEQUENCE_UI_PREVIEW_OK");
    }

    static Text At(Transform parent, string name, string text, int size, Color color, float x, float y, float w, float h, bool heavy = false)
    {
        var label = Label(parent, name, text, size, color);
        if (heavy) label.font = bold;
        label.resizeTextForBestFit = false;
        Place(label.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, y), new Vector2(w, h));
        return label;
    }

    static Button MakeButton(Transform parent, string name, string text, Color background, Color foreground, int size)
    {
        var button = Button(parent, name, text, foreground, size);
        button.GetComponent<Image>().color = background;
        button.GetComponentInChildren<Text>().font = bold;
        return button;
    }

    static void Set(SerializedObject data, string name, UnityEngine.Object value) => data.FindProperty(name).objectReferenceValue = value;
}
