using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ProductDisplay : MonoBehaviour
{
    [Header("=== GIAO DIỆN THÔNG TIN (UI) ===")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI brandText;
    public TextMeshProUGUI materialText;
    public TextMeshProUGUI ageText;

    [Header("=== CHỌN MÀU SẮC ===")]
    public TMP_Dropdown colorDropdown;

    [Header("=== HỆ THỐNG TẢI MODEL ===")]
    public LoadModel modelLoader;

    [Header("API Config")]
    public string apiUrlGetBySku = "http://localhost:5035/api/Product/barcode/";
    public string bundleServerUrl = "http://localhost:5035/";

    void Start()
    {
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnProductReceived += HandleProductReceived;
        }
    }

    void OnDestroy()
    {
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnProductReceived -= HandleProductReceived;
        }
    }

    public void LoadProductScene()
    {
        SceneManager.LoadScene("Product Scene");
    }

    void HandleProductReceived(string barCode)
    {
        barCode = barCode.Trim();
        Debug.Log($"[TÍN HIỆU] Đã nhận Barcode: {barCode}. Đang tiến hành gọi API...");

        // Xóa model cũ
        foreach (Transform child in transform) Destroy(child.gameObject);

        // Bắt đầu tải dữ liệu mới
        StartCoroutine(FetchUrlAndLoadModel(barCode));
    }

    IEnumerator FetchUrlAndLoadModel(string barCode)
    {
        string finalUrl = apiUrlGetBySku + barCode;

        using (UnityWebRequest request = UnityWebRequest.Get(finalUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<ProductBarcodeResponse>(request.downloadHandler.text);

                if (response != null && response.data != null)
                {
                    // GỌI HÀM CẬP NHẬT GIAO DIỆN (UI)
                    SetupUI(response.data);
                }
                else
                {
                    Debug.LogWarning($"[API] Phân tích JSON thất bại hoặc data bị null cho Barcode {barCode}.");
                }
            }
            else
            {
                Debug.LogError($"[API LỖI] Không thể tìm thấy Barcode {barCode}: " + request.error);
            }
        }
    }

    // ==========================================
    // HÀM MỚI: CẬP NHẬT GIAO DIỆN VÀ DROPDOWN
    // ==========================================
    private void SetupUI(ProductBarcodeData data)
    {
        // 1. Đổ dữ liệu vào các ô chữ
        if (nameText != null) nameText.text = data.name;
        if (priceText != null) priceText.text = data.basePrice.ToString("N0") + " ĐỒNG";
        if (brandText != null) brandText.text = data.brand;
        if (materialText != null) materialText.text = data.material;
        if (ageText != null) ageText.text = data.ageRange;

        // Xử lý Loại sản phẩm (Category)
        if (typeText != null)
        {
            typeText.text = "Đang tải...";
            if (!string.IsNullOrEmpty(data.productCategoryId))
                StartCoroutine(FetchCategoryName(data.productCategoryId));
            else
                typeText.text = "Chưa phân loại";
        }

        // 2. Xử lý Dropdown Màu Sắc
        if (colorDropdown != null)
        {
            colorDropdown.ClearOptions();

            if (data.colors != null && data.colors.Count > 0)
            {
                colorDropdown.gameObject.SetActive(true);
                List<string> options = new List<string>();

                foreach (var colorData in data.colors)
                {
                    // Lấy trực tiếp tên màu từ JSON mới, KHÔNG CẦN gọi thêm API màu nữa!
                    string cName = !string.IsNullOrEmpty(colorData.colorName) ? colorData.colorName : colorData.sku;
                    options.Add("Màu: " + cName);
                }

                colorDropdown.AddOptions(options);

                // Gắn sự kiện khi đổi màu trong Dropdown
                colorDropdown.onValueChanged.RemoveAllListeners();
                colorDropdown.onValueChanged.AddListener((index) =>
                {
                    OnColorSelected(data.colors[index].sku);
                });

                // Tự động tải 3D của màu đầu tiên
                OnColorSelected(data.colors[0].sku);
            }
            else
            {
                // Nếu sản phẩm không có chia màu
                colorDropdown.gameObject.SetActive(false);
                OnColorSelected(data.sku);
            }
        }
        else
        {
            // Nếu UI không có Dropdown thì tự động tải màu đầu tiên
            if (data.colors != null && data.colors.Count > 0) OnColorSelected(data.colors[0].sku);
            else OnColorSelected(data.sku);
        }
    }

    private void OnColorSelected(string modelSku)
    {
        if (string.IsNullOrEmpty(modelSku)) return;

        Debug.Log($"[HIỂN THỊ] Đã chọn màu! Đang gọi tải Model 3D: {modelSku}");

        string fullBundleUrl = bundleServerUrl + modelSku;

        if (modelLoader != null)
        {
            modelLoader.DownloadAndShow(fullBundleUrl, modelSku);
        }
        else
        {
            Debug.LogError("Chưa kéo tham chiếu LoadModel vào script ProductDisplay!");
        }
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
                }
            }
            else
            {
                typeText.text = "Lỗi kết nối";
            }
        }
    }
}