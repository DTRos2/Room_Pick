using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 색상 팝오버 안, 색상 버튼 바로 위에 들어가는 상품 정보 화면의 진행을 맡는다.
/// 최저가 카드(ProductCardView)를 먼저 보여 주고, 화살표를 누르면 화면 오른쪽 끝에
/// 상품 목록(ProductListView)을 펼친다. 카드나 목록 줄을 누르면 IProductLinkOpener로 그 상품 페이지를 연다.
/// 레이아웃은 각 뷰가, 크기·색은 ProductInfoStyle이 정하고 이 클래스는 상태(펼침 여부, 현재 상품)만 다룬다.
/// </summary>
public sealed class ProductInfoView
{
    const string LoadingMessage = "상품을 찾는 중...";
    const string EmptyMessage = "검색 결과가 없습니다";

    readonly RectTransform _root;
    readonly Func<float> _measureContentBottom;
    readonly ProductInfoStyle _style;
    readonly IProductLinkOpener _linkOpener;
    readonly ThumbnailPresenter _thumbnails;
    readonly ProductCardView _card;
    readonly ProductListView _list;

    IReadOnlyList<Product> _products = Array.Empty<Product>();
    bool _expanded;

    /// <param name="popoverPanel">이 화면을 안에 넣을 색상 팝오버 패널</param>
    /// <param name="measureContentBottom">패널 바닥에서 카드 아래쪽까지 비워 둘 높이를 알려 주는 함수. 음수면 모른다는 뜻이다.</param>
    public ProductInfoView(
        RectTransform popoverPanel, Func<float> measureContentBottom, IProductLinkOpener linkOpener,
        ThumbnailLoader thumbnails, ProductInfoStyle style, int listCount, CancellationToken lifetime)
    {
        if (popoverPanel == null) throw new ArgumentNullException(nameof(popoverPanel));
        _measureContentBottom = measureContentBottom;
        _linkOpener = linkOpener ?? throw new ArgumentNullException(nameof(linkOpener));
        _style = style ?? throw new ArgumentNullException(nameof(style));
        _thumbnails = new ThumbnailPresenter(thumbnails, style.thumbnailPlaceholder, lifetime);
        var kit = UguiKit.CreateDefault();

        // 카드 영역: 패널 안에서 색상 버튼 위부터 패널 위쪽 여백까지 꽉 채운다.
        _root = UguiKit.NewRect("ProductInfoView", popoverPanel);
        _root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.pivot = new Vector2(0.5f, 0.5f);
        var rootLayout = _root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.childControlWidth = rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = rootLayout.childForceExpandHeight = true;
        FitToPanel();

        _card = new ProductCardView(_root, kit, style, () => OpenProduct(0), ToggleList);
        _list = new ProductListView(popoverPanel, _root.gameObject, kit, style, listCount, OpenProduct);

        Hide();
    }

    public void ShowLoading() => ShowMessage(LoadingMessage);
    public void ShowEmpty() => ShowMessage(EmptyMessage);
    public void ShowError(string message) => ShowMessage(message);

    /// <summary>가격 오름차순으로 정렬된 상품을 보여 준다. 첫 번째가 최저가 카드가 된다.</summary>
    public void ShowProducts(IReadOnlyList<Product> sortedProducts)
    {
        if (sortedProducts == null || sortedProducts.Count == 0)
        {
            ShowEmpty();
            return;
        }

        _products = sortedProducts;
        _card.ShowProduct(sortedProducts[0], canExpand: sortedProducts.Count > 1, _thumbnails);
        _list.Bind(sortedProducts, _thumbnails);
        Present();
    }

    public void Hide()
    {
        _thumbnails.CancelPending();
        SetExpanded(false);
        _root.gameObject.SetActive(false);
    }

    public void Destroy()
    {
        if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
        _list.Destroy();
    }

    void ShowMessage(string message)
    {
        _thumbnails.CancelPending();
        _products = Array.Empty<Product>();
        _card.ShowMessage(message);
        Present();
    }

    /// <summary>접힌 상태로, 패널 크기에 맞춰 화면에 올린다.</summary>
    void Present()
    {
        SetExpanded(false);
        FitToPanel();
        _root.gameObject.SetActive(true);
    }

    /// <summary>버튼 위치를 실제로 읽으므로 패널이나 버튼 크기를 씬에서 바꿔도 따로 고칠 필요가 없다.</summary>
    void FitToPanel()
    {
        float measured = _measureContentBottom != null ? _measureContentBottom() : -1f;
        float bottomInset = measured >= 0f ? measured + _style.gap * 2f : _style.fallbackBottomInset;

        _root.offsetMin = new Vector2(_style.margin, bottomInset);
        _root.offsetMax = new Vector2(-_style.margin, -_style.margin);
    }

    void ToggleList() => SetExpanded(!_expanded);

    void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        _list.SetOpen(expanded);
        _card.SetExpanded(expanded);
    }

    // 클릭 이벤트에서 부르는 async void다. 예외가 나도 아무도 받지 못하므로 여기서 직접 기록한다.
    async void OpenProduct(int index)
    {
        if (index < 0 || index >= _products.Count) return;

        try
        {
            await _linkOpener.OpenAsync(_products[index]);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProductInfoView] 상품 링크를 열지 못했습니다: {e}");
        }
    }
}
