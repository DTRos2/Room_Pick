using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 1인칭 걷기 카메라. WASD/방향키로 이동하고, 마우스 우클릭 드래그로 시점을 돌린다.
/// 좌클릭과 커서는 호버/선택(ObjectPicker)이 쓰므로 건드리지 않는다.
/// 벽과 가구는 CharacterController 충돌로 막는다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CameraController : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float sprintMultiplier = 2f;

    [Header("시점")]
    [SerializeField] private float lookSensitivity = 0.12f;
    [SerializeField] private float minPitch = -60f;
    [SerializeField] private float maxPitch = 60f;

    [Header("몸통(충돌)")]
    [SerializeField] private float eyeHeight = 1.65f;
    [SerializeField] private float bodyRadius = 0.3f;
    [SerializeField] private float bodyHeight = 1.8f;

    private CharacterController _controller;
    private float _yaw;
    private float _pitch;
    private bool _looking;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _controller.radius = bodyRadius;
        _controller.height = bodyHeight;
        // 카메라(눈) 위치에서 발밑까지 내려가도록 캡슐 중심을 아래로 둔다.
        _controller.center = new Vector3(0f, bodyHeight * 0.5f - eyeHeight, 0f);
        _controller.stepOffset = 0.1f;

        Vector3 euler = transform.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    private void Update()
    {
        Look();
        Move();
    }

    private void Look()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            // UI 위에서 시작한 드래그는 시점을 돌리지 않는다.
            _looking = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
        }
        else if (mouse.rightButton.wasReleasedThisFrame)
        {
            _looking = false;
        }

        if (!_looking) return;

        Vector2 delta = mouse.delta.ReadValue();
        _yaw += delta.x * lookSensitivity;
        _pitch = Mathf.Clamp(_pitch - delta.y * lookSensitivity, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void Move()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

        // 시선의 위아래 각도와 무관하게 수평으로만 걷는다.
        Vector3 direction = Quaternion.Euler(0f, _yaw, 0f) * new Vector3(x, 0f, z);
        direction = Vector3.ClampMagnitude(direction, 1f);

        float speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? sprintMultiplier : 1f);
        Vector3 step = direction * (speed * Time.deltaTime);
        step.y = eyeHeight - transform.position.y; // 눈높이 고정

        _controller.Move(step);
    }
}
