using System.Collections.Generic;

/// <summary>
/// 상품 정보 화면이 보여 줄 수 있는 상태. ProductInfoPresenter는 이 인터페이스에만 의존한다.
/// </summary>
public interface IProductInfoView
{
    void ShowLoading();
    void ShowEmpty();
    void ShowError(string message);
    void ShowProducts(IReadOnlyList<Product> sortedProducts);
    void Hide();
}
