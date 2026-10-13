using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상품 목록의 한 줄: 이미지, 상품명(최대 2줄), 가격, 오른쪽 ↗ 표시. 누르면 onClick이 불린다.
/// 첫 줄(최저가)에는 이미지 위에 "최저가" 뱃지가 붙는다.
/// </summary>
public sealed class ProductRowView
{
    const int TitleMaxLines = 2;
    const string ExternalMark = "↗";
    const float BadgeWidth = 48f, BadgeHeight = 20f, MarkWidth = 32f;

    readonly RectTransform _root;
    readonly RawImage _thumb;
    readonly Text _title, _price;
    readonly GameObject _badge;
    readonly float _titleWidth;

    public ProductRowView(RectTransform parent, UguiKit kit, ProductInfoStyle style, Action onClick)
    {
        _root = UguiKit.NewRect("Row", parent);
        _root.gameObject.AddComponent<LayoutElement>().preferredHeight = style.rowHeight;
        RoundedRect background = _root.gameObject.AddComponent<RoundedRect>();
        background.Radius = style.cornerRadius;
        Button button = _root.gameObject.AddComponent<Button>();
        UguiKit.ApplyHover(button, background, style.rowColor, style.rowHoverColor, style.rowPressedColor);
        button.onClick.AddListener(() => onClick?.Invoke());

        float thumbSize = style.rowHeight - style.pad * 4f;
        float thumbLeft = style.pad * 2f;
        RectTransform thumbRect = UguiKit.NewRect("Thumb", _root);
        UguiKit.PlaceLeftCenter(thumbRect, thumbLeft, thumbSize);
        _thumb = thumbRect.gameObject.AddComponent<RawImage>();
        _thumb.raycastTarget = false;

        _badge = BuildBadge(kit, style, thumbLeft);

        // 오른쪽 끝에는 "누르면 밖으로 나간다"는 ↗ 표시를 둔다.
        Text mark = kit.NewText("ExternalMark", _root, style.rowTitleFont + 4, FontStyle.Bold, style.mutedText, TextAnchor.MiddleCenter);
        mark.rectTransform.anchorMin = new Vector2(1f, 0f);
        mark.rectTransform.anchorMax = Vector2.one;
        mark.rectTransform.pivot = new Vector2(1f, 0.5f);
        mark.rectTransform.sizeDelta = new Vector2(MarkWidth, 0f);
        mark.rectTransform.anchoredPosition = new Vector2(-style.pad, 0f);
        mark.text = ExternalMark;

        float textLeft = thumbLeft + thumbSize + style.pad * 2f;
        float textRight = style.pad + MarkWidth + style.pad;
        _title = kit.NewText("Title", _root, style.rowTitleFont, FontStyle.Bold, style.lightText, TextAnchor.MiddleLeft);
        UguiKit.Stretch(_title.rectTransform, new Vector2(0f, 0.38f), Vector2.one, textLeft, 0f, textRight, style.pad);
        _price = kit.NewText("Price", _root, style.rowPriceFont, FontStyle.Bold, style.accent, TextAnchor.MiddleLeft);
        UguiKit.Stretch(_price.rectTransform, Vector2.zero, new Vector2(1f, 0.38f), textLeft, style.pad, textRight, 0f);

        // 목록 폭은 style이 정하므로, 레이아웃이 계산되기 전에도 글이 들어갈 폭을 알 수 있다.
        _titleWidth = style.listWidth - style.margin * 2f - textLeft - textRight;
    }

    public void SetActive(bool active) => _root.gameObject.SetActive(active);

    /// <param name="lowest">가장 싼 상품이면 "최저가" 뱃지를 보여 준다.</param>
    public void Bind(Product product, ThumbnailPresenter thumbnails, bool lowest)
    {
        UguiKit.SetTextWithEllipsis(_title, product.Title, _titleWidth, TitleMaxLines);
        _price.text = PriceFormatter.Format(product.Price);
        _badge.SetActive(lowest);
        thumbnails.Show(_thumb, product.ImageUrl);
    }

    /// <summary>이미지 왼쪽 위 모서리에 걸치는 작은 "최저가" 뱃지.</summary>
    GameObject BuildBadge(UguiKit kit, ProductInfoStyle style, float thumbLeft)
    {
        RoundedRect badge = UguiKit.NewRect("LowestBadge", _root).gameObject.AddComponent<RoundedRect>();
        badge.color = style.accent;
        badge.Radius = style.smallRadius;
        badge.raycastTarget = false;

        RectTransform rect = badge.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(BadgeWidth, BadgeHeight);
        rect.anchoredPosition = new Vector2(thumbLeft, -style.pad * 2f);

        Text label = kit.NewText("Label", rect, style.badgeFont, FontStyle.Bold, style.panelColor, TextAnchor.MiddleCenter);
        UguiKit.Stretch(label.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        label.text = "최저가";
        return badge.gameObject;
    }
}
