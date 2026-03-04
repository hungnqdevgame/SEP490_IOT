using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro; // Nếu bạn dùng TextMeshPro để hiện trạng thái
using UnityEngine.UI;

public class PiConnectionChecker : MonoBehaviour
{
    [Header("Settings")]
    public string piLocalDomain ; // Thay bằng tên miền cục bộ của bạn
    public float checkInterval = 5f; // Kiểm tra lại sau mỗi 5 giây

    [Header("UI Feedback")]
    public TextMeshProUGUI statusText;
    public Image statusLight; // Một cái đèn nhỏ màu xanh/đỏ

    void Start()
    {
        // Bắt đầu vòng lặp kiểm tra tự động
        StartCoroutine(AutoCheckConnection());
    }

    IEnumerator AutoCheckConnection()
    {
        while (true)
        {
            yield return StartCoroutine(CheckPiConnection());
          //  yield return StartCoroutine(CheckConnection());
            yield return new WaitForSeconds(checkInterval);
        }
    }

    public IEnumerator CheckPiConnection()
    {
        // Gửi một yêu cầu GET đơn giản tới Pi
        using (UnityWebRequest request = UnityWebRequest.Get(piLocalDomain))
        {
            // Thiết lập thời gian chờ (timeout) ngắn để không làm treo UI
            request.timeout = 3;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                UpdateUI(true, "Connected to Pi");
                Debug.Log("Kết nối Pi thành công!");
            }
            else
            {
                UpdateUI(false, "Pi Disconnected: " + request.error);
                Debug.LogWarning("Không thể kết nối tới Raspberry Pi qua tên miền cục bộ.");
            }
        }
    }

    void UpdateUI(bool isConnected, string message)
    {
        if (statusText != null) statusText.text = message;
        if (statusLight != null) statusLight.color = isConnected ? Color.green : Color.red;
    }

    IEnumerator CheckConnection()
    {
        // Thử dùng tên miền .local trước
        using (UnityWebRequest v1 = UnityWebRequest.Get("http://raspberrypi4.local:5000"))
        {
            yield return v1.SendWebRequest();
            if (v1.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Kết nối qua .local thành công!");
                yield break;
            }
        }

        // Nếu thất bại, thử dùng IP cứng đã biết
        using (UnityWebRequest v2 = UnityWebRequest.Get("http://192.168.11.7:5000"))
        {
            yield return v2.SendWebRequest();
            if (v2.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Kết nối qua IP thành công!");
            }
        }
    }
}