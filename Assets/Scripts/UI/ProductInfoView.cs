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
    const float Margin = 12f, CardHeight = 100f, RowHeight = 128f, Gap = 4f, Pad = 6f, ArrowWidth = 40f;

    /// <summary>색상 버튼을 찾지 못했을 때 쓰는, 패널 바닥에서 카드 아래쪽까지의 거리.</summary>
    const float FallbackBottomInset = 62f;

    /// <summary>화면 오른쪽 끝에 고정되는 목록의 폭과 오른쪽 여백(Canvas 기준 해상도 단위).</summary>
    const float ListWidth = 600f, ListRightMargin = 16f;

    /// <summary>목록이 Canvas의 자식이라 팝오버와 함께 꺼지지 않으므로, 기준 오브젝트가 화면에 보일 때만 보이게 한다.</summary>
    sealed class VisibilityFollower : MonoBehaviour
    {
        public GameObject Source;
        CanvasGroup _group;

        void OnEnable() => Apply();
        void Update() => Apply();

        void Apply()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            bool visible = Source != null && Source.activeInHierarchy;
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
        }
    }

    static readonly Color Background = new Color(1f, 1f, 1f, 0.97f);
    static readonly Color RowBackground = new Color(0.96f, 0.96f, 0.96f, 0.97f);
    static readonly Color Dark = new Color(0.04f, 0.04f, 0.04f);
    static readonly Color Gray = new Color(0.3f, 0.3f, 0.3f);
    static readonly Color Placeholder = new Color(0.85f, 0.85f, 0.85f);

    struct Row
    {
        public GameObject Root;
        public RawImage Thumb;
        public Text Title, Price;
    }

    readonly RectTransform _root, _popoverPanel;
    readonly ThumbnailLoader _thumbnails;
    readonly CancellationToken _lifetime;
    readonly Font _font;

    readonly GameObject _listGroup;
    readonly List<Row> _rows = new List<Row>();

    readonly GameObject _card, _cardContent;
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
        _popoverPanel = popoverPanel;
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.pivot = new Vector2(0.5f, 0.5f);
        FitToPanel();

        var rootLayout = _root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.spacing = Gap;
        rootLayout.childControlWidth = rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true; // 카드가 패널 안쪽 영역 전체를 채운다.
        rootLayout.childForceExpandHeight = true;

        // 위쪽: 상품 목록(처음에는 접혀 있음)
        // 팝오버가 아니라 Canvas 바로 아래에 두어, 팝오버 위치와 상관없이 화면 맨 오른쪽 가운데에 고정한다.
        var canvasRoot = popoverPanel.GetComponentInParent<Canvas>().rootCanvas.transform;
        var list = NewRect("ProductList", canvasRoot);
        _listGroup = list.gameObject;
        list.anchorMin = list.anchorMax = new Vector2(1f, 0.5f);
        list.pivot = new Vector2(1f, 0.5f);
        list.sizeDelta = new Vector2(ListWidth, 0f);
        list.anchoredPosition = new Vector2(-ListRightMargin, 0f);
        list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        // 색상 팝오버 패널과 같은 배경(스프라이트·색)을 목록 뒤에 깐다. 패널에 Image가 없으면 반투명 검정으로 대신한다.
        var background = list.gameObject.AddComponent<Image>();
        var panelImage = popoverPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            background.sprite = panelImage.sprite;
            background.type = panelImage.type;
            background.color = panelImage.color;
            background.material = panelImage.material;
        }
        else
        {
            background.color = new Color(0f, 0f, 0f, 0.5f);
        }
        // 팝오버가 닫히거나 숨겨지면 목록도 함께 숨긴다.
        list.gameObject.AddComponent<CanvasGroup>();
        list.gameObject.AddComponent<VisibilityFollower>().Source = _root.gameObject;
        var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.padding = new RectOffset((int)Margin, (int)Margin, (int)Margin, (int)Margin);
        listLayout.spacing = 3f;
        listLayout.childControlWidth = listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        for (int i = 0; i < listCount; i++)
            _rows.Add(BuildRow(list));

        // 아래쪽: 최저가 카드
        var card = NewRect("Card", _root);
        _card = card.gameObject;
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
        _cardMall = NewText("Mall", _cardContent.transform, 22, FontStyle.Normal, Gray, TextAnchor.MiddleLeft);
        Stretch(_cardMall.rectTransform, new Vector2(0f, 0.7f), Vector2.one, textLeft, 0f, textRight, 0f);
        _cardTitle = NewText("Title", _cardContent.transform, 26, FontStyle.Bold, Dark, TextAnchor.MiddleLeft);
        Stretch(_cardTitle.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 0.7f), textLeft, 0f, textRight, 0f);
        _cardPrice = NewText("Price", _cardContent.transform, 34, FontStyle.Bold, Dark, TextAnchor.MiddleLeft);
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
        _arrowLabel = NewText("Label", arrow, 28, FontStyle.Bold, Gray, TextAnchor.MiddleCenter);
        Stretch(_arrowLabel.rectTransform, Vector2.zero, Vector2.one, 0f, 0f, 0f, 0f);

        _status = NewText("Status", card, 22, FontStyle.Normal, Gray, TextAnchor.MiddleCenter);
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

            row.Title.text = sortedProducts[i].Title;
            row.Price.text = FormatPrice(sortedProducts[i].Price);
            LoadThumbnail(row.Thumb, sortedProducts[i], _version);
        }

        // 최저가 카드만 먼저 보여 주고, 오른쪽 화살표를 누르면 그 위로 오름차순 목록이 펼쳐진다.
        // 상품이 하나뿐이면 펼칠 목록이 없다.
        _card.SetActive(true);
        _arrowButton.gameObject.SetActive(sortedProducts.Count > 1);
        SetExpanded(false);
        FitToPanel();
        _root.gameObject.SetActive(true);
    }

    public void Hide()
    {
        _version++;
        SetExpanded(false);
        _root.gameObject.SetActive(false);
    }

    public void Destroy()
    {
        if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
        if (_listGroup != null) UnityEngine.Object.Destroy(_listGroup);
    }

    void ShowMessage(string message)
    {
        _version++;
        _products = Array.Empty<Product>();
        _card.SetActive(true);
        _cardContent.SetActive(false);
        _status.gameObject.SetActive(true);
        _status.text = message;
        SetExpanded(false);
        FitToPanel();
        _root.gameObject.SetActive(true);
    }

    /// <summary>
    /// 카드를 패널 안에서 색상 버튼 줄 바로 위부터 패널 위쪽 여백까지 꽉 차게 맞춘다.
    /// 버튼 위치를 실제로 읽으므로 패널이나 버튼 크기를 씬에서 바꿔도 따로 고칠 필요가 없다.
    /// </summary>
    void FitToPanel()
    {
        float bottomInset = FallbackBottomInset;

        var buttons = _popoverPanel.GetComponentsInChildren<ColorButton>(true);
        if (buttons.Length > 0)
        {
            Canvas.ForceUpdateCanvases(); // 켜진 직후에는 버튼 격자 배치가 아직일 수 있다.
            var corners = new Vector3[4];
            float top = float.NegativeInfinity;
            foreach (var button in buttons)
            {
                ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach (var corner in corners)
                    top = Mathf.Max(top, _popoverPanel.InverseTransformPoint(corner).y);
            }
            bottomInset = top - _popoverPanel.rect.yMin + Gap * 2f;
        }

        _root.offsetMin = new Vector2(Margin, bottomInset);
        _root.offsetMax = new Vector2(-Margin, -Margin);
    }

    void ToggleList() => SetExpanded(!_expanded);

    void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        _listGroup.SetActive(expanded);
        _arrowLabel.text = expanded ? "<" : ">";
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

        float textLeft = Pad + thumbSize + Pad;
        var title = NewText("Title", rect, 30, FontStyle.Bold, Dark, TextAnchor.MiddleLeft);
        Stretch(title.rectTransform, new Vector2(0f, 0.42f), Vector2.one, textLeft, 0f, Pad, 0f);

        var price = NewText("Price", rect, 38, FontStyle.Bold, Dark, TextAnchor.MiddleLeft);
        Stretch(price.rectTransform, Vector2.zero, new Vector2(1f, 0.42f), textLeft, 0f, Pad, 0f);

        return new Row { Root = rect.gameObject, Thumb = thumb, Title = title, Price = price };
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
