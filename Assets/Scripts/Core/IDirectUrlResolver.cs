using System.Threading.Tasks;

/// <summary>
/// 추적·중계 주소를 사용자가 실제로 가야 할 주소로 바꾼다.
/// </summary>
public interface IDirectUrlResolver
{
    /// <summary>바꿀 수 없으면 받은 주소를 그대로 돌려준다.</summary>
    Task<string> ResolveAsync(string url);
}
