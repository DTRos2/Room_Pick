using System;
using UnityEngine;

/// <summary>
/// 오브젝트를 클릭해 팝오버가 열리면 그 오브젝트의 검색어·필터로 상품을 찾아 상품 정보 화면에 보여 준다.
/// ProductPopover가 있는 오브젝트(Canvas 등)에 붙인다.
/// 이 컴포넌트는 객체를 만들어 연결하는 조립 지점이고, 검색 흐름은 ProductInfoPresenter가 맡는다.
/// </summary>
public class ProductInfoController : MonoBehaviour
{
    [SerializeField, Tooltip("비워 두면 같은 오브젝트에서 찾는다.")] ProductPopover popover;
    [SerializeField, Min(1)] int listCount = 5;
    [SerializeField, Tooltip("상품 정보 화면의 크기·글자·색")] ProductInfoStyle style = new ProductInfoStyle();

    readonly ThumbnailLoader _thumbnails = new ThumbnailLoader();

    ProductInfoView _view;
    ProductInfoPresenter _presenter;

    void Awake()
    {
        if (popover == null) popover = GetComponent<ProductPopover>();
        if (popover == null)
        {
            Debug.LogError("[ProductInfoController] ProductPopover를 찾을 수 없습니다.", this);
            enabled = false;
            return;
        }

        IProductSearch search = null;
        RoomPickException configError = null;
        try
        {
            search = new ProductService(new SerpApiProductRepository(new SerpApiConfig()));
        }
        catch (RoomPickException e)
        {
            // 키가 없는 경우. 클릭할 때 화면에 알리고, 원인은 Console에 남긴다.
            configError = e;
            Debug.LogException(e);
        }

        try
        {
            IProductLinkOpener linkOpener = new ProductLinkOpener(new NaverAdRedirectResolver(), new UnityUrlLauncher());
            _view = ProductInfoViewFactory.Create(
                popover.Panel, popover.MeasureButtonsTop, popover.Close, linkOpener, _thumbnails,
                style ?? new ProductInfoStyle(), listCount, destroyCancellationToken);
            _presenter = new ProductInfoPresenter(search, configError, _view, destroyCancellationToken);
        }
        catch (Exception e)
        {
            // 화면을 못 만들어도 씬의 나머지(색상 버튼 등)는 계속 동작해야 한다.
            Debug.LogException(e, this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (popover == null) return;
        popover.Opened += OnOpened;
        popover.Closed += OnClosed;
    }

    void OnDisable()
    {
        if (popover == null) return;
        popover.Opened -= OnOpened;
        popover.Closed -= OnClosed;
    }

    void OnDestroy()
    {
        _presenter?.Dispose();
        _view?.Destroy();
        _thumbnails.Dispose();
    }

    async void OnOpened(ProductTarget target)
    {
        // Play 중 스크립트가 다시 컴파일되면 Awake가 다시 돌지 않아 _presenter가 비어 있을 수 있다.
        if (_presenter == null) return;
        await _presenter.OpenAsync(target);
    }

    void OnClosed() => _presenter?.Close();
}
