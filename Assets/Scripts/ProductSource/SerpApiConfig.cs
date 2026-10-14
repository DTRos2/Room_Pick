/// <summary>
/// 상품 검색 호출 설정. SerpApi 키는 클라이언트에 두지 않고 Cloudflare Worker 프록시(proxy/)가 갖고 있다.
/// </summary>
public sealed class SerpApiConfig
{
    /// <summary>프록시 주소. 비밀이 아니므로 코드에 둔다.</summary>
    public const string DefaultProxyUrl = "https://model-house-proxy.devlearning98.workers.dev/";

    public string ProxyUrl { get; }

    /// <summary>요청 제한 시간(초)</summary>
    public int TimeoutSeconds { get; }

    public SerpApiConfig(string proxyUrl = DefaultProxyUrl, int timeoutSeconds = 10)
    {
        ProxyUrl = proxyUrl;
        TimeoutSeconds = timeoutSeconds;
    }
}
