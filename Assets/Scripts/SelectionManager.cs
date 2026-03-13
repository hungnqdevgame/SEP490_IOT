using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    [Header("Danh Sách TOÀN BỘ Dữ Liệu Sản Phẩm")]
    public List<ModelPlaylistItem> totalProductData;

    [Header("List of Product Cards (4 Thẻ UI)")]
    public List<ProductItemUI> productItems;

    [Header("API Settings")]
    public string apiUrl = "http://localhost:5035/api/Product/paginated";

    [Header("Pagination Control")]
    public TextMeshProUGUI pageText;
    public Button nextButton;
    public Button prevButton;
    private int currentPage = 1;
    private int itemsPerPage = 4;
    private int maxPage = 1;

    [Header("UI Control")]
    public TextMeshProUGUI btnSelectText;
    public GameObject panel;
    public GameObject mainListPanel;
    public GameObject reviewPanel;
    public GameObject productItemPrefab;
    public Transform selectedItemsContainer;

    public TextMeshProUGUI counterText;
    public GameObject playButton;

    private List<ModelPlaylistItem> selectedList = new List<ModelPlaylistItem>();
    private bool isMultiSelectMode = false;

    void Start()
    {
        panel.SetActive(false);
        LoadPage(1);
    }

    public void NextPage()
    {
        if (currentPage < maxPage) LoadPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        if (currentPage > 1) LoadPage(currentPage - 1);
    }

    private void LoadPage(int page)
    {
        if (nextButton != null) nextButton.interactable = false;
        if (prevButton != null) prevButton.interactable = false;

        StartCoroutine(FetchPageCoroutine(page));
    }

    private IEnumerator FetchPageCoroutine(int page)
    {
        string url = $"{apiUrl}?pageNumber={page}&pageSize={itemsPerPage}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                ProductRoot response = JsonUtility.FromJson<ProductRoot>(jsonResponse);

                if (response != null && response.data != null && response.data.items != null)
                {
                    currentPage = response.data.pageNumber;
                    maxPage = response.data.totalPages;

                    UpdateUIWithApiData(response.data.items);
                }
            }
            else
            {
                Debug.LogError($"[API LỖI] {request.error}");
            }
        }

        if (pageText != null) pageText.text = currentPage.ToString();
        if (prevButton != null) prevButton.interactable = (currentPage > 1);
        if (nextButton != null) nextButton.interactable = (currentPage < maxPage);
    }

    private void UpdateUIWithApiData(List<ProductItem> apiItems)
    {
        for (int i = 0; i < productItems.Count; i++)
        {
            if (i < apiItems.Count)
            {
                productItems[i].gameObject.SetActive(true);
                ProductItem data = apiItems[i];

                // 1. TẠM THỜI GỠ SỰ KIỆN TRƯỚC KHI UI LÀM VIỆC (Quan trọng nhất)
                if (productItems[i].selectionToggle != null)
                {
                    productItems[i].selectionToggle.onValueChanged.RemoveAllListeners();
                }

                // 2. Đổ chữ và hình ảnh an toàn
                productItems[i].DisplayProduct(data);

                // 3. ÉP GIỮ TRẠNG THÁI "CHỌN NHIỀU" KHI QUA TRANG
                productItems[i].SetMultiSelectMode(isMultiSelectMode);

                // 4. TỰ ĐỘNG ĐÁNH DẤU TICK NẾU ĐÃ CHỌN TỪ TRƯỚC
                if (productItems[i].selectionToggle != null)
                {
                    bool isSelected = selectedList.Exists(x => x.assetName == data.name);

                    // Dùng SetIsOnWithoutNotify để Checkbox đánh tick mà không gửi tín hiệu
                    productItems[i].selectionToggle.SetIsOnWithoutNotify(isSelected);

                    // 5. GẮN LẠI SỰ KIỆN GHI NHẬN CHO NGƯỜI DÙNG BẤM
                    productItems[i].selectionToggle.onValueChanged.AddListener((isOn) =>
                    {
                        // Lấy thêm ModelUrl để trình diễn Slideshow 3D sau này
                        ModelPlaylistItem newItem = new ModelPlaylistItem
                        {
                            assetName = data.name,
                            modelUrl = data.model3DUrl
                        };
                        UpdateSelection(newItem, isOn);
                    });
                }
            }
            else
            {
                productItems[i].gameObject.SetActive(false);
            }
        }
    }

    public void ToggleMultiSelectMode()
    {
        isMultiSelectMode = !isMultiSelectMode;

        if (!isMultiSelectMode)
        {
            selectedList.Clear();
            UpdateCounterUI();
        }

        foreach (var item in productItems)
        {
            if (item != null && item.gameObject.activeSelf)
            {
                item.SetMultiSelectMode(isMultiSelectMode);

                if (!isMultiSelectMode && item.selectionToggle != null)
                {
                    // Fix lỗi hủy ngầm khi tắt chế độ MultiSelect
                    item.selectionToggle.SetIsOnWithoutNotify(false);
                }
            }
        }

        if (counterText != null) counterText.gameObject.SetActive(isMultiSelectMode);
        if (panel != null) panel.SetActive(isMultiSelectMode);

        if (btnSelectText != null)
        {
            btnSelectText.text = isMultiSelectMode ? "Hủy chọn" : "Chọn nhiều";
            btnSelectText.gameObject.SetActive(true);
        }
    }

    public void UpdateSelection(ModelPlaylistItem item, bool add)
    {
        if (add)
        {
            bool exists = selectedList.Exists(x => x.assetName == item.assetName);
            if (!exists)
            {
                selectedList.Add(item);
                Debug.Log("Đã THÊM: " + item.assetName + " | Tổng: " + selectedList.Count);
            }
        }
        else
        {
            selectedList.RemoveAll(x => x.assetName == item.assetName);
            Debug.Log("Đã XÓA: " + item.assetName + " | Tổng: " + selectedList.Count);
        }
        UpdateCounterUI();
    }

    void UpdateCounterUI()
    {
        if (counterText != null) counterText.text = $"Đã chọn: {selectedList.Count}";
    }

    public void OnPlayClick()
    {
        if (selectedList.Count > 0)
            FindObjectOfType<ModelSlideshow>().StartNewPlaylist(selectedList);
    }

    public void OnNextButtonClick()
    {
        if (selectedList.Count == 0) return;

        mainListPanel.SetActive(false);
        reviewPanel.SetActive(true);

        foreach (Transform child in selectedItemsContainer) Destroy(child.gameObject);

        foreach (var itemData in selectedList)
        {
            try
            {
                GameObject newItem = Instantiate(productItemPrefab, selectedItemsContainer);
                newItem.transform.localScale = Vector3.one;

                ProductItemUI ui = newItem.GetComponent<ProductItemUI>();
                if (ui.btnRemove != null)
                {
                    ui.btnRemove.gameObject.SetActive(true);
                    ui.btnRemove.onClick.AddListener(() =>
                    {
                        selectedList.Remove(itemData);
                        Destroy(newItem);
                        UpdateCounterUI();
                    });
                }
                if (ui != null)
                {
                    ProductItem pItem = new ProductItem { name = itemData.assetName };
                    ui.DisplayProduct(pItem);
                    ui.SetMultiSelectMode(false);
                    if (ui.showButton != null) ui.showButton.gameObject.SetActive(false);
                }
            }
            catch (System.Exception e) { Debug.LogError($"LỖI: {e.Message}"); }
        }
    }

    public void ShowSelectedReview() { OnNextButtonClick(); }
}