using System;

/// <summary>
/// SerpApi(engine=naver) 응답 전체를 받는 DTO. JsonUtility가 읽을 수 있도록 필드 이름을 JSON과 같게 둔다.
/// </summary>
[Serializable]
public class SerpApiResponse
{
    /// <summary>오류가 있을 때만 들어오는 메시지</summary>
    public string error;

    public SerpApiShoppingItem[] shopping_results;
}

/// <summary>
/// 응답의 상품 한 건 DTO.
/// </summary>
[Serializable]
public class SerpApiShoppingItem
{
    public int position;
    public string title;
    public string link;
    public string thumbnail;
    public int price;
    public string stores;
}
