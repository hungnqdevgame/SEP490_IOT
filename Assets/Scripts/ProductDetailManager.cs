using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProductDetailManager : MonoBehaviour
{
    [Header("Giao diện thông vịn")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI brandText;
    public TextMeshProUGUI materialText;
    public TextMeshProUGUI ageText;

    //[Header("Chọn màu sắc (Dropdown)")]
    //public TMP_Dropdown colorDropdown;
    //public Toggle colorToggle;

    void Start()
    {
        if (DataBridge.isSlideshowMode && DataBridge.playlist != null && DataBridge.playlist.Count > 0)
        {
            Debug.Log("[MÀN 2] Khởi động chế độ Trình Chiếu (Slideshow)!");
            // ĐÃ XÓA LỆNH ẨN DROPDOWN Ở ĐÂY ĐỂ DROPDOWN LUÔN HIỆN
            StartCoroutine(PlaySlideshowRoutine());
        }
        else if (DataBridge.selectedProduct != null)
        {
            SetupUI(DataBridge.selectedProduct);
        }
        else
        {
            Debug.LogError("❌ [MÀN 2] Balo TRỐNG RỖNG! Mất data khi chuyển Scene!");
        }
    }

    // Hàm SetupUI giờ sẽ nhận thêm tham số currentSku để biết đang chiếu màu nào
    private void SetupUI(ProductItem product, string currentSku = "")
    {
        if (nameText != null) nameText.text = product.name;
        if (priceText != null) priceText.text ="Giá tiền :" + product.price.ToString("N0") + " ĐỒNG";
        if (brandText != null) brandText.text ="Hãng : " +  product.brand;
        if (materialText != null) materialText.text = "Chất liệu : " + product.material;
        if (ageText != null) ageText.text = "Độ tuổi : " + product.ageRange;

        if (typeText != null)
        {
            typeText.text = "Đang tải...";
            if (!string.IsNullOrEmpty(product.productCategoryId)) StartCoroutine(FetchCategoryName(product.productCategoryId));
            else typeText.text = "Chưa phân loại";
        }

        //// --- CẬP NHẬT DROPDOWN ---
        //if (colorDropdown != null)
        //{
        //    colorDropdown.ClearOptions();
        //    if (product.colors != null && product.colors.Count > 0)
        //    {
        //        colorDropdown.gameObject.SetActive(true);
        //        colorDropdown.AddOptions(new List<string> { "Đang tải màu..." });
        //        StartCoroutine(FetchColorNamesAndSetupDropdown(product, currentSku));
        //    }
        //    else
        //    {
        //        colorDropdown.gameObject.SetActive(false);
        //        if (!DataBridge.isSlideshowMode) OnColorSelected(product.model3DUrl);
        //    }
        //}
        //else if (product.colors != null && product.colors.Count > 0 && !DataBridge.isSlideshowMode)
        //{
        //    OnColorSelected(product.colors[0].sku);
        //}
    }

    private IEnumerator FetchColorNamesAndSetupDropdown(ProductItem product, string currentSku)
    {
        List<string> options = new List<string>();

        foreach (var colorData in product.colors)
        {
            if (!string.IsNullOrEmpty(colorData.colorId))
            {
                string url = "https://toyshelf-backend.onrender.com/api/Color/" + colorData.colorId;
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<SingleColorResponse>(request.downloadHandler.text);
                        if (response != null && response.data != null) options.Add("Màu: " + response.data.name);
                        else options.Add("Màu: " + colorData.sku);
                    }
                    else options.Add("Màu: " + colorData.sku);
                }
            }
            else options.Add("Màu: " + colorData.sku);
        }

        //colorDropdown.ClearOptions();
        //colorDropdown.AddOptions(options);

        // Tìm vị trí màu đang được chiếu để chỉnh Dropdown khớp với 3D Model
        int selectedIndex = 0;
        if (!string.IsNullOrEmpty(currentSku))
        {
            selectedIndex = product.colors.FindIndex(c => c.sku == currentSku);
            if (selectedIndex == -1) selectedIndex = 0;
        }

        //colorDropdown.SetValueWithoutNotify(selectedIndex); // Hiển thị giá trị chuẩn

        //colorDropdown.onValueChanged.RemoveAllListeners();
        //colorDropdown.onValueChanged.AddListener((index) =>
        //{
        //    OnColorSelected(product.colors[index].sku);
        //});

        // Chỉ tải model 1 lần duy nhất để tránh giật lag
        if (!DataBridge.isSlideshowMode)
        {
            OnColorSelected(product.colors[selectedIndex].sku);
        }
    }

    private void OnColorSelected(string modelUrl)
    {
        LoadModel modelLoader = FindFirstObjectByType<LoadModel>();
        if (modelLoader != null)
        {
            string bundleServerUrl = "https://toyshelf-backend.onrender.com/";
            string fullBundleUrl = bundleServerUrl + modelUrl.ToLower();
            modelLoader.DownloadAndShow(fullBundleUrl, modelUrl.ToLower());
        }
    }

    private IEnumerator FetchCategoryName(string categoryId)
    {
        string url = "https://toyshelf-backend.onrender.com/api/ProductCategory/" + categoryId;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<SingleCategoryResponse>(request.downloadHandler.text);
                if (response != null && response.data != null) typeText.text = response.data.name;
            }
            else typeText.text = "Lỗi kết nối";
        }
    }

    private IEnumerator PlaySlideshowRoutine()
    {
        LoadModel modelLoader = FindFirstObjectByType<LoadModel>();
        if (modelLoader == null) yield break;

        string bundleServerUrl = "https://toyshelf-backend.onrender.com/";
        int currentIndex = 0;

        while (true)
        {
            var currentItem = DataBridge.playlist[currentIndex];

            // 1. GỌI SETUP UI ĐỂ VỪA IN THÔNG TIN VỪA TẠO DROPDOWN MÀU
            if (currentItem.fullProductData != null)
            {
                SetupUI(currentItem.fullProductData, currentItem.sku);
            }
            else if (nameText != null) nameText.text = currentItem.assetName;

            // 2. Tải model 3D hiển thị
            string skuUrl = currentItem.modelUrl;
            if (string.IsNullOrEmpty(skuUrl) || skuUrl.Trim() == "") skuUrl = "robot";
            string fullBundleUrl = bundleServerUrl + skuUrl.ToLower();
            modelLoader.DownloadAndShow(fullBundleUrl, skuUrl.ToLower());

            // 3. Đợi đủ số giây rồi chuyển sang món đồ chơi tiếp theo
            yield return new WaitForSeconds(currentItem.displayDuration);

            currentIndex++;
            if (currentIndex >= DataBridge.playlist.Count) currentIndex = 0;
        }
    }

    public void BackToScene1()
    {
        DataBridge.isSlideshowMode = false;
        if (DataBridge.playlist != null) DataBridge.playlist.Clear();
        SceneManager.LoadScene("Product Scene");
    }

    public void LoadProductScene()
    {
        SceneManager.LoadScene("Product Scene");
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