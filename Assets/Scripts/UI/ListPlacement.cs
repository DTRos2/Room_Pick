using UnityEngine;

/// <summary>
/// 이 오브젝트를 기준 패널(Anchor)과 겹치지 않는 자리에 놓는다. 패널 오른쪽에 들어가면 오른쪽에,
/// 안 들어가면 왼쪽에 두고, 높이는 패널 중앙에 맞추되 화면 밖으로 나가지 않게 한다.
/// 팝오버가 움직여도 따라가도록 팝오버(ProductPopover)보다 늦게 실행한다.
/// 화면 픽셀 좌표를 쓰므로 Overlay Canvas를 기준으로 한다.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class ListPlacement : MonoBehaviour
{
    /// <summary>겹치지 않아야 할 기준 패널</summary>
    public RectTransform Anchor;

    /// <summary>패널과 이 오브젝트 사이 간격 (Canvas 기준 단위)</summary>
    public float Gap;

    readonly Vector3[] _corners = new Vector3[4];
    RectTransform _rect;

    void OnEnable() => Place();
    void LateUpdate() => Place();

    void Place()
    {
        if (Anchor == null || !Anchor.gameObject.activeInHierarchy) return;
        if (_rect == null) _rect = (RectTransform)transform;

        Anchor.GetWorldCorners(_corners); // 0: 좌하, 2: 우상
        float anchorLeft = _corners[0].x;
        float anchorRight = _corners[2].x;
        float anchorCenterY = (_corners[0].y + _corners[2].y) * 0.5f;

        float scale = _rect.lossyScale.x;
        float width = _rect.rect.width * scale;
        float height = _rect.rect.height * scale;
        float gap = Gap * scale;

        // 오른쪽을 먼저 보고, 안 들어가면 왼쪽을 본다. 둘 다 안 들어가면 여유가 더 많은 쪽에 둔다.
        bool fitsRight = anchorRight + gap + width <= Screen.width;
        bool fitsLeft = anchorLeft - gap - width >= 0f;
        bool placeRight = fitsRight || (!fitsLeft && Screen.width - anchorRight >= anchorLeft);

        float x;
        if (placeRight)
        {
            _rect.pivot = new Vector2(0f, 0.5f);
            x = Mathf.Max(0f, Mathf.Min(anchorRight + gap, Screen.width - width));
        }
        else
        {
            _rect.pivot = new Vector2(1f, 0.5f);
            x = Mathf.Min(Screen.width, Mathf.Max(anchorLeft - gap, width));
        }

        float halfHeight = height * 0.5f;
        float y = height >= Screen.height
            ? Screen.height * 0.5f
            : Mathf.Clamp(anchorCenterY, halfHeight, Screen.height - halfHeight);

        _rect.position = new Vector3(Mathf.Round(x), Mathf.Round(y), _rect.position.z);
    }
}
