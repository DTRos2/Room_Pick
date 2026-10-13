using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 상품 검색 인터페이스. 구현체는 실패 시 RoomPickException(ErrorCode)을 던진다.
/// </summary>
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> SearchAsync(SearchKeyword keyword, CancellationToken cancellationToken = default);
}
