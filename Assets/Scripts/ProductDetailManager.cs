using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProductDetailManager : MonoBehaviour
{
    [Header("Giao diện thông tin")]
    public TextMeshProUGUI nameText;  // Kéo chữ "WIBU" to nhất vào đây
    public TextMeshProUGUI priceText; // Kéo chữ "200 ĐỒNG" vào đây
    public TextMeshProUGUI typeText;  // Loại
    public TextMeshProUGUI brandText; // Hãng
    public TextMeshProUGUI materialText;
    public TextMeshProUGUI ageText;

    [Header("Chọn màu sắc (Dropdown)")]
    public TMP_Dropdown colorDropdown;

    void Start()
    {
        if (DataBridge.selectedProduct != null)
        {
            Debug.Log($"[MÀN 2] Đã mở balo và thấy: {DataBridge.selectedProduct.name}");
            SetupUI(DataBridge.selectedProduct); // Gọi hàm SetupUI ở ngay bên dưới nó
        }
        else
        {
            Debug.LogError("❌ [MÀN 2] Balo TRỐNG RỖNG! Mất data khi chuyển Scene!");
        }
    }

    private void SetupUI(ProductItem product)
    {
        // 1. ĐỔI TOÀN BỘ CHỮ TRÊN MÀN HÌNH BẰNG DATA THẬT
        if (nameText != null) nameText.text = product.name;
        if (priceText != null) priceText.text = product.price.ToString("N0") + " ĐỒNG";

        // Fix: Lấy chữ từ Data đắp lên giao diện
        if (typeText != null) typeText.text = product.productCategoryId;
        if (brandText != null) brandText.text = product.brand;
        if (materialText != null) materialText.text = product.material;
        if (ageText != null) ageText.text = product.ageRange;

        if (typeText != null)
        {
            typeText.text = "Đang tải..."; // Hiển thị chữ tạm trong lúc chờ mạng

            if (!string.IsNullOrEmpty(product.productCategoryId))
            {
                // Gọi luồng phụ tải tên Category từ API
                StartCoroutine(FetchCategoryName(product.productCategoryId));
            }
            else
            {
                typeText.text = "Chưa phân loại";
            }
        }
        // 2. Xử lý các nút màu (Giữ nguyên code của bạn)
        if (colorDropdown != null)
        {
            colorDropdown.ClearOptions();

            if (product.colors != null && product.colors.Count > 0)
            {
                colorDropdown.gameObject.SetActive(true);
                List<string> options = new List<string>();

                foreach (var colorData in product.colors)
                {
                    string[] skuParts = colorData.sku.Split('-');
                    string colorName = skuParts.Length > 0 ? skuParts[skuParts.Length - 1] : colorData.sku;
                    options.Add("Màu: " + colorName);
                }

                colorDropdown.AddOptions(options);
                colorDropdown.onValueChanged.RemoveAllListeners();
                colorDropdown.onValueChanged.AddListener((index) =>
                {
                    OnColorSelected(product.colors[index].model3DUrl);
                });

                OnColorSelected(product.colors[0].model3DUrl);
            }
            else
            {
                colorDropdown.gameObject.SetActive(false);
                OnColorSelected(product.model3DUrl);
            }
        }
        else if (product.colors != null && product.colors.Count > 0)
        {
            OnColorSelected(product.colors[0].model3DUrl);
        }
    }

    private void OnColorSelected(string modelUrl)
    {
        // Chống lỗi rỗng: Nếu API không có tên model, mặc định gọi file "robot"
        if (string.IsNullOrEmpty(modelUrl) || modelUrl == "string") modelUrl = "robot";

        Debug.Log($"[MÀN 2] Đã chọn màu! Đang gọi tải Model 3D: {modelUrl}");

        // 1. Tìm thợ tải Model (LoadModel) đang có mặt trong Màn 2
        LoadModel modelLoader = FindFirstObjectByType<LoadModel>();

        if (modelLoader != null)
        {
            // 2. Ghép link server với tên model (API đang trả về chữ "robot")
            // Kết quả sẽ thành: http://localhost:5035/robot
            string bundleServerUrl = "http://localhost:5035/";
            string fullBundleUrl = bundleServerUrl + modelUrl;

            // 3. Ra lệnh tải file AssetBundle từ mạng về!
            modelLoader.DownloadAndShow(fullBundleUrl, modelUrl);
        }
        else
        {
            Debug.LogError("❌ LỖI: Không tìm thấy script LoadModel ở Màn 2! Đảm bảo GameManager có gắn LoadModel.");
        }
    }
    public void BackToScene1()
    {
        SceneManager.LoadScene("Product Scene");
    }


    private IEnumerator FetchCategoryName(string categoryId)
    {
        string url = "http://localhost:5035/api/ProductCategory/" + categoryId;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Đọc dữ liệu JSON
                var response = JsonUtility.FromJson<SingleCategoryResponse>(request.downloadHandler.text);

                if (response != null && response.data != null)
                {
                    // Lấy thành công, đổi chữ trên màn hình thành tên Category
                    typeText.text = response.data.name;
                    Debug.Log($"[API] Đã dịch Category ID thành tên: {response.data.name}");
                }
            }
            else
            {
                Debug.LogError("Lỗi gọi API Category: " + request.error);
                typeText.text = "Lỗi kết nối";
            }
        }
    }
}
