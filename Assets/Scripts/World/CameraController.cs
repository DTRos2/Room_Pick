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
    [Tooltip("카메라가 이 거리보다 가까운 물체는 그리지 않는다. 몸통 반지름보다 충분히 작아야 벽 앞에 멈췄을 때 벽 너머가 보이지 않는다.")]
    [SerializeField, Min(0.01f)] private float nearClip = 0.05f;

    [Header("이동 범위")]
    [Tooltip("이름이 이 글자로 시작하는 오브젝트들의 바깥 경계를 걸어 다닐 수 있는 한계로 삼는다. 벽 충돌을 뚫고 나가도 이 범위 밖으로는 못 간다.")]
    [SerializeField] private string[] boundaryNamePrefixes = { "sm_walls" };
    [Tooltip("벽 앞에서 멈추는 여유 거리. 몸통 표면이 벽에서 이만큼 떨어진 곳에서 멈추고, 바깥 경계에서도 몸통 반지름에 더해 이만큼 안쪽까지만 걷는다.")]
    [SerializeField, Min(0f)] private float wallMargin = 0.1f;

    private CharacterController _controller;
    private Vector2 _walkMin;
    private Vector2 _walkMax;
    private bool _hasWalkArea;
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

        // 벽에 막혀 멈추면 카메라는 벽에서 몸통 반지름만큼 떨어져 있다. near가 그보다 크면 벽이 잘려 너머가 보인다.
        if (TryGetComponent(out Camera eye)) eye.nearClipPlane = nearClip;

        CacheWalkArea();

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
        step.y = eyeHeight - transform.position.y;

        _controller.Move(ClampToWalkArea(BlockAtWalls(step)));
    }

    /// <summary>
    /// 가는 방향으로 몸통만 한 구를 쏘아 벽까지의 거리를 재고, 벽 앞 wallMargin 거리에서 멈추게 한다.
    /// 벽에 닿으면 벽을 따라 미끄러진다. 메시 충돌체는 앞면에서만 막는데, 뒷면에서 오는 경우(앞면이 바깥을 향한 벽)도
    /// 막으려고 이 검사에서만 뒷면도 감지한다.
    /// </summary>
    private Vector3 BlockAtWalls(Vector3 step)
    {
        Vector3 move = new Vector3(step.x, 0f, step.z);
        Vector3 bodyCenter = transform.position + Vector3.up * (bodyHeight * 0.5f - eyeHeight);

        bool previous = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;
        try
        {
            // 벽을 따라 미끄러진 방향에 또 다른 벽이 있을 수 있어 두 번까지 다시 검사한다.
            for (int i = 0; i < 2; i++)
            {
                float distance = move.magnitude;
                if (distance < 1e-5f) break;

                Vector3 direction = move / distance;
                if (!Physics.SphereCast(bodyCenter, bodyRadius, direction, out RaycastHit hit,
                        distance + wallMargin, ~0, QueryTriggerInteraction.Ignore))
                    break;

                float allowed = Mathf.Max(0f, hit.distance - wallMargin);
                Vector3 slide = Vector3.ProjectOnPlane(direction * (distance - allowed), hit.normal);
                move = direction * allowed + new Vector3(slide.x, 0f, slide.z);
            }
        }
        finally
        {
            Physics.queriesHitBackfaces = previous;
        }

        step.x = move.x;
        step.z = move.z;
        return step;
    }

    /// <summary>
    /// 경계 오브젝트(벽)들을 감싸는 영역을 구해, 몸통 반지름과 여유를 뺀 만큼 안쪽을 걸을 수 있는 범위로 정한다.
    /// 벽 충돌만으로는 얇은 벽을 뚫고 나가는 경우가 있어, 벽까지의 거리를 직접 계산해 막는다.
    /// </summary>
    private void CacheWalkArea()
    {
        Bounds bounds = default;
        bool found = false;
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!IsBoundary(renderer.name)) continue;

            if (found) bounds.Encapsulate(renderer.bounds);
            else bounds = renderer.bounds;
            found = true;
        }

        if (!found)
        {
            Debug.LogWarning("이동 범위를 정할 벽 오브젝트를 찾지 못했습니다. boundaryNamePrefixes를 확인하세요.");
            return;
        }

        float inset = bodyRadius + wallMargin;
        _walkMin = new Vector2(bounds.min.x + inset, bounds.min.z + inset);
        _walkMax = new Vector2(bounds.max.x - inset, bounds.max.z - inset);
        _hasWalkArea = _walkMax.x > _walkMin.x && _walkMax.y > _walkMin.y;
    }

    private bool IsBoundary(string objectName)
    {
        foreach (string prefix in boundaryNamePrefixes)
            if (objectName.StartsWith(prefix)) return true;
        return false;
    }

    /// <summary>이번 이동으로 도착할 위치가 걸을 수 있는 범위를 벗어나면 범위 안쪽 끝에서 멈추게 한다.</summary>
    private Vector3 ClampToWalkArea(Vector3 step)
    {
        if (!_hasWalkArea) return step;

        Vector3 position = transform.position;
        step.x = Mathf.Clamp(position.x + step.x, _walkMin.x, _walkMax.x) - position.x;
        step.z = Mathf.Clamp(position.z + step.z, _walkMin.y, _walkMax.y) - position.z;
        return step;
    }
}
