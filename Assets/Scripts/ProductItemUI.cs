using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ProductItemUI : MonoBehaviour
{
    [Header("UI Components")]
    public RawImage productImage;       
    public TextMeshProUGUI nameText;   
    

    public void DisplayProduct(ProductItem item)
    {
        // 1. Hiện panel lên (phòng trường hợp nó đang bị ẩn)
        gameObject.SetActive(true);

        // 2. Gán Text
        nameText.text = item.name;
        

        // 3. Tải ảnh
        if (!string.IsNullOrEmpty(item.imageUrl) && item.imageUrl != "string")
        {
            StartCoroutine(DownloadImage(item.imageUrl));
        }
        else
        {
            productImage.color = Color.gray; // Màu mặc định nếu không có ảnh
        }
    }

    // Hàm này dùng để ẩn Panel nếu API trả về ít hơn 4 món
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    IEnumerator DownloadImage(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                productImage.texture = DownloadHandlerTexture.GetContent(request);
                productImage.color = Color.white;
            }
        }
    }

   
    
}
