using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// API 연동 확인용 임시 테스트. 씬의 빈 오브젝트에 붙이고 Play하면 한 번 검색해서 결과를 Console에 출력한다.
/// 결과는 화면 왼쪽 위에 썸네일과 버튼으로도 나오며, 누르면 상품 페이지가 열린다.
/// RoomPickManager가 만들어지면 그쪽에서 같은 조립을 하게 되므로 그때 지운다.
/// </summary>
public class ApiConnectionTester : MonoBehaviour
{
    [SerializeField] string baseKeyword = "소파";
    [SerializeField] string color = "";
    [SerializeField] ProductFilter filter = new ProductFilter { minPrice = 50000, requiredWords = new[] { "소파", "쇼파" }, excludeWords = new[] { "커버", "다리", "부품" } };
    [SerializeField, Min(1)] int printCount = 10;

    // 화면에 클릭 가능한 버튼으로 보여 줄 결과. 버튼을 누르면 상품 페이지가 열린다.
    IReadOnlyList<Product> _products = Array.Empty<Product>();

    readonly ThumbnailLoader _thumbnailLoader = new ThumbnailLoader();
    readonly Dictionary<string, Texture2D> _thumbnails = new Dictionary<string, Texture2D>();

    void OnDestroy()
    {
        _thumbnailLoader.Dispose();
    }

    async void Start()
    {
        try
        {
            var service = new ProductService(new SerpApiProductRepository(SerpApiConfig.LoadFromEnvFile()));
            var keyword = new SearchKeyword(baseKeyword, color);

            Debug.Log($"[ApiConnectionTester] 검색 시작: \"{keyword.Full}\"");
            var products = await service.SearchAsync(keyword, filter, destroyCancellationToken);

            if (products.Count == 0)
            {
                Debug.LogWarning("[ApiConnectionTester] 필터를 통과한 상품이 없습니다.");
                return;
            }

            _products = products;
            var lowest = ProductService.FindLowest(products);
            Debug.Log($"[ApiConnectionTester] 필터 통과 {products.Count}건, 최저가: {lowest.Title} / {lowest.Price:N0}원 / {lowest.MallName}\n{lowest.Link}");

            for (int i = 0; i < Math.Min(printCount, products.Count); i++)
            {
                Debug.Log($"[{i + 1}] {products[i].Price:N0}원 | {products[i].Title} | {products[i].MallName}");
                LoadThumbnail(products[i]);
            }
        }
        catch (RoomPickException e)
        {
            Debug.LogError($"[ApiConnectionTester] {e.ErrorCode}: {e.Message}");
            Debug.LogException(e);
        }
        catch (OperationCanceledException)
        {
            // Play를 멈추면 요청이 취소된다. 정상 동작이다.
        }
    }

    async void LoadThumbnail(Product product)
    {
        try
        {
            var texture = await _thumbnailLoader.LoadAsync(product.ImageUrl, destroyCancellationToken);
            if (texture != null) _thumbnails[product.ImageUrl] = texture;
        }
        catch (OperationCanceledException)
        {
            // Play를 멈추면 요청이 취소된다. 정상 동작이다.
        }
    }

    // 링크 열기 확인용 임시 UI. 실제 화면은 ProductListItemUI가 맡게 되므로 그때 지운다.
    void OnGUI()
    {
        const float thumbSize = 56f, width = 640f, gap = 4f;
        for (int i = 0; i < Math.Min(printCount, _products.Count); i++)
        {
            var product = _products[i];
            float y = 10f + i * (thumbSize + gap);

            if (product.ImageUrl != null && _thumbnails.TryGetValue(product.ImageUrl, out var texture))
                GUI.DrawTexture(new Rect(10f, y, thumbSize, thumbSize), texture, ScaleMode.ScaleToFit);

            var label = $"{product.Price:N0}원 | {product.Title}";
            if (GUI.Button(new Rect(10f + thumbSize + gap, y, width, thumbSize), label))
                _ = ProductLinkOpener.OpenAsync(product);
        }
    }
}
