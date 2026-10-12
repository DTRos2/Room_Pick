/// <summary>
/// ErrorCode를 화면에 보여 줄 문구로 바꾼다.
/// </summary>
public static class ProductErrorMessages
{
    public static string For(ErrorCode code)
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
