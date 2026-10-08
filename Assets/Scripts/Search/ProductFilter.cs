using System;
using UnityEngine;

/// <summary>
/// 에셋별 검색 필터 기준 (인스펙터에서 입력).
/// "소파"로 검색하면 커버·다리 같은 부품도 섞여 나오므로, 최저가를 고르기 전에 이 기준으로 걸러낸다.
/// SerpApi 네이버 응답에는 카테고리가 없어서 카테고리 대신 상품명의 필수 단어·제외 단어를 쓴다.
/// </summary>
[Serializable]
public struct ProductFilter
{
    [Tooltip("이 가격(원) 미만의 상품은 부품·소품으로 보고 제외한다.")]
    [PriceField]
    public int minPrice;

    [Tooltip("상품명에 이 단어 중 하나는 반드시 들어 있어야 한다. 비워 두면 검사하지 않는다. 예: 소파, 쇼파")]
    public string[] requiredWords;

    [Tooltip("상품명에 이 단어가 하나라도 들어 있으면 제외한다. 예: 커버, 다리, 부품")]
    public string[] excludeWords;

    public bool IsMatch(Product product)
    {
        if (product == null) return false;
        // 가격 정보가 없는(0원) 상품은 최저가로 잘못 뽑히므로 minPrice와 상관없이 제외한다.
        if (product.Price <= 0 || product.Price < minPrice) return false;

        // 검색어와 상관없는 연관 상품(의자, 테이블 등)을 거른다. 철자 변형(소파/쇼파)을 위해 하나만 맞아도 통과한다.
        if (HasWords(requiredWords) && !ContainsAny(product.Title, requiredWords)) return false;

        return !ContainsAny(product.Title, excludeWords);
    }

    static bool HasWords(string[] words)
    {
        if (words == null) return false;
        foreach (var word in words)
        {
            if (!string.IsNullOrWhiteSpace(word)) return true;
        }
        return false;
    }

    static bool ContainsAny(string text, string[] words)
    {
        if (words == null) return false;
        foreach (var word in words)
        {
            if (string.IsNullOrWhiteSpace(word)) continue;
            if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }
}
