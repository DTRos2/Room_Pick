using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상품 목록의 한 줄: 이미지, 상품명(최대 2줄), 가격, 오른쪽 ↗ 표시. 누르면 onClick이 불린다.
/// 첫 줄(최저가)에는 가격 옆에 "최저가" 알약이 붙는다.
/// </summary>
public sealed class ProductRowView
{
    const int TitleMaxLines = 2;
    const string ExternalMark = "↗";
    const float BadgeWidth = 52f, BadgeHeight = 20f, MarkWidth = 32f;

    static readonly TextGenerator Measurer = new TextGenerator();

    readonly RectTransform _root;
    readonly RawImage _thumb;
    readonly Text _title, _price;
    readonly RectTransform _badge;
    readonly float _titleWidth, _badgeGap;
    readonly Vector2 _priceSize;

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
        // 가격 줄: 가격 글자는 줄을 꽉 채우고, 최저가 알약은 글자 끝 옆에 따로 놓는다(위치는 PlaceBadge가 잰다).
        RectTransform priceLine = UguiKit.NewRect("PriceLine", _root);
        UguiKit.Stretch(priceLine, Vector2.zero, new Vector2(1f, 0.38f), textLeft, style.pad, textRight, 0f);
        _price = kit.NewText("Price", priceLine, style.rowPriceFont, FontStyle.Bold, style.accent, TextAnchor.MiddleLeft);
        _price.horizontalOverflow = HorizontalWrapMode.Overflow;
        UguiKit.Stretch(_price.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        _priceSize = new Vector2(style.listWidth - style.margin * 2f - textLeft - textRight, style.rowHeight * 0.38f - style.pad);
        _badgeGap = style.pad + 2f;
        _badge = BuildBadge(kit, style, priceLine);

        // 목록 폭은 style이 정하므로, 레이아웃이 계산되기 전에도 글이 들어갈 폭을 알 수 있다.
        _titleWidth = style.listWidth - style.margin * 2f - textLeft - textRight;
    }

    public void SetActive(bool active) => _root.gameObject.SetActive(active);

    /// <param name="lowest">가장 싼 상품이면 "최저가" 뱃지를 보여 준다.</param>
    public void Bind(Product product, ThumbnailPresenter thumbnails, bool lowest)
    {
        UguiKit.SetTextWithEllipsis(_title, product.Title, _titleWidth, TitleMaxLines);
        _price.text = PriceFormatter.Format(product.Price);
        _badge.gameObject.SetActive(lowest);
        if (lowest) PlaceBadge();
        thumbnails.Show(_thumb, product.ImageUrl);
    }

    /// <summary>
    /// 알약을 가격 글자 끝 옆에, 가격 숫자가 실제로 그려진 높이의 가운데에 놓는다.
    /// 글자 상자의 가운데와 숫자가 보이는 높이는 글꼴마다 달라서, 첫 글자(숫자)의 그려진 범위를 직접 읽는다.
    /// </summary>
    void PlaceBadge()
    {
        float digitCenter = 0f;
        Measurer.Populate(_price.text, _price.GetGenerationSettings(_priceSize));
        IList<UIVertex> verts = Measurer.verts;
        if (verts.Count >= 4)
        {
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                min = Mathf.Min(min, verts[i].position.y);
                max = Mathf.Max(max, verts[i].position.y);
            }
            digitCenter = (min + max) * 0.5f / _price.pixelsPerUnit;
        }

        _badge.anchoredPosition = new Vector2(_price.preferredWidth + _badgeGap, digitCenter);
    }

    /// <summary>가격 옆에 붙는 작은 "최저가" 알약.</summary>
    RectTransform BuildBadge(UguiKit kit, ProductInfoStyle style, RectTransform parent)
    {
        RoundedRect badge = UguiKit.NewRect("LowestBadge", parent).gameObject.AddComponent<RoundedRect>();
        badge.color = style.badgeColor;
        badge.Radius = BadgeHeight * 0.5f; // 높이의 절반이면 양끝이 둥근 알약이 된다.
        badge.raycastTarget = false;

        RectTransform rect = badge.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(BadgeWidth, BadgeHeight);

        Text label = kit.NewText("Label", rect, style.badgeFont, FontStyle.Bold, style.badgeText, TextAnchor.MiddleCenter);
        UguiKit.Stretch(label.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);
        label.text = "최저가";
        return rect;
    }
}
