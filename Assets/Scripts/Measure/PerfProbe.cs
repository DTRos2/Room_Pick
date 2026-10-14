#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 발표 자료(NFR-01, NFR-02)의 측정값을 얻기 위한 성능 측정 도구.
/// 에디터와 Development Build에서만 컴파일되므로 일반(Release) 빌드에는 들어가지 않는다.
/// 씬에 따로 놓지 않아도 실행하면 스스로 붙고, 에디터와 빌드(exe) 모두에서 동작한다.
///  - F8: fps 측정 시작/끝. 끝낼 때 평균·최저 fps를 화면과 로그에 남긴다. (카메라를 움직이는 동안 재 둔다)
///  - F9: 화면 표시 켜기/끄기
///  - 선택 버튼을 누르면 상품 카드가 뜰 때까지 걸린 시간을 자동으로 잰다.
///    검색은 프록시(Cloudflare Worker)를 거치므로 "프록시 경유" 시간에는 Worker와 SerpApi 응답이 포함된다.
///    같은 가구를 다시 고르면 클라이언트 캐시로 즉시 뜬다. 프록시도 같은 검색어를 1시간 캐시하므로,
///    프록시 응답 시간을 처음부터 재려면 검색어가 다른 가구를 고르거나 1시간 뒤에 잰다.
///  - F10: 프록시 경유 시간 기록(평균·중앙값·최소·최대) 초기화
/// 로그는 에디터 Console, 빌드에서는 Player.log(%USERPROFILE%\AppData\LocalLow\&lt;회사&gt;\&lt;제품&gt;\Player.log)에 남는다.
/// </summary>
public sealed class PerfProbe : MonoBehaviour
{
    const string LoadingText = "상품을 찾는 중…";
    const float PopoverTimeout = 15f;
    const float CachedThresholdMs = 60f;

    ProductPopover _popover;

    // fps
    float _sampleTime, _fps;
    int _sampleFrames;
    bool _measuring;
    float _measureTime;
    readonly List<float> _frameTimes = new List<float>();
    string _lastFps = "";

    // 팝오버
    bool _waiting;
    float _openedAt, _panelMs;
    bool _panelSeen;
    string _targetName = "";
    string _lastPopover = "";

    // 클라이언트 캐시를 거치지 않은(= 프록시를 호출한) 상품 표시 시간(ms)
    readonly List<float> _proxyMs = new List<float>();

    bool _visible = true;
    GUIStyle _style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        GameObject go = new GameObject("PerfProbe");
        DontDestroyOnLoad(go);
        go.AddComponent<PerfProbe>();
    }

    void Start()
    {
        _popover = FindFirstObjectByType<ProductPopover>();
        if (_popover == null) return;
        _popover.Opened += OnOpened;
        _popover.Closed += OnClosed;
    }

    void OnDestroy()
    {
        if (_popover == null) return;
        _popover.Opened -= OnOpened;
        _popover.Closed -= OnClosed;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.f8Key.wasPressedThisFrame) ToggleMeasure();
            if (keyboard.f9Key.wasPressedThisFrame) _visible = !_visible;
            if (keyboard.f10Key.wasPressedThisFrame)
            {
                _proxyMs.Clear();
                Debug.Log("[PerfProbe] 프록시 경유 시간 기록 초기화");
            }
        }

        TrackFps();
        PollPopover();
    }

    void TrackFps()
    {
        float dt = Time.unscaledDeltaTime;

        // 화면에 보이는 fps는 0.5초마다 갱신해 숫자가 덜 흔들리게 한다.
        _sampleTime += dt;
        _sampleFrames++;
        if (_sampleTime >= 0.5f)
        {
            _fps = _sampleFrames / _sampleTime;
            _sampleTime = 0f;
            _sampleFrames = 0;
        }

        if (!_measuring) return;
        _measureTime += dt;
        _frameTimes.Add(dt);
    }

    void ToggleMeasure()
    {
        if (!_measuring)
        {
            _measuring = true;
            _measureTime = 0f;
            _frameTimes.Clear();
            Debug.Log("[PerfProbe] fps 측정 시작");
            return;
        }

        _measuring = false;
        if (_frameTimes.Count == 0) return;

        // 평균 fps는 fps 값들의 평균이 아니라 (프레임 수 / 걸린 시간)으로 구한다.
        float avg = _frameTimes.Count / _measureTime;
        float worstFrame = 0f;
        foreach (float t in _frameTimes) worstFrame = Mathf.Max(worstFrame, t);

        // 1% low: 가장 느린 1% 프레임의 평균을 fps로 바꾼 값. 순간 끊김을 보여 준다.
        List<float> sorted = new List<float>(_frameTimes);
        sorted.Sort();
        int count = Mathf.Max(1, sorted.Count / 100);
        float sum = 0f;
        for (int i = sorted.Count - count; i < sorted.Count; i++) sum += sorted[i];
        float low1 = 1f / (sum / count);

        _lastFps = $"평균 {avg:0.0} fps / 최저 {1f / worstFrame:0.0} / 1% low {low1:0.0} ({_measureTime:0.0}초, {_frameTimes.Count}프레임)";
        Debug.Log($"[PerfProbe] {_lastFps} · {Screen.width}x{Screen.height} · 품질 {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
    }

    void OnOpened(ProductTarget target)
    {
        _waiting = true;
        _panelSeen = false;
        _openedAt = Time.realtimeSinceStartup;
        _targetName = target != null ? target.name : "?";
    }

    void OnClosed() => _waiting = false;

    /// <summary>
    /// 선택 버튼을 누른 순간부터 카드에 상품(또는 오류·없음 문구)이 보이기까지의 시간을 잰다.
    /// 화면 오브젝트의 켜짐 상태를 읽을 뿐이라 상품 UI 코드는 건드리지 않는다. 썸네일 이미지는 나중에 채워지므로 포함하지 않는다.
    /// </summary>
    void PollPopover()
    {
        if (!_waiting || _popover == null) return;

        float ms = (Time.realtimeSinceStartup - _openedAt) * 1000f;
        if (!_panelSeen && _popover.Panel.gameObject.activeInHierarchy)
        {
            _panelSeen = true;
            _panelMs = ms;
        }

        Transform card = _popover.Panel.Find("ProductInfoView/Card");
        if (card != null)
        {
            Transform content = card.Find("Content");
            Transform status = card.Find("Status");

            if (content != null && content.gameObject.activeInHierarchy)
            {
                bool cached = ms < CachedThresholdMs;
                if (!cached) _proxyMs.Add(ms);
                string kind = cached ? "상품 표시(캐시)" : "상품 표시(프록시 경유)";
                Finish($"{_targetName}: 팝오버 {_panelMs:0}ms · {kind} {ms:0}ms");
                return;
            }

            Text message = status != null && status.gameObject.activeInHierarchy ? status.GetComponent<Text>() : null;
            if (message != null && !string.IsNullOrEmpty(message.text) && message.text != LoadingText)
            {
                Finish($"{_targetName}: 팝오버 {_panelMs:0}ms · 안내 문구 \"{message.text}\" {ms:0}ms");
                return;
            }
        }

        if (ms > PopoverTimeout * 1000f) Finish($"{_targetName}: {PopoverTimeout:0}초 안에 상품이 표시되지 않음");
    }

    void Finish(string result)
    {
        _waiting = false;
        _lastPopover = result;
        Debug.Log("[PerfProbe] " + result);
        if (_proxyMs.Count > 0) Debug.Log("[PerfProbe] 프록시 경유 누적: " + ProxySummary());
    }

    /// <summary>프록시 경유 시간의 횟수·평균·중앙값·최소·최대를 한 줄로 만든다.</summary>
    string ProxySummary()
    {
        List<float> sorted = new List<float>(_proxyMs);
        sorted.Sort();

        float sum = 0f;
        foreach (float v in sorted) sum += v;

        int n = sorted.Count;
        float median = n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2f;
        return $"{n}회 · 평균 {sum / n:0}ms · 중앙값 {median:0}ms · 최소 {sorted[0]:0}ms · 최대 {sorted[n - 1]:0}ms";
    }

    void OnGUI()
    {
        if (!_visible) return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            _style.normal.textColor = Color.yellow;
        }

        List<string> lines = new List<string>
        {
            _measuring
                ? $"● 측정 중 {_measureTime:0.0}초 · 현재 {_fps:0} fps  (F8: 끝내기)"
                : $"FPS {_fps:0}  (F8: 측정 시작, F9: 숨기기)",
        };
        if (_lastFps.Length > 0) lines.Add("마지막 fps: " + _lastFps);
        lines.Add(_waiting ? "팝오버: 측정 중…" : "팝오버: " + (_lastPopover.Length > 0 ? _lastPopover : "선택 버튼을 눌러 보세요"));

        if (_proxyMs.Count > 0) lines.Add("프록시 경유: " + ProxySummary() + "  (F10: 초기화)");

        float height = lines.Count * 26f + 12f;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(8f, 8f, 980f, height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        for (int i = 0; i < lines.Count; i++)
            GUI.Label(new Rect(16f, 12f + i * 26f, 960f, 26f), lines[i], _style);
    }
}
#endif
