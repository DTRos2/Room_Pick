using UnityEngine;

/// <summary>
/// 클릭하면 팝오버가 열리는 상품 오브젝트. Collider가 있어야 클릭이 감지된다.
/// 팝오버가 이 오브젝트의 색상을 바꿀 수 있도록 ColorChanger를 노출한다.
/// </summary>
[RequireComponent(typeof(ColorChanger), typeof(Collider))]
public class ProductTarget : MonoBehaviour
{
    private Collider _collider;

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
