using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 최저가 상품 카드: 이미지, 쇼핑몰, 상품명, 가격, 목록을 여닫는 화살표.
/// 상품이 없을 때는 안내 문구(찾는 중, 결과 없음, 오류)를 같은 칸에 보여 준다.
/// </summary>
public sealed class ProductCardView
{
    readonly RectTransform _root;
    readonly GameObject _content;
    readonly RawImage _thumb;
    readonly Text _mall, _title, _price, _status, _arrowLabel;
    readonly Button _arrowButton;

    /// <param name="onClick">카드를 눌렀을 때</param>
    /// <param name="onToggle">화살표를 눌렀을 때</param>
    public ProductCardView(RectTransform parent, UguiKit kit, ProductInfoStyle style, Action onClick, Action onToggle)
    {
        _root = UguiKit.NewRect("Card", parent);
        _root.gameObject.AddComponent<LayoutElement>().preferredHeight = style.cardHeight;
        var background = _root.gameObject.AddComponent<Image>();
        background.color = style.cardBackground;
        var button = _root.gameObject.AddComponent<Button>();
        UguiKit.ApplyHover(button, background, style);
        button.onClick.AddListener(() => onClick?.Invoke());

        _content = UguiKit.NewRect("Content", _root).gameObject;
        UguiKit.Stretch(_content.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);

        float thumbSize = style.cardHeight - style.pad * 2f;
        var thumbRect = UguiKit.NewRect("Thumb", _content.transform);
        UguiKit.PlaceLeftCenter(thumbRect, style.pad, thumbSize);
        _thumb = thumbRect.gameObject.AddComponent<RawImage>();
        _thumb.raycastTarget = false;

        float textLeft = style.pad + thumbSize + style.pad;
        float textRight = style.arrowWidth + style.pad;
        _mall = kit.NewText("Mall", _content.transform, style.cardMallFont, FontStyle.Normal, style.grayText, TextAnchor.MiddleLeft);
        UguiKit.Stretch(_mall.rectTransform, new Vector2(0f, 0.7f), Vector2.one, textLeft, 0f, textRight, 0f);
        _title = kit.NewText("Title", _content.transform, style.cardTitleFont, FontStyle.Bold, style.darkText, TextAnchor.MiddleLeft);
        UguiKit.Stretch(_title.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 0.7f), textLeft, 0f, textRight, 0f);
        _price = kit.NewText("Price", _content.transform, style.cardPriceFont, FontStyle.Bold, style.darkText, TextAnchor.MiddleLeft);
        UguiKit.Stretch(_price.rectTransform, Vector2.zero, new Vector2(1f, 0.38f), textLeft, 0f, textRight, 0f);

        var arrow = UguiKit.NewRect("Arrow", _content.transform);
        arrow.anchorMin = new Vector2(1f, 0f);
        arrow.anchorMax = Vector2.one;
        arrow.pivot = new Vector2(1f, 0.5f);
        arrow.sizeDelta = new Vector2(style.arrowWidth, 0f);
        arrow.anchoredPosition = Vector2.zero;
        arrow.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        _arrowButton = arrow.gameObject.AddComponent<Button>();
        _arrowButton.transition = Selectable.Transition.None;
        _arrowButton.onClick.AddListener(() => onToggle?.Invoke());
        _arrowLabel = kit.NewText("Label", arrow, style.arrowFont, FontStyle.Bold, style.grayText, TextAnchor.MiddleCenter);
        UguiKit.Stretch(_arrowLabel.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);

        _status = kit.NewText("Status", _root, style.statusFont, FontStyle.Normal, style.grayText, TextAnchor.MiddleCenter);
        UguiKit.Stretch(_status.rectTransform, Vector2.zero, Vector2.one, style.pad, 0f, style.pad, 0f);
    }

    /// <param name="canExpand">펼칠 목록이 있을 때만 화살표를 보여 준다.</param>
    public void ShowProduct(Product product, bool canExpand, ThumbnailPresenter thumbnails)
    {
        _status.gameObject.SetActive(false);
        _content.SetActive(true);
        _mall.text = product.MallName;
        _title.text = product.Title;
        _price.text = PriceFormatter.Format(product.Price);
        _arrowButton.gameObject.SetActive(canExpand);
        thumbnails.Show(_thumb, product.ImageUrl);
    }

    public void ShowMessage(string message)
    {
        _content.SetActive(false);
        _status.gameObject.SetActive(true);
        _status.text = message;
    }

    /// <summary>목록이 펼쳐졌으면 "&lt;", 접혀 있으면 "&gt;"를 그린다.</summary>
    public void SetExpanded(bool expanded) => _arrowLabel.text = expanded ? "<" : ">";
}
