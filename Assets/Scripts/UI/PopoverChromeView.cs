using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 색상 팝오버 패널에 얹는 장식: 오른쪽 위 닫기(×) 버튼과, 색상 버튼 줄 바로 위의 "색상" 라벨.
/// 색상 버튼 자체는 건드리지 않고, 버튼 줄의 위치를 읽어서 그 위에 라벨만 놓는다.
/// 닫기 동작은 onClose로 받으므로, 빈 곳을 눌러 닫는 기존 동작과 같은 경로(팝오버 Close)를 쓴다.
/// </summary>
public sealed class PopoverChromeView
{
    const string CloseMark = "×";
    const string ColorLabelText = "색상";
    const float ColorLabelWidth = 80f;
    const float CloseMarkLift = 5f;

    readonly RectTransform _root;
    readonly RectTransform _colorLabel;
    readonly ProductInfoStyle _style;

    public PopoverChromeView(RectTransform popoverPanel, UguiKit kit, ProductInfoStyle style, Action onClose)
    {
        _style = style;

        // 패널을 덮는 투명한 틀. 자기 자신은 클릭을 막지 않고, 레이아웃에도 끼지 않는다.
        _root = UguiKit.NewRect("PopoverChrome", popoverPanel);
        _root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        UguiKit.Stretch(_root, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);

        BuildCloseButton(kit, onClose);

        Text label = kit.NewText("ColorLabel", _root, style.labelFont, FontStyle.Bold, style.mutedText, TextAnchor.MiddleLeft);
        label.text = ColorLabelText;
        _colorLabel = label.rectTransform;
        _colorLabel.anchorMin = _colorLabel.anchorMax = _colorLabel.pivot = Vector2.zero;
        _colorLabel.sizeDelta = new Vector2(ColorLabelWidth, style.labelHeight);
    }

    /// <param name="buttonsTop">패널 바닥에서 색상 버튼 줄의 위쪽 끝까지의 높이. 음수면 위치를 몰라 라벨을 숨긴다.</param>
    public void PlaceColorLabel(float buttonsTop)
    {
        _colorLabel.gameObject.SetActive(buttonsTop >= 0f);
        if (buttonsTop < 0f) return;

        _colorLabel.anchoredPosition = new Vector2(_style.margin, buttonsTop + _style.colorLabelGap);
    }

    public void Destroy()
    {
        if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
    }

    void BuildCloseButton(UguiKit kit, Action onClose)
    {
        RoundedRect background = UguiKit.NewRect("CloseButton", _root).gameObject.AddComponent<RoundedRect>();
        background.Radius = _style.closeSize * 0.5f;
        background.raycastTarget = true;

        RectTransform rect = background.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.sizeDelta = new Vector2(_style.closeSize, _style.closeSize);
        rect.anchoredPosition = new Vector2(-_style.pad * 2f, -_style.pad * 2f);

        Button button = rect.gameObject.AddComponent<Button>();
        UguiKit.ApplyHover(button, background, new Color(1f, 1f, 1f, 0.12f), new Color(1f, 1f, 1f, 0.28f), new Color(1f, 1f, 1f, 0.08f));
        button.onClick.AddListener(() => onClose?.Invoke());

        Text mark = kit.NewText("Label", rect, _style.labelFont + 4, FontStyle.Bold, _style.lightText, TextAnchor.MiddleCenter);
        // × 글자는 글꼴 기준선 때문에 원 안에서 아래로 처져 보이므로, 글자 영역을 위로 올려 가운데에 맞춘다.
        // 높이를 줄이면 글자가 영역보다 커서 아예 그려지지 않으므로, 아래는 올리고 위는 그만큼 늘려 높이를 유지한다.
        UguiKit.Stretch(mark.rectTransform, Vector2.zero, Vector2.one, 0f, CloseMarkLift, 0f, -CloseMarkLift);
        mark.verticalOverflow = VerticalWrapMode.Overflow;
        mark.text = CloseMark;
    }
}
