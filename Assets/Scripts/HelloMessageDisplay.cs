using System.Collections; // BẮT BUỘC phải thêm thư viện này để dùng Coroutine
using TMPro;
using UnityEngine;

public class HelloMessageDisplay : MonoBehaviour
{
    [Header("Kéo ô chữ UI Text của bạn vào đây")]
    public TextMeshProUGUI helloTextUI;
    public GameObject helloPanel;


    // Biến dùng để ghi nhớ tiến trình đếm ngược hiện tại
    private Coroutine hideCoroutine;

    void Start()
    {

        if (helloPanel != null) helloPanel.SetActive(false);

        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnMessageReceivedEvent += HandleMessage;
        }
    }

    void OnDestroy()
    {
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnMessageReceivedEvent -= HandleMessage;
        }
    }

    void HandleMessage(string text)
    {
        if (helloTextUI == null || helloPanel == null) return;

        // Nếu chuỗi bị rỗng hoặc null -> Ẩn cục UI đó đi ngay
        if (string.IsNullOrWhiteSpace(text))
        {
            helloPanel.SetActive(false);

            // Dừng bộ đếm thời gian nếu có
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }
        }
        else
        {
            // Nếu có nội dung -> Mở UI lên và in chữ ra
            helloPanel.SetActive(true);
            helloTextUI.text = text;

            // RESET BỘ ĐẾM: Nếu đang đếm ngược để tắt, thì hủy bộ đếm cũ đi...
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }

            // ... và bắt đầu bộ đếm 10 giây mới
            hideCoroutine = StartCoroutine(HidePanelAfterDelay(10f));
        }
    }

    // Hàm đếm ngược thời gian chạy ngầm
    private IEnumerator HidePanelAfterDelay(float delay)
    {
        // Chờ đúng số giây được truyền vào (10s)
        yield return new WaitForSeconds(delay);

        // Hết thời gian thì tắt Panel
        if (helloPanel != null)
        {
            helloPanel.SetActive(false);
        }
    }


}