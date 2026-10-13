using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 최저가 상품 카드: 이미지, 쇼핑몰, 상품명, 가격, 목록을 여닫는 "다른 상품 N개 보기" 글자 버튼.
/// 상품이 없을 때는 안내 문구(찾는 중, 결과 없음, 오류)를 같은 칸에 보여 준다.
/// </summary>
public sealed class ProductCardView
{
    const float ToggleHeight = 26f;

    readonly RectTransform _root;
    readonly GameObject _content;
    readonly RawImage _thumb;
    readonly Text _mall, _title, _price, _status, _toggleLabel;
    readonly Button _toggleButton;

    int _otherCount;
    bool _expanded;

    /// <param name="onClick">카드를 눌렀을 때</param>
    /// <param name="onToggle">"다른 상품 보기" 버튼을 눌렀을 때</param>
    public ProductCardView(RectTransform parent, UguiKit kit, ProductInfoStyle style, Action onClick, Action onToggle)
    {
        _root = UguiKit.NewRect("Card", parent);
        _root.gameObject.AddComponent<LayoutElement>().preferredHeight = style.cardHeight;
        RoundedRect background = _root.gameObject.AddComponent<RoundedRect>();
        background.Radius = style.cornerRadius;
        Button button = _root.gameObject.AddComponent<Button>();
        UguiKit.ApplyHover(button, background, style.cardColor, style.rowHoverColor, style.rowPressedColor);
        button.onClick.AddListener(() => onClick?.Invoke());

        _content = UguiKit.NewRect("Content", _root).gameObject;
        UguiKit.Stretch(_content.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);

        float thumbSize = style.cardHeight - style.pad * 2f;
        RectTransform thumbRect = UguiKit.NewRect("Thumb", _content.transform);
        UguiKit.PlaceLeftCenter(thumbRect, style.pad, thumbSize);
        _thumb = thumbRect.gameObject.AddComponent<RawImage>();
        _thumb.raycastTarget = false;

        // 쇼핑몰 줄의 오른쪽 끝에는 팝오버의 닫기(×) 버튼이 겹치므로 비워 둔다.
        float textLeft = style.pad + thumbSize + style.pad;
        float mallRight = style.closeSize + style.pad * 2f;
        float priceRight = style.toggleWidth + style.pad * 2f;
        _mall = kit.NewText("Mall", _content.transform, style.cardMallFont, FontStyle.Normal, style.mutedText, TextAnchor.MiddleLeft);
        UguiKit.Stretch(_mall.rectTransform, new Vector2(0f, 0.7f), Vector2.one, textLeft, 0f, mallRight, 0f);
        _title = kit.NewText("Title", _content.transform, style.cardNameFont, FontStyle.Bold, style.lightText, TextAnchor.MiddleLeft);
        UguiKit.Stretch(_title.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 0.7f), textLeft, 0f, style.pad, 0f);
        _price = kit.NewText("Price", _content.transform, style.cardPriceFont, FontStyle.Bold, style.accent, TextAnchor.MiddleLeft);
        UguiKit.Stretch(_price.rectTransform, Vector2.zero, new Vector2(1f, 0.42f), textLeft, 0f, priceRight, 0f);

        RoundedRect toggle = UguiKit.NewRect("Toggle", _content.transform).gameObject.AddComponent<RoundedRect>();
        toggle.Radius = style.smallRadius;
        RectTransform toggleRect = toggle.rectTransform;
        toggleRect.anchorMin = toggleRect.anchorMax = toggleRect.pivot = new Vector2(1f, 0f);
        toggleRect.sizeDelta = new Vector2(style.toggleWidth, ToggleHeight);
        toggleRect.anchoredPosition = new Vector2(-style.pad * 2f, style.pad);
        _toggleButton = toggleRect.gameObject.AddComponent<Button>();
        UguiKit.ApplyHover(_toggleButton, toggle, new Color(1f, 1f, 1f, 0.12f), new Color(1f, 1f, 1f, 0.24f), new Color(1f, 1f, 1f, 0.08f));
        _toggleButton.onClick.AddListener(() => onToggle?.Invoke());
        _toggleLabel = kit.NewText("Label", toggleRect, style.toggleFont, FontStyle.Bold, style.lightText, TextAnchor.MiddleCenter);
        UguiKit.Stretch(_toggleLabel.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);

        _status = kit.NewText("Status", _root, style.statusFont, FontStyle.Normal, style.lightText, TextAnchor.MiddleCenter);
        UguiKit.Stretch(_status.rectTransform, Vector2.zero, Vector2.one, style.pad, 0f, style.pad, 0f);
    }

    /// <param name="otherCount">최저가 말고 목록에서 더 볼 수 있는 상품 수. 0이면 버튼을 숨긴다.</param>
    public void ShowProduct(Product product, int otherCount, ThumbnailPresenter thumbnails)
    {
        _status.gameObject.SetActive(false);
        _content.SetActive(true);
        _mall.text = product.MallName;
        _title.text = product.Title;
        _price.text = PriceFormatter.Format(product.Price);
        _otherCount = otherCount;
        _toggleButton.gameObject.SetActive(otherCount > 0);
        RefreshToggleLabel();
        thumbnails.Show(_thumb, product.ImageUrl);
    }

    public void ShowMessage(string message)
    {
        _content.SetActive(false);
        _status.gameObject.SetActive(true);
        _status.text = message;
    }

    /// <summary>목록이 펼쳐졌으면 "목록 접기", 접혀 있으면 "다른 상품 N개 보기"를 그린다.</summary>
    public void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        RefreshToggleLabel();
    }

    void RefreshToggleLabel() => _toggleLabel.text = _expanded ? "목록 접기" : $"다른 상품 {_otherCount}개 보기";
}
