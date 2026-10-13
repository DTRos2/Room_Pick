using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사용 안내창(GuidePanel)을 씬의 Canvas 아래에 만든다. 만든 뒤에는 Hierarchy에서 글과 모양을 직접 고친다.
/// 모서리 둥글기는 각 오브젝트의 RoundedRect Radius, 그림자는 Shadow, ? 버튼 색은 Button의 Colors에서 고친다.
/// 메뉴: RoomPick/안내창 만들기
/// </summary>
public static class GuidePanelSetup
{
    private const float PanelWidth = 960f;
    private const float KeySize = 36f;

    private static readonly Color PanelColor = new Color(0.11f, 0.12f, 0.15f, 0.92f);
    private static readonly Color CellColor = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color KeyFaceColor = new Color(0.93f, 0.94f, 0.96f);
    private static readonly Color KeyBaseColor = new Color(0.55f, 0.58f, 0.66f);
    private static readonly Color KeyTextColor = new Color(0.12f, 0.13f, 0.16f);
    private static readonly Color BodyTextColor = new Color(0.92f, 0.94f, 0.97f);
    private static readonly Color HintTextColor = new Color(0.66f, 0.69f, 0.75f);
    private static readonly Color StartColor = new Color(0.2f, 0.45f, 0.9f);

    /// <summary>항목 하나: 왼쪽 아이콘을 만드는 함수와 한 줄 설명.</summary>
    private static readonly (Action<RectTransform, UguiKit> icon, string text)[] Rows =
    {
        ((box, kit) => { foreach (string k in new[] { "W", "A", "S", "D" }) NewKey(box, kit, k, KeySize); }, "또는 방향키로 이동"),
        ((box, kit) => NewKey(box, kit, "Shift", 72f), "누르면 빨리 걷기"),
        ((box, kit) => NewKey(box, kit, "우클릭", 84f), "누른 채 시점 돌리기"),
        ((box, kit) => NewPill(box, kit, "선택"), "가구 위 버튼으로 선택"),
        ((box, kit) => NewColorDots(box), "색 바꾸기·되돌리기"),
        ((box, kit) => NewProductCard(box), "카드 눌러 상품 보기"),
    };

    [MenuItem("RoomPick/안내창 만들기")]
    private static void Setup()
    {
        if (UnityEngine.Object.FindFirstObjectByType<GuidePanel>() != null)
        {
            Debug.LogWarning("이미 씬에 안내창(GuidePanel)이 있습니다. 지우고 다시 만들려면 먼저 삭제하세요.");
            return;
        }

        ProductPopover popover = UnityEngine.Object.FindFirstObjectByType<ProductPopover>();
        Canvas canvas = popover != null ? popover.GetComponentInParent<Canvas>() : UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("씬에서 Canvas를 찾을 수 없습니다.");
            return;
        }

        // 한글 글꼴은 실행할 때 GuidePanel이 입히므로, 씬에는 기본 글꼴을 둔다.
        UguiKit kit = new UguiKit(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        ProductInfoStyle style = new ProductInfoStyle();

        RectTransform root = UguiKit.NewRect("Guide", canvas.rootCanvas.transform);
        UguiKit.Stretch(root, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        Button helpButton = BuildHelpButton(root, kit);
        RectTransform overlay = BuildOverlay(root, kit, style, out Button startButton);

        GuidePanel guide = root.gameObject.AddComponent<GuidePanel>();
        SerializedObject serialized = new SerializedObject(guide);
        serialized.FindProperty("overlay").objectReferenceValue = overlay.gameObject;
        serialized.FindProperty("startButton").objectReferenceValue = startButton;
        serialized.FindProperty("helpButton").objectReferenceValue = helpButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(root.gameObject, "안내창 만들기");
        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Debug.Log("안내창을 만들었습니다. 씬을 저장하세요.");
    }

    /// <summary>둥근 사각형 오브젝트를 만든다. 클릭을 받지 않는 장식용이 기본이다.</summary>
    private static RoundedRect NewRounded(string name, Transform parent, Color color, float radius)
    {
        GameObject go = UguiKit.NewRect(name, parent).gameObject;
        // 그리려면 CanvasRenderer가 꼭 필요하다. 없으면 클릭 검사에서 오류가 나므로 직접 확인해 붙인다.
        if (go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();
        RoundedRect rounded = go.AddComponent<RoundedRect>();
        rounded.color = color;
        rounded.Radius = radius;
        rounded.raycastTarget = false;
        return rounded;
    }

    private static void SetSize(RectTransform rect, float width, float height)
    {
        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = height;
    }

    private static Button BuildHelpButton(RectTransform parent, UguiKit kit)
    {
        // 정사각형에 반지름을 절반으로 주면 원이 된다. 색은 Button의 Colors가 정한다(그래픽은 흰색).
        RoundedRect background = NewRounded("HelpButton", parent, Color.white, 28f);
        background.raycastTarget = true;
        RectTransform rect = background.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-24f, -24f);
        rect.sizeDelta = new Vector2(56f, 56f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        Color normal = new Color(0.08f, 0.09f, 0.12f, 0.55f);
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = new Color(0.2f, 0.45f, 0.9f, 0.95f);
        colors.pressedColor = new Color(0.15f, 0.35f, 0.75f, 1f);
        colors.selectedColor = normal; // 클릭한 뒤에도 강조색이 남지 않게 한다.
        colors.disabledColor = normal;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        Text label = kit.NewText("Label", rect, 30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        UguiKit.Stretch(label.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        label.text = "?";
        return button;
    }

    /// <summary>화면을 어둡게 덮고 가운데에 안내 카드를 놓는다. 덮개가 클릭을 막아 뒤의 가구는 눌리지 않는다.</summary>
    private static RectTransform BuildOverlay(RectTransform parent, UguiKit kit, ProductInfoStyle style, out Button startButton)
    {
        RectTransform overlay = UguiKit.NewRect("Overlay", parent);
        UguiKit.Stretch(overlay, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        overlay.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        RoundedRect cardBackground = NewRounded("Card", overlay, PanelColor, 16f);
        cardBackground.raycastTarget = true;
        RectTransform card = cardBackground.rectTransform;
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(PanelWidth, 0f);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 그림자는 겹쳐 두 겹으로 둬서 가장자리가 은은하게 번지게 한다.
        AddShadow(card.gameObject, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -4f));
        AddShadow(card.gameObject, new Color(0f, 0f, 0f, 0.14f), new Vector2(0f, -12f));

        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 28, 28);
        layout.spacing = 20f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text title = kit.NewText("Title", card, 36, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        title.text = "모델하우스 둘러보기";

        BuildGrid(card, kit);

        Text hint = kit.NewText("Hint", card, 20, FontStyle.Normal, HintTextColor, TextAnchor.MiddleCenter);
        hint.text = "H 키 · ? 버튼으로 다시 보기";

        // 빈 칸을 하나 끼워 안내문과 시작하기 버튼 사이를 넓힌다. 간격은 Spacer의 Preferred Height로 고친다.
        UguiKit.NewRect("Spacer", card).gameObject.AddComponent<LayoutElement>().preferredHeight = 4f;

        startButton = BuildStartButton(card, kit, style);
        return overlay;
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    /// <summary>항목 6개를 2열(3×2) 격자로 놓는다. 칸 크기와 간격은 GridLayoutGroup에서 고친다.</summary>
    private static void BuildGrid(RectTransform card, UguiKit kit)
    {
        RectTransform grid = UguiKit.NewRect("Grid", card);
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(436f, 72f);
        layout.spacing = new Vector2(24f, 16f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;
        layout.childAlignment = TextAnchor.UpperCenter;

        foreach ((Action<RectTransform, UguiKit> icon, string text) in Rows)
            BuildCell(grid, kit, icon, text);
    }

    /// <summary>한 칸: 왼쪽에 키캡·아이콘, 오른쪽에 짧은 설명.</summary>
    private static void BuildCell(RectTransform grid, UguiKit kit, Action<RectTransform, UguiKit> icon, string text)
    {
        RoundedRect cell = NewRounded("Cell_" + text, grid, CellColor, 12f);
        HorizontalLayoutGroup layout = cell.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 12, 8, 8);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        // 아이콘 폭을 고정해 두어야 설명 글이 모든 칸에서 같은 자리에서 시작한다.
        RectTransform iconBox = UguiKit.NewRect("Icon", cell.rectTransform);
        LayoutElement iconSize = iconBox.gameObject.AddComponent<LayoutElement>();
        iconSize.minWidth = iconSize.preferredWidth = 164f;
        iconSize.flexibleWidth = 0f;
        HorizontalLayoutGroup iconLayout = iconBox.gameObject.AddComponent<HorizontalLayoutGroup>();
        iconLayout.spacing = 4f;
        iconLayout.childAlignment = TextAnchor.MiddleLeft;
        iconLayout.childControlWidth = iconLayout.childControlHeight = true;
        iconLayout.childForceExpandWidth = iconLayout.childForceExpandHeight = false;
        icon(iconBox, kit);

        Text label = kit.NewText("Text", cell.rectTransform, 20, FontStyle.Normal, BodyTextColor, TextAnchor.MiddleLeft);
        label.text = text;
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    /// <summary>키캡: 어두운 밑판 위에 밝은 면을 살짝 위로 올려, 눌리는 키처럼 보이게 한다.</summary>
    private static void NewKey(RectTransform parent, UguiKit kit, string key, float width)
    {
        RoundedRect keyBase = NewRounded("Key_" + key, parent, KeyBaseColor, 8f);
        SetSize(keyBase.rectTransform, width, KeySize);

        RoundedRect face = NewRounded("Face", keyBase.rectTransform, KeyFaceColor, 8f);
        UguiKit.Stretch(face.rectTransform, Vector2.zero, Vector2.one, 0f, 3f, 0f, 0f);

        Text label = kit.NewText("Label", face.rectTransform, 18, FontStyle.Bold, KeyTextColor, TextAnchor.MiddleCenter);
        UguiKit.Stretch(label.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        label.text = key;
    }

    /// <summary>가구 위에 뜨는 '선택' 버튼을 흉내 낸 알약 모양.</summary>
    private static void NewPill(RectTransform parent, UguiKit kit, string text)
    {
        RoundedRect pill = NewRounded("Pill", parent, Color.white, 18f);
        SetSize(pill.rectTransform, 72f, 36f);

        Text label = kit.NewText("Label", pill.rectTransform, 18, FontStyle.Bold, KeyTextColor, TextAnchor.MiddleCenter);
        UguiKit.Stretch(label.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        label.text = text;
    }

    private static void NewColorDots(RectTransform parent)
    {
        Color[] colors =
        {
            new Color(0.93f, 0.36f, 0.33f),
            new Color(0.96f, 0.78f, 0.28f),
            new Color(0.32f, 0.66f, 0.96f),
        };
        foreach (Color color in colors)
            SetSize(NewRounded("Dot", parent, color, 14f).rectTransform, 28f, 28f);
    }

    /// <summary>썸네일과 글줄이 있는 상품 카드 모양.</summary>
    private static void NewProductCard(RectTransform parent)
    {
        RoundedRect card = NewRounded("ProductCard", parent, Color.white, 6f);
        SetSize(card.rectTransform, 76f, 40f);

        RoundedRect thumb = NewRounded("Thumb", card.rectTransform, new Color(0.78f, 0.82f, 0.9f), 4f);
        thumb.rectTransform.anchorMin = thumb.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        thumb.rectTransform.pivot = new Vector2(0f, 0.5f);
        thumb.rectTransform.anchoredPosition = new Vector2(6f, 0f);
        thumb.rectTransform.sizeDelta = new Vector2(26f, 26f);

        for (int i = 0; i < 2; i++)
        {
            RoundedRect line = NewRounded("Line", card.rectTransform, new Color(0.65f, 0.68f, 0.75f), 2f);
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            line.rectTransform.pivot = new Vector2(0f, 0.5f);
            line.rectTransform.anchoredPosition = new Vector2(38f, i == 0 ? 6f : -6f);
            line.rectTransform.sizeDelta = new Vector2(i == 0 ? 30f : 20f, 5f);
        }
    }

    private static Button BuildStartButton(RectTransform card, UguiKit kit, ProductInfoStyle style)
    {
        RoundedRect background = NewRounded("StartButton", card, StartColor, 12f);
        background.raycastTarget = true;
        RectTransform rect = background.rectTransform;
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;

        Button button = rect.gameObject.AddComponent<Button>();
        UguiKit.ApplyHover(button, background, style);

        Text label = kit.NewText("Label", rect, style.cardTitleFont, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        UguiKit.Stretch(label.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        label.text = "시작하기";
        return button;
    }
}
