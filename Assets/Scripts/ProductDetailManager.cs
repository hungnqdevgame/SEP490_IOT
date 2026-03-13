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

        // 2. XỬ LÝ DROPDOWN CHỌN MÀU BẰNG API
        if (colorDropdown != null)
        {
            colorDropdown.ClearOptions();

            if (product.colors != null && product.colors.Count > 0)
            {
                colorDropdown.gameObject.SetActive(true);
                // Đặt chữ tạm trong lúc gọi mạng
                colorDropdown.AddOptions(new List<string> { "Đang tải màu..." });

                // Bắt đầu gọi API lấy tên màu
                StartCoroutine(FetchColorNamesAndSetupDropdown(product));
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

    // ==========================================
    // HÀM MỚI: TẢI TÊN MÀU TỪ API
    // ==========================================
    private IEnumerator FetchColorNamesAndSetupDropdown(ProductItem product)
    {
        List<string> options = new List<string>();

        foreach (var colorData in product.colors)
        {
            // Nếu có colorId thì gọi API
            if (!string.IsNullOrEmpty(colorData.colorId))
            {
                string url = "http://localhost:5035/api/Color/" + colorData.colorId;
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<SingleColorResponse>(request.downloadHandler.text);
                        if (response != null && response.data != null)
                        {
                            // Lấy được tên màu thành công (VD: White, Red)
                            options.Add("Màu: " + response.data.name);
                        }
                        else
                        {
                            options.Add("Màu: " + colorData.sku); // Lỗi JSON thì dùng tạm SKU
                        }
                    }
                    else
                    {
                        options.Add("Màu: " + colorData.sku); // Lỗi mạng dùng tạm SKU
                    }
                }
            }
            else
            {
                options.Add("Màu: " + colorData.sku); // Không có ID màu dùng tạm SKU
            }
        }

        // Sau khi gom đủ tên, cập nhật Dropdown
        colorDropdown.ClearOptions();
        colorDropdown.AddOptions(options);

        // Đăng ký sự kiện chọn
        colorDropdown.onValueChanged.RemoveAllListeners();
        colorDropdown.onValueChanged.AddListener((index) =>
        {
            OnColorSelected(product.colors[index].model3DUrl);
        });

        // Bắt đầu tải Model 3D của màu ĐẦU TIÊN
        OnColorSelected(product.colors[0].model3DUrl);
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
            // 2. Ghép link server với tên model
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
                var response = JsonUtility.FromJson<SingleCategoryResponse>(request.downloadHandler.text);

                if (response != null && response.data != null)
                {
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

// ==========================================
// CÁC CLASS HỖ TRỢ ĐỌC JSON TỪ API MÀU
// ==========================================
[System.Serializable]
public class SingleColorResponse
{
    public bool success;
    public string message;
    public ColorDetail data;
}

[System.Serializable]
public class ColorDetail
{
    public string id;
    public string name;
    public string hexCode;
    public string skuCode;
}