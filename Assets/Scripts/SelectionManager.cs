using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    [Header("Danh Sách TOÀN BỘ Dữ Liệu Sản Phẩm")]
    // Bạn nhập TẤT CẢ các model của bạn vào đây (vd: 10 sản phẩm)
    public List<ModelPlaylistItem> totalProductData;

    [Header("List of Product Cards (4 Thẻ UI)")]
    public List<ProductItemUI> productItems; // Kéo đúng 4 thẻ Panel 1,2,3,4 vào đây

    [Header("API Settings")]
    public string apiUrl = "http://localhost:5035/api/Product/paginated";

    [Header("Pagination Control")]
    public TextMeshProUGUI pageText;
    public Button nextButton;
    public Button prevButton;
    private int currentPage = 1;
    private int itemsPerPage = 4;
    private int maxPage = 1;

    [Header("UI Control")]
    public TextMeshProUGUI btnSelectText;
    public GameObject panel;
    public GameObject mainListPanel;
    public GameObject reviewPanel;
    public GameObject productItemPrefab;
    public Transform selectedItemsContainer;

    public TextMeshProUGUI counterText;
    public GameObject playButton;

    private List<ModelPlaylistItem> selectedList = new List<ModelPlaylistItem>();
    private bool isMultiSelectMode = false;

    void Start()
    {
        panel.SetActive(false);
        // Khởi động thì load ngay trang 1
        LoadPage(1);
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ PHÂN TRANG (PAGINATION)
    // ==========================================

    public void NextPage()
    {
        if (currentPage < maxPage) LoadPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        if (currentPage > 1) LoadPage(currentPage - 1);
    }

    private void LoadPage(int page)
    {
        // Tạm khóa 2 nút bấm để tránh người dùng click liên tục làm nghẽn mạng
        if (nextButton != null) nextButton.interactable = false;
        if (prevButton != null) prevButton.interactable = false;

        StartCoroutine(FetchPageCoroutine(page));
    }

    private IEnumerator FetchPageCoroutine(int page)
    {
        // Tạo link API hoàn chỉnh
        string url = $"{apiUrl}?pageNumber={page}&pageSize={itemsPerPage}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Đợi tải xong
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"[API] Trả về trang {page}: " + jsonResponse);

                // Ép kiểu JSON thành class ProductRoot của bạn
                ProductRoot response = JsonUtility.FromJson<ProductRoot>(jsonResponse);

                // =========================================================
                // HỆ THỐNG MÁY DÒ LỖI (Báo cáo chi tiết ra Console)
                // =========================================================
                if (response == null)
                {
                    Debug.LogError("❌ [LỖI 1] JsonUtility không đọc được gì cả!");
                }
                else if (response.data == null)
                {
                    Debug.LogError("❌ [LỖI 2] Giải mã được JSON nhưng biến 'data' bị rỗng. (Có thể bạn quên đặt [Serializable] trước class ProductData)");
                }
                else if (response.data.items == null)
                {
                    Debug.LogError("❌ [LỖI 3] Đọc được data nhưng danh sách 'items' bị rỗng! (Có thể quên [Serializable] trước class ProductItem)");
                }
                else
                {
                    // NẾU VÀO ĐƯỢC ĐÂY NGHĨA LÀ JSON ĐÃ ĐƯỢC ĐỌC HOÀN HẢO 100%
                    Debug.Log($"✅ [THÀNH CÔNG] Đã giải mã được {response.data.items.Count} sản phẩm từ API!");
                    Debug.Log($"✅ [KIỂM TRA UI] Số lượng thẻ Panel UI đang có là: {productItems.Count}");

                    // Cập nhật thông số trang
                    currentPage = response.data.pageNumber;
                    maxPage = response.data.totalPages;

                    // Đổ dữ liệu lên UI
                    UpdateUIWithApiData(response.data.items);
                }
                // =========================================================
            }
            else
            {
                Debug.LogError($"[API LỖI] {request.error}");
            }
        }

        // Cập nhật lại UI số trang và nút bấm
        if (pageText != null) pageText.text = currentPage.ToString();
        if (prevButton != null) prevButton.interactable = (currentPage > 1);
        if (nextButton != null) nextButton.interactable = (currentPage < maxPage);
    }
    private void UpdateUIWithApiData(List<ProductItem> apiItems)
    {
        for (int i = 0; i < productItems.Count; i++)
        {
            if (i < apiItems.Count)
            {
                productItems[i].gameObject.SetActive(true);
                ProductItem data = apiItems[i];

                // 1. Đổ chữ và hình ảnh
                productItems[i].DisplayProduct(data);

                // 2. ÉP GIỮ TRẠNG THÁI "CHỌN NHIỀU" KHI QUA TRANG
                productItems[i].SetMultiSelectMode(isMultiSelectMode);

                // 3. TỰ ĐỘNG ĐÁNH DẤU TICK NẾU ĐÃ CHỌN TỪ TRƯỚC
                if (productItems[i].selectionToggle != null)
                {
                    // Gỡ bỏ các sự kiện của thẻ cũ để tránh gọi nhầm
                    productItems[i].selectionToggle.onValueChanged.RemoveAllListeners();

                    // Kiểm tra xem món mới này đã nằm trong giỏ hàng chưa
                    bool isSelected = selectedList.Exists(  x => x.assetName == data.name);

                    // CỨU TINH Ở ĐÂY: Dùng SetIsOnWithoutNotify thay vì .isOn
                    // Lệnh này ép cái ô UI đổi trạng thái tick mà KHÔNG làm kích hoạt sự kiện xóa ngầm
                    productItems[i].selectionToggle.SetIsOnWithoutNotify(isSelected);

                    // Gắn lại sự kiện: Từ bây giờ, nếu người dùng thực sự lấy tay bấm tick, thì mới thêm/xóa
                    productItems[i].selectionToggle.onValueChanged.AddListener((isOn) =>
                    {
                        ModelPlaylistItem newItem = new ModelPlaylistItem { assetName = data.name };
                        UpdateSelection(newItem, isOn);
                    });
                }
            }
            else
            {
                // Ẩn các thẻ dư thừa ở trang cuối
                productItems[i].gameObject.SetActive(false);
            }
        }
    }

    // ==========================================
    // CÁC HÀM CŨ GIỮ NGUYÊN
    // ==========================================

    public void ToggleMultiSelectMode()
    {
        isMultiSelectMode = !isMultiSelectMode;

        // Xử lý giỏ hàng khi thoát chế độ
        if (!isMultiSelectMode)
        {
            selectedList.Clear(); // Xóa bộ nhớ
            UpdateCounterUI();
        }

        // Ép toàn bộ thẻ UI đổi giao diện theo đúng chế độ
        foreach (var item in productItems)
        {
            if (item != null && item.gameObject.activeSelf)
            {
                item.SetMultiSelectMode(isMultiSelectMode);

                // NẾU HỦY CHỌN: Phải tắt luôn dấu tick trên màn hình đi
                if (!isMultiSelectMode && item.selectionToggle != null)
                {
                    item.selectionToggle.isOn = false;
                }
            }
        }

        // Bật/tắt các khung viền và nút bấm chung
      //  playButton.SetActive(isMultiSelectMode);
        if (counterText != null) counterText.gameObject.SetActive(isMultiSelectMode);
        if (panel != null) panel.SetActive(isMultiSelectMode);

        // Đổi chữ nút điều khiển
        if (btnSelectText != null)
        {
            btnSelectText.text = isMultiSelectMode ? "Hủy chọn" : "Chọn nhiều";
            btnSelectText.gameObject.SetActive(true);
        }
    }

    public void UpdateSelection(ModelPlaylistItem item, bool add)
    {
        if (add)
        {
            bool exists = selectedList.Exists(x => x.assetName == item.assetName);
            if (!exists)
            {
                selectedList.Add(item);
                Debug.Log("Đã THÊM: " + item.assetName + " | Tổng: " + selectedList.Count);
            }
        }
        else
        {
            selectedList.RemoveAll(x => x.assetName == item.assetName);
            Debug.Log("Đã XÓA: " + item.assetName + " | Tổng: " + selectedList.Count);
        }
        UpdateCounterUI();
    }

    void UpdateCounterUI()
    {
        if (counterText != null) counterText.text = $"Đã chọn: {selectedList.Count}";
    }

    public void OnPlayClick()
    {
        if (selectedList.Count > 0)
            FindObjectOfType<ModelSlideshow>().StartNewPlaylist(selectedList);
    }

    public void OnNextButtonClick()
    {
        if (selectedList.Count == 0) return;

        mainListPanel.SetActive(false);
        reviewPanel.SetActive(true);

        foreach (Transform child in selectedItemsContainer) Destroy(child.gameObject);

        foreach (var itemData in selectedList)
        {
            try
            {
                GameObject newItem = Instantiate(productItemPrefab, selectedItemsContainer);
                newItem.transform.localScale = Vector3.one;

                ProductItemUI ui = newItem.GetComponent<ProductItemUI>();
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
                if (ui != null)
                {
                    ProductItem pItem = new ProductItem { name = itemData.assetName };
                    ui.DisplayProduct(pItem);
                    ui.SetMultiSelectMode(false);
                    if (ui.showButton != null) ui.showButton.gameObject.SetActive(false);
                }
            }
            catch (System.Exception e) { Debug.LogError($"LỖI: {e.Message}"); }
        }
    }

    public void ShowSelectedReview() { OnNextButtonClick(); } // Gộp chung cho gọn
}