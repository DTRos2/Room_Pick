using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 상품 링크를 기본 브라우저로 연다. 외부 API가 준 주소이므로 http/https만 허용한다.
/// SerpApi가 주는 링크는 ader.naver.com 광고 추적 주소라 그대로 열면 네이버 로그인이 필요하다.
/// 그래서 클릭한 시점에 리다이렉트를 따라가 판매처의 직접 주소를 찾아 연다.
/// </summary>
public static class ProductLinkOpener
{
    const int MaxHops = 5;
    const int TimeoutSeconds = 8;
    const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";

    /// <summary>리다이렉트를 따라가 직접 주소를 찾아 연다. 찾지 못하면 원래 링크를 연다.</summary>
    public static async Task<bool> OpenAsync(Product product)
    {
        if (product == null || !IsOpenable(product.Link))
        {
            Debug.LogWarning($"[ProductLinkOpener] 열 수 없는 링크입니다: {product?.Link}");
            return false;
        }

        var target = await ResolveDirectUrlAsync(product.Link);
        Application.OpenURL(target);
        return true;
    }

    /// <summary>
    /// 3xx 응답의 Location을 따라 최종 주소를 찾는다. 자동 리다이렉트를 끄고 한 단계씩 직접 따라가므로
    /// 최종 페이지를 내려받지 않는다. 실패하면 마지막으로 알아낸 주소(없으면 원래 링크)를 돌려준다.
    /// </summary>
    static async Task<string> ResolveDirectUrlAsync(string url)
    {
        var current = url;
        for (int hop = 0; hop < MaxHops; hop++)
        {
            using var request = UnityWebRequest.Get(current);
            request.redirectLimit = 0;
            request.timeout = TimeoutSeconds;
            request.SetRequestHeader("User-Agent", BrowserUserAgent);

            var completion = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += _ => completion.TrySetResult(true);
            await completion.Task;

            var location = request.GetResponseHeader("Location");
            if (string.IsNullOrEmpty(location)) return current;

            // 상대 주소일 수 있으므로 현재 주소를 기준으로 절대 주소로 바꾼다.
            if (!Uri.TryCreate(new Uri(current), location, out var next) || !IsOpenable(next.AbsoluteUri))
                return current;

            current = next.AbsoluteUri;
        }
        return current;
    }

    static bool IsOpenable(string link)
    {
        return Uri.TryCreate(link, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
