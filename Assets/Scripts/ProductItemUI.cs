using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProductItemUI : MonoBehaviour
{
    [Header("Core UI Components")]
    public RawImage productImage;
    public TextMeshProUGUI nameText;

    [Header("Multi-Select Components")]
    public GameObject checkboxObject;
    public Toggle selectionToggle;
    public Button showButton;
    public Button btnRemove;

    private void Start()
    {
        // 1. Chỉ ẩn Toggle ban đầu. 
        // ĐÃ XÓA SẠCH sự kiện OnToggleChanged để tránh xung đột với SelectionManager!
        if (selectionToggle != null)
        {
            selectionToggle.gameObject.SetActive(false);
        }
    }

    public void DisplayProduct(ProductItem item)
    {
        gameObject.SetActive(true);
        if (nameText != null) nameText.text = item.name;

        // ==========================================
        // LOGIC LẤY ẢNH TỪ MẢU ĐẦU TIÊN (HOẶC MẶC ĐỊNH)
        // ==========================================
        string targetImageUrl = "";

        // 1. Ưu tiên lấy ảnh từ màu đầu tiên (nếu có mảng colors)
        if (item.colors != null && item.colors.Count > 0 && !string.IsNullOrEmpty(item.colors[0].imageUrl) && item.colors[0].imageUrl != "string")
        {
            targetImageUrl = item.colors[0].imageUrl;
        }
        // 2. Dự phòng: Nếu màu không có ảnh, lấy ảnh mặc định của Product
        else if (!string.IsNullOrEmpty(item.imageUrl) && item.imageUrl != "string")
        {
            targetImageUrl = item.imageUrl;
        }

        // 3. Tiến hành tải ảnh nếu tìm thấy URL hợp lệ
        if (!string.IsNullOrEmpty(targetImageUrl))
        {
            StartCoroutine(DownloadImage(targetImageUrl));
        }
        else
        {
            // Nếu không có bất kỳ ảnh nào, tô màu xám báo hiệu rỗng
            if (productImage != null) productImage.color = Color.gray;
        }
        // ==========================================

        if (showButton != null)
        {
            showButton.onClick.RemoveAllListeners();
            showButton.onClick.AddListener(() =>
            {
                // Cất dữ liệu vào Balo
                DataBridge.selectedProduct = item;

                // Ép tắt chế độ Slideshow để Màn 2 chỉ chiếu đúng 1 đồ chơi này
                DataBridge.isSlideshowMode = false;

                // Chuyển cảnh
                SceneManager.LoadScene("Display Product");
            });
        }
    }

    public void SetMultiSelectMode(bool isActive)
    {
        if (checkboxObject != null) checkboxObject.SetActive(isActive);
        if (showButton != null) showButton.gameObject.SetActive(!isActive);

        if (!isActive && selectionToggle != null)
        {
            // Dùng SetIsOnWithoutNotify cực kỳ an toàn
            selectionToggle.SetIsOnWithoutNotify(false);
            selectionToggle.gameObject.SetActive(false);
        }
        else if (selectionToggle != null)
        {
            selectionToggle.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    IEnumerator DownloadImage(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Kiểm tra xem productImage có bị xóa/hủy trong lúc chờ mạng tải không
                if (productImage != null)
                {
                    productImage.texture = DownloadHandlerTexture.GetContent(request);
                    productImage.color = Color.white;
                }
            }
            else
            {
                Debug.LogWarning($"[LỖI TẢI ẢNH UI] Link: {url} | Lỗi: {request.error}");
                if (productImage != null) productImage.color = Color.gray;
            }
        }
    }
}