using System.IO;
using UnityEngine;

/// <summary>
/// SerpApi 호출에 필요한 설정. 키는 git에 올리지 않는 프로젝트 루트의 .env에서 읽는다.
/// .env는 에디터에서만 읽을 수 있으므로 빌드에는 키가 포함되지 않는다.
/// </summary>
public sealed class SerpApiConfig
{
    const string ApiKeyName = "SERPAPI_API_KEY";
    const string EngineName = "SERPAPI_ENGINE";
    const string DefaultEngine = "naver";

    public string ApiKey { get; }
    public string Engine { get; }

    /// <summary>요청 제한 시간(초)</summary>
    public int TimeoutSeconds { get; }

    public SerpApiConfig(string apiKey, string engine = DefaultEngine, int timeoutSeconds = 10)
    {
        ApiKey = apiKey;
        Engine = engine;
        TimeoutSeconds = timeoutSeconds;
    }

    /// <summary>프로젝트 루트의 .env에서 설정을 읽는다. 키가 없으면 ApiAuthFailed를 던진다.</summary>
    public static SerpApiConfig LoadFromEnvFile()
    {
        var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".env"));
        var values = EnvFileReader.Read(path);

        if (!values.TryGetValue(ApiKeyName, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            throw new RoomPickException(ErrorCode.ApiAuthFailed, $".env에 {ApiKeyName} 값이 없습니다. ({path})");

        values.TryGetValue(EngineName, out var engine);
        return new SerpApiConfig(apiKey, string.IsNullOrWhiteSpace(engine) ? DefaultEngine : engine);
    }
}
