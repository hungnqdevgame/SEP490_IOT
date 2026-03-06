using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}

public class LoadModel : MonoBehaviour
{
    // Đã chuyển thành private để Inspector không ghi đè được nữa
    public string bundleURL = "http://localhost:5035/robot";
    public string assetName = "robot";
    private GameObject currentModel;

    void Start()
    {
        // Fix cứng URL ở đây: Dùng http, IP cục bộ 127.0.0.1, và cổng 5035
        // Thêm tham số tick để chống cache
        //bundleURL = "http://10.87.21.29:5035/robot?t=" + System.DateTime.Now.Ticks;

        Debug.Log("Đang kết nối tới: " + bundleURL);
        StartCoroutine(DownloadAndPlace());
    }

    public void DownloadAndShow(string bundleUrl, string assetName = "KyleRobot")
    {
        Debug.Log("Bắt đầu tải AssetBundle từ: " + bundleUrl);
        StartCoroutine(DownloadAndPlace1(bundleUrl, assetName));
    }

    IEnumerator DownloadAndPlace()
    {
        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(bundleURL))
        {
            uwr.certificateHandler = new BypassCertificate();

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("LỖI MẠNG THỰC SỰ: " + uwr.responseCode + " - " + uwr.error);
            }
            else
            {
                Debug.Log("Tải thành công! Đang giải nén model...");
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);

                if (bundle != null)
                {
                    GameObject prefab = bundle.LoadAsset<GameObject>(assetName);
                    if (prefab != null)
                    {
                        Instantiate(prefab);
                        Debug.Log("Đã hiển thị Robot lên màn hình!");
                    }
                    else
                    {
                        Debug.LogError("Không tìm thấy Asset nào tên là: " + assetName + " trong bundle này.");
                    }
                    bundle.Unload(false);
                }
            }
        }
    }

    IEnumerator DownloadAndPlace1(string bundleURL, string assetName)
    {
        // 1. Xóa model cũ đi trước khi tải
        if (currentModel != null) Destroy(currentModel);
        foreach (Transform child in transform) Destroy(child.gameObject);

        // 2. Tiến hành tải Bundle
        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(bundleURL))
        {
            uwr.certificateHandler = new BypassCertificate();

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("LỖI MẠNG TẢI BUNDLE: " + uwr.responseCode + " - " + uwr.error);
            }
            else
            {
                Debug.Log("Tải thành công! Đang giải nén model...");
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);

                if (bundle != null)
                {
                    // [TÙY CHỌN] In ra cửa sổ Console tất cả các tên file có trong Bundle để bạn kiểm tra
                    string[] allNames = bundle.GetAllAssetNames();
                    Debug.Log("Tên thật của các file trong Bundle này là: " + string.Join(", ", allNames));

                    // [BÍ QUYẾT TỐI ƯU] Không cần tìm theo tên nữa, lôi hết GameObject ra và lấy cái đầu tiên!
                    GameObject[] allPrefabs = bundle.LoadAllAssets<GameObject>();

                    if (allPrefabs != null && allPrefabs.Length > 0)
                    {
                        GameObject prefab = allPrefabs[0];

                        currentModel = Instantiate(prefab, transform);

                        // 1. Gán CHÍNH XÁC Position (Y = 0.7, Z = -7)
                        // Lưu ý: Cần thêm chữ 'f' ở sau số thập phân trong C#
                        currentModel.transform.localPosition = new Vector3(0f, 0.7f, -7f);

                        // 2. Gán CHÍNH XÁC Rotation (Quay mặt 180 độ)
                        currentModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                        // 3. Gán CHÍNH XÁC Scale (1, 1, 1) cho chắc chắn không bị phóng to/thu nhỏ
                        currentModel.transform.localScale = new Vector3(1f, 1f, 1f);

                        Debug.Log($"🎉 Đã hiển thị thành công Model: {prefab.name} lên đúng vị trí!");
                    }
                    else
                    {
                        Debug.LogError("Bundle này tải về được nhưng bên trong trống rỗng, không có Model 3D nào cả!");
                    }

                    // Bắt buộc phải Unload để tránh rò rỉ bộ nhớ
                    bundle.Unload(false);
                }
            }
        }
    }

}