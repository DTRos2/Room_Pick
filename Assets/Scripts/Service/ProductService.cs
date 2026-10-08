using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 상품을 조회하고, ProductFilter로 거른 뒤 가격 오름차순으로 정렬한다.
/// 필터를 거친 뒤에 최저가를 고르므로 부품·소품이 최저가로 추천되지 않는다.
/// </summary>
public class ProductService
{
    readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>필터를 통과한 상품을 가격 오름차순으로 돌려준다. 결과가 없으면 빈 목록이다.</summary>
    public async Task<IReadOnlyList<Product>> SearchAsync(
        SearchKeyword keyword, ProductFilter filter, CancellationToken cancellationToken = default)
    {
        var products = await _repository.SearchAsync(keyword, cancellationToken);

        return products
            .Where(filter.IsMatch)
            .OrderBy(product => product.Price)
            .ToList();
    }

    /// <summary>목록에서 가장 싼 상품을 찾는다. 정렬 여부와 상관없이 동작하며, 목록이 비어 있으면 null이다.</summary>
    public static Product FindLowest(IReadOnlyList<Product> products)
    {
        Product lowest = null;
        if (products == null) return null;

        foreach (var product in products)
        {
            if (lowest == null || product.Price < lowest.Price)
                lowest = product;
        }
        return lowest;
    }
}
