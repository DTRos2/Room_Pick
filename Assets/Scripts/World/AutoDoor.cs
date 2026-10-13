using UnityEngine;

/// <summary>
/// 플레이어가 가까이 가면 열리고 멀어지면 닫히는 문.
/// 문 메시의 피벗이 경첩 쪽 가장자리에 있다고 보고, 그 가장자리를 축으로 돌려 연다.
/// 문은 플레이어에게서 멀어지는 쪽으로 열린다.
/// </summary>
public class AutoDoor : MonoBehaviour
{
    [Tooltip("비워 두면 CameraController가 붙은 카메라를 찾는다.")]
    [SerializeField] private Transform player;
    [SerializeField, Min(0f)] private float openAngle = 90f;
    [Tooltip("플레이어가 문 중심에서 이 거리 안으로 들어오면 열린다.")]
    [SerializeField, Min(0f)] private float openDistance = 2f;
    [Tooltip("열린 문은 플레이어가 이 거리보다 멀어져야 닫힌다. openDistance보다 커야 문 앞에서 열렸다 닫혔다 떨리지 않는다.")]
    [SerializeField, Min(0f)] private float closeDistance = 2.6f;
    [Tooltip("문이 도는 속도(초당 각도)")]
    [SerializeField, Min(1f)] private float angularSpeed = 120f;

    [Header("열린 자세를 씬에서 정하는 문")]
    [Tooltip("켜면 씬에 놓인 자세를 '열린 자세'로 본다. 시작할 때 문을 Closed Yaw로 돌려 닫아 두고, 다가가면 씬에 놓인 자세로 열린다. 이때 Open Angle과 열리는 방향 판단은 쓰지 않는다.")]
    [SerializeField] private bool authoredPoseIsOpen;
    [Tooltip("닫아 둘 때의 로컬 Y 회전값(도)")]
    [SerializeField] private float closedYaw = -90f;

    private Vector3 _hinge;
    private Vector3 _center;
    private Vector3 _closedPosition;
    private Quaternion _closedRotation;
    private float _authoredOpenAngle; // authoredPoseIsOpen일 때, 닫힌 자세에서 씬 자세까지 돌아야 하는 각도
    private float _angle;  // 지금 열린 각도(부호는 열리는 방향)
    private float _target; // 가려는 각도. 0이면 닫힌다

    private void OnValidate()
    {
        closeDistance = Mathf.Max(closeDistance, openDistance);
    }

    private void Awake()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        Renderer meshRenderer = GetComponent<Renderer>();
        if (filter == null || filter.sharedMesh == null || meshRenderer == null)
        {
            Disable("MeshFilter와 Renderer가 필요합니다.");
            return;
        }

        // 피벗(로컬 원점)에 더 가까운 가로 가장자리가 경첩이다.
        Bounds mesh = filter.sharedMesh.bounds;
        float hingeX = Mathf.Abs(mesh.min.x) < Mathf.Abs(mesh.max.x) ? mesh.min.x : mesh.max.x;
        if (Mathf.Abs(hingeX) > mesh.size.x * 0.2f)
        {
            Disable("피벗이 문 가장자리에 있지 않아 경첩 위치를 알 수 없습니다.");
            return;
        }

        _hinge = transform.TransformPoint(new Vector3(hingeX, 0f, 0f));

        if (authoredPoseIsOpen)
        {
            // 씬에 놓인 자세는 열린 자세다. 경첩을 축으로 돌려 Closed Yaw로 닫아 두고, 열 때 이 자세로 돌아온다.
            float toClosed = Mathf.DeltaAngle(transform.localEulerAngles.y, closedYaw);
            transform.RotateAround(_hinge, Vector3.up, toClosed);
            _authoredOpenAngle = -toClosed;
        }

        _center = meshRenderer.bounds.center;
        _closedPosition = transform.position;
        _closedRotation = transform.rotation;
    }

    private void Start()
    {
        if (player != null) return;

        CameraController controller = FindFirstObjectByType<CameraController>();
        if (controller != null) player = controller.transform;
        else if (Camera.main != null) player = Camera.main.transform;
        else Disable("플레이어(카메라)를 찾지 못했습니다.");
    }

    private void Update()
    {
        float distance = Flat(player.position - _center).magnitude;
        bool opening = _target != 0f;

        if (!opening && distance < openDistance)
            _target = TargetWhenOpen();
        else if (opening && distance > closeDistance)
            _target = 0f;

        float next = Mathf.MoveTowards(_angle, _target, angularSpeed * Time.deltaTime);
        if (next == _angle) return;

        transform.RotateAround(_hinge, Vector3.up, next - _angle);
        _angle = next;

        // 돌릴 때마다 생기는 작은 오차가 쌓이지 않게, 다 닫히면 원래 자리로 되돌린다.
        if (_angle == 0f) transform.SetPositionAndRotation(_closedPosition, _closedRotation);
    }

    /// <summary>열릴 때 가려는 각도. 닫히는 중에 다시 열리면 같은 방향을 유지한다.</summary>
    private float TargetWhenOpen()
    {
        if (authoredPoseIsOpen) return _authoredOpenAngle;
        return _angle != 0f ? Mathf.Sign(_angle) * openAngle : ChooseOpenAngle();
    }

    /// <summary>문 끝이 플레이어에게서 더 멀어지는 쪽(+/-)으로 열린다.</summary>
    private float ChooseOpenAngle()
    {
        Vector3 arm = Flat(_center - _hinge);
        float positive = Flat(_hinge + Quaternion.AngleAxis(openAngle, Vector3.up) * arm - player.position).sqrMagnitude;
        float negative = Flat(_hinge + Quaternion.AngleAxis(-openAngle, Vector3.up) * arm - player.position).sqrMagnitude;
        return positive >= negative ? openAngle : -openAngle;
    }

    private static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    private void Disable(string reason)
    {
        Debug.LogWarning($"{name}: 자동문을 쓸 수 없습니다. {reason}", this);
        enabled = false;
    }
}
