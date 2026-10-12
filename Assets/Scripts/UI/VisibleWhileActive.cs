using UnityEngine;

/// <summary>
/// 기준 오브젝트가 화면에 보일 때(activeInHierarchy)만 이 오브젝트도 보이게 한다.
/// 부모-자식이 아니라서 함께 꺼지지 않는 UI를 묶어 둘 때 쓴다. CanvasGroup이 필요하다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class VisibleWhileActive : MonoBehaviour
{
    public GameObject Source;

    CanvasGroup _group;

    void OnEnable() => Apply();
    void Update() => Apply();

    void Apply()
    {
        if (_group == null) _group = GetComponent<CanvasGroup>();
        bool visible = Source != null && Source.activeInHierarchy;
        _group.alpha = visible ? 1f : 0f;
        _group.blocksRaycasts = visible;
    }
}
