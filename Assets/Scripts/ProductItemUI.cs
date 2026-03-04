
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ProductItemUI : MonoBehaviour
{
    [Header("Core UI Components")]
    public RawImage productImage;
    public TextMeshProUGUI nameText;

    [Header("Multi-Select Components")]
    public GameObject checkboxObject;   // Cái khung của ô tích (ví dụ: hình tròn)
    public Toggle selectionToggle;      // Thành phần Toggle để bắt sự kiện Click
    public Button showButton; 
    // Nút "Show" hiện tại của bạn

    private ProductItem currentItem;    // Lưu trữ data của sản phẩm hiện tại
    private SelectionManager manager; // Quản lý tổng việc chọn nhiều

    private void Start()
    {
        // Tìm manager trong Scene để báo cáo mỗi khi được tích chọn
        manager = FindFirstObjectByType<SelectionManager>();
        selectionToggle.gameObject.SetActive(false); // Ẩn Toggle ban đầu
        // Lắng nghe sự kiện khi người dùng tích vào ô
        if (selectionToggle != null)
        {
            selectionToggle.onValueChanged.AddListener(OnToggleChanged);
        }

    }

    public void DisplayProduct(ProductItem item)
    {
        currentItem = item; // Lưu lại data để dùng khi chọn
        gameObject.SetActive(true);
        nameText.text = item.name;

        if (!string.IsNullOrEmpty(item.imageUrl) && item.imageUrl != "string")
        {
            StartCoroutine(DownloadImage(item.imageUrl));
        }
        else
        {
            productImage.color = Color.gray;
        }

        // Mặc định khi mới hiện ra thì không ở chế độ chọn nhiều
        SetMultiSelectMode(false);
    }

    // --- LOGIC CHỌN NHIỀU BẠN CẦN CHÈN ---

    public void SetMultiSelectMode(bool isActive)
    {
        // Ẩn/Hiện ô Checkbox/Toggle dựa trên biến isActive
        if (checkboxObject != null)
            checkboxObject.SetActive(isActive);

        // Ngược lại, nút Show sẽ ẩn khi Toggle hiện và ngược lại
        if (showButton != null)
            showButton.gameObject.SetActive(!isActive);

        // Reset lại trạng thái tích về false khi tắt chế độ chọn nhiều
        if (!isActive && selectionToggle != null)
        {
            selectionToggle.isOn = false;
            selectionToggle.gameObject.SetActive(false);
        }
        else selectionToggle.gameObject.SetActive(true);




    }

    private void OnToggleChanged(bool isOn)
    {
        if (manager != null && currentItem != null)
        {
            // Chuyển data từ ProductItem sang dạng Playlist để phát
            ModelPlaylistItem playlistData = new ModelPlaylistItem
            {
                modelUrl = currentItem.model3DUrl, // Đảm bảo ProductItem có trường này từ API
                assetName = currentItem.name,
                displayDuration = 5.0f // Thời gian mặc định
            };
            Debug.Log($"Toggle changed: {isOn} for {currentItem.name}");
            manager.UpdateSelection(playlistData, isOn);
        }
    }

    // ------------------------------------

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
                productImage.texture = DownloadHandlerTexture.GetContent(request);
                productImage.color = Color.white;
            }
        }
    }
}