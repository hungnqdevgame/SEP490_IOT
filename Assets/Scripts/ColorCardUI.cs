using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ColorCardUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI colorNameText; // Kéo Txt_ColorName vào đây
    public RawImage productImage;         // Kéo Img_Product vào đây

    // Hàm này sẽ được gọi khi bạn sinh ra Card
    public void SetupCard(string nameStr, string imageUrl)
    {
        // 1. Gán tên màu
        colorNameText.text = nameStr;

        // 2. Tải ảnh từ URL
        if (!string.IsNullOrEmpty(imageUrl))
        {
            StartCoroutine(DownloadImage(imageUrl));
        }
    }

    // Coroutine tải ảnh mượt mà không làm lag game
    IEnumerator DownloadImage(string MediaUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Lỗi tải ảnh: " + request.error);
        }
        else
        {
            // Tải thành công -> Dán ảnh vào RawImage
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            productImage.texture = texture;
        }
    }
}
