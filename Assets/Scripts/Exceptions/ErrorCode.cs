/// <summary>
/// RoomPickException이 나타내는 오류 종류.
/// NetworkFailed, ApiQuotaExceeded는 FallbackProductRepository가 잡아 예비 JSON으로 대체하고,
/// ApiAuthFailed, ApiResponseInvalid는 RoomPickManager가 잡아 ErrorNotifierUI로 알린다.
/// </summary>
public enum ErrorCode
{
    /// <summary>연결 실패, 시간 초과</summary>
    NetworkFailed,

    /// <summary>인증 실패 (키 오류, 401)</summary>
    ApiAuthFailed,

    /// <summary>호출 한도 초과 (429)</summary>
    ApiQuotaExceeded,

    /// <summary>응답 JSON 파싱 실패, 필수 값 누락</summary>
    ApiResponseInvalid,
}
