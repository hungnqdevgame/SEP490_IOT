using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectionManager : MonoBehaviour
{
    [Header("=== GIAO DIỆN XÁC NHẬN ===")]
    // Kéo Panel chứa nút "Xác nhận" ở màn hình chính vào đây
    public GameObject confirmPanel;

    [Header("=== CÀI ĐẶT SLIDESHOW & REVIEW ===")]
    public TMP_InputField timeInput;
    public GameObject mainListPanel;
    public GameObject reviewPanel;
    public Transform selectedItemsContainer;
    public GameObject productItemPrefab;

    // Dữ liệu giỏ hàng ngầm
    private List<ModelPlaylistItem> selectedList = new List<ModelPlaylistItem>();

    void Start()
    {
        // Khởi tạo: Ẩn nút Xác nhận và màn hình Review
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (reviewPanel != null) reviewPanel.SetActive(false);

        // ĐĂNG KÝ LẮNG NGHE: Mỗi khi tải xong data, chạy hàm SetupSelectionUI
        if (ProductManager.Instance != null)
        {
            ProductManager.Instance.OnDataLoaded += SetupSelectionUI;
        }
    }

    void OnDestroy()
    {
        if (ProductManager.Instance != null)
        {
            ProductManager.Instance.OnDataLoaded -= SetupSelectionUI;
        }
    }

    // --- HÀM TỰ ĐỘNG CHẠY KHI ĐỔI TRANG ---
    // --- HÀM TỰ ĐỘNG CHẠY KHI ĐỔI TRANG ---
    private void SetupSelectionUI(List<ProductItem> apiItems)
    {
        var slots = ProductManager.Instance.productSlots;

        // Kiểm tra xem hiện tại có đang có thẻ nào được chọn sẵn không
        bool hasAnySelection = selectedList.Count > 0;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < apiItems.Count)
            {
                ProductItem data = apiItems[i];
                ProductItemUI ui = slots[i];

                // 1. LUÔN HIỆN TOGGLE (CHECKBOX)
                ui.SetMultiSelectMode(true);

                if (ui.selectionToggle != null)
                {
                    // Dọn dẹp sự kiện cũ để chống lỗi
                    ui.selectionToggle.onValueChanged.RemoveAllListeners();

                    // Giữ lại dấu tick nếu thẻ này đã được chọn từ trước
                    bool isSelected = selectedList.Exists(x => x.assetName == data.name);
                    ui.selectionToggle.SetIsOnWithoutNotify(isSelected);

                    // 2. ẨN NÚT SHOW TRÊN TOÀN BỘ CÁC THẺ NẾU ĐANG CÓ ÍT NHẤT 1 THẺ ĐƯỢC CHỌN
                    if (ui.showButton != null)
                    {
                        ui.showButton.gameObject.SetActive(!hasAnySelection);
                    }

                    // 3. LẮNG NGHE KHI NGƯỜI DÙNG BẤM VÀO CHECKBOX
                    ui.selectionToggle.onValueChanged.AddListener((isOn) =>
                    {
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

                        // Cập nhật lại danh sách giỏ hàng
                        UpdateSelection(newItem, isOn);

                        // 4. GỌI HÀM CẬP NHẬT LẠI TOÀN BỘ NÚT SHOW TRÊN MÀN HÌNH
                        RefreshAllShowButtons();
                    });
                }
            }
        }
    }

    // ==============================================================
    // HÀM MỚI: Tự động quét và Tắt/Bật nút Show của cả 4 thẻ cùng lúc
    // ==============================================================
    private void RefreshAllShowButtons()
    {
        // Nếu có ít nhất 1 thẻ được chọn trong giỏ hàng -> hasAnySelection sẽ là TRUE
        bool hasAnySelection = selectedList.Count > 0;

        foreach (var ui in ProductManager.Instance.productSlots)
        {
            // Chỉ tương tác với các thẻ đang hiển thị
            if (ui != null && ui.gameObject.activeInHierarchy && ui.showButton != null)
            {
                // Nếu hasAnySelection là TRUE -> Tắt nút Show (!TRUE = false)
                // Nếu hasAnySelection là FALSE (Giỏ trống) -> Bật lại nút Show (!FALSE = true)
                ui.showButton.gameObject.SetActive(!hasAnySelection);
            }
        }
    }

    // --- LOGIC CẬP NHẬT DANH SÁCH CHỌN ---
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

        // BẬT/TẮT NÚT XÁC NHẬN DỰA TRÊN SỐ LƯỢNG SẢN PHẨM
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(selectedList.Count > 0);
        }
    }

    // --- NÚT "XÁC NHẬN" NGOÀI MÀN HÌNH CHÍNH -> MỞ BẢNG REVIEW ---
    public void OnNextButtonClick()
    {
        if (selectedList.Count == 0) return;

        // Tắt màn chính, bật màn Review
        mainListPanel.SetActive(false);
        reviewPanel.SetActive(true);

        // Xóa các thẻ review cũ
        foreach (Transform child in selectedItemsContainer) Destroy(child.gameObject);

        // Sinh ra các thẻ review mới
        foreach (var itemData in selectedList)
        {
            GameObject newItem = Instantiate(productItemPrefab, selectedItemsContainer);
            newItem.transform.localScale = Vector3.one;

            ProductItemUI ui = newItem.GetComponent<ProductItemUI>();
            if (ui != null)
            {
                ProductItem pItem = new ProductItem { name = itemData.assetName };
                ui.DisplayProduct(pItem);

                // Ở trong Review Panel thì không cần Checkbox và Nút Show nữa
                ui.SetMultiSelectMode(false);
                if (ui.showButton != null) ui.showButton.gameObject.SetActive(false);

                // Kích hoạt nút Xóa (Thùng rác hoặc dấu X)
                if (ui.btnRemove != null)
                {
                    ui.btnRemove.gameObject.SetActive(true);
                    ui.btnRemove.onClick.AddListener(() =>
                    {
                        // Xóa khỏi danh sách ngầm và xóa UI
                        selectedList.Remove(itemData);
                        Destroy(newItem);

                        // Bổ sung xịn xò: Nếu xóa sạch đồ trong Review Panel, tự động quay về màn hình chính
                        if (selectedList.Count == 0)
                        {
                            reviewPanel.SetActive(false);
                            mainListPanel.SetActive(true);
                            confirmPanel.SetActive(false);
                            // (Mẹo: Về màn hình chính thì các thẻ vừa bị xóa sẽ tự động mất dấu tick ở lần lật trang tiếp theo)
                        }
                    });
                }
            }
        }
    }

    // --- NÚT BẮT ĐẦU PHÁT TRONG BẢNG REVIEW ---
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

    // --- (Tùy chọn) Hàm để gắn vào Nút BACK trong Review Panel ---
    public void CloseReviewPanel()
    {
        reviewPanel.SetActive(false);
        mainListPanel.SetActive(true);
    }
}