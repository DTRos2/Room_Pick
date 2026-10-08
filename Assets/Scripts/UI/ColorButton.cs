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

    /// <summary>팝오버가 열릴 때 색상을 바꿀 대상을 교체한다.</summary>
    public void SetTarget(ColorChanger newTarget)
    {
        target = newTarget;
    }

    private void Awake()
    {
        ApplyVisual();
        GetComponent<Button>().onClick.AddListener(OnClick);
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