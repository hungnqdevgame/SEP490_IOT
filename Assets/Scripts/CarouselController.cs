using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarouselController : MonoBehaviour
{
    [Header("Cấu hình UI")]
    public ScrollRect scrollRect;
    public RectTransform centerPoint; // Kéo GameObject CenterPoint vào đây

    [Header("Hiệu ứng")]
    public float centerScale = 1.1f;  // Kích thước khi ở giữa
    public float sideScale = 0.8f;    // Kích thước khi ở hai bên
    public float centerAlpha = 1f;    // Độ sáng khi ở giữa (1 = 100%)
    public float sideAlpha = 0.4f;    // Độ mờ khi ở hai bên (0.4 = 40%)
    public float snapSpeed = 10f;     // Tốc độ hút vào giữa

    // Danh sách lưu trữ các thẻ được sinh ra từ API
    public List<RectTransform> cards = new List<RectTransform>();
    public List<CanvasGroup> canvasGroups = new List<CanvasGroup>();

    private int closestCardIndex = 0;

    void Update()
    {
        if (cards.Count == 0) return;

        float closestDist = float.MaxValue;
        bool isDragging = Input.GetMouseButton(0) || Input.touchCount > 0;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;

            // 1. Tính khoảng cách từ tâm thẻ đến hồng tâm CenterPoint
            float distance = Mathf.Abs(centerPoint.position.x - cards[i].position.x);

            // Tìm thẻ gần chính giữa nhất
            if (distance < closestDist)
            {
                closestDist = distance;
                closestCardIndex = i;
            }

            // 2. Chuyển đổi khoảng cách thành hiệu ứng Scale và Alpha
            // distanceThreshold (ví dụ 300f) là khoảng cách để thẻ mờ hoàn toàn
            float distanceThreshold = 300f;
            float normalizedDist = Mathf.Clamp01(distance / distanceThreshold);

            float targetScale = Mathf.Lerp(centerScale, sideScale, normalizedDist);
            float targetAlpha = Mathf.Lerp(centerAlpha, sideAlpha, normalizedDist);

            // Cập nhật hiệu ứng mượt mà (Lerp)
            cards[i].localScale = Vector3.Lerp(cards[i].localScale, new Vector3(targetScale, targetScale, 1f), Time.deltaTime * 10f);
            canvasGroups[i].alpha = Mathf.Lerp(canvasGroups[i].alpha, targetAlpha, Time.deltaTime * 10f);
        }

        // 3. Tính năng Snapping: Tự động hút thẻ gần nhất vào giữa khi thả tay
        if (!isDragging)
        {
            SnapToCard(closestCardIndex);
        }
    }

    void SnapToCard(int index)
    {
        // Tính toán khoảng cách chênh lệch và đẩy Content Panel đi để khớp vị trí
        float diff = centerPoint.position.x - cards[index].position.x;
        Vector3 newPos = scrollRect.content.position + new Vector3(diff, 0, 0);
        scrollRect.content.position = Vector3.Lerp(scrollRect.content.position, newPos, Time.deltaTime * snapSpeed);
    }
}
