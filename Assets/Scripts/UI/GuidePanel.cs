using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 실행하면 맨 먼저 나오는 사용 안내창. 이동·시점·가구 선택 방법을 알려 준다.
/// "시작하기"를 누르거나 Enter·Esc로 닫고, H 키나 오른쪽 위 ? 버튼으로 다시 연다.
/// 안내창 UI는 씬에 미리 만들어 두고(메뉴: RoomPick/안내창 만들기), 글과 위치는 Hierarchy에서 고친다.
/// 이 컴포넌트는 항상 활성인 오브젝트에 붙이고, overlay만 켜고 끈다.
/// </summary>
public class GuidePanel : MonoBehaviour
{
    [Tooltip("화면을 덮는 안내창. 켜고 끄는 대상")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private Button startButton;
    [SerializeField] private Button helpButton;

    private void Awake()
    {
        // 씬에는 기본 글꼴로 저장돼 있으므로, 한글이 나오는 글꼴을 실행할 때 입힌다.
        UguiKit.CreateDefault().ApplyFont(transform);

        startButton.onClick.AddListener(Close);
        helpButton.onClick.AddListener(Open);
    }

    private void Open() => overlay.SetActive(true);

    private void Close() => overlay.SetActive(false);

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.hKey.wasPressedThisFrame) overlay.SetActive(!overlay.activeSelf);
        else if (keyboard.escapeKey.wasPressedThisFrame) Close();
        // 안내창이 떠 있을 때만 Enter로 "시작하기"를 대신한다.
        else if (overlay.activeSelf && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) Close();
    }
}
