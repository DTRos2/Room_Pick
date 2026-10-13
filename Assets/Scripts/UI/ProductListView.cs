using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 팝오버 옆에 펼쳐지는 상품 목록(가격 오름차순). 팝오버와 겹치지 않도록 ListPlacement가 오른쪽이나 왼쪽에 놓는다.
/// 팝오버의 자식이 아니라 Canvas 바로 아래에 만들어지므로, visibilitySource가 화면에 보일 때만 보이게 묶는다.
/// 배경은 backgroundSource 패널과 같은 색·모서리를 쓰고, 맨 위에는 "낮은 가격순 · N개" 라벨이 한 줄 붙는다.
/// </summary>
public sealed class ProductListView
{
    readonly RectTransform _root;
    readonly Text _label;
    readonly List<ProductRowView> _rows = new List<ProductRowView>();

    public ProductListView(
        RectTransform backgroundSource, GameObject visibilitySource, UguiKit kit, ProductInfoStyle style,
        int rowCount, Action<int> onRowClick)
    {
        // Canvas를 못 찾으면 패널의 부모에라도 붙여서 화면이 아예 안 만들어지는 일은 피한다.
        Canvas canvas = backgroundSource.GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.rootCanvas.transform : backgroundSource.parent;

        _root = UguiKit.NewRect("ProductList", parent);
        _root.anchorMin = _root.anchorMax = new Vector2(1f, 0.5f);
        _root.pivot = new Vector2(1f, 0.5f);
        _root.sizeDelta = new Vector2(style.listWidth, 0f);
        _root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ListPlacement placement = _root.gameObject.AddComponent<ListPlacement>();
        placement.Anchor = backgroundSource;
        placement.Gap = style.listGap;

        ApplyBackground(_root.gameObject.AddComponent<RoundedRect>(), backgroundSource, style);

        _root.gameObject.AddComponent<CanvasGroup>();
        _root.gameObject.AddComponent<VisibleWhileActive>().Source = visibilitySource;

        VerticalLayoutGroup layout = _root.gameObject.AddComponent<VerticalLayoutGroup>();
        int padding = Mathf.RoundToInt(style.margin);
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = style.listSpacing;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _label = kit.NewText("Label", _root, style.labelFont, FontStyle.Bold, style.mutedText, TextAnchor.MiddleLeft);
        _label.gameObject.AddComponent<LayoutElement>().preferredHeight = style.labelHeight;

        for (int i = 0; i < rowCount; i++)
        {
            int index = i;
            _rows.Add(new ProductRowView(_root, kit, style, () => onRowClick?.Invoke(index)));
        }
    }

    /// <summary>한 번에 보여 줄 수 있는 줄 수. 상품이 이보다 많아도 이만큼만 보인다.</summary>
    public int RowCapacity => _rows.Count;

    public void SetOpen(bool open) => _root.gameObject.SetActive(open);

    /// <summary>앞에서부터 상품을 채우고, 남는 줄은 숨긴다. 첫 줄이 최저가다.</summary>
    public void Bind(IReadOnlyList<Product> sortedProducts, ThumbnailPresenter thumbnails)
    {
        _label.text = $"낮은 가격순 · {Mathf.Min(sortedProducts.Count, _rows.Count)}개";

        for (int i = 0; i < _rows.Count; i++)
        {
            bool hasProduct = i < sortedProducts.Count;
            _rows[i].SetActive(hasProduct);
            if (hasProduct) _rows[i].Bind(sortedProducts[i], thumbnails, lowest: i == 0);
        }
    }

    public void Destroy()
    {
        if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
    }

    /// <summary>패널이 RoundedRect면 그 색과 모서리를 따르고, 아니면 style의 어두운 패널 값을 쓴다.</summary>
    static void ApplyBackground(RoundedRect target, RectTransform source, ProductInfoStyle style)
    {
        RoundedRect sourceRect = source.GetComponent<RoundedRect>();
        target.color = sourceRect != null ? sourceRect.color : style.panelColor;
        target.Radius = sourceRect != null ? sourceRect.Radius : style.cornerRadius;
    }
}
