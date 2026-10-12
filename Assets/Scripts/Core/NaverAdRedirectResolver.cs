using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

/// <summary>
/// SerpApi(engine=naver)가 주는 ader.naver.com 광고 추적 주소는 그대로 열면 네이버 로그인이 필요하다.
/// 3xx 응답의 Location을 한 단계씩 따라가 판매처의 직접 주소를 찾는다.
/// 자동 리다이렉트를 끄고 직접 따라가므로 최종 페이지는 내려받지 않는다.
/// </summary>
public sealed class NaverAdRedirectResolver : IDirectUrlResolver
{
    const int MaxHops = 5;
    const int TimeoutSeconds = 8;
    const string AdHost = "ader.naver.com";
    const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";

    /// <summary>실패하면 마지막으로 알아낸 주소(없으면 받은 주소)를 돌려준다.</summary>
    public async Task<string> ResolveAsync(string url)
    {
        var current = url;
        for (int hop = 0; hop < MaxHops; hop++)
        {
            // 광고 주소만 따라간다. 그 밖의 주소(판매처, 네이버 쇼핑 searchGate 등)는 요청해도
            // 응답이 없어 시간 초과까지 기다리게 되므로 기다리지 않고 그대로 쓴다.
            if (!IsAdRedirectHost(current)) return current;

            using var request = UnityWebRequest.Get(current);
            request.redirectLimit = 0;
            request.timeout = TimeoutSeconds;
            request.SetRequestHeader("User-Agent", BrowserUserAgent);

            var completion = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += _ => completion.TrySetResult(true);
            await completion.Task;

            // redirectLimit=0이면 Unity가 ConnectionError로 표시하지만, 응답 헤더는 읽을 수 있다.
            var location = request.GetResponseHeader("Location");
            if (string.IsNullOrEmpty(location)) return current;

            // 상대 주소일 수 있으므로 현재 주소를 기준으로 절대 주소로 바꾼다.
            if (!Uri.TryCreate(new Uri(current), location, out var next) || !WebUrl.IsOpenable(next.AbsoluteUri))
                return current;

            current = next.AbsoluteUri;
        }
        return current;
    }

    static bool IsAdRedirectHost(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && uri.Host.Equals(AdHost, StringComparison.OrdinalIgnoreCase);
    }
}
