using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 상품 링크를 기본 브라우저로 연다. 외부 API가 준 주소이므로 http/https만 허용하고,
/// 열기 전에 IDirectUrlResolver로 직접 주소를 찾아 보되 실패하면 원래 링크를 연다.
/// </summary>
public sealed class ProductLinkOpener : IProductLinkOpener
{
    readonly IDirectUrlResolver _resolver;

    public ProductLinkOpener(IDirectUrlResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public async Task<bool> OpenAsync(Product product)
    {
        if (product == null || !WebUrl.IsOpenable(product.Link))
        {
            Debug.LogWarning($"[ProductLinkOpener] 열 수 없는 링크입니다: {product?.Link}");
            return false;
        }

        var target = product.Link;
        try
        {
            var resolved = await _resolver.ResolveAsync(product.Link);
            if (WebUrl.IsOpenable(resolved)) target = resolved;
        }
        catch (Exception e)
        {
            // 직접 주소를 못 찾아도 원래 링크는 열 수 있게 한다.
            Debug.LogWarning($"[ProductLinkOpener] 직접 주소 확인 실패, 원래 링크를 엽니다: {e.Message}");
        }

        Application.OpenURL(target);
        return true;
    }
}
