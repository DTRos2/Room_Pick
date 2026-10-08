using UnityEngine;

/// <summary>
/// 클릭하면 팝오버가 열리는 상품 오브젝트. Collider가 있어야 클릭이 감지된다.
/// 팝오버가 이 오브젝트의 색상을 바꿀 수 있도록 ColorChanger를 노출한다.
/// </summary>
[RequireComponent(typeof(ColorChanger), typeof(Collider))]
public class ProductTarget : MonoBehaviour
{
    [Header("상품 검색")]
    [Tooltip("이 오브젝트로 검색할 기본 검색어. 예: 소파. 비워 두면 상품을 검색하지 않는다.")]
    [SerializeField] private string searchKeyword;
    [Tooltip("부품·연관 상품을 거르는 기준. 필수 단어에 검색어(예: 소파, 쇼파)를 넣는다.")]
    [SerializeField] private ProductFilter filter;

    private Collider _collider;

    public SearchKeyword Keyword => new SearchKeyword(searchKeyword);
    public ProductFilter Filter => filter;

    public ColorChanger ColorChanger { get; private set; }

    /// <summary>팝오버를 띄울 기준 월드 좌표(오브젝트 윗면 중앙).</summary>
    public Vector3 PopoverAnchor
    {
        get
        {
            Bounds bounds = _collider.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }
    }

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        ColorChanger = GetComponent<ColorChanger>();
    }
}
