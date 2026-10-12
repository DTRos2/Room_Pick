using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상품 목록의 한 줄: 이미지, 상품명, 가격. 누르면 onClick이 불린다.
/// </summary>
public sealed class ProductRowView
{
    readonly RectTransform _root;
    readonly RawImage _thumb;
    readonly Text _title, _price;

    public ProductRowView(RectTransform parent, UguiKit kit, ProductInfoStyle style, Action onClick)
    {
        _root = UguiKit.NewRect("Row", parent);
        _root.gameObject.AddComponent<LayoutElement>().preferredHeight = style.rowHeight;
        Image background = _root.gameObject.AddComponent<Image>();
        background.color = style.rowBackground;
        Button button = _root.gameObject.AddComponent<Button>();
        UguiKit.ApplyHover(button, background, style);
        button.onClick.AddListener(() => onClick?.Invoke());

        float thumbSize = style.rowHeight - style.pad * 2f;
        RectTransform thumbRect = UguiKit.NewRect("Thumb", _root);
        UguiKit.PlaceLeftCenter(thumbRect, style.pad, thumbSize);
        _thumb = thumbRect.gameObject.AddComponent<RawImage>();
        _thumb.raycastTarget = false;

        float textLeft = style.pad + thumbSize + style.pad;
        _title = kit.NewText("Title", _root, style.rowTitleFont, FontStyle.Bold, style.darkText, TextAnchor.MiddleLeft);
        UguiKit.Stretch(_title.rectTransform, new Vector2(0f, 0.42f), Vector2.one, textLeft, 0f, style.pad, 0f);
        _price = kit.NewText("Price", _root, style.rowPriceFont, FontStyle.Bold, style.darkText, TextAnchor.MiddleLeft);
        UguiKit.Stretch(_price.rectTransform, Vector2.zero, new Vector2(1f, 0.42f), textLeft, 0f, style.pad, 0f);
    }

    public void SetActive(bool active) => _root.gameObject.SetActive(active);

    public void Bind(Product product, ThumbnailPresenter thumbnails)
    {
        _title.text = product.Title;
        _price.text = PriceFormatter.Format(product.Price);
        thumbnails.Show(_thumb, product.ImageUrl);
    }
}
