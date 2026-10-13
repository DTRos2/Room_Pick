using System.Text.RegularExpressions;

/// <summary>
/// SerpApiShoppingItem(응답 DTO)을 Product(화면용 모델)로 변환한다.
/// </summary>
public static class ProductMapper
{
    static readonly Regex HtmlTag = new Regex("<.*?>", RegexOptions.Compiled);

    /// <summary>필수 값(제목, 링크)이 없는 항목은 변환하지 않고 false를 돌려준다.</summary>
    public static bool TryToProduct(SerpApiShoppingItem item, out Product product)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.title) || string.IsNullOrWhiteSpace(item.link))
        {
            product = null;
            return false;
        }

        product = new Product(
            title: HtmlTag.Replace(item.title, string.Empty).Trim(),
            price: item.price,
            link: item.link,
            imageUrl: item.thumbnail,
            mallName: item.stores);
        return true;
    }
}
