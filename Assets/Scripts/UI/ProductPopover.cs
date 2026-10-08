using System;
using UnityEngine;

/// <summary>
/// 상품 팝오버. 선택된 ProductTarget 위에 패널을 띄우고,
/// 패널 안의 ColorButton들이 그 상품의 색상을 바꾸도록 연결한다.
/// 이 컴포넌트는 항상 활성인 오브젝트(Canvas 등)에 붙이고, panel만 켜고 끈다.
/// 패널 안에 상품명·가격 표시 자리(TMP_Text)를 두고, 내용은 이후 단계에서 채운다.
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
        if (screenPoint.z < 0f) // 카메라 뒤쪽
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
    }
}
