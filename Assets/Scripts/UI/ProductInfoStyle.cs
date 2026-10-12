using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 상품 정보 화면의 크기·글자·색 설정. 값은 Canvas 기준 해상도(1920x1080) 단위다.
/// ProductInfoController의 인스펙터에서 코드를 고치지 않고 조정한다.
/// </summary>
[Serializable]
public sealed class ProductInfoStyle
{
    [Header("Layout")]
    public float margin = 12f;
    public float gap = 4f;
    public float pad = 6f;
    public float cardHeight = 100f;
    public float rowHeight = 128f;
    public float arrowWidth = 40f;
    public float listSpacing = 3f;
    public float listWidth = 600f;
    [Tooltip("상품 목록과 색상 팝오버 사이 간격")]
    [FormerlySerializedAs("listRightMargin")]
    public float listGap = 16f;

    [Tooltip("색상 버튼을 찾지 못했을 때 쓰는, 패널 바닥에서 카드 아래쪽까지의 거리")]
    public float fallbackBottomInset = 62f;

    [Header("Font size")]
    public int cardMallFont = 22;
    public int cardTitleFont = 26;
    public int cardPriceFont = 34;
    public int rowTitleFont = 30;
    public int rowPriceFont = 38;
    public int arrowFont = 28;
    public int statusFont = 22;

    [Header("Colors")]
    public Color cardBackground = new Color(1f, 1f, 1f, 0.97f);
    public Color rowBackground = new Color(0.96f, 0.96f, 0.96f, 0.97f);
    public Color darkText = new Color(0.04f, 0.04f, 0.04f);
    public Color grayText = new Color(0.3f, 0.3f, 0.3f);
    public Color thumbnailPlaceholder = new Color(0.85f, 0.85f, 0.85f);
    public Color hover = new Color(0.86f, 0.91f, 1f);
    public Color pressed = new Color(0.76f, 0.84f, 0.97f);
}
