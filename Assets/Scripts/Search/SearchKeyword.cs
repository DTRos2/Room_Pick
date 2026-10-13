/// <summary>
/// 검색 조건. 기본 검색어(Base)와 색상(Color)을 합쳐 전체 검색어를 만든다.
/// </summary>
public readonly struct SearchKeyword
{
    public string Base { get; }
    public string Color { get; }

    public SearchKeyword(string baseKeyword, string color = null)
    {
        Base = baseKeyword;
        Color = color;
    }

    /// <summary>"색상 기본검색어" 형태의 전체 검색어. 색상이 없으면 기본 검색어만 쓴다.</summary>
    public string Full => string.IsNullOrWhiteSpace(Color) ? Base : $"{Color} {Base}";
}
