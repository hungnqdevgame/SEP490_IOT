using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    [Header("List of Product Cards")]
    public List<ProductItemUI> productItems;

    [Header("UI Control")]
    public TextMeshProUGUI btnSelectText;
    public GameObject panel;
    public GameObject mainListPanel;
    public GameObject reviewPanel;
    public GameObject productItemPrefab;
    public Transform selectedItemsContainer;


    public List<ProductItemUI> allItems; // Kéo tất cả thẻ sản phẩm vào đây
    public TextMeshProUGUI counterText; // Chỗ hiển thị "Đã chọn: X"
    public GameObject playButton;       // Nút "Phát danh sách"

    private List<ModelPlaylistItem> selectedList = new List<ModelPlaylistItem>();
    private bool isMultiSelectMode = false;

    void Start()
    {

        panel.SetActive(false);
    }

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
            // Kiểm tra trùng lặp dựa trên tên thay vì địa chỉ bộ nhớ
            bool exists = selectedList.Exists(x => x.assetName == item.assetName);

            if (!exists)
            {
                selectedList.Add(item);
                Debug.Log("Đã THÊM: " + item.assetName + " | Tổng: " + selectedList.Count);
            }
            else
            {
                Debug.LogWarning("BỊ TRÙNG! Không thể thêm: " + item.assetName);
            }
        }
        else
        {
            // XÓA tất cả các item trong danh sách có cùng tên với item đang bị bỏ chọn
            int removedCount = selectedList.RemoveAll(x => x.assetName == item.assetName);

            if (removedCount > 0)
            {
                Debug.Log("Đã XÓA: " + item.assetName + " | Tổng: " + selectedList.Count);
            }
            else
            {
                Debug.LogWarning("KHÔNG TÌM THẤY ĐỂ XÓA: " + item.assetName);
            }
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
            panel.SetActive(isMultiSelectMode);
        }

        // Đổi tên nút để người dùng biết cách quay lại
        if (btnSelectText != null)
        {
            btnSelectText.text = isMultiSelectMode ? "Hủy chọn" : "Chọn nhiều";
        }
    }

    public void OnNextButtonClick()
    {
        Debug.Log("==== BẮT ĐẦU CHUYỂN MÀN HÌNH REVIEW ====");
        Debug.Log("Số lượng sản phẩm hệ thống đang nhớ: " + selectedList.Count);

        if (selectedList.Count == 0) return;

        // 1. Chuyển đổi UI
        mainListPanel.SetActive(false);
        reviewPanel.SetActive(true);

        // 2. Dọn dẹp đồ cũ
        foreach (Transform child in selectedItemsContainer)
        {
            Destroy(child.gameObject);
        }

        // 3. Vòng lặp chống lỗi (Try-Catch)
        int count = 1;
        foreach (var itemData in selectedList)
        {
            try
            {
                Debug.Log($"Đang tạo thẻ thứ {count} cho sản phẩm: {itemData.assetName}");

                GameObject newItem = Instantiate(productItemPrefab, selectedItemsContainer);
                newItem.transform.localScale = Vector3.one; // Chống lún, tàng hình

                ProductItemUI ui = newItem.GetComponent<ProductItemUI>();

                if (ui != null)
                {
                    ProductItem pItem = new ProductItem { name = itemData.assetName };
                    ui.DisplayProduct(pItem);

                    ui.SetMultiSelectMode(false);
                    if (ui.showButton != null) ui.showButton.gameObject.SetActive(false);
                }

                Debug.Log($"-> TẠO THÀNH CÔNG THẺ SỐ {count}!");
                count++;
            }
            catch (System.Exception e)
            {
                // Nếu có lỗi, nó sẽ in ra màu đỏ nhưng KHÔNG làm chết vòng lặp
                Debug.LogError($"-> LỖI TẠI THẺ SỐ {count}: {e.Message}");
                count++;
            }
        }

        Debug.Log("==== HOÀN THÀNH QUÁ TRÌNH ====");
    }

    public void ShowSelectedReview()
    {
        // 1. Chuyển đổi màn hình
        mainListPanel.SetActive(false);
        reviewPanel.SetActive(true);

        // 2. Dọn dẹp các thẻ cũ trong màn hình Review để không bị trùng
        foreach (Transform child in selectedItemsContainer)
        {
            Destroy(child.gameObject);
        }

        // 3. Lặp qua danh sách đã chọn và tạo thẻ UI mới
        foreach (var itemData in selectedList)
        {
            // Tạo thẻ sản phẩm mới bên trong vùng chứa
            GameObject newItem = Instantiate(productItemPrefab, selectedItemsContainer);

            // Lấy script UI để đổ dữ liệu vào
            ProductItemUI ui = newItem.GetComponent<ProductItemUI>();

            // Hiển thị thông tin (Tên, Ảnh từ Render)
            ProductItem p = new ProductItem { name = itemData.assetName };
            ui.DisplayProduct(p);

            // Tắt chế độ chọn (Toggle) và nút Show ở màn hình Review này
            ui.SetMultiSelectMode(false);
            if (ui.showButton != null) ui.showButton.gameObject.SetActive(false);
        }
    }
}
