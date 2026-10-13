using System.Collections.Generic;

/// <summary>
/// 오브젝트(ProductTarget)별 검색 결과를 보관한다. 같은 오브젝트는 다시 검색하지 않아 SerpApi 호출을 아낀다.
/// </summary>
public sealed class ProductSearchCache
{
    readonly Dictionary<ProductTarget, IReadOnlyList<Product>> _results =
        new Dictionary<ProductTarget, IReadOnlyList<Product>>();

    public bool TryGet(ProductTarget target, out IReadOnlyList<Product> products)
        => _results.TryGetValue(target, out products);

    public void Store(ProductTarget target, IReadOnlyList<Product> products) => _results[target] = products;
}
