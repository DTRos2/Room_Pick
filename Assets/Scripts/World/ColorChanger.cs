using UnityEngine;

/// <summary>
/// 오브젝트의 지정한 머티리얼 슬롯 색상을 변경한다.
/// 인스턴스 머티리얼을 사용하므로 원본 머티리얼 에셋은 수정되지 않는다.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ColorChanger : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Tooltip("색상을 바꿀 머티리얼 슬롯 번호")]
    [SerializeField] private int materialIndex = 0;

    private Material _material;
    private Color _originalColor;

    private void Awake()
    {
        Renderer targetRenderer = GetComponent<Renderer>();
        Material[] materials = targetRenderer.materials; // 인스턴스 복제본

        if (materialIndex < 0 || materialIndex >= materials.Length)
        {
            Debug.LogError($"[ColorChanger] 머티리얼 슬롯 {materialIndex}이(가) 없습니다: {name}", this);
            enabled = false;
            return;
        }

        _material = materials[materialIndex];
        _originalColor = _material.GetColor(BaseColorId);
    }

    /// <summary>색상이 바뀔 때마다 불린다. 어떤 색이 선택됐는지 표시하는 UI가 듣는다.</summary>
    public event System.Action Changed;

    /// <summary>지금 적용된 색상. 머티리얼이 없으면 투명이다.</summary>
    public Color CurrentColor => _material != null ? _material.GetColor(BaseColorId) : Color.clear;

    /// <summary>처음 색상</summary>
    public Color OriginalColor => _originalColor;

    public void SetColor(Color color)
    {
        if (_material == null) return;
        _material.SetColor(BaseColorId, color);
        Changed?.Invoke();
    }

    public void ResetColor()
    {
        SetColor(_originalColor);
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
