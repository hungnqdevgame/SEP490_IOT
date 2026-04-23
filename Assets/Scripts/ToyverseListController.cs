using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ToyverseListController : MonoBehaviour
{
    [Header("=== GIAO DIỆN UI TOOLKIT ===")]
    public UIDocument uiDocument;

    [Header("=== CẤU HÌNH API ===")]
    public string baseUrl = "https://toyshelf-backend.onrender.com/api/Product/paginated";
    public int pageSize = 4;

    private VisualElement _root;
    private TextField _searchField;
    private Button _btnHome;
    private Button _btnConfirm;
    private Button _btnPrev;
    private Button _btnNext;
    private Label _lblPage;
    private List<VisualElement> _slots = new List<VisualElement>();

    // Khai báo biến cho Bảng Thời Gian
    private VisualElement _reviewModal;
    private TextField _inputTime;
    private Button _btnCancelShow;
    private Button _btnStartShow;

    private int _currentPage = 1;
    private int _maxPage = 1;
    private string _searchText = "";
    private List<ProductItem> _selectedItems = new List<ProductItem>();

    private void OnEnable()
    {
        if (uiDocument == null) return;
        _root = uiDocument.rootVisualElement;

        BindUIElements();
        LoadPage(1);
    }

    private void BindUIElements()
    {
        _searchField = _root.Q<TextField>("input-search");
        _btnHome = _root.Q<Button>("btn-home");
        _btnConfirm = _root.Q<Button>("btn-confirm");
        _btnPrev = _root.Q<Button>("btn-prev");
        _btnNext = _root.Q<Button>("btn-next");
        _lblPage = _root.Q<Label>("lbl-page");

        // Tìm các phần tử của Bảng Thời Gian
        _reviewModal = _root.Q<VisualElement>("review-modal");
        _inputTime = _root.Q<TextField>("input-time");
        _btnCancelShow = _root.Q<Button>("btn-cancel-show");
        _btnStartShow = _root.Q<Button>("btn-start-show");

        _slots.Clear();
        for (int i = 0; i < pageSize; i++)
        {
            var slot = _root.Q<VisualElement>($"slot-{i}");
            if (slot != null) _slots.Add(slot);
        }

        // ==========================================
        // SỰ KIỆN NÚT BẤM
        // ==========================================
        _btnConfirm.style.display = DisplayStyle.None;

        // Bấm Xác nhận -> Hiện bảng Thời Gian
        _btnConfirm.clicked += () => {
            if (_selectedItems.Count > 0 && _reviewModal != null)
            {
                _reviewModal.style.display = DisplayStyle.Flex;
            }
        };

        // Bấm Hủy bỏ -> Tắt bảng
        _btnCancelShow?.RegisterCallback<ClickEvent>(evt => {
            if (_reviewModal != null) _reviewModal.style.display = DisplayStyle.None;
        });

        // Bấm Bắt đầu Phát -> Lưu DataBridge và Chuyển Scene
        _btnStartShow?.RegisterCallback<ClickEvent>(evt => StartSlideshow());

        // ĐÃ SỬA NÚT HOME CHUYỂN VỀ PRODUCT SCENE
        _btnHome.clicked += () => {
            SceneManager.LoadScene("Display Product");
        };

        _searchField?.RegisterCallback<ChangeEvent<string>>(evt => {
            _searchText = evt.newValue;
            LoadPage(1);
        });

        _btnPrev.clicked += () => { if (_currentPage > 1) LoadPage(_currentPage - 1); };
        _btnNext.clicked += () => { if (_currentPage < _maxPage) LoadPage(_currentPage + 1); };
    }

    public void LoadPage(int page)
    {
        _currentPage = page;
        if (_lblPage != null) _lblPage.text = _currentPage.ToString();
        if (_btnPrev != null) _btnPrev.SetEnabled(false);
        if (_btnNext != null) _btnNext.SetEnabled(false);
        StartCoroutine(FetchDataCoroutine());
    }

    private IEnumerator FetchDataCoroutine()
    {
        string finalUrl = $"{baseUrl}?pageNumber={_currentPage}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(_searchText))
            finalUrl += $"&searchItem={UnityWebRequest.EscapeURL(_searchText)}";

        using (UnityWebRequest request = UnityWebRequest.Get(finalUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<ProductRoot>(request.downloadHandler.text);
                if (response != null && response.data != null)
                {
                    _currentPage = response.data.pageNumber;
                    _maxPage = response.data.totalPages > 0 ? response.data.totalPages : 1;
                    if (_btnPrev != null) _btnPrev.SetEnabled(_currentPage > 1);
                    if (_btnNext != null) _btnNext.SetEnabled(_currentPage < _maxPage);
                    UpdateGridUI(response.data.items);
                }
            }
        }
    }

    private void UpdateGridUI(List<ProductItem> items)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (i < items.Count)
            {
                slot.style.display = DisplayStyle.Flex;
                BindCardData(slot, items[i]);
            }
            else
            {
                slot.style.display = DisplayStyle.None;
            }
        }
        RefreshAllShowButtons();
    }

    private void BindCardData(VisualElement card, ProductItem item)
    {
        var titleLabel = card.Q<Label>("card-title");
        if (titleLabel != null) titleLabel.text = item.name;

        // --- XỬ LÝ TẢI ẢNH TỪ SERVER ---
        var imgBox = card.Q<VisualElement>("card-img");
        if (imgBox != null)
        {
            // Reset ảnh cũ về null (để tránh bị ám ảnh từ sản phẩm trang trước)
            imgBox.style.backgroundImage = null;

            // Tìm link ảnh trong mảng colors
            string imageUrl = "";
            if (item.colors != null && item.colors.Count > 0 && !string.IsNullOrEmpty(item.colors[0].imageUrl))
            {
                imageUrl = item.colors[0].imageUrl;
                if (imageUrl.EndsWith(".webp", System.StringComparison.OrdinalIgnoreCase))
                {
                    imageUrl = imageUrl.Substring(0, imageUrl.Length - 5) + ".png";
                }
            }

            // Gọi hàm tải ảnh
            if (!string.IsNullOrEmpty(imageUrl))
            {
                StartCoroutine(LoadImageRoutine(imageUrl, imgBox));
            }
        }
        // --------------------------------

        var toggle = card.Q<Toggle>("card-toggle");
        if (toggle != null)
        {
            toggle.UnregisterValueChangedCallback(OnToggleChanged);
            bool isAlreadySelected = _selectedItems.Exists(x => x.name == item.name);
            toggle.SetValueWithoutNotify(isAlreadySelected);
            toggle.userData = item;
            toggle.RegisterValueChangedCallback(OnToggleChanged);
        }

        var btnShow = card.Q<Button>("btn-show");
        if (btnShow != null)
        {
            btnShow.userData = item;
            btnShow.UnregisterCallback<ClickEvent>(OnShowItemClicked);
            btnShow.RegisterCallback<ClickEvent>(OnShowItemClicked);
        }
    }
    private IEnumerator LoadImageRoutine(string url, VisualElement imgBox)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Lấy ảnh tải về thành công
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null && imgBox != null)
                {
                    // Đắp ảnh vào làm Background cho VisualElement
                    imgBox.style.backgroundImage = new StyleBackground(texture);
                }
            }
            else
            {
                Debug.LogError($"[LỖI TẢI ẢNH] Không thể tải {url}. Lỗi: {request.error}");
            }
        }
    }
    private void OnShowItemClicked(ClickEvent evt)
    {
        var btn = evt.currentTarget as Button;
        var item = btn?.userData as ProductItem;
        if (item == null) return;

        string correctSku = (item.colors != null && item.colors.Count > 0 && !string.IsNullOrEmpty(item.colors[0].sku))
                            ? item.colors[0].sku : item.sku;

        // Đóng gói 1 sản phẩm duy nhất vào DataBridge
        DataBridge.playlist = new List<ModelPlaylistItem>
        {
            new ModelPlaylistItem {
                assetName = item.name,
                modelUrl = correctSku,
                sku = correctSku,
                fullProductData = item,
                displayDuration = 60f
            }
        };
        DataBridge.isSlideshowMode = false;

        // Bay sang màn hình hiển thị 3D!
        SceneManager.LoadScene("Display Product");
    }
    private void OnToggleChanged(ChangeEvent<bool> evt)
    {
        var toggle = evt.currentTarget as Toggle;
        var item = toggle?.userData as ProductItem;
        if (item == null) return;

        if (evt.newValue)
        {
            if (!_selectedItems.Exists(x => x.name == item.name)) _selectedItems.Add(item);
        }
        else
        {
            _selectedItems.RemoveAll(x => x.name == item.name);
        }

        if (_btnConfirm != null)
        {
            _btnConfirm.style.display = _selectedItems.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        RefreshAllShowButtons();
    }

    private void RefreshAllShowButtons()
    {
        bool hasAnySelection = _selectedItems.Count > 0;

        foreach (var slot in _slots)
        {
            var btnShow = slot.Q<Button>("btn-show");
            if (btnShow != null)
            {
                btnShow.style.visibility = hasAnySelection ? Visibility.Hidden : Visibility.Visible;
            }
        }
    }

    private void OnShowClicked()
    {
        Debug.Log("Đã bấm xem trực tiếp 1 sản phẩm!");
    }

    // ==========================================
    // LOGIC CHUYỂN SANG MÀN HÌNH DISPLAY PRODUCT
    // ==========================================
    private void StartSlideshow()
    {
        float seconds = 60f; // Mặc định 60 giây

        // Đọc thời gian từ ô Input (Cố gắng chuyển chữ thành số)
        if (_inputTime != null && !string.IsNullOrEmpty(_inputTime.value))
        {
            float.TryParse(_inputTime.value, out seconds);
        }
        if (seconds <= 0) seconds = 60f; // Chống người dùng nhập số âm

        DataBridge.playlist = new List<ModelPlaylistItem>();
        foreach (var item in _selectedItems)
        {
            string correctSku = (item.colors != null && item.colors.Count > 0 && !string.IsNullOrEmpty(item.colors[0].sku))
                                ? item.colors[0].sku : item.sku;

            DataBridge.playlist.Add(new ModelPlaylistItem
            {
                assetName = item.name,
                modelUrl = correctSku,
                sku = correctSku,
                fullProductData = item,
                displayDuration = seconds // Áp dụng số giây người dùng vừa nhập
            });
        }

        DataBridge.isSlideshowMode = true;

        // Chuyển scene!
        SceneManager.LoadScene("Display Product");
    }
}