using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic; // Bổ sung thư viện quản lý Kho (Dictionary)
using System.IO;

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) { return true; }
}

public class LoadModel : MonoBehaviour
{
    public string assetName;
    private GameObject currentModel;
    public bool isLoading = false;

    private Dictionary<string, GameObject> ramCache = new Dictionary<string, GameObject>();
    void Awake()
    {
        // Xóa cache lúc mới mở App (phòng hờ lần trước tắt ngang)
        ClearDiskCache("Mở App");
    }

    void OnApplicationQuit()
    {
        // Xóa cache lúc tắt App tử tế
        ClearDiskCache("Tắt App");
    }

    private void ClearDiskCache(string phase)
    {
        bool success = Caching.ClearCache();
        if (success)
        {
            Debug.Log($"[CACHE] Đã dọn dẹp sạch sẽ ổ cứng lúc: {phase}");
        }
    }
    void Start() { }

    public void DownloadAndShow(string bundleUrl, string assetName)
    {
        isLoading = true;

        // [ĐÃ SỬA] Gọi sang ToyverseDisplayController
        ToyverseDisplayController uiController = FindObjectOfType<ToyverseDisplayController>();
        if (uiController != null) uiController.SetSwatchesInteractable(false);
        StartCoroutine(DownloadAndPlace(bundleUrl, assetName));
    }

    IEnumerator DownloadAndPlace(string bundleURL, string assetName)
    {
        foreach (Transform child in transform) child.gameObject.SetActive(false);

        // Trường hợp 1: Có trong RAM
        if (ramCache.ContainsKey(assetName) && ramCache[assetName] != null)
        {
            currentModel = ramCache[assetName];
            currentModel.SetActive(true);

            // [MỚI] 3. Lấy từ RAM ra xong (mất 0 giây) -> TẮT KHÓA
            isLoading = false;
            yield break;
        }

        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(bundleURL))
        {
            uwr.certificateHandler = new BypassCertificate();
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ LỖI MẠNG: " + uwr.error);
                // [MỚI] 4. Bị lỗi mạng cũng phải TẮT KHÓA (để user còn bấm tải lại được)
                isLoading = false;
                UnlockSystem();
                yield break;
            }

            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
            if (bundle == null)
            {
                // [MỚI] 5. File hỏng cũng phải TẮT KHÓA
                isLoading = false;
                yield break;
            }

            GameObject[] allPrefabs = bundle.LoadAllAssets<GameObject>();
            if (allPrefabs != null && allPrefabs.Length > 0)
            {
                GameObject prefab = allPrefabs[0];
                currentModel = Instantiate(prefab, transform);
                currentModel.transform.localPosition = new Vector3(0f, 0.7f, -7f);
                currentModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                currentModel.transform.localScale = new Vector3(1f, 1f, 1f);

                ramCache[assetName] = currentModel;
            }
            bundle.Unload(false);
            UnlockSystem();
        }

        // [MỚI] 6. Tải và tạo mô hình thành công -> TẮT KHÓA
        isLoading = false;
    }
    private void UnlockSystem()
    {
        isLoading = false;

        // [ĐÃ SỬA] Gọi sang ToyverseDisplayController
        ToyverseDisplayController uiController = FindObjectOfType<ToyverseDisplayController>();
        if (uiController != null) uiController.SetSwatchesInteractable(true);
    }

    public void ClearCurrentModel()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }
    private IEnumerator DownloadModelFromDb(string sku)
    {
        // Đường dẫn trỏ thẳng vào API Download của bạn
        string apiUrl = $"http://localhost:5035/api/Bundle/download-bundle/{sku}";

        using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(apiUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Lỗi tải từ DB: " + www.error);
            }
            else
            {
                // Lấy Bundle ra từ kết quả tải về
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);

                if (bundle != null)
                {
                    // Load mô hình 3D từ trong Bundle (tên phải khớp lúc bạn đặt khi Build)
                    GameObject prefab = bundle.LoadAsset<GameObject>(sku);
                    Instantiate(prefab);

                    // Giải phóng bundle để tránh tràn RAM
                    bundle.Unload(false);
                }
            }
        }
    }
}