using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectionManager : MonoBehaviour
{
    [Header("=== GIAO DIỆN CHỌN NHIỀU (MULTI-SELECT) ===")]
    public TextMeshProUGUI btnSelectText;
    public TextMeshProUGUI counterText;
    public GameObject multiSelectPanel; // Panel chứa nút "Phát" hoặc "Tiếp tục"

    [Header("=== CÀI ĐẶT SLIDESHOW & REVIEW ===")]
    public TMP_InputField timeInput;
    public GameObject mainListPanel;
    public GameObject reviewPanel;
    public Transform selectedItemsContainer;
    public GameObject productItemPrefab;

    // Dữ liệu giỏ hàng ngầm
    private List<ModelPlaylistItem> selectedList = new List<ModelPlaylistItem>();
    private bool isMultiSelectMode = false;

    void Start()
    {
        // Khởi tạo ẩn các giao diện giỏ hàng
        if (multiSelectPanel != null) multiSelectPanel.SetActive(false);
        if (counterText != null) counterText.gameObject.SetActive(false);

        // ĐĂNG KÝ LẮNG NGHE: Mỗi khi ProductManager tải xong data, chạy hàm SetupSelectionUI
        if (ProductManager.Instance != null)
        {
            ProductManager.Instance.OnDataLoaded += SetupSelectionUI;
        }
    }

    void OnDestroy()
    {
        // Hủy đăng ký khi tắt app để chống lỗi tràn RAM
        if (ProductManager.Instance != null)
        {
            ProductManager.Instance.OnDataLoaded -= SetupSelectionUI;
        }
    }

    // --- HÀM TỰ ĐỘNG CHẠY KHI ĐỔI TRANG ---
    private void SetupSelectionUI(List<ProductItem> apiItems)
    {
        var slots = ProductManager.Instance.productSlots;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < apiItems.Count)
            {
                ProductItem data = apiItems[i];
                ProductItemUI ui = slots[i];

                ui.SetMultiSelectMode(isMultiSelectMode);

                if (ui.selectionToggle != null)
                {
                    // 1. Dọn dẹp sự kiện cũ (Chống lỗi gọi đúp khi đổi trang)
                    ui.selectionToggle.onValueChanged.RemoveAllListeners();

                    // 2. Kiểm tra xem thẻ này đã nằm trong giỏ hàng chưa? Nếu có thì giữ nguyên dấu Tích
                    bool isSelected = selectedList.Exists(x => x.assetName == data.name);
                    ui.selectionToggle.SetIsOnWithoutNotify(isSelected);

                    // 3. Lắng nghe người dùng bấm tích
                    ui.selectionToggle.onValueChanged.AddListener((isOn) =>
                    {
                        // Lấy chuẩn Sku
                        string correctSku = (data.colors != null && data.colors.Count > 0 && !string.IsNullOrEmpty(data.colors[0].sku))
                                            ? data.colors[0].sku
                                            : data.sku;

                        ModelPlaylistItem newItem = new ModelPlaylistItem
                        {
                            assetName = data.name,
                            modelUrl = correctSku,
                            sku = correctSku,
                            fullProductData = data
                        };

                        UpdateSelection(newItem, isOn);
                    });
                }
            }
        }
    }

    // --- LOGIC NÚT "CHỌN NHIỀU" ---
    public void ToggleMultiSelectMode()
    {
        isMultiSelectMode = !isMultiSelectMode;

        if (!isMultiSelectMode)
        {
            selectedList.Clear();
            UpdateCounterUI();
        }

        // Cập nhật lại 4 thẻ đang hiển thị
        if (ProductManager.Instance != null)
        {
            foreach (var ui in ProductManager.Instance.productSlots)
            {
                if (ui.gameObject.activeSelf)
                {
                    ui.SetMultiSelectMode(isMultiSelectMode);
                    if (!isMultiSelectMode && ui.selectionToggle != null) ui.selectionToggle.SetIsOnWithoutNotify(false);
                }
            }
        }

        // Bật/tắt UI Giỏ hàng
        if (counterText != null) counterText.gameObject.SetActive(isMultiSelectMode);
        if (multiSelectPanel != null) multiSelectPanel.SetActive(isMultiSelectMode);
        if (btnSelectText != null) btnSelectText.text = isMultiSelectMode ? "Hủy chọn" : "Chọn nhiều";
    }

    public void UpdateSelection(ModelPlaylistItem item, bool add)
    {
        if (add)
        {
            if (!selectedList.Exists(x => x.assetName == item.assetName)) selectedList.Add(item);
        }
        else
        {
            selectedList.RemoveAll(x => x.assetName == item.assetName);
        }
        UpdateCounterUI();
    }

    void UpdateCounterUI()
    {
        if (counterText != null) counterText.text = $"Đã chọn: {selectedList.Count}";
    }

    // --- LOGIC XEM TRƯỚC VÀ XÁC NHẬN ---
    public void OnNextButtonClick()
    {
        if (selectedList.Count == 0) return;

        mainListPanel.SetActive(false);
        reviewPanel.SetActive(true);

        foreach (Transform child in selectedItemsContainer) Destroy(child.gameObject);

        foreach (var itemData in selectedList)
        {
            GameObject newItem = Instantiate(productItemPrefab, selectedItemsContainer);
            newItem.transform.localScale = Vector3.one;

            ProductItemUI ui = newItem.GetComponent<ProductItemUI>();
            if (ui != null)
            {
                ProductItem pItem = new ProductItem { name = itemData.assetName };
                ui.DisplayProduct(pItem);
                ui.SetMultiSelectMode(false);
                if (ui.showButton != null) ui.showButton.gameObject.SetActive(false);

                if (ui.btnRemove != null)
                {
                    ui.btnRemove.gameObject.SetActive(true);
                    ui.btnRemove.onClick.AddListener(() =>
                    {
                        selectedList.Remove(itemData);
                        Destroy(newItem);
                        UpdateCounterUI();
                    });
                }
            }
        }
    }

    public void OnConfirmSlideshowClick()
    {
        if (selectedList.Count == 0) return;

        float minutes = 1f;
        if (timeInput != null && !string.IsNullOrEmpty(timeInput.text)) float.TryParse(timeInput.text, out minutes);
        if (minutes <= 0) minutes = 1f;

        DataBridge.playlist = new List<ModelPlaylistItem>(selectedList);
        foreach (var item in DataBridge.playlist) item.displayDuration = minutes * 60f;
        DataBridge.isSlideshowMode = true;

        SceneManager.LoadScene("Display Product");
    }
}