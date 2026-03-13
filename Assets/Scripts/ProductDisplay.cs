using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ProductDisplay : MonoBehaviour
{
    public TextMeshPro productNameText;

    [Header("UI & References")]
    public LoadModel modelLoader;

    [Header("API Config")]
    public string apiUrlGetBySku = "http://localhost:5035/api/ProductColor/variant/by-sku/";

    public string bundleServerUrl = "http://localhost:5035/";

    void Start()
    {
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnProductReceived += HandleProductReceived;
            //  SignalRManager.Instance.OnProductRotatedEvent += RotateMyModel;
        }
        else
        {
            //      SignalRManager.Instance.OnProductRotatedEvent -= RotateMyModel;        }
        }

        void OnDestroy()
        {
            // Nhớ hủy đăng ký khi chuyển màn chơi để tránh lỗi
            if (SignalRManager.Instance != null)
            {
                //  SignalRManager.Instance.OnProductReceived -= ShowProduct;
                SignalRManager.Instance.OnProductReceived -= HandleProductReceived;
            }
        }
        // Update is called once per frame
        void ShowProduct(string productId)
        {
            Debug.LogError($"[TEST] Đang cố tải ID: '{productId}' (Độ dài: {productId.Length})");

            // Xử lý khoảng trắng thừa (Rất quan trọng!)
            productId = productId.Trim();
            // Xóa model cũ nếu có
            foreach (Transform child in transform) Destroy(child.gameObject);

            // Load model mới từ thư mục Resources
            GameObject prefab = Resources.Load<GameObject>(productId); // Tên file phải trùng productId
            if (prefab == null)
            {
                Debug.LogError($"[LỖI] Không tìm thấy file nào tên là '{productId}' trong thư mục Resources!");
                return; // Dừng lại luôn
            }
            if (prefab != null)
            {
                GameObject newObj = Instantiate(prefab, transform);
                newObj.transform.localPosition = new Vector3(0, 0, -7);
                newObj.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogError("Không tìm thấy model có tên: " + productId);
            }

        }
    }


    //private void RotateMyModel(float angle)
    //{
    //    // Đặt trục Y bằng biến angle, trục X và Z giữ ở mức 0
    //    transform.localRotation = Quaternion.Euler(0, transform.rotation.y+angle, 0);

    //    Debug.Log($"[XOAY] Đã xoay model đến góc Y = {angle}");
    //}

    public void LoadProductScene()
    {
        SceneManager.LoadScene("Product Scene");
        Debug.Log("Đã chuyển về Scene 1 (Product Scene)");
    }

    void HandleProductReceived(string skuCode)
    {
        skuCode = skuCode.Trim();
        Debug.Log($"[TÍN HIỆU] Đã nhận SKU: {skuCode}. Đang tiến hành gọi API...");

        // Xóa model cũ (nếu có)
        foreach (Transform child in transform) Destroy(child.gameObject);

        // Gọi hàm tải API
        StartCoroutine(FetchUrlAndLoadModel(skuCode));
    }

    IEnumerator FetchUrlAndLoadModel(string sku)
    {
        // 1. Ghép link API gọi dữ liệu: http://localhost:5035/api/ProductColor/variant/by-sku/S-000005-W
        string finalUrl = apiUrlGetBySku + sku;

        using (UnityWebRequest request = UnityWebRequest.Get(finalUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Giải mã JSON
                var response = JsonUtility.FromJson<ProductColorApiResponse>(request.downloadHandler.text);

                if (response != null && response.data != null && !string.IsNullOrEmpty(response.data.model3DUrl))
                {
                    // 2. Xử lý đường link AssetBundle
                    string modelFileName = response.data.model3DUrl; // Ví dụ: "robot"

                    // Ghép link tải Bundle hoàn chỉnh: http://localhost:5035/robot
                    string fullBundleUrl = bundleServerUrl + modelFileName;

                    Debug.Log($"[API] Phân tích thành công! Đang tải Model từ: {fullBundleUrl}");

                    if (modelLoader != null)
                    {
                        // Truyền Link tải và Tên Asset (giả sử tên prefab trong bundle trùng với tên file luôn)
                        modelLoader.DownloadAndShow(fullBundleUrl, modelFileName);
                    }
                    else
                    {
                        Debug.LogError("Chưa kéo tham chiếu LoadModel vào script ProductDisplay!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[API] Lấy data thành công nhưng Model3DUrl của SKU {sku} bị trống!");
                }
            }
            else
            {
                Debug.LogError($"[API LỖI] Không thể tìm thấy SKU {sku}: " + request.error);
            }
        }
    }

}
