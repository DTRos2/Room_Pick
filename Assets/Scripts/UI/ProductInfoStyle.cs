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
    public float rowHeight = 96f;
    [Tooltip("카드의 '다른 상품 보기' 글자 버튼 너비")]
    [FormerlySerializedAs("arrowWidth")]
    public float toggleWidth = 150f;
    public float listSpacing = 8f;
    public float listWidth = 480f;
    [Tooltip("상품 목록과 색상 팝오버 사이 간격")]
    [FormerlySerializedAs("listRightMargin")]
    public float listGap = 16f;

    [Tooltip("색상 버튼을 찾지 못했을 때 쓰는, 패널 바닥에서 카드 아래쪽까지의 거리")]
    public float fallbackBottomInset = 62f;

    [Header("Shape")]
    [Tooltip("패널·카드·행의 모서리 반지름")]
    public float cornerRadius = 12f;
    [Tooltip("뱃지·작은 버튼의 모서리 반지름")]
    public float smallRadius = 6f;
    [Tooltip("목록 위 라벨과 '색상' 라벨 한 줄의 높이")]
    public float labelHeight = 26f;
    [Tooltip("색상 라벨과 색상 버튼 줄 사이 간격. 선택 링이 버튼 위로 나오는 만큼 넉넉히 둔다.")]
    public float colorLabelGap = 10f;
    [Tooltip("팝오버 오른쪽 위 닫기(×) 버튼 크기")]
    public float closeSize = 28f;

    [Header("Font size")]
    public int cardMallFont = 20;
    [Tooltip("카드 상품명 글자 크기. 선택 버튼이 쓰는 cardTitleFont와 따로 둔다.")]
    public int cardNameFont = 22;
    public int cardPriceFont = 26;
    public int cardTitleFont = 26;
    public int rowTitleFont = 22;
    public int rowPriceFont = 22;
    [FormerlySerializedAs("arrowFont")]
    public int toggleFont = 18;
    public int statusFont = 22;
    public int labelFont = 18;
    public int badgeFont = 14;

    [Header("Colors")]
    public Color cardBackground = new Color(1f, 1f, 1f, 0.97f);
    public Color rowBackground = new Color(0.96f, 0.96f, 0.96f, 0.97f);
    public Color darkText = new Color(0.04f, 0.04f, 0.04f);
    public Color grayText = new Color(0.3f, 0.3f, 0.3f);
    public Color thumbnailPlaceholder = new Color(0.85f, 0.85f, 0.85f);
    public Color hover = new Color(0.86f, 0.91f, 1f);
    public Color pressed = new Color(0.76f, 0.84f, 0.97f);

    // 위 색은 선택 버튼 등 흰 UI도 함께 쓰므로 그대로 두고, 어두운 상품 UI는 아래 색을 따로 쓴다.
    [Header("Dark theme")]
    public Color panelColor = new Color(0.11f, 0.12f, 0.15f, 0.92f);
    public Color cardColor = new Color(0.17f, 0.19f, 0.24f, 0.96f);
    public Color rowColor = new Color(0.17f, 0.19f, 0.24f, 0.96f);
    public Color rowHoverColor = new Color(0.24f, 0.27f, 0.34f, 0.98f);
    public Color rowPressedColor = new Color(0.13f, 0.15f, 0.2f, 1f);
    public Color lightText = new Color(1f, 1f, 1f);
    public Color mutedText = new Color(0.72f, 0.75f, 0.8f);
    [Tooltip("가격·뱃지 등 강조하는 포인트 파랑")]
    public Color accent = new Color(0.36f, 0.64f, 1f);
    [Tooltip("최저가 뱃지 바탕색. 가격의 파랑과 다른 색이어야 눈에 띈다.")]
    public Color badgeColor = new Color(1f, 0.78f, 0.28f);
    public Color badgeText = new Color(0.14f, 0.12f, 0.06f);
}
