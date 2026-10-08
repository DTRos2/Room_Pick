using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 색상 팝오버 안, 색상 버튼 바로 위에 들어가는 상품 정보 화면. 최저가 상품 카드를 먼저 보여 주고,
/// 오른쪽 화살표를 누르면 그 위로 상품 목록(이미지·가격)이 펼쳐진다. 카드나 목록을 누르면 그 상품 페이지가 열린다.
/// 씬에 따로 UI를 만들지 않도록 코드로 uGUI를 구성하며, 팝오버의 자식이라 팝오버와 함께 움직이고 숨는다.
/// </summary>
public sealed class ProductInfoView
{
    const float Margin = 8f, CardHeight = 56f, RowHeight = 48f, Gap = 4f, Pad = 4f, ArrowWidth = 28f;

    /// <summary>팝오버 세로 중앙에서 카드 아래쪽까지의 거리. 색상 버튼(중앙 기준 약 +25까지) 바로 위에 오도록 맞춘 값이다.</summary>
    const float CardBottomOffset = 27f;

    static readonly Color Background = new Color(1f, 1f, 1f, 0.97f);
    static readonly Color RowBackground = new Color(0.96f, 0.96f, 0.96f, 0.97f);
    static readonly Color Dark = new Color(0.12f, 0.12f, 0.12f);
    static readonly Color Gray = new Color(0.45f, 0.45f, 0.45f);
    static readonly Color Placeholder = new Color(0.85f, 0.85f, 0.85f);

    struct Row
    {
        public GameObject Root;
        public RawImage Thumb;
        public Text Price;
    }

    readonly RectTransform _root;
    readonly ThumbnailLoader _thumbnails;
    readonly CancellationToken _lifetime;
    readonly Font _font;

    readonly GameObject _listGroup;
    readonly List<Row> _rows = new List<Row>();

    readonly GameObject _cardContent;
    readonly RawImage _cardThumb;
    readonly Text _cardMall, _cardTitle, _cardPrice, _status, _arrowLabel;
    readonly Button _arrowButton;

    IReadOnlyList<Product> _products = Array.Empty<Product>();
    bool _expanded;
    int _version;

    /// <param name="popoverPanel">이 화면을 안에 넣을 색상 팝오버 패널</param>
    public ProductInfoView(RectTransform popoverPanel, ThumbnailLoader thumbnails, int listCount, CancellationToken lifetime)
    {
        _thumbnails = thumbnails;
        _lifetime = lifetime;
        _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 16);

        // 팝오버 가로 폭에 맞춰 늘어나고, 세로는 팝오버 중앙을 기준으로 색상 버튼 위에 아래쪽을 맞춘다.
        _root = NewRect("ProductInfoView", popoverPanel);
        _root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _root.anchorMin = new Vector2(0f, 0.5f);
        _root.anchorMax = new Vector2(1f, 0.5f);
        _root.pivot = new Vector2(0.5f, 0f);
        _root.offsetMin = new Vector2(Margin, 0f);
        _root.offsetMax = new Vector2(-Margin, 0f);
        _root.anchoredPosition = new Vector2(0f, CardBottomOffset);

        var rootLayout = _root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.spacing = Gap;
        rootLayout.childControlWidth = rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true; // 카드·목록이 부모 폭에 맞게 펼쳐지도록 한다. 높이는 각자 정한다.
        rootLayout.childForceExpandHeight = false;
        _root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 위쪽: 상품 목록(처음에는 접혀 있음)
        var list = NewRect("List", _root);
        _listGroup = list.gameObject;
        var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 2f;
        listLayout.childControlWidth = listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        for (int i = 0; i < listCount; i++)
            _rows.Add(BuildRow(list));

        // 아래쪽: 최저가 카드
        var card = NewRect("Card", _root);
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = CardHeight;
        var cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = Background;
        var cardButton = card.gameObject.AddComponent<Button>();
        ApplyHover(cardButton, cardImage);
        cardButton.onClick.AddListener(() => OpenProduct(0));

        _cardContent = NewRect("Content", card).gameObject;
        Stretch(_cardContent.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);

        float thumbSize = CardHeight - Pad * 2f;
        var thumbRect = NewRect("Thumb", _cardContent.transform);
        PlaceLeftCenter(thumbRect, Pad, thumbSize);
        _cardThumb = thumbRect.gameObject.AddComponent<RawImage>();
        _cardThumb.raycastTarget = false;

        float textLeft = Pad + thumbSize + Pad;
        float textRight = ArrowWidth + Pad;
        _cardMall = NewText("Mall", _cardContent.transform, 11, FontStyle.Normal, Gray, TextAnchor.MiddleLeft);
        Stretch(_cardMall.rectTransform, new Vector2(0f, 0.7f), Vector2.one, textLeft, 0f, textRight, 0f);
        _cardTitle = NewText("Title", _cardContent.transform, 12, FontStyle.Normal, Dark, TextAnchor.MiddleLeft);
        Stretch(_cardTitle.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 0.7f), textLeft, 0f, textRight, 0f);
        _cardPrice = NewText("Price", _cardContent.transform, 14, FontStyle.Bold, Dark, TextAnchor.MiddleLeft);
        Stretch(_cardPrice.rectTransform, Vector2.zero, new Vector2(1f, 0.38f), textLeft, 0f, textRight, 0f);

        var arrow = NewRect("Arrow", _cardContent.transform);
        arrow.anchorMin = new Vector2(1f, 0f);
        arrow.anchorMax = Vector2.one;
        arrow.pivot = new Vector2(1f, 0.5f);
        arrow.sizeDelta = new Vector2(ArrowWidth, 0f);
        arrow.anchoredPosition = Vector2.zero;
        arrow.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        _arrowButton = arrow.gameObject.AddComponent<Button>();
        _arrowButton.transition = Selectable.Transition.None;
        _arrowButton.onClick.AddListener(ToggleList);
        _arrowLabel = NewText("Label", arrow, 18, FontStyle.Bold, Gray, TextAnchor.MiddleCenter);
        Stretch(_arrowLabel.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);

        _status = NewText("Status", card, 12, FontStyle.Normal, Gray, TextAnchor.MiddleCenter);
        Stretch(_status.rectTransform, Vector2.zero, Vector2.one, Pad, 0f, Pad, 0f);

        Hide();
    }

    public void ShowLoading() => ShowMessage("상품을 찾는 중...");
    public void ShowEmpty() => ShowMessage("검색 결과가 없습니다");
    public void ShowError(string message) => ShowMessage(message);

    /// <summary>가격 오름차순으로 정렬된 상품을 보여 준다. 첫 번째가 최저가 카드가 된다.</summary>
    public void ShowProducts(IReadOnlyList<Product> sortedProducts)
    {
        if (sortedProducts == null || sortedProducts.Count == 0)
        {
            ShowEmpty();
            return;
        }

        _version++;
        _products = sortedProducts;
        _status.gameObject.SetActive(false);
        _cardContent.SetActive(true);

        var lowest = sortedProducts[0];
        _cardMall.text = lowest.MallName;
        _cardTitle.text = lowest.Title;
        _cardPrice.text = FormatPrice(lowest.Price);
        LoadThumbnail(_cardThumb, lowest, _version);

        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            bool hasProduct = i < sortedProducts.Count;
            row.Root.SetActive(hasProduct);
            if (!hasProduct) continue;

            row.Price.text = FormatPrice(sortedProducts[i].Price);
            LoadThumbnail(row.Thumb, sortedProducts[i], _version);
        }

        // 상품이 하나뿐이면 펼칠 목록이 없다.
        _arrowButton.gameObject.SetActive(sortedProducts.Count > 1);
        SetExpanded(false);
        _root.gameObject.SetActive(true);
    }

    public void Hide()
    {
        _version++;
        _root.gameObject.SetActive(false);
    }

    public void Destroy()
    {
        if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
    }

    void ShowMessage(string message)
    {
        _version++;
        _products = Array.Empty<Product>();
        _cardContent.SetActive(false);
        _status.gameObject.SetActive(true);
        _status.text = message;
        SetExpanded(false);
        _root.gameObject.SetActive(true);
    }

    void ToggleList() => SetExpanded(!_expanded);

    void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        _listGroup.SetActive(expanded);
        _arrowLabel.text = expanded ? "v" : ">";
    }

    void OpenProduct(int index)
    {
        if (index < 0 || index >= _products.Count) return;
        _ = ProductLinkOpener.OpenAsync(_products[index]);
    }

    Row BuildRow(RectTransform parent)
    {
        var rect = NewRect("Row", parent);
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;
        var rowImage = rect.gameObject.AddComponent<Image>();
        rowImage.color = RowBackground;
        var button = rect.gameObject.AddComponent<Button>();
        ApplyHover(button, rowImage);
        int index = _rows.Count;
        button.onClick.AddListener(() => OpenProduct(index));

        float thumbSize = RowHeight - Pad * 2f;
        var thumbRect = NewRect("Thumb", rect);
        PlaceLeftCenter(thumbRect, Pad, thumbSize);
        var thumb = thumbRect.gameObject.AddComponent<RawImage>();
        thumb.raycastTarget = false;

        var price = NewText("Price", rect, 14, FontStyle.Bold, Dark, TextAnchor.MiddleLeft);
        Stretch(price.rectTransform, Vector2.zero, Vector2.one, Pad + thumbSize + Pad, 0f, Pad, 0f);

        return new Row { Root = rect.gameObject, Thumb = thumb, Price = price };
    }

    /// <summary>이미지를 받아 오는 동안 회색으로 두고, 도착했을 때 같은 화면(version)일 때만 적용한다.</summary>
    async void LoadThumbnail(RawImage target, Product product, int version)
    {
        target.texture = null;
        target.color = Placeholder;
        try
        {
            var texture = await _thumbnails.LoadAsync(product.ImageUrl, _lifetime);
            if (texture == null || target == null || version != _version) return;
            target.texture = texture;
            target.color = Color.white;
        }
        catch (OperationCanceledException)
        {
            // 화면이 사라지는 중이다. 정상 동작이다.
        }
    }

    /// <summary>마우스를 올리면 배경이 살짝 푸른빛으로 바뀌고, 누르는 동안 더 진해진다.</summary>
    static void ApplyHover(Button button, Graphic background)
    {
        // 코드로 붙인 Button은 targetGraphic이 비어 있으므로 직접 지정해야 색이 바뀐다.
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.86f, 0.91f, 1f);
        colors.pressedColor = new Color(0.76f, 0.84f, 0.97f);
        colors.selectedColor = Color.white; // 클릭한 뒤에도 눌린 색이 남지 않게 한다.
        colors.disabledColor = Color.white;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    static string FormatPrice(int price) => $"{price:N0}원";

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    Text NewText(string name, Transform parent, int size, FontStyle style, Color color, TextAnchor alignment)
    {
        var text = NewRect(name, parent).gameObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, float left, float bottom, float right, float top)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    static void PlaceLeftCenter(RectTransform rect, float left, float size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(left, 0f);
        rect.sizeDelta = new Vector2(size, size);
    }
}
