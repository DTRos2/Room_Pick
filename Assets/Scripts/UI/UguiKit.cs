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

    /// <summary>씬에 미리 만들어 둔 UI의 모든 글자에 한글이 나오는 글꼴을 적용한다.</summary>
    public void ApplyFont(Transform root)
    {
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
            text.font = _font;
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

    /// <summary>
    /// 어두운 배경용 hover. 그래픽 색은 흰색으로 두고, 평소·hover·누름 색을 Button이 정한다.
    /// 흰 배경용 ApplyHover는 색을 곱해 어둡게 만들 뿐이라 어두운 배경에서는 변화가 거의 없다.
    /// </summary>
    public static void ApplyHover(Button button, Graphic background, Color normal, Color hover, Color pressed)
    {
        background.color = Color.white;
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = hover;
        colors.pressedColor = pressed;
        colors.selectedColor = normal; // 클릭한 뒤에도 눌린 색이 남지 않게 한다.
        colors.disabledColor = normal;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    static readonly TextGenerator Measurer = new TextGenerator();

    /// <summary>
    /// 글이 폭 안에서 maxLines줄에 들어가게 넣는다. 넘치면 뒤를 잘라 …을 붙인다.
    /// 기본 Text에는 말줄임이 없어서, 글자 수를 줄여 가며 직접 잰다.
    /// </summary>
    public static void SetTextWithEllipsis(Text text, string value, float width, int maxLines)
    {
        TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(width, 10000f));
        if (CountLines(value, settings) <= maxLines)
        {
            text.text = value;
            return;
        }

        // 줄 수에 맞는 가장 긴 앞부분을 이분 탐색으로 찾는다.
        int low = 0, high = value.Length;
        while (low < high)
        {
            int mid = (low + high + 1) / 2;
            if (CountLines(value.Substring(0, mid).TrimEnd() + "…", settings) <= maxLines) low = mid;
            else high = mid - 1;
        }
        text.text = value.Substring(0, low).TrimEnd() + "…";
    }

    static int CountLines(string value, TextGenerationSettings settings)
    {
        Measurer.Populate(value, settings);
        return Measurer.lineCount;
    }
}
