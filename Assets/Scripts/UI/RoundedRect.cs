using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모서리가 둥근 사각형을 그리는 uGUI 그래픽. Image처럼 Color를 쓰고 Button의 targetGraphic도 될 수 있다.
/// 둥근 정도는 Inspector의 Radius로 고친다. 정사각형에 Radius를 한 변의 절반으로 주면 원이 된다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
[AddComponentMenu("UI/Rounded Rect")]
public class RoundedRect : MaskableGraphic
{
    [Tooltip("모서리 반지름(px). 가로·세로의 절반을 넘으면 자동으로 줄어든다")]
    [SerializeField, Min(0f)] private float radius = 16f;
    [Tooltip("모서리 한 곳을 몇 조각으로 나눠 그릴지. 클수록 부드럽다")]
    [SerializeField, Range(2, 16)] private int cornerSegments = 8;

    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float r = Mathf.Min(radius, rect.width * 0.5f, rect.height * 0.5f);
        int segments = r > 0f ? cornerSegments : 1;

        // 가운데 점을 먼저 넣고, 오른쪽 위부터 시계 반대로 네 모서리의 바깥 점을 돌아가며 넣는다.
        vh.AddVert(rect.center, color, Vector2.zero);

        Vector2[] centers =
        {
            new Vector2(rect.xMax - r, rect.yMax - r),
            new Vector2(rect.xMin + r, rect.yMax - r),
            new Vector2(rect.xMin + r, rect.yMin + r),
            new Vector2(rect.xMax - r, rect.yMin + r),
        };

        for (int corner = 0; corner < 4; corner++)
        {
            for (int i = 0; i <= segments; i++)
            {
                float angle = (corner + (float)i / segments) * Mathf.PI * 0.5f;
                Vector2 point = centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                vh.AddVert(point, color, Vector2.zero);
            }
        }

        // 바깥 점이 시계 반대로 놓여 있으므로, 화면 앞면이 되도록 거꾸로 이어 붙인다.
        int outline = 4 * (segments + 1);
        for (int i = 1; i <= outline; i++)
            vh.AddTriangle(0, i % outline + 1, i);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
