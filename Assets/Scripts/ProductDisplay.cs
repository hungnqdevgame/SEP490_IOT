using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Thêm thư viện UI

public class ProductDisplay : MonoBehaviour
{
    [Header("=== GIAO DIỆN THÔNG TIN (UI) ===")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI brandText;
    public TextMeshProUGUI materialText;
    public TextMeshProUGUI ageText;

    [Header("=== CAROUSEL MÀU SẮC ===")]
    // Bỏ TMP_Dropdown colorDropdown đi, thay bằng 3 biến này:
    public GameObject colorCardPrefab;       // Kéo Prefab RobotCard (đã gắn ColorCardUI) vào đây
    public Transform carouselContent;        // Kéo GameObject 'Content' của Scroll View vào đây
    public CarouselController carouselScript;// Kéo Script CarouselController vào đây

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
        Debug.Log(apiUrlGetBySku);
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
                // In chuỗi JSON gốc ra để kiểm tra
                string rawJson = request.downloadHandler.text;
                Debug.Log($"[RAW JSON TỪ API]: \n{rawJson}");

                try
                {
                    // DÙNG LẠI JSONUTILITY: Vì bạn đã gắn [System.Serializable] rất chuẩn rồi!
                    var response = JsonUtility.FromJson<ProductBarcodeResponse>(rawJson);

                    if (response != null && response.data != null && !string.IsNullOrEmpty(response.data.id))
                    {
                        Debug.Log($"[THÀNH CÔNG] Đã bóc tách được sản phẩm: {response.data.name}");
                        SetupUI(response.data);
                    }
                    else
                    {
                        Debug.LogError("[LỖI PARSE] JsonUtility trả về null. Hãy xem lại log [RAW JSON] ở trên xem API có trả đúng cấu trúc không nhé.");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[NGOẠI LỆ PARSE JSON] {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[API LỖI] Không thể tìm thấy Barcode {barCode}: " + request.error);
            }
        }
    }
    private void SetupUI(ProductBarcodeData data)
    {
        // 1. Đổ dữ liệu Text (Giữ nguyên của bạn)
        if (nameText != null) nameText.text = data.name;
        if (priceText != null) priceText.text = "Giá : "+ data.basePrice.ToString("N0") + " ĐỒNG";
        if (brandText != null) brandText.text = "Hãng :" + data.brand;
        if (materialText != null) materialText.text = "Chất liệu :"+data.material;
        if (ageText != null) ageText.text = "Độ tuổi : " + data.ageRange;

        if (typeText != null)
        {
            typeText.text = "Đang tải...";
            if (!string.IsNullOrEmpty(data.productCategoryId))
                StartCoroutine(FetchCategoryName(data.productCategoryId));
            else
                typeText.text = "Chưa phân loại";
        }

        // 2. TẠO THẺ MÀU SẮC CHO CAROUSEL
        if (carouselContent != null && colorCardPrefab != null)
        {
            // Xóa các thẻ cũ nếu tải sản phẩm mới
            foreach (Transform child in carouselContent) Destroy(child.gameObject);

            // Xóa list trong script Carousel
            if (carouselScript != null)
            {
                carouselScript.cards.Clear();
                carouselScript.canvasGroups.Clear();
            }

            if (data.colors != null && data.colors.Count > 0)
            {
                foreach (var colorData in data.colors)
                {
                    // Đẻ ra 1 cái thẻ mới
                    GameObject newCard = Instantiate(colorCardPrefab, carouselContent);

                    // Lấy component để đổ data (Tên và Ảnh)
                    ColorCardUI cardUI = newCard.GetComponent<ColorCardUI>();
                    string cName = !string.IsNullOrEmpty(colorData.colorName) ? colorData.colorName : colorData.sku;
                    cardUI.SetupCard(cName, colorData.imageUrl);

                    // Khai báo thẻ này cho hệ thống Carousel để nó làm hiệu ứng sáng/tối
                    if (carouselScript != null)
                    {
                        carouselScript.cards.Add(newCard.GetComponent<RectTransform>());
                        carouselScript.canvasGroups.Add(newCard.GetComponent<CanvasGroup>());
                    }

                    // Thêm nút bấm: Khi nhấn vào thẻ, gọi hàm OnColorSelected để tải Model 3D
                    Button cardBtn = newCard.GetComponent<Button>();
                    if (cardBtn == null) cardBtn = newCard.AddComponent<Button>(); // Tự thêm nếu chưa có

                    // Lấy biến tạm để dùng trong sự kiện OnClick
                    string skuToLoad = colorData.sku;
                    cardBtn.onClick.AddListener(() => OnColorSelected(skuToLoad));
                }

                // Tự động hiển thị Model 3D của thẻ đầu tiên
                OnColorSelected(data.colors[0].sku);
            }
            else
            {
                // Nếu sản phẩm không chia màu
                OnColorSelected(data.sku);
            }
        }
    }

    public void OnColorSelected(string modelSku)
    {
        if (string.IsNullOrEmpty(modelSku)) return;

        Debug.Log($"[HIỂN THỊ] Đã chọn màu! Đang gọi tải Model 3D: {modelSku}");

        string fullBundleUrl = bundleServerUrl + modelSku.ToLower();

        if (modelLoader != null)
        {
            modelLoader.DownloadAndShow(fullBundleUrl, modelSku.ToLower());
        }
    }

    private IEnumerator FetchCategoryName(string categoryId)
    {
        // (Giữ nguyên logic của bạn)
        string url = "http://localhost:5035/api/ProductCategory/" + categoryId;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<SingleCategoryResponse>(request.downloadHandler.text);
                if (response != null && response.data != null) typeText.text = "Loại : "  +  response.data.name;
            }
            else typeText.text = "Lỗi kết nối";
        }
    }
}