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

    // [VŨ KHÍ MỚI] Kho RAM: Cất giữ các model đã tải để bật/tắt tức thì
    private Dictionary<string, GameObject> ramCache = new Dictionary<string, GameObject>();

    void Start() { }

    public void DownloadAndShow(string bundleUrl, string assetName)
    {
        StartCoroutine(DownloadAndPlace(bundleUrl, assetName));
    }

    IEnumerator DownloadAndPlace(string bundleURL, string assetName)
    {
        // ==========================================================
        // TẦNG 1: KIỂM TRA RAM CACHE (TỐC ĐỘ 0 GIÂY)
        // ==========================================================
        if (ramCache.ContainsKey(assetName) && ramCache[assetName] != null)
        {
            // Tắt tất cả các đồ chơi đang hiển thị trên màn hình (thay vì Destroy chúng)
            foreach (Transform child in transform) child.gameObject.SetActive(false);

            // Bật con robot đã lưu trong Kho RAM lên
            currentModel = ramCache[assetName];
            currentModel.SetActive(true);

            Debug.Log($"⚡ [RAM] Đã bật {assetName} ngay lập tức (0 giây delay)!");
            yield break; // NGỪNG CODE TẠI ĐÂY! Không cần đọc ổ cứng hay mạng nữa.
        }

        // Nếu RAM chưa có, ta phải dọn dẹp màn hình (Tắt các model cũ đi) để chuẩn bị load con mới
        foreach (Transform child in transform) child.gameObject.SetActive(false);

        string savePath = Path.Combine(Application.persistentDataPath, assetName + ".bundle");

        // ==========================================================
        // TẦNG 2: KIỂM TRA MẠNG VÀ TẢI VÀO Ổ CỨNG (NẾU CHƯA CÓ)
        // ==========================================================
        if (!File.Exists(savePath))
        {
            Debug.Log($"[NETWORK] Tải từ Server về máy...");
            using (UnityWebRequest uwr = UnityWebRequest.Get(bundleURL))
            {
                uwr.certificateHandler = new BypassCertificate();
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("❌ LỖI MẠNG: " + uwr.error);
                    yield break; // Rớt mạng thì nghỉ
                }
                File.WriteAllBytes(savePath, uwr.downloadHandler.data);
            }
        }

        // ==========================================================
        // TẦNG 3: ĐỌC TỪ Ổ CỨNG VÀ ÉP HIỂN THỊ (BỎ CHẾ ĐỘ ASYNC)
        // ==========================================================
        Debug.Log("[HARD DRIVE] Đang đọc file ép buộc...");

        // Dùng lệnh đọc đồng bộ (Synchronous) - Bắt máy tính nặn ra model ngay lập tức
        AssetBundle bundle = AssetBundle.LoadFromFile(savePath);

        if (bundle == null)
        {
            Debug.LogError("❌ Lỗi file hỏng!");
            if (File.Exists(savePath)) File.Delete(savePath); // Xóa file hỏng
            yield break;
        }

        // Lấy model ra ngay lập tức
        GameObject[] allPrefabs = bundle.LoadAllAssets<GameObject>();

        if (allPrefabs != null && allPrefabs.Length > 0)
        {
            GameObject prefab = allPrefabs[0];

            currentModel = Instantiate(prefab, transform);
            currentModel.transform.localPosition = new Vector3(0f, 0.7f, -7f);
            currentModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            currentModel.transform.localScale = new Vector3(1f, 1f, 1f);

            // QUAN TRỌNG: Lưu con robot vừa tạo vào KHO RAM
            ramCache.Add(assetName, currentModel);

            Debug.Log($"🚀 Đã tạo thành công {assetName} và lưu vào KHO RAM!");
        }

        // Dọn rác
        bundle.Unload(false);
    }
}