using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ProductManager : MonoBehaviour
{
    // Tạo biến tĩnh để SelectionManager có thể dễ dàng liên lạc
    public static ProductManager Instance;

    [Header("=== CẤU HÌNH API ===")]
    public string baseUrl = "http://localhost:5035/api/Product/paginated";
    public int pageSize = 4;

    [Header("=== GIAO DIỆN CHUYỂN TRANG & TÌM KIẾM ===")]
    public TextMeshProUGUI pageText;
    public Button nextButton;
    public Button prevButton;
    public TMP_InputField searchInput;

    [Header("=== 4 THẺ SẢN PHẨM TRÊN MÀN HÌNH ===")]
    public ProductItemUI[] productSlots;

    // Các biến trạng thái ngầm
    private int currentPage = 1;
    private int maxPage = 1;
    private string currentCategoryId = "";
    private string currentSearchText = "";

    // SỰ KIỆN: Báo cho SelectionManager biết mỗi khi đã tải xong trang mới
    public event Action<List<ProductItem>> OnDataLoaded;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (searchInput != null) searchInput.onEndEdit.AddListener(OnSearchTermChanged);
        LoadPage(1); // Tự động tải trang 1 khi mới mở app
    }

    // --- CÁC HÀM XỬ LÝ SỰ KIỆN TỪ GIAO DIỆN ---
    public void FilterByCategory(string categoryId)
    {
        currentCategoryId = categoryId;
        LoadPage(1);
    }

    public void OnSearchTermChanged(string text)
    {
        currentSearchText = text;
        LoadPage(1);
    }

    public void NextPage() { if (currentPage < maxPage) LoadPage(currentPage + 1); }
    public void PreviousPage() { if (currentPage > 1) LoadPage(currentPage - 1); }

    // --- LOGIC TẢI DỮ LIỆU ---
    public void LoadPage(int page)
    {
        currentPage = page;
        if (pageText != null) pageText.text = currentPage.ToString();

        // Khóa nút Next/Prev trong lúc chờ API để tránh bấm spam
        if (nextButton != null) nextButton.interactable = false;
        if (prevButton != null) prevButton.interactable = false;

        StartCoroutine(FetchDataCoroutine());
    }

    IEnumerator FetchDataCoroutine()
    {
        // Ghép nối URL
        string finalUrl = $"{baseUrl}?pageNumber={currentPage}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(currentCategoryId)) finalUrl += $"&productCategoryId={currentCategoryId}";
        if (!string.IsNullOrEmpty(currentSearchText)) finalUrl += $"&searchItem={UnityWebRequest.EscapeURL(currentSearchText)}";

        using (UnityWebRequest request = UnityWebRequest.Get(finalUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<ProductRoot>(request.downloadHandler.text);

                if (response != null && response.data != null)
                {
                    currentPage = response.data.pageNumber;
                    maxPage = response.data.totalPages;

                    // Mở khóa nút phân trang
                    if (prevButton != null) prevButton.interactable = (currentPage > 1);
                    if (nextButton != null) nextButton.interactable = (currentPage < maxPage);

                    UpdateUI(response.data.items);
                }
            }
            else
            {
                Debug.LogError("[LỖI API PRODUCT] " + request.error);
            }
        }
    }

    void UpdateUI(List<ProductItem> items)
    {
        // Cập nhật thông tin lên 4 thẻ UI
        for (int i = 0; i < productSlots.Length; i++)
        {
            if (i < items.Count)
            {
                productSlots[i].gameObject.SetActive(true);
                productSlots[i].DisplayProduct(items[i]);
            }
            else
            {
                productSlots[i].gameObject.SetActive(false); // Ẩn thẻ thừa
            }
        }

        // KÍCH HOẠT SỰ KIỆN: Gọi SelectionManager tới gắn chức năng "Tích chọn"
        OnDataLoaded?.Invoke(items);
    }
}