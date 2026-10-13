using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼을 누르면 대상 ColorChanger에 지정한 색상을 적용한다.
/// resetToOriginal이 켜져 있으면 색상 대신 원래 색상으로 되돌린다.
/// 버튼 이미지 색은 color 값 하나로 관리되며, 에디터에서도 즉시 반영된다.
/// 리셋 버튼은 흰 배경에 되돌리기 아이콘(resetIcon)을 표시해 일반 색상 버튼과 구분한다.
/// </summary>
[RequireComponent(typeof(Button), typeof(Image))]
public class ColorButton : MonoBehaviour
{
    [SerializeField] private ColorChanger target;
    [SerializeField] private Color color = Color.white;
    [Tooltip("켜면 색상 대신 원래 색상으로 되돌리는 버튼으로 동작")]
    [SerializeField] private bool resetToOriginal = false;

    [Header("리셋 버튼 표시")]
    [Tooltip("리셋 버튼일 때 보여줄 되돌리기(↺) 아이콘. 버튼의 자식 오브젝트를 연결")]
    [SerializeField] private GameObject resetIcon;
    [Tooltip("리셋 버튼의 배경색")]
    [SerializeField] private Color resetBackground = Color.white;

    // 버튼 사이 간격(8px) 안에 들어가도록 링은 버튼 바깥 5px까지만 나가고, 선 두께는 2.5px로 한다.
    private const float RingGap = 5f;
    private const float RingThickness = 2.5f;

    private GameObject _selectedRing;

    /// <summary>팝오버가 열릴 때 색상을 바꿀 대상을 교체한다.</summary>
    public void SetTarget(ColorChanger newTarget)
    {
        if (target != null) target.Changed -= RefreshSelected;
        target = newTarget;
        if (target != null) target.Changed += RefreshSelected;
        RefreshSelected();
    }

    private void Awake()
    {
        ApplyVisual();
        GetComponent<Button>().onClick.AddListener(OnClick);

        BuildSelectedRing();
        if (target != null) target.Changed += RefreshSelected;
        RefreshSelected();
    }

    private void OnDestroy()
    {
        if (target != null) target.Changed -= RefreshSelected;
    }

    /// <summary>
    /// 선택된 색을 알리는 흰 링. 버튼 바깥으로 한 칸 띄워 그려서, 흰색·밝은 색 버튼에서도 눈에 띈다.
    /// 버튼의 색·크기·위치는 건드리지 않고 링만 켜고 끈다.
    /// </summary>
    private void BuildSelectedRing()
    {
        GameObject ring = new GameObject("SelectedRing", typeof(RectTransform));
        ring.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)ring.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-RingGap, -RingGap);
        rect.offsetMax = new Vector2(RingGap, RingGap);

        RoundedRect rounded = ring.AddComponent<RoundedRect>();
        rounded.color = Color.white;
        rounded.Radius = 10f;
        rounded.RingThickness = RingThickness;
        rounded.raycastTarget = false;
        _selectedRing = ring;
    }

    /// <summary>지금 적용된 색이 이 버튼의 색이면(리셋 버튼은 원래 색이면) 링을 켠다.</summary>
    private void RefreshSelected()
    {
        // 팝오버가 열리기 전(Awake 전)에도 SetTarget이 불리므로 링이 아직 없을 수 있다.
        if (_selectedRing == null) return;

        bool selected = target != null
            && IsSame(target.CurrentColor, resetToOriginal ? target.OriginalColor : color);
        _selectedRing.SetActive(selected);
    }

    private static bool IsSame(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
    }

#if UNITY_EDITOR
    /// <summary>Inspector에서 값을 바꿀 때마다 버튼 모양을 즉시 갱신한다.</summary>
    private void OnValidate()
    {
        // OnValidate 안에서 SetActive를 바로 호출하면 경고가 나므로 한 프레임 미룬다.
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) ApplyVisual();
        };
    }
#endif

    /// <summary>color / resetToOriginal 값에 맞춰 버튼 이미지와 아이콘을 맞춘다.</summary>
    private void ApplyVisual()
    {
        if (TryGetComponent(out Image image))
            image.color = resetToOriginal ? resetBackground : color;

        if (resetIcon != null)
            resetIcon.SetActive(resetToOriginal);
    }

    private void OnClick()
    {
        if (target == null)
        {
            Debug.LogError("[ColorButton] target이 지정되지 않았습니다.", this);
            return;
        }
        if (resetToOriginal) target.ResetColor();
        else target.SetColor(color);
    }
}