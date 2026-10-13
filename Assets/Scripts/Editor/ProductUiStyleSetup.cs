using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬의 상품 UI를 어두운 반투명 카드 스타일로 맞춘다.
/// ProductInfoController의 style을 기본값으로 되돌리고, 팝오버 패널의 Image를 둥근 RoundedRect로 바꾼다.
/// 색상 버튼(ColorChgBtn)은 건드리지 않는다. 적용한 뒤에는 Inspector에서 값을 직접 고친다.
/// 메뉴: RoomPick/상품 UI 스타일 적용
/// </summary>
public static class ProductUiStyleSetup
{
    [MenuItem("RoomPick/상품 UI 스타일 적용")]
    private static void Apply()
    {
        ProductInfoController controller = Object.FindFirstObjectByType<ProductInfoController>();
        if (controller == null)
        {
            Debug.LogError("씬에서 ProductInfoController를 찾을 수 없습니다.");
            return;
        }

        ProductPopover popover = controller.GetComponent<ProductPopover>();
        if (popover == null || popover.Panel == null)
        {
            Debug.LogError("ProductPopover 또는 패널을 찾을 수 없습니다.");
            return;
        }

        // 씬에 저장된 style은 옛 값이라, 코드의 기본값으로 다시 채운다. 값은 Inspector에서 고친다.
        ProductInfoStyle style = new ProductInfoStyle();
        Undo.RecordObject(controller, "상품 UI 스타일 적용");
        typeof(ProductInfoController)
            .GetField("style", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, style);
        EditorUtility.SetDirty(controller);

        ApplyPanel(popover.Panel.gameObject, style);
        EnsurePanelHeight(popover, style);

        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Debug.Log("상품 UI 스타일을 적용했습니다. 씬을 저장하세요.");
    }

    /// <summary>
    /// 색상 버튼 위에 "색상" 라벨과 카드가 들어갈 만큼 패널이 높은지 재 보고, 모자라면 위쪽으로 키운다.
    /// 색상 버튼은 패널 가운데 기준으로 놓여 있어서, 패널이 d만큼 커지면 버튼도 d/2 올라간다.
    /// 그만큼 앵커 위치를 내려 화면에서의 버튼 위치는 그대로 둔다.
    /// </summary>
    private static void EnsurePanelHeight(ProductPopover popover, ProductInfoStyle style)
    {
        float buttonsTop = popover.MeasureButtonsTop();
        if (buttonsTop < 0f)
        {
            Debug.LogWarning("색상 버튼을 찾지 못해 패널 높이를 확인하지 못했습니다.");
            return;
        }

        RectTransform panel = popover.Panel;
        float required = buttonsTop + style.gap * 2f + style.labelHeight + style.cardHeight + style.margin;
        float shortage = Mathf.Ceil(required - panel.rect.height);
        if (shortage <= 0f)
        {
            Debug.Log($"패널 높이 {panel.rect.height:0}이(가) 필요한 높이 {required:0} 이상이라 그대로 둡니다.");
            return;
        }

        Undo.RecordObject(panel, "상품 UI 패널 높이 조정");
        panel.sizeDelta += new Vector2(0f, shortage);
        foreach (RectTransform child in panel)
        {
            Undo.RecordObject(child, "상품 UI 패널 높이 조정");
            child.anchoredPosition -= new Vector2(0f, child.anchorMin.y * shortage);
        }

        Debug.Log($"패널 높이를 {shortage:0} 키웠습니다 ({required - shortage:0} → {required:0}). 색상 버튼은 화면 위치를 유지하도록 보정했습니다.");
    }

    /// <summary>패널의 흰 Image를 지우고, 같은 자리에 어두운 둥근 사각형을 둔다.</summary>
    private static void ApplyPanel(GameObject panel, ProductInfoStyle style)
    {
        RoundedRect rounded = panel.GetComponent<RoundedRect>();
        if (rounded == null)
        {
            Image image = panel.GetComponent<Image>();
            if (image != null) Undo.DestroyObjectImmediate(image);
            rounded = Undo.AddComponent<RoundedRect>(panel);
        }

        Undo.RecordObject(rounded, "상품 UI 스타일 적용");
        rounded.color = style.panelColor;
        rounded.Radius = style.cornerRadius;
        rounded.raycastTarget = true; // 패널 위 클릭이 뒤의 가구로 새지 않게 한다.
        EditorUtility.SetDirty(rounded);
    }
}
