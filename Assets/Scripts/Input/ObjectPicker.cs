using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 팝오버가 열리는 가구마다 선택 버튼을 띄우고(UI-02), 빈 곳을 클릭하면 팝오버를 닫는다.
/// 팝오버는 선택 버튼을 눌러서 연다.
/// </summary>
public class ObjectPicker : MonoBehaviour
{
    [SerializeField] private Camera pickCamera;
    [Tooltip("비워 두면 팝오버가 있는 Canvas 아래에 코드로 만든다.")]
    [SerializeField] private SelectButtonLayer selectButtons;
    [SerializeField] private ProductPopover popover;
    [SerializeField] private LayerMask pickMask = ~0;

    private void Awake()
    {
        if (pickCamera == null) pickCamera = Camera.main;
        if (selectButtons == null) selectButtons = SelectButtonLayer.Create(popover, pickCamera);
    }

    private void Update()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        // 선택 버튼·팝오버 등 UI 위 클릭은 각 UI가 처리한다.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // 빈 곳(상품이 아닌 곳) 클릭 시 팝오버를 닫는다.
        if (!IsProductAt(pointer.position.ReadValue())) popover.Close();
    }

    private bool IsProductAt(Vector2 screenPosition)
    {
        Ray ray = pickCamera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, pickMask)
            && hit.collider.GetComponentInParent<ProductTarget>() != null;
    }
}
