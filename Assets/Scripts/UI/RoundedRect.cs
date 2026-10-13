using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모서리가 둥근 사각형을 그리는 uGUI 그래픽. Image처럼 Color를 쓰고 Button의 targetGraphic도 될 수 있다.
/// 둥근 정도는 Inspector의 Radius로 고친다. 정사각형에 Radius를 한 변의 절반으로 주면 원이 된다.
/// Ring Thickness를 0보다 크게 주면 속이 빈 테두리 선만 그린다(선택 표시용).
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
[AddComponentMenu("UI/Rounded Rect")]
public class RoundedRect : MaskableGraphic
{
    [Tooltip("모서리 반지름(px). 가로·세로의 절반을 넘으면 자동으로 줄어든다")]
    [SerializeField, Min(0f)] private float radius = 16f;
    [Tooltip("모서리 한 곳을 몇 조각으로 나눠 그릴지. 클수록 부드럽다")]
    [SerializeField, Range(2, 16)] private int cornerSegments = 8;
    [Tooltip("0이면 속이 찬 사각형, 0보다 크면 이 두께의 테두리 선만 그린다(px)")]
    [SerializeField, Min(0f)] private float ringThickness = 0f;

    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public float RingThickness
    {
        get => ringThickness;
        set
        {
            ringThickness = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float r = Mathf.Min(radius, rect.width * 0.5f, rect.height * 0.5f);
        Vector2[] outer = Outline(rect, r);

        if (ringThickness <= 0f)
        {
            // 가운데 점에서 바깥 점들로 부채꼴을 펼친다.
            vh.AddVert(rect.center, color, Vector2.zero);
            foreach (Vector2 point in outer) vh.AddVert(point, color, Vector2.zero);

            // 바깥 점이 시계 반대로 놓여 있으므로, 화면 앞면이 되도록 거꾸로 이어 붙인다.
            for (int i = 1; i <= outer.Length; i++)
                vh.AddTriangle(0, i % outer.Length + 1, i);
            return;
        }

        // 테두리 선: 바깥 둘레와, 두께만큼 안으로 줄인 안쪽 둘레 사이를 띠로 잇는다.
        float t = Mathf.Min(ringThickness, rect.width * 0.5f, rect.height * 0.5f);
        Rect inner = new Rect(rect.x + t, rect.y + t, rect.width - t * 2f, rect.height - t * 2f);
        Vector2[] inside = Outline(inner, Mathf.Max(0f, r - t));

        foreach (Vector2 point in outer) vh.AddVert(point, color, Vector2.zero);
        foreach (Vector2 point in inside) vh.AddVert(point, color, Vector2.zero);

        int n = outer.Length;
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            vh.AddTriangle(i, n + i, n + next);
            vh.AddTriangle(i, n + next, next);
        }
    }

    /// <summary>오른쪽 위 모서리부터 시계 반대로 네 모서리를 돌며 둘레의 점을 만든다.</summary>
    private Vector2[] Outline(Rect rect, float r)
    {
        // 바깥·안쪽 둘레의 점 개수가 같아야 띠로 이을 수 있으므로, 반지름이 0이어도 같은 수로 나눈다.
        int segments = cornerSegments;
        Vector2[] centers =
        {
            new Vector2(rect.xMax - r, rect.yMax - r),
            new Vector2(rect.xMin + r, rect.yMax - r),
            new Vector2(rect.xMin + r, rect.yMin + r),
            new Vector2(rect.xMax - r, rect.yMin + r),
        };

        Vector2[] points = new Vector2[4 * (segments + 1)];
        for (int corner = 0; corner < 4; corner++)
        {
            for (int i = 0; i <= segments; i++)
            {
                float angle = (corner + (float)i / segments) * Mathf.PI * 0.5f;
                points[corner * (segments + 1) + i] = centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            }
        }
        return points;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
