using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 상품 썸네일을 주소에서 내려받아 밉맵이 있는 Texture2D로 돌려준다. 같은 주소는 한 번만 받고 보관한다.
/// 보관한 텍스처는 Dispose에서 해제하므로, 쓰는 쪽이 파괴될 때 호출해야 한다.
/// </summary>
public sealed class ThumbnailLoader : IDisposable
{
    const int TimeoutSeconds = 10;

    readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

    /// <summary>받지 못하면(주소 없음, 네트워크 오류, 이미지가 아님) 경고만 남기고 null을 돌려준다.</summary>
    public async Task<Texture2D> LoadAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (_cache.TryGetValue(url, out var cached)) return cached;

        using var request = UnityWebRequest.Get(url);
        request.timeout = TimeoutSeconds;

        var completion = new TaskCompletionSource<bool>();
        request.SendWebRequest().completed += _ => completion.TrySetResult(true);
        using (cancellationToken.Register(() =>
        {
            request.Abort();
            completion.TrySetCanceled();
        }))
        {
            await completion.Task;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ThumbnailLoader] 이미지를 받지 못했습니다 ({request.responseCode}): {request.error}");
            return null;
        }

        // 큰 원본(300px)을 작은 칸에 줄여 그릴 때 흐려지거나 깨지지 않도록 밉맵을 만들고 트라일리니어로 그린다.
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        if (!texture.LoadImage(request.downloadHandler.data, true))
        {
            UnityEngine.Object.Destroy(texture);
            Debug.LogWarning($"[ThumbnailLoader] 이미지 형식을 읽지 못했습니다: {url}");
            return null;
        }
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 8;
        texture.wrapMode = TextureWrapMode.Clamp;

        _cache[url] = texture;
        return texture;
    }

    public void Dispose()
    {
        foreach (var texture in _cache.Values)
        {
            if (texture != null) UnityEngine.Object.Destroy(texture);
        }
        _cache.Clear();
    }
}
