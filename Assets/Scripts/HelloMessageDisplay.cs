using TMPro;
using UnityEngine;


public class HelloMessageDisplay : MonoBehaviour
{
    [Header("Kéo ô chữ UI Text của bạn vào đây")]
    public TextMeshProUGUI helloTextUI;

    void Start()
    {
        // Tắt chữ lúc mới vào game
        if (helloTextUI != null) helloTextUI.gameObject.SetActive(false);

        // Lắng nghe tín hiệu Hello từ Pi
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
        if (helloTextUI == null) return;

        // Nếu chuỗi bị rỗng hoặc null -> Ẩn cục UI đó đi
        if (string.IsNullOrWhiteSpace(text))
        {
            helloTextUI.gameObject.SetActive(false);
        }
        else
        {
            // Nếu có nội dung -> Mở UI lên và in chữ ra
            helloTextUI.gameObject.SetActive(true);
            helloTextUI.text = text;
        }
    }
}
