using System;

/// <summary>
/// 외부 API가 준 주소를 브라우저로 열어도 되는지 판단하는 규칙. http/https만 허용한다.
/// </summary>
public static class WebUrl
{
    public static bool IsOpenable(string link)
    {
        return Uri.TryCreate(link, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
