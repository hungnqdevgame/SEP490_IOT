using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    [Header("List of Product Cards")]
    public List<ProductItemUI> productItems;

    [Header("UI Control")]
    public TextMeshProUGUI btnSelectText;



    public List<ProductItemUI> allItems; // Kéo tất cả thẻ sản phẩm vào đây
    public TextMeshProUGUI counterText; // Chỗ hiển thị "Đã chọn: X"
    public GameObject playButton;       // Nút "Phát danh sách"

    private List<ModelPlaylistItem> selectedList = new List<ModelPlaylistItem>();
    private bool isMultiSelectMode = false;

    // Gọi khi nhấn nút "Chọn nhiều" trên UI
    public void ToggleMultiSelectMode()
    {
        isMultiSelectMode = !isMultiSelectMode;

        foreach (var item in allItems)
        {
            item.SetMultiSelectMode(isMultiSelectMode);
        }

        playButton.SetActive(isMultiSelectMode);
        counterText.gameObject.SetActive(isMultiSelectMode);

        if (!isMultiSelectMode)
        {
            selectedList.Clear();
            UpdateCounterUI();
        }
    }

    public void UpdateSelection(ModelPlaylistItem item, bool add)
    {
        if (add)
        {
            if (!selectedList.Contains(item)) selectedList.Add(item);
        }
        else
        {
            selectedList.Remove(item);
        }
        UpdateCounterUI();
    }

    void UpdateCounterUI()
    {
        counterText.text = $"Đã chọn: {selectedList.Count}";
    }

    public void OnPlayClick()
    {
        if (selectedList.Count > 0)
        {
            // Gọi sang Script ModelSlideshow đã viết trước đó
            FindObjectOfType<ModelSlideshow>().StartNewPlaylist(selectedList);
        }
    }

    public void ToggleSelectionMode()
    {
        isMultiSelectMode = !isMultiSelectMode;

        // Duyệt qua toàn bộ danh sách 4 sản phẩm
        foreach (var item in productItems)
        {
            if (item != null)
            {
                // Gọi hàm bật/tắt chế độ chọn nhiều đã viết trong ProductItemUI
                item.SetMultiSelectMode(isMultiSelectMode);
            }
        }

        // Đổi tên nút để người dùng biết cách quay lại
        if (btnSelectText != null)
        {
            btnSelectText.text = isMultiSelectMode ? "Hủy chọn" : "Chọn nhiều";
        }
    }
}
