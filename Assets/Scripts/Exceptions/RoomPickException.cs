using System;

/// <summary>
/// 프로젝트의 유일한 예외. ErrorCode로 오류 종류를 구분한다.
/// </summary>
public class RoomPickException : Exception
{
    public ErrorCode ErrorCode { get; }

    public RoomPickException(ErrorCode errorCode, string message = null, Exception innerException = null)
        : base(message ?? errorCode.ToString(), innerException)
    {
        ErrorCode = errorCode;
    }
}
