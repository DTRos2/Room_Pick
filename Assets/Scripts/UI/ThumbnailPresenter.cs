using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RawImage에 상품 이미지를 채운다. 받아 오는 동안은 회색으로 두고, 도착했을 때 그 RawImage가
/// 아직 같은 요청을 기다리는 경우에만 적용한다. (다른 상품으로 바뀐 뒤에 늦게 온 이미지는 버린다.)
/// </summary>
public sealed class ThumbnailPresenter
{
    readonly ThumbnailLoader _loader;
    readonly CancellationToken _lifetime;
    readonly Color _placeholder;
    readonly Dictionary<RawImage, int> _tokens = new Dictionary<RawImage, int>();
    int _counter;

    public ThumbnailPresenter(ThumbnailLoader loader, Color placeholder, CancellationToken lifetime)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _placeholder = placeholder;
        _lifetime = lifetime;
    }

    public async void Show(RawImage target, string url)
    {
        int token = ++_counter;
        _tokens[target] = token;
        target.texture = null;
        target.color = _placeholder;

        try
        {
            Texture2D texture = await _loader.LoadAsync(url, _lifetime);
            if (texture == null || target == null) return;
            if (!_tokens.TryGetValue(target, out int current) || current != token) return;

            target.texture = texture;
            target.color = Color.white;
        }
        catch (OperationCanceledException)
        {
            // 화면이 사라지는 중이다. 정상 동작이다.
        }
        catch (Exception e)
        {
            // async void라 예외를 던지면 아무도 받지 못한다. 회색 칸으로 두고 원인만 남긴다.
            Debug.LogWarning($"[ThumbnailPresenter] 이미지를 표시하지 못했습니다: {e.Message}");
        }
    }

    /// <summary>아직 오지 않은 이미지가 있어도 나중에 적용되지 않게 한다.</summary>
    public void CancelPending() => _tokens.Clear();
}
