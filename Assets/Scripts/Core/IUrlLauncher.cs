/// <summary>
/// 주소를 사용자의 브라우저로 연다. ProductLinkOpener가 Unity에 직접 매이지 않도록 분리했다.
/// </summary>
public interface IUrlLauncher
{
    void Open(string url);
}
