using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 화면을 클릭했을 때 ProductTarget이면 팝오버를 열고, 빈 곳이면 닫는다.
/// UI(팝오버 등)를 누른 경우는 무시한다.
/// </summary>
public class ObjectPicker : MonoBehaviour
{
    [SerializeField] private Camera pickCamera;
    [SerializeField] private ProductPopover popover;
    [SerializeField] private LayerMask pickMask = ~0;

    private void Awake()
    {
        if (pickCamera == null) pickCamera = Camera.main;
    }

    private void Update()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        // UI 위 클릭은 버튼 등이 처리한다.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = pickCamera.ScreenPointToRay(pointer.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, pickMask)
            && hit.collider.GetComponentInParent<ProductTarget>() is ProductTarget target)
        {
            popover.Open(target);
        }
        else
        {
            popover.Close();
        }
    }
}
