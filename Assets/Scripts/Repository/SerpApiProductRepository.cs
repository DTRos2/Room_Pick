using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// UnityWebRequest로 SerpApi(engine=naver)를 호출해 IProductRepository를 구현한다.
/// 색상을 합친 전체 검색어로 요청하고, 실패하면 RoomPickException(ErrorCode)을 던진다.
/// </summary>
public class SerpApiProductRepository : IProductRepository
{
    const string Endpoint = "https://serpapi.com/search.json";

    readonly SerpApiConfig _config;

    public SerpApiProductRepository(SerpApiConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(SearchKeyword keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword.Full))
            return Array.Empty<Product>();

        using var request = UnityWebRequest.Get(BuildUrl(keyword));
        request.timeout = _config.TimeoutSeconds;

        await SendAsync(request, cancellationToken);
        ThrowIfFailed(request);

        return ParseProducts(request.downloadHandler.text);
    }

    string BuildUrl(SearchKeyword keyword)
    {
        return $"{Endpoint}?engine={UnityWebRequest.EscapeURL(_config.Engine)}" +
               $"&where=nexearch" +
               $"&query={UnityWebRequest.EscapeURL(keyword.Full)}" +
               $"&api_key={UnityWebRequest.EscapeURL(_config.ApiKey)}";
    }

    static async Task SendAsync(UnityWebRequest request, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<bool>();
        var operation = request.SendWebRequest();
        operation.completed += _ => completion.TrySetResult(true);

        // 요청이 끝난 뒤에는 등록을 해제해, 이미 dispose된 request에 Abort가 호출되지 않게 한다.
        using (cancellationToken.Register(() =>
        {
            request.Abort();
            completion.TrySetCanceled();
        }))
        {
            await completion.Task;
        }
    }

    /// <summary>HTTP 결과를 ErrorCode로 바꾼다. API 키가 담긴 URL은 메시지에 넣지 않는다.</summary>
    static void ThrowIfFailed(UnityWebRequest request)
    {
        if (request.result == UnityWebRequest.Result.Success) return;

        long status = request.responseCode;
        if (status == 401 || status == 403)
            throw new RoomPickException(ErrorCode.ApiAuthFailed, $"SerpApi 인증 실패 (HTTP {status}). API 키를 확인하세요.");
        if (status == 429)
            throw new RoomPickException(ErrorCode.ApiQuotaExceeded, "SerpApi 호출 한도를 초과했습니다. (HTTP 429)");

        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                throw new RoomPickException(ErrorCode.NetworkFailed, $"SerpApi 연결 실패: {request.error}");
            case UnityWebRequest.Result.DataProcessingError:
                throw new RoomPickException(ErrorCode.ApiResponseInvalid, $"SerpApi 응답 처리 실패: {request.error}");
            default:
                // 5xx는 서버 쪽 일시 장애이므로 네트워크 실패로, 그 밖의 4xx는 잘못된 요청으로 본다.
                var code = status >= 500 ? ErrorCode.NetworkFailed : ErrorCode.ApiResponseInvalid;
                throw new RoomPickException(code, $"SerpApi 요청 실패 (HTTP {status}): {request.error}");
        }
    }

    static IReadOnlyList<Product> ParseProducts(string json)
    {
        SerpApiResponse response;
        try
        {
            response = JsonUtility.FromJson<SerpApiResponse>(json);
        }
        catch (Exception e)
        {
            throw new RoomPickException(ErrorCode.ApiResponseInvalid, $"응답 JSON 파싱 실패. 원본: {json}", e);
        }

        if (response == null)
            throw new RoomPickException(ErrorCode.ApiResponseInvalid, $"응답이 비어 있습니다. 원본: {json}");

        if (!string.IsNullOrEmpty(response.error))
        {
            // 검색 결과가 없는 경우도 error 메시지로 오므로 빈 목록으로 처리한다.
            if (response.error.Contains("hasn't returned any results"))
                return Array.Empty<Product>();
            throw new RoomPickException(ErrorCode.ApiResponseInvalid, $"SerpApi 오류: {response.error}");
        }

        if (response.shopping_results == null)
            return Array.Empty<Product>();

        var products = new List<Product>(response.shopping_results.Length);
        foreach (var item in response.shopping_results)
        {
            // 필수 값이 빠진 항목 하나 때문에 정상 상품까지 버리지 않고 그 항목만 건너뛴다.
            if (ProductMapper.TryToProduct(item, out var product))
                products.Add(product);
        }

        // 항목은 왔는데 하나도 쓸 수 없다면 응답 형식이 바뀐 것이므로 오류로 알린다.
        if (products.Count == 0 && response.shopping_results.Length > 0)
            throw new RoomPickException(ErrorCode.ApiResponseInvalid, $"상품 {response.shopping_results.Length}건이 모두 title 또는 link가 없습니다. 원본: {json}");

        return products;
    }
}
