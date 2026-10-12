using System.Threading.Tasks;

/// <summary>
/// 상품 링크를 사용자에게 보여 준다. 화면은 이 인터페이스에만 의존하므로,
/// 브라우저로 여는 방식 대신 다른 구현(테스트용 가짜 등)으로 바꿔 끼울 수 있다.
/// </summary>
public interface IProductLinkOpener
{
    /// <returns>링크를 열었으면 true, 열 수 없는 링크라서 열지 않았으면 false</returns>
    Task<bool> OpenAsync(Product product);
}
