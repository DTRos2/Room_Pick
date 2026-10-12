using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 코드로 uGUI를 만들 때 쓰는 도우미. 글꼴을 한 번만 만들어 모든 글자에 쓰고,
/// 사각형·글자·위치 지정·hover 같은 반복 작업을 모았다.
/// </summary>
public sealed class UguiKit
{
    static readonly string[] FontNames = { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" };

    readonly Font _font;

    public UguiKit(Font font)
    {
        _font = font;
    }

    public static UguiKit CreateDefault()
    {
        return new UguiKit(Font.CreateDynamicFontFromOSFont(FontNames, 16));
    }

    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public Text NewText(string name, Transform parent, int size, FontStyle style, Color color, TextAnchor alignment)
    {
        Text text = NewRect(name, parent).gameObject.AddComponent<Text>();
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

    public static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, float left, float bottom, float right, float top)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    public static void PlaceLeftCenter(RectTransform rect, float left, float size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(left, 0f);
        rect.sizeDelta = new Vector2(size, size);
    }

    /// <summary>마우스를 올리면 배경이 살짝 푸른빛으로 바뀌고, 누르는 동안 더 진해진다.</summary>
    public static void ApplyHover(Button button, Graphic background, ProductInfoStyle style)
    {
        // 코드로 붙인 Button은 targetGraphic이 비어 있으므로 직접 지정해야 색이 바뀐다.
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = style.hover;
        colors.pressedColor = style.pressed;
        colors.selectedColor = Color.white; // 클릭한 뒤에도 눌린 색이 남지 않게 한다.
        colors.disabledColor = Color.white;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }
}
