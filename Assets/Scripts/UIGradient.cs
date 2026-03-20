using UnityEngine;
using UnityEngine.UI;


[AddComponentMenu("UI/Effects/Gradient")]
[RequireComponent(typeof(Graphic))]
public class UIGradient : BaseMeshEffect
{
    [Header("=== CÀI ĐẶT MÀU GRADIENT ===")]
    public Color topColor = Color.white; // Màu phía trên (Trắng)
    public Color bottomColor = new Color(0.941f, 0.949f, 0.961f, 1f); // Màu phía dưới (Mã #F0F2F5)

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        Rect bounds = GetComponent<RectTransform>().rect;
        float bottomY = bounds.yMin;
        float height = bounds.height;

        UIVertex vertex = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            // Tính toán tỷ lệ chiều cao (0 đến 1)
            float normalizedY = (vertex.position.y - bottomY) / height;

            // Trộn màu dựa trên vị trí Y
            vertex.color = Color.Lerp(bottomColor, topColor, normalizedY);

            vh.SetUIVertex(vertex, i);
        }
    }
}
