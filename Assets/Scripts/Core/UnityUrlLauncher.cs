using UnityEngine;

/// <summary>
/// Application.OpenURL로 기본 브라우저를 연다.
/// </summary>
public sealed class UnityUrlLauncher : IUrlLauncher
{
    public void Open(string url) => Application.OpenURL(url);
}
