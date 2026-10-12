using System;
using UnityEngine;

/// <summary>
/// 상품 팝오버. 선택된 ProductTarget 위에 패널을 띄우고,
/// 패널 안의 ColorButton들이 그 상품의 색상을 바꾸도록 연결한다.
/// 이 컴포넌트는 항상 활성인 오브젝트(Canvas 등)에 붙이고, panel만 켜고 끈다.
/// </summary>
public class ProductPopover : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private ColorButton[] colorButtons;
    [SerializeField] private Camera worldCamera;
    [Tooltip("앵커 지점에서 패널을 띄울 화면 픽셀 간격")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 20f);

    private ProductTarget _current;

    /// <summary>팝오버가 열릴 때(대상 오브젝트를 누를 때마다) 호출된다.</summary>
    public event Action<ProductTarget> Opened;

    /// <summary>팝오버가 닫힐 때 호출된다.</summary>
    public event Action Closed;

    /// <summary>색상 버튼이 들어 있는 패널. 다른 UI를 이 패널 위에 붙일 때 기준으로 쓴다.</summary>
    public RectTransform Panel => panel;

    /// <summary>
    /// 패널 바닥에서 색상 버튼 줄의 위쪽 끝까지의 높이. 그 위 공간에 다른 UI를 넣을 때 기준으로 쓴다.
    /// 버튼이 없으면 -1이다.
    /// </summary>
    public float MeasureButtonsTop()
    {
        if (colorButtons == null || colorButtons.Length == 0) return -1f;

        Canvas.ForceUpdateCanvases(); // 패널이 켜진 직후에는 버튼 격자 배치가 아직일 수 있다.
        Vector3[] corners = new Vector3[4];
        float top = float.NegativeInfinity;
        foreach (ColorButton button in colorButtons)
        {
            if (button == null) continue;
            ((RectTransform)button.transform).GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
                top = Mathf.Max(top, panel.InverseTransformPoint(corner).y);
        }

        return float.IsNegativeInfinity(top) ? -1f : top - panel.rect.yMin;
    }

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        panel.gameObject.SetActive(false);
    }

    public void Open(ProductTarget target)
    {
        _current = target;
        foreach (ColorButton button in colorButtons)
            button.SetTarget(target.ColorChanger);

        panel.gameObject.SetActive(true);
        UpdatePosition();
        Opened?.Invoke(target);
    }

    public void Close()
    {
        if (_current == null && !panel.gameObject.activeSelf) return;

        _current = null;
        panel.gameObject.SetActive(false);
        Closed?.Invoke();
    }

    private void LateUpdate()
    {
        if (_current != null) UpdatePosition();
    }

    /// <summary>선택된 상품 위에 패널을 두고, 화면 밖으로 나가면 안쪽으로 밀어 넣는다.</summary>
    private void UpdatePosition()
    {
        Vector3 screenPoint = worldCamera.WorldToScreenPoint(_current.PopoverAnchor);
        if (screenPoint.z < 0f)
        {
            panel.gameObject.SetActive(false);
            return;
        }
        panel.gameObject.SetActive(true);

        // 패널 피벗이 아래 중앙(0.5, 0)이라고 가정하고 앵커 위에 놓는다.
        panel.position = (Vector2)screenPoint + screenOffset;

        Vector3[] corners = new Vector3[4];
        panel.GetWorldCorners(corners); // 0: 좌하, 2: 우상 (Overlay Canvas에서는 화면 픽셀 좌표)
        Vector2 shift = Vector2.zero;
        if (corners[0].x < 0f) shift.x = -corners[0].x;
        else if (corners[2].x > Screen.width) shift.x = Screen.width - corners[2].x;
        if (corners[0].y < 0f) shift.y = -corners[0].y;
        else if (corners[2].y > Screen.height) shift.y = Screen.height - corners[2].y;
        panel.position += (Vector3)shift;

        // 글자가 화면 픽셀 경계에 걸쳐 흐려지지 않도록 위치를 정수 픽셀로 맞춘다.
        Vector3 snapped = panel.position;
        panel.position = new Vector3(Mathf.Round(snapped.x), Mathf.Round(snapped.y), snapped.z);
    }
}
