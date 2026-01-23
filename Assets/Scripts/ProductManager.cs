using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;


public class ProductManager : MonoBehaviour
{
    public string apiUrl = "http://localhost:5035/api/Product/paginated?pageNumber=1&pageSize=4"; 
    private string baseUrl = "http://localhost:5035/api/Product/paginated";
    int pageSize = 4;   
    private string currentCategoryId = "";
    private int currentPage = 1;
    private string currentSearchText = "";

    public void FilterByCategory(string categoryId)
    {
        currentCategoryId = categoryId;
        currentPage = 1; // Reset về trang 1 mỗi khi đổi danh mục
        LoadPage(1);
    }
    // Kéo thả 4 cái Panel (đã gắn script ProductCardUI) vào mảng này
    public ProductItemUI[] productSlots;
    public TMP_InputField searchInput;
    void Start()
    {
        if (searchInput != null)
        {
            searchInput.onEndEdit.AddListener(OnSearchTermChanged);
        }

        StartCoroutine(GetData());
    }

    

    IEnumerator GetData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse JSON (Nhớ giữ file ProductModels.cs đã tạo ở câu trước)
                var response = JsonUtility.FromJson<ProductRoot>(request.downloadHandler.text);

                if (response != null && response.data != null && response.data.items != null)
                {
                    UpdateUI(response.data.items);
                }
            }
            else
            {
                Debug.LogError("Lỗi API: " + request.error);
            }
        }
    }

    void UpdateUI(List<ProductItem> items)
    {
        // Duyệt qua tất cả 4 slot đang có trên màn hình
        for (int i = 0; i < productSlots.Length; i++)
        {
            // Kiểm tra: Nếu dữ liệu có đủ cho slot này thì hiện, không thì ẩn
            if (i < items.Count)
            {
                // Có dữ liệu -> Hiển thị
                productSlots[i].DisplayProduct(items[i]);
            }
            else
            {
                // Hết dữ liệu (ví dụ JSON chỉ trả về 2 món) -> Ẩn slot thứ 3, 4 đi
                productSlots[i].Hide();
            }
        }
    }

    public void LoadPage(int page)
    {
        currentPage = page;

        // Bắt đầu ghép chuỗi URL
        // Kết quả: http://.../paginated?pageNumber=1&pageSize=4
        string finalUrl = $"{baseUrl}?pageNumber={page}&pageSize={pageSize}";

        // Nếu có Category ID (khác rỗng) thì nối thêm vào URL
        if (!string.IsNullOrEmpty(currentCategoryId))
        {
            // Kết quả: ...&pageSize=4&productCategoryId=abc-xyz
            finalUrl += $"&productCategoryId={currentCategoryId}";
        }
        if (!string.IsNullOrEmpty(currentSearchText))
        {
            finalUrl += $"&searchItem={UnityWebRequest.EscapeURL(currentSearchText)}";
        }

        Debug.Log("Đang gọi API: " + finalUrl); // Debug để kiểm tra URL
        StartCoroutine(GetData(finalUrl));
    }

    IEnumerator GetData(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse JSON (Dùng class ProductResponse bạn đã tạo ở bước trước)
                // Lưu ý: Kiểm tra lại tên class Model của bạn là ProductResponse hay ProductRoot
                var response = JsonUtility.FromJson<ProductRoot>(request.downloadHandler.text);

                if (response != null && response.data != null)
                {
                    UpdateUI(response.data.items);
                }
            }
            else
            {
                Debug.LogError("Lỗi API: " + request.error);
            }
        }
    }

    public void OnSearchTermChanged(string text)
    {
        // Cập nhật từ khóa
        currentSearchText = text;

        // Reset về trang 1 (khi tìm kiếm mới thì luôn phải về trang đầu)
        currentPage = 1;

        Debug.Log("Đang tìm kiếm: " + text);
        LoadPage(1);
    }


}
