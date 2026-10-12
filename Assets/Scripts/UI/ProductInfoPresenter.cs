using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 오브젝트를 클릭해 팝오버가 열리면 그 오브젝트의 검색어·필터로 상품을 찾아 화면에 보여 주는 흐름.
/// 같은 오브젝트는 한 번만 검색하고, 다른 오브젝트를 누르면 이전 요청을 취소한다.
/// Unity 컴포넌트가 아니므로 가짜 IProductSearch와 IProductInfoView만 있으면 흐름을 따로 확인할 수 있다.
/// </summary>
public sealed class ProductInfoPresenter : IDisposable
{
    readonly IProductSearch _search;
    readonly RoomPickException _configError;
    readonly IProductInfoView _view;
    readonly CancellationToken _lifetime;
    readonly ProductSearchCache _cache = new ProductSearchCache();

    CancellationTokenSource _requestCts;
    ProductTarget _pendingTarget;

    /// <param name="search">설정 오류로 만들지 못했다면 null</param>
    /// <param name="configError">search를 만들지 못한 이유. 클릭할 때 화면에 알린다.</param>
    public ProductInfoPresenter(IProductSearch search, RoomPickException configError, IProductInfoView view, CancellationToken lifetime)
    {
        _search = search;
        _configError = configError;
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _lifetime = lifetime;
    }

    public async Task OpenAsync(ProductTarget target)
    {
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
            _view.ShowError(ProductErrorMessages.For(_configError.ErrorCode));
            return;
        }

        if (_cache.TryGet(target, out var cached))
        {
            _view.ShowProducts(cached);
            return;
        }

        _view.ShowLoading();
        _pendingTarget = target;

        // 다른 오브젝트를 누르면 이전 요청을 취소해, 늦게 온 응답이 새 화면을 덮어쓰지 않게 한다.
        _requestCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime);
        var token = _requestCts.Token;

        try
        {
            var products = await _search.SearchAsync(target.Keyword, target.Filter, token);
            if (token.IsCancellationRequested) return;

            _cache.Store(target, products);
            _view.ShowProducts(products);
        }
        catch (RoomPickException e)
        {
            if (token.IsCancellationRequested) return;
            Debug.LogException(e);
            _view.ShowError(ProductErrorMessages.For(e.ErrorCode));
        }
        catch (OperationCanceledException)
        {
            // 다른 오브젝트를 눌렀거나 Play를 멈춘 경우다. 정상 동작이다.
        }
        catch (Exception e)
        {
            // 호출하는 쪽이 async void라 예상 밖의 예외는 아무도 받지 못하고, 화면은 "찾는 중..."에 멈춘다. 오류 문구로 바꾼다.
            if (token.IsCancellationRequested) return;
            Debug.LogException(e);
            _view.ShowError(ProductErrorMessages.For(ErrorCode.ApiResponseInvalid));
        }
        finally
        {
            if (_pendingTarget == target) _pendingTarget = null;
        }
    }

    public void Close()
    {
        CancelRequest();
        _view.Hide();
    }

    public void Dispose() => CancelRequest();

    void CancelRequest()
    {
        _pendingTarget = null;
        if (_requestCts == null) return;
        _requestCts.Cancel();
        _requestCts.Dispose();
        _requestCts = null;
    }
}
