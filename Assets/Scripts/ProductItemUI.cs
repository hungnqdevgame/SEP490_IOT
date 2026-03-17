using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProductItemUI : MonoBehaviour
{
    [Header("Core UI Components")]
    public RawImage productImage;
    public TextMeshProUGUI nameText;

    [Header("Multi-Select Components")]
    public GameObject checkboxObject;
    public Toggle selectionToggle;
    public Button showButton;
    public Button btnRemove;

    private void Start()
    {
        // 1. Chỉ ẩn Toggle ban đầu. 
        // ĐÃ XÓA SẠCH sự kiện OnToggleChanged để tránh xung đột với SelectionManager!
        if (selectionToggle != null)
        {
            selectionToggle.gameObject.SetActive(false);
        }
    }

    public void DisplayProduct(ProductItem item)
    {
        gameObject.SetActive(true);
        nameText.text = item.name;

        if (!string.IsNullOrEmpty(item.imageUrl) && item.imageUrl != "string")
        {
            StartCoroutine(DownloadImage(item.imageUrl));
        }
        else
        {
            productImage.color = Color.gray;
        }

        if (showButton != null)
        {
            showButton.onClick.RemoveAllListeners();
            showButton.onClick.AddListener(() =>
            {
                // Cất dữ liệu vào Balo
                DataBridge.selectedProduct = item;

                // Ép tắt chế độ Slideshow để Màn 2 chỉ chiếu đúng 1 đồ chơi này
                DataBridge.isSlideshowMode = false;

                // Chuyển cảnh
                SceneManager.LoadScene("Display Product");
            });
        }
    }

    public void SetMultiSelectMode(bool isActive)
    {
        if (checkboxObject != null) checkboxObject.SetActive(isActive);
        if (showButton != null) showButton.gameObject.SetActive(!isActive);

        if (!isActive && selectionToggle != null)
        {
            // Dùng SetIsOnWithoutNotify cực kỳ an toàn
            selectionToggle.SetIsOnWithoutNotify(false);
            selectionToggle.gameObject.SetActive(false);
        }
        else if (selectionToggle != null)
        {
            selectionToggle.gameObject.SetActive(true);
        }
    }

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