/// <summary>
/// 가격을 화면에 보여 줄 문자열로 바꾼다.
/// </summary>
public static class PriceFormatter
{
    public static string Format(int price) => $"{price:N0}원";
}
