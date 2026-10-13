/// <summary>
/// 화면에서 쓰는 불변 상품 모델.
/// </summary>
public sealed class Product
{
    public string Title { get; }
    public int Price { get; }
    public string Link { get; }
    public string ImageUrl { get; }
    public string MallName { get; }

    public Product(string title, int price, string link, string imageUrl, string mallName)
    {
        Title = title;
        Price = price;
        Link = link;
        ImageUrl = imageUrl;
        MallName = mallName;
    }
}
