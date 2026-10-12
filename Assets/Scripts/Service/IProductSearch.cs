using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 필터를 거쳐 가격 오름차순으로 정렬된 상품을 찾는다. 화면 흐름은 이 인터페이스에만 의존한다.
/// </summary>
public interface IProductSearch
{
    Task<IReadOnlyList<Product>> SearchAsync(
        SearchKeyword keyword, ProductFilter filter, CancellationToken cancellationToken = default);
}
