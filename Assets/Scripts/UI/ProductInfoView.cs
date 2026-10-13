using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 색상 팝오버 안, 색상 버튼 바로 위에 들어가는 상품 정보 화면의 진행을 맡는다.
/// 최저가 카드(ProductCardView)를 먼저 보여 주고, "다른 상품 N개 보기"를 누르면 화면 오른쪽 끝에
/// 상품 목록(ProductListView)을 펼친다. 카드나 목록 줄을 누르면 IProductLinkOpener로 그 상품 페이지를 연다.
/// 화면을 만드는 일은 ProductInfoViewFactory가, 레이아웃은 각 뷰가, 크기·색은 ProductInfoStyle이 정하고
/// 이 클래스는 상태(펼침 여부, 현재 상품)만 다룬다.
/// </summary>
public sealed class ProductInfoView : IProductInfoView
{
    const string LoadingMessage = "상품을 찾는 중…";
    const string EmptyMessage = "검색 결과가 없습니다";

    readonly RectTransform _root;
    readonly Func<float> _measureContentBottom;
    readonly ProductInfoStyle _style;
    readonly IProductLinkOpener _linkOpener;
    readonly ThumbnailPresenter _thumbnails;
    readonly ProductCardView _card;
    readonly ProductListView _list;
    readonly PopoverChromeView _chrome;

    IReadOnlyList<Product> _products = Array.Empty<Product>();
    bool _expanded;
    bool _warnedTooShort;

    /// <param name="root">카드 영역. 팝오버 패널 안에서 색상 버튼 위부터 패널 위쪽 여백까지 채운다.</param>
    /// <param name="measureContentBottom">패널 바닥에서 카드 아래쪽까지 비워 둘 높이를 알려 주는 함수. 음수면 모른다는 뜻이다.</param>
    public ProductInfoView(
        RectTransform root, Func<float> measureContentBottom, ProductInfoStyle style, IProductLinkOpener linkOpener,
        ThumbnailPresenter thumbnails, ProductCardView card, ProductListView list, PopoverChromeView chrome)
    {
        _root = root != null ? root : throw new ArgumentNullException(nameof(root));
        _measureContentBottom = measureContentBottom;
        _style = style ?? throw new ArgumentNullException(nameof(style));
        _linkOpener = linkOpener ?? throw new ArgumentNullException(nameof(linkOpener));
        _thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
        _card = card ?? throw new ArgumentNullException(nameof(card));
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _chrome = chrome ?? throw new ArgumentNullException(nameof(chrome));
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
        // 목록에는 최대 RowCapacity개까지만 보이므로, 최저가를 뺀 나머지 수도 그에 맞춘다.
        int others = Math.Min(sortedProducts.Count, _list.RowCapacity) - 1;
        _card.ShowProduct(sortedProducts[0], others, _thumbnails);
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
        _chrome.Destroy();
    }

    /// <summary>버튼 위치를 실제로 읽으므로 패널이나 버튼 크기를 씬에서 바꿔도 따로 고칠 필요가 없다.</summary>
    public void FitToPanel()
    {
        float measured = _measureContentBottom != null ? _measureContentBottom() : -1f;
        // 카드 아래에는 "색상" 라벨 한 줄이 들어가므로 그만큼 더 비운다.
        float bottomInset = measured >= 0f ? measured + _style.gap * 2f + _style.labelHeight : _style.fallbackBottomInset;
        _chrome.PlaceColorLabel(measured);

        _root.offsetMin = new Vector2(_style.margin, bottomInset);
        _root.offsetMax = new Vector2(-_style.margin, -_style.margin);

        if (_root.rect.height < _style.cardHeight * 0.9f && !_warnedTooShort)
        {
            _warnedTooShort = true;
            Debug.LogWarning($"[ProductInfoView] 카드 영역({_root.rect.height:0}) 이 cardHeight({_style.cardHeight:0})보다 낮습니다. 팝오버 패널 높이를 키우세요. (메뉴: RoomPick/상품 UI 스타일 적용)");
        }
    }

    public void ToggleList() => SetExpanded(!_expanded);

    // 클릭 이벤트에서 부르는 async void다. 예외는 OpenProductAsync가 직접 기록한다.
    public async void OpenProduct(int index) => await OpenProductAsync(index);

    public async Task OpenProductAsync(int index)
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

    void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        _list.SetOpen(expanded);
        _card.SetExpanded(expanded);
    }
}
