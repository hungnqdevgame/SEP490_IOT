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

    void Start()
    {
        // Fix cứng URL ở đây: Dùng http, IP cục bộ 127.0.0.1, và cổng 5035
        // Thêm tham số tick để chống cache
        //bundleURL = "http://10.87.21.29:5035/robot?t=" + System.DateTime.Now.Ticks;

        Debug.Log("Đang kết nối tới: " + bundleURL);
        StartCoroutine(DownloadAndPlace());
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
}