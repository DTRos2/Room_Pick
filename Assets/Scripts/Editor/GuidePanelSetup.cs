using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사용 안내창(GuidePanel)을 씬의 Canvas 아래에 만든다. 만든 뒤에는 Hierarchy에서 글과 모양을 직접 고친다.
/// 메뉴: RoomPick/안내창 만들기
/// </summary>
public static class GuidePanelSetup
{
    private const float PanelWidth = 760f;

    private static readonly (string title, string body)[] Rows =
    {
        ("이동", "W A S D 또는 방향키로 걸어 다닙니다. Shift를 함께 누르면 빨리 걷습니다."),
        ("시점 돌리기", "마우스 오른쪽 버튼을 누른 채 움직이면 시선이 돌아갑니다."),
        ("가구 선택", "가구 위에 보이는 '선택' 버튼을 누르면 그 가구의 색상을 바꾸고 상품 정보를 볼 수 있습니다."),
        ("색상 바꾸기", "나타난 창에서 색상 버튼을 누르면 가구 색이 바뀝니다. 되돌리기 버튼을 누르면 원래 색으로 돌아갑니다."),
        ("상품 보기", "같은 창의 상품 카드를 누르면 판매 페이지가 열립니다. 화살표 버튼으로 다른 상품 목록을 펼칠 수 있습니다."),
        ("창 닫기", "화면의 빈 곳을 클릭하면 가구 창이 닫힙니다."),
    };

    [MenuItem("RoomPick/안내창 만들기")]
    private static void Setup()
    {
        if (Object.FindFirstObjectByType<GuidePanel>() != null)
        {
            Debug.LogWarning("이미 씬에 안내창(GuidePanel)이 있습니다. 지우고 다시 만들려면 먼저 삭제하세요.");
            return;
        }

        ProductPopover popover = Object.FindFirstObjectByType<ProductPopover>();
        Canvas canvas = popover != null ? popover.GetComponentInParent<Canvas>() : Object.FindFirstObjectByType<Canvas>();
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
        Button helpButton = BuildHelpButton(root, kit, style);
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

    private static Button BuildHelpButton(RectTransform parent, UguiKit kit, ProductInfoStyle style)
    {
        RectTransform rect = UguiKit.NewRect("HelpButton", parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-24f, -24f);
        rect.sizeDelta = new Vector2(56f, 56f);

        Image background = rect.gameObject.AddComponent<Image>();
        background.color = style.cardBackground;
        Button button = rect.gameObject.AddComponent<Button>();
        UguiKit.ApplyHover(button, background, style);

        Text label = kit.NewText("Label", rect, style.cardPriceFont, FontStyle.Bold, style.darkText, TextAnchor.MiddleCenter);
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

        RectTransform card = UguiKit.NewRect("Card", overlay);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(PanelWidth, 0f);
        card.gameObject.AddComponent<Image>().color = style.cardBackground;
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 28, 28);
        layout.spacing = 16f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text title = kit.NewText("Title", card, 36, FontStyle.Bold, style.darkText, TextAnchor.MiddleCenter);
        title.text = "모델하우스 둘러보기";

        foreach ((string rowTitle, string body) in Rows)
        {
            Text row = kit.NewText("Row_" + rowTitle, card, 24, FontStyle.Normal, style.darkText, TextAnchor.UpperLeft);
            row.text = "<b>" + rowTitle + "</b>\n" + body;
        }

        Text hint = kit.NewText("Hint", card, 20, FontStyle.Normal, style.grayText, TextAnchor.MiddleCenter);
        hint.text = "이 안내는 H 키 또는 오른쪽 위 ? 버튼으로 다시 볼 수 있습니다.";

        startButton = BuildStartButton(card, kit, style);
        return overlay;
    }

    private static Button BuildStartButton(RectTransform card, UguiKit kit, ProductInfoStyle style)
    {
        RectTransform rect = UguiKit.NewRect("StartButton", card);
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;

        Image background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.2f, 0.45f, 0.9f);
        Button button = rect.gameObject.AddComponent<Button>();
        UguiKit.ApplyHover(button, background, style);

        Text label = kit.NewText("Label", rect, style.cardTitleFont, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        UguiKit.Stretch(label.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        label.text = "시작하기";
        return button;
    }
}
