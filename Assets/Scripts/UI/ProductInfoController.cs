using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// 오브젝트를 클릭해 팝오버가 열리면 그 오브젝트의 검색어·필터로 상품을 찾아 ProductInfoView에 보여 준다.
/// ProductPopover가 있는 오브젝트(Canvas 등)에 붙인다. 같은 오브젝트는 한 번만 검색해서 SerpApi 호출을 아낀다.
/// </summary>
public class ProductInfoController : MonoBehaviour
{
    [SerializeField, Tooltip("비워 두면 같은 오브젝트에서 찾는다.")] ProductPopover popover;
    [SerializeField, Min(1)] int listCount = 5;

    readonly Dictionary<ProductTarget, IReadOnlyList<Product>> _cache = new Dictionary<ProductTarget, IReadOnlyList<Product>>();
    readonly ThumbnailLoader _thumbnails = new ThumbnailLoader();

    ProductService _service;
    RoomPickException _configError;
    ProductInfoView _view;
    CancellationTokenSource _requestCts;
    ProductTarget _pendingTarget;

    void Awake()
    {
        if (popover == null) popover = GetComponent<ProductPopover>();
        if (popover == null)
        {
            Debug.LogError("[ProductInfoController] ProductPopover를 찾을 수 없습니다.", this);
            enabled = false;
            return;
        }

        try
        {
            _service = new ProductService(new SerpApiProductRepository(SerpApiConfig.LoadFromEnvFile()));
        }
        catch (RoomPickException e)
        {
            // 키가 없는 경우. 클릭할 때 화면에 알리고, 원인은 Console에 남긴다.
            _configError = e;
            Debug.LogException(e);
        }

        _view = new ProductInfoView(popover.Panel, _thumbnails, listCount, destroyCancellationToken);
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
        CancelRequest();
        _view?.Destroy();
        _thumbnails.Dispose();
    }

    async void OnOpened(ProductTarget target)
    {
        // Play 중 스크립트가 다시 컴파일되면 Awake가 다시 돌지 않아 _view가 비어 있을 수 있다.
        if (_view == null) return;

        // 같은 오브젝트를 연타해도 이미 진행 중인 검색을 다시 보내지 않는다. (SerpApi 호출 한도 보호)
        if (target == _pendingTarget) return;

        CancelRequest();

        if (string.IsNullOrWhiteSpace(target.Keyword.Base))
        {
            _view.Hide();
            return;
        }

        if (_configError != null)
        {
            _view.ShowError(MessageFor(_configError.ErrorCode));
            return;
        }

        if (_cache.TryGetValue(target, out var cached))
        {
            _view.ShowProducts(cached);
            return;
        }

        _view.ShowLoading();
        _pendingTarget = target;

        // 다른 오브젝트를 누르면 이전 요청을 취소해, 늦게 온 응답이 새 화면을 덮어쓰지 않게 한다.
        _requestCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        var token = _requestCts.Token;

        try
        {
            var products = await _service.SearchAsync(target.Keyword, target.Filter, token);
            if (token.IsCancellationRequested) return;

            _cache[target] = products;
            _view.ShowProducts(products);
        }
        catch (RoomPickException e)
        {
            if (token.IsCancellationRequested) return;
            Debug.LogException(e);
            _view.ShowError(MessageFor(e.ErrorCode));
        }
        catch (OperationCanceledException)
        {
            // 다른 오브젝트를 눌렀거나 Play를 멈춘 경우다. 정상 동작이다.
        }
        finally
        {
            if (_pendingTarget == target) _pendingTarget = null;
        }
    }

    void OnClosed()
    {
        CancelRequest();
        _view?.Hide();
    }

    void CancelRequest()
    {
        _pendingTarget = null;
        if (_requestCts == null) return;
        _requestCts.Cancel();
        _requestCts.Dispose();
        _requestCts = null;
    }

    static string MessageFor(ErrorCode code)
    {
        switch (code)
        {
            case ErrorCode.NetworkFailed: return "네트워크 연결을 확인해 주세요";
            case ErrorCode.ApiQuotaExceeded: return "검색 한도를 초과했습니다";
            case ErrorCode.ApiAuthFailed: return "API 키를 확인해 주세요";
            default: return "상품 정보를 읽지 못했습니다";
        }
    }
}
