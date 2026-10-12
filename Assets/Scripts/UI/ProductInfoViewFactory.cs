using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상품 정보 화면(ProductInfoView)에 필요한 uGUI 오브젝트와 하위 뷰를 만들어 조립한다.
/// ProductInfoView는 상태만 다루도록, GameObject를 만드는 일은 모두 여기서 한다.
/// </summary>
public static class ProductInfoViewFactory
{
    /// <param name="popoverPanel">이 화면을 안에 넣을 색상 팝오버 패널</param>
    /// <param name="measureContentBottom">패널 바닥에서 카드 아래쪽까지 비워 둘 높이를 알려 주는 함수. 음수면 모른다는 뜻이다.</param>
    public static ProductInfoView Create(
        RectTransform popoverPanel, Func<float> measureContentBottom, IProductLinkOpener linkOpener,
        ThumbnailLoader thumbnails, ProductInfoStyle style, int listCount, CancellationToken lifetime)
    {
        if (popoverPanel == null) throw new ArgumentNullException(nameof(popoverPanel));
        if (linkOpener == null) throw new ArgumentNullException(nameof(linkOpener));
        if (style == null) throw new ArgumentNullException(nameof(style));

        ThumbnailPresenter thumbnailPresenter = new ThumbnailPresenter(thumbnails, style.thumbnailPlaceholder, lifetime);
        UguiKit kit = UguiKit.CreateDefault();

        // 카드 영역: 패널 안에서 색상 버튼 위부터 패널 위쪽 여백까지 꽉 채운다.
        RectTransform root = UguiKit.NewRect("ProductInfoView", popoverPanel);
        root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.childControlWidth = rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = rootLayout.childForceExpandHeight = true;

        // 카드와 목록의 클릭 콜백이 뷰를 가리키므로, 뷰는 하위 뷰를 만든 뒤에 만든다.
        ProductInfoView view = null;
        ProductCardView card = new ProductCardView(root, kit, style, () => view.OpenProduct(0), () => view.ToggleList());
        ProductListView list = new ProductListView(popoverPanel, root.gameObject, kit, style, listCount, index => view.OpenProduct(index));

        view = new ProductInfoView(root, measureContentBottom, style, linkOpener, thumbnailPresenter, card, list);
        view.FitToPanel();
        view.Hide();
        return view;
    }
}
