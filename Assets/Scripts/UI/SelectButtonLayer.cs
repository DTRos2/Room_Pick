using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 실행하면 팝오버가 열리는 모든 ProductTarget 위에 선택 버튼을 항상 띄운다(UI-02).
/// 사용자가 한눈에 색상을 바꿀 수 있는 가구를 알 수 있고, 버튼을 누르면 그 가구의 팝오버가 열린다.
/// 벽 같은 다른 물체에 가려진 가구의 버튼과, 팝오버가 열려 있는 가구의 버튼은 숨긴다.
/// 버튼 UI는 코드로 만들므로 씬에 따로 배치하지 않는다.
/// </summary>
public class SelectButtonLayer : MonoBehaviour
{
    private static readonly Vector2 ButtonSize = new Vector2(120f, 48f);
    private const string Label = "선택";

    private sealed class Marker
    {
        public ProductTarget Target;
        public RectTransform Rect;
    }

    private readonly List<Marker> _markers = new List<Marker>();
    private ProductPopover _popover;
    private Camera _worldCamera;
    private ProductTarget _openTarget;

    /// <summary>팝오버가 있는 Canvas 아래에, 씬의 모든 ProductTarget마다 선택 버튼을 만든다.</summary>
    public static SelectButtonLayer Create(ProductPopover popover, Camera worldCamera)
    {
        RectTransform root = UguiKit.NewRect("SelectButtons", popover.transform);
        root.SetAsFirstSibling(); // 팝오버 패널 뒤에 그려서 패널을 가리지 않게 한다.

        SelectButtonLayer layer = root.gameObject.AddComponent<SelectButtonLayer>();
        layer.Initialize(popover, worldCamera);
        return layer;
    }

    private void Initialize(ProductPopover popover, Camera worldCamera)
    {
        _popover = popover;
        _worldCamera = worldCamera != null ? worldCamera : Camera.main;

        UguiKit kit = UguiKit.CreateDefault();
        ProductInfoStyle style = new ProductInfoStyle();
        foreach (ProductTarget target in FindObjectsByType<ProductTarget>(FindObjectsSortMode.None))
            _markers.Add(CreateMarker(target, kit, style));

        _popover.Opened += OnPopoverOpened;
        _popover.Closed += OnPopoverClosed;
    }

    private void OnDestroy()
    {
        if (_popover == null) return;
        _popover.Opened -= OnPopoverOpened;
        _popover.Closed -= OnPopoverClosed;
    }

    private Marker CreateMarker(ProductTarget target, UguiKit kit, ProductInfoStyle style)
    {
        RectTransform rect = UguiKit.NewRect("SelectButton_" + target.name, transform);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = ButtonSize;

        Image background = rect.gameObject.AddComponent<Image>();
        background.color = style.cardBackground;
        Button button = rect.gameObject.AddComponent<Button>();
        UguiKit.ApplyHover(button, background, style);
        button.onClick.AddListener(() => _popover.Open(target));

        Text text = kit.NewText("Label", rect, style.cardTitleFont, FontStyle.Bold, style.darkText, TextAnchor.MiddleCenter);
        UguiKit.Stretch(text.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        text.text = Label;

        return new Marker { Target = target, Rect = rect };
    }

    private void OnPopoverOpened(ProductTarget target) => _openTarget = target;

    private void OnPopoverClosed() => _openTarget = null;

    private void LateUpdate()
    {
        foreach (Marker marker in _markers) UpdateMarker(marker);
    }

    private void UpdateMarker(Marker marker)
    {
        ProductTarget target = marker.Target;
        Vector3 screenPoint = Vector3.zero;
        bool visible = target != null && target != _openTarget && TryGetScreenPoint(target, out screenPoint);
        marker.Rect.gameObject.SetActive(visible);
        if (!visible) return;

        marker.Rect.position = new Vector3(Mathf.Round(screenPoint.x), Mathf.Round(screenPoint.y), 0f);
    }

    /// <summary>카메라 앞에 있고 다른 물체에 가려지지 않은 가구만 화면 좌표를 돌려준다.</summary>
    private bool TryGetScreenPoint(ProductTarget target, out Vector3 screenPoint)
    {
        Vector3 center = target.CenterPoint;
        screenPoint = _worldCamera.WorldToScreenPoint(center);
        if (screenPoint.z < 0f) return false;

        if (Physics.Linecast(_worldCamera.transform.position, center, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<ProductTarget>() == target;
        return true;
    }
}
