using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ToyverseDisplayController : MonoBehaviour
{
    [Header("── UI Toolkit ──")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset productTemplate;
    [SerializeField] private StyleSheet toyverseStyles;

    [Header("── Kết nối Script Hiện Có ──")]
    [SerializeField] private LoadModel modelLoader;

    [Header("── API Config ──")]
    [SerializeField] private string apiUrlGetBySku = "http://localhost:5035/api/Product/barcode/";
    [SerializeField] private string apiUrlCategory = "http://localhost:5035/api/ProductCategory/";
    [SerializeField] private string bundleServerUrl = "http://localhost:5035/";

    private VisualElement _root;
    private VisualElement _productView;
    private Coroutine _helloCoroutine;
    private Coroutine _slideshowCoroutine;

    private readonly List<(VisualElement element, string sku)> _swatches = new();

    private IVisualElementScheduledItem _progressAnim;
    private IVisualElementScheduledItem _blinkAnim;
    private IVisualElementScheduledItem _scanAnim;
    private IVisualElementScheduledItem _ringOuterAnim;
    private IVisualElementScheduledItem _ringInnerAnim;

    private void Awake()
    {
        _root = uiDocument.rootVisualElement;
        _root.styleSheets.Add(toyverseStyles);
        _root.style.backgroundColor = new StyleColor(Color.clear);

        BuildViews();
        if (_productView != null) _productView.style.backgroundColor = new StyleColor(Color.clear);
    }

    private void Start()
    {
        _root.style.flexGrow = 1;
        _root.style.backgroundColor = new StyleColor(Color.clear);

        ShowIdle();

        // ================================================================
        // KHỞI ĐỘNG CHẾ ĐỘ SLIDESHOW HOẶC ĐƠN LẺ TỪ DATABRIDGE
        // ================================================================
        if (DataBridge.playlist != null && DataBridge.playlist.Count > 0)
        {
            if (DataBridge.isSlideshowMode)
            {
                Debug.Log("[ToyverseUI] Bật chế độ Trình Chiếu (Slideshow)!");
                _slideshowCoroutine = StartCoroutine(PlaySlideshowRoutine());
            }
            else
            {
                Debug.Log("[ToyverseUI] Bật chế độ 1 Sản phẩm!");
                LoadSingleProductFromBridge(DataBridge.playlist[0]);
            }
        }

        // ================================================================
        // ĐĂNG KÝ SỰ KIỆN SIGNALR VÀ NÚT BẤM (GIỮ NGUYÊN)
        // ================================================================
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnMessageReceivedEvent += HandleMessageReceived;
            SignalRManager.Instance.OnProductReceived += HandleProductReceived;
        }

        var btnSettings = _productView?.Q<Button>("btn-settings");
        var settingsPanel = _productView?.Q<VisualElement>("settings-panel");
        var btnCloseSettings = _productView?.Q<Button>("btn-close-settings");
        var btnSaveSettings = _productView?.Q<Button>("btn-save-settings");
        var inputUrl = _productView?.Q<TextField>("input-server-url");

        // Mẹo: Cho phép bấm vào Logo TOYVERSE để thoát về màn hình danh sách
        var logoLabel = _productView?.Q<Label>("logo");
        logoLabel?.RegisterCallback<ClickEvent>(e => {
            DataBridge.isSlideshowMode = false;
            SceneManager.LoadScene("Product Scene"); // Đổi về tên Scene danh sách của bạn
        });

        btnSettings?.RegisterCallback<ClickEvent>(e => {
            if (settingsPanel != null)
            {
                settingsPanel.style.display = DisplayStyle.Flex;
                if (inputUrl != null) inputUrl.value = PlayerPrefs.GetString("SignalR_URL", "http://localhost:5035/productHub");
            }
        });
        btnCloseSettings?.RegisterCallback<ClickEvent>(e => {
            if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.None;
        });
        btnSaveSettings?.RegisterCallback<ClickEvent>(e => {
            if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.None;
            if (inputUrl != null && SignalRManager.Instance != null) SignalRManager.Instance.ReconnectWithNewUrl(inputUrl.value);
        });
    }

    private void OnDestroy()
    {
        StopAllAnimations();
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnProductReceived -= HandleProductReceived;
            SignalRManager.Instance.OnMessageReceivedEvent -= HandleMessageReceived;
        }
    }

    private void BuildViews()
    {
        _root.Clear();
        _productView = productTemplate.CloneTree();
        _productView.styleSheets.Add(toyverseStyles);
        _productView.style.flexGrow = 1;
        _productView.style.backgroundColor = new StyleColor(Color.clear);
        _root.Add(_productView);

        WireTabs();
        WireAngleButtons();
    }

    // ================================================================
    // LOGIC SLIDESHOW & ĐỌC DATABRIDGE
    // ================================================================
    private IEnumerator PlaySlideshowRoutine()
    {
        int currentIndex = 0;
        while (true)
        {
            var currentItem = DataBridge.playlist[currentIndex];
            Debug.Log($"[ToyverseUI] Đang chiếu: {currentItem.assetName} trong {currentItem.displayDuration}s");

            LoadSingleProductFromBridge(currentItem);

            // Chờ hết số giây mà user đã nhập ở màn hình trước
            yield return new WaitForSeconds(currentItem.displayDuration);

            currentIndex++;
            if (currentIndex >= DataBridge.playlist.Count) currentIndex = 0; // Quay lại từ đầu
        }
    }

    private void LoadSingleProductFromBridge(ModelPlaylistItem playItem)
    {
        if (playItem.fullProductData != null)
        {
            // Chuyển đổi dữ liệu từ ProductItem sang ProductBarcodeData
            string json = JsonUtility.ToJson(playItem.fullProductData);
            ProductBarcodeData mappedData = JsonUtility.FromJson<ProductBarcodeData>(json);

            if (mappedData != null)
            {
                // Bổ sung các field bị lệch tên giữa 2 class
                mappedData.basePrice = playItem.fullProductData.price;

                // GỌI HIỂN THỊ VÀ TRUYỀN SKU MÀU ĐANG CHỌN
                SetupUI(mappedData, playItem.sku);
            }
        }
    }

    // ================================================================
    // SIGNALR & GỌI API BARCODE (CHO SÚNG QUÉT)
    // ================================================================
    private void HandleMessageReceived(string text)
    {
        var helloPanel = _productView?.Q<VisualElement>("hello-panel");
        var helloText = _productView?.Q<Label>("hello-text");
        if (helloPanel == null || helloText == null) return;

        if (string.IsNullOrWhiteSpace(text))
        {
            helloPanel.style.display = DisplayStyle.None;
            if (_helloCoroutine != null) StopCoroutine(_helloCoroutine);
        }
        else
        {
            helloPanel.style.display = DisplayStyle.Flex;
            helloText.text = text;
            if (_helloCoroutine != null) StopCoroutine(_helloCoroutine);
            _helloCoroutine = StartCoroutine(HideHelloPanelAfterDelay(5f));
        }
    }

    private IEnumerator HideHelloPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        var helloPanel = _productView?.Q<VisualElement>("hello-panel");
        if (helloPanel != null) helloPanel.style.display = DisplayStyle.None;
    }

    private void HandleProductReceived(string barCode)
    {
        // Khi dùng súng quét mã vạch, ta phải dừng Slideshow lại
        if (_slideshowCoroutine != null) StopCoroutine(_slideshowCoroutine);

        barCode = barCode.Trim();
        StartCoroutine(FetchUrlAndLoadModel(barCode));
    }

    private IEnumerator FetchUrlAndLoadModel(string barCode)
    {
        string finalUrl = apiUrlGetBySku + barCode;
        using var request = UnityWebRequest.Get(finalUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            ShowIdle();
            yield break;
        }

        ProductBarcodeResponse response = null;
        try { response = JsonUtility.FromJson<ProductBarcodeResponse>(request.downloadHandler.text); }
        catch { yield break; }

        if (response?.data != null && !string.IsNullOrEmpty(response.data.id))
        {
            SetupUI(response.data);
        }
    }

    // ================================================================
    // CẬP NHẬT UI (CÓ THÊM TARGET SKU ĐỂ LOAD ĐÚNG MÀU KHI SLIDESHOW)
    // ================================================================
    private void SetupUI(ProductBarcodeData data, string targetSku = "")
    {
        ShowProduct();
        PopulateTextFields(data);

        if (!string.IsNullOrEmpty(data.productCategoryId))
        {
            SetLabel("spec-type", "Đang tải...");
            StartCoroutine(FetchCategoryName(data.productCategoryId));
        }
        else SetLabel("spec-type", "Chưa phân loại");

        // Gửi mã SKU mục tiêu (Nếu có) để tự động bấm chọn đúng ô màu đó
        BuildColorSwatches(data, targetSku);
    }

    private void PopulateTextFields(ProductBarcodeData data)
    {
        SetLabel("prod-name", data.name);
        SetLabel("prod-sub", BuildSubtitle(data));
        SetLabel("prod-id", data.sku);

        SetLabel("spec-material", data.material);
        SetLabel("spec-brand", data.brand);
        SetLabel("spec-age", data.ageRange);
        SetLabel("spec-price", data.basePrice.ToString("N0") + " ₫");

        SetLabel("spec-length", data.length > 0 ? $"{data.length} cm" : "—");
        SetLabel("spec-width", data.width > 0 ? $"{data.width} cm" : "—");
        SetLabel("spec-height", data.height > 0 ? $"{data.height} cm" : "—");
        SetLabel("spec-weight", data.weight > 0 ? $"{data.weight} kg" : "—");
        SetLabel("spec-battery", string.IsNullOrEmpty(data.originCountry) ? "—" : data.originCountry);
    }

    private string BuildSubtitle(ProductBarcodeData data)
    {
        string brand = string.IsNullOrEmpty(data.brand) ? "" : data.brand;
        string age = string.IsNullOrEmpty(data.ageRange) ? "" : $"· {data.ageRange}";
        return $"{brand} {age}".Trim();
    }

    private IEnumerator FetchCategoryName(string categoryId)
    {
        string url = apiUrlCategory + categoryId;
        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<SingleCategoryResponse>(request.downloadHandler.text);
            SetLabel("spec-type", (resp?.data != null) ? resp.data.name : "Chưa phân loại");
        }
        else SetLabel("spec-type", "Lỗi kết nối");
    }

    private void BuildColorSwatches(ProductBarcodeData data, string targetSku)
    {
        var swatchContainer = _productView.Q<VisualElement>("swatches");
        if (swatchContainer == null) return;

        swatchContainer.Clear();
        _swatches.Clear();

        if (data.colors != null && data.colors.Count > 0)
        {
            foreach (var colorData in data.colors)
            {
                var swatchEntry = CreateSwatchElement(colorData);
                swatchContainer.Add(swatchEntry.element);
                _swatches.Add(swatchEntry);
            }

            // Tự động tìm và chọn ô màu theo mã targetSku (nếu có), nếu không thì chọn ô đầu tiên
            int selectedIndex = 0;
            if (!string.IsNullOrEmpty(targetSku))
            {
                selectedIndex = _swatches.FindIndex(s => s.sku.ToLower() == targetSku.ToLower());
                if (selectedIndex == -1) selectedIndex = 0;
            }

            SelectSwatch(_swatches[selectedIndex].element, _swatches[selectedIndex].sku);
        }
        else
        {
            OnColorSelected(data.sku);
        }
    }

    private (VisualElement element, string sku) CreateSwatchElement(ProductBarcodeColor colorData)
    {
        var sw = new VisualElement();
        sw.AddToClassList("swatch");
        sw.tooltip = !string.IsNullOrEmpty(colorData.colorName) ? colorData.colorName : colorData.sku;

        if (!string.IsNullOrEmpty(colorData.hexcode))
        {
            string hex = colorData.hexcode.StartsWith("#") ? colorData.hexcode : "#" + colorData.hexcode;
            if (ColorUtility.TryParseHtmlString(hex, out Color c)) sw.style.backgroundColor = c;
            else sw.style.backgroundColor = Color.white;
        }
        else sw.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f);

        var check = new Label { text = "✓" };
        check.AddToClassList("swatch-check");
        check.style.opacity = 0;
        sw.Add(check);

        string skuCapture = colorData.sku;
        sw.RegisterCallback<ClickEvent>(_ => SelectSwatch(sw, skuCapture));

        return (sw, colorData.sku);
    }

    private void SelectSwatch(VisualElement selected, string sku)
    {
        foreach (var (el, _) in _swatches)
        {
            el.RemoveFromClassList("swatch-active");
            var chk = el.Q<Label>(className: "swatch-check");
            if (chk != null) chk.style.opacity = 0;
        }

        selected.AddToClassList("swatch-active");
        var activeCheck = selected.Q<Label>(className: "swatch-check");
        if (activeCheck != null) activeCheck.style.opacity = 1;

        OnColorSelected(sku);
    }

    private void OnColorSelected(string modelSku)
    {
        if (string.IsNullOrEmpty(modelSku)) return;
        string fullBundleUrl = bundleServerUrl + modelSku.ToLower();
        if (modelLoader != null) modelLoader.DownloadAndShow(fullBundleUrl, modelSku.ToLower());
    }

    // ================================================================
    // QUẢN LÝ GIAO DIỆN CHUNG VÀ ANIMATION
    // ================================================================
    public void ShowIdle()
    {
        StopAllAnimations();
        var overlay = _productView?.Q<VisualElement>("idle-overlay");
        if (overlay != null) overlay.style.display = DisplayStyle.Flex;

        var topbar = _productView?.Q<VisualElement>("topbar");
        if (topbar != null) topbar.style.display = DisplayStyle.Flex;

        var infoPanel = _productView?.Q<VisualElement>("info-panel");
        if (infoPanel != null) infoPanel.style.display = DisplayStyle.Flex;

        ClearTextFields();
        StartIdleAnimations();
    }

    private void ShowProduct()
    {
        StopAllAnimations();
        var overlay = _productView?.Q<VisualElement>("idle-overlay");
        if (overlay != null) overlay.style.display = DisplayStyle.None;

        var topbar = _productView?.Q<VisualElement>("topbar");
        if (topbar != null) topbar.style.display = DisplayStyle.Flex;

        var infoPanel = _productView?.Q<VisualElement>("info-panel");
        if (infoPanel != null) infoPanel.style.display = DisplayStyle.Flex;

        ResetToOverviewTab();
        StartProductAnimations();
    }

    private void ClearTextFields()
    {
        SetLabel("prod-name", "CHƯA CÓ SẢN PHẨM");
        SetLabel("prod-sub", "Vui lòng đặt sản phẩm lên kệ để quét");
        SetLabel("prod-id", "---");
        SetLabel("spec-material", "---");
        SetLabel("spec-brand", "---");
        SetLabel("spec-age", "---");
        SetLabel("spec-price", "---");
        SetLabel("spec-length", "---");
        SetLabel("spec-width", "---");
        SetLabel("spec-height", "---");
        SetLabel("spec-weight", "---");
        SetLabel("spec-battery", "---");
        SetLabel("spec-type", "---");

        var swatchContainer = _productView?.Q<VisualElement>("swatches");
        if (swatchContainer != null) swatchContainer.Clear();
        _swatches.Clear();
    }

    private void WireTabs()
    {
        var tabOverview = _productView.Q<Button>("tab-overview");
        var tabSpecs = _productView.Q<Button>("tab-specs");
        var contentOverview = _productView.Q<VisualElement>("tab-content-overview");
        var contentSpecs = _productView.Q<VisualElement>("tab-content-specs");

        tabOverview?.RegisterCallback<ClickEvent>(_ => SwitchTab(tabOverview, tabSpecs, contentOverview, contentSpecs));
        tabSpecs?.RegisterCallback<ClickEvent>(_ => SwitchTab(tabSpecs, tabOverview, contentSpecs, contentOverview));
    }

    private void SwitchTab(Button on, Button off, VisualElement show, VisualElement hide)
    {
        on.AddToClassList("tab-active");
        off.RemoveFromClassList("tab-active");
        show.RemoveFromClassList("tab-content-hidden");
        hide.AddToClassList("tab-content-hidden");
    }

    private void ResetToOverviewTab()
    {
        _productView.Q<Button>("tab-overview")?.AddToClassList("tab-active");
        _productView.Q<Button>("tab-specs")?.RemoveFromClassList("tab-active");
        _productView.Q<VisualElement>("tab-content-overview")?.RemoveFromClassList("tab-content-hidden");
        _productView.Q<VisualElement>("tab-content-specs")?.AddToClassList("tab-content-hidden");
    }

    private void WireAngleButtons()
    {
        var angleNames = new[] { "angle-front", "angle-side", "angle-top" };
        var angles = new List<Button>();

        foreach (var n in angleNames)
        {
            var btn = _productView.Q<Button>(n);
            if (btn != null) angles.Add(btn);
        }

        foreach (var btn in angles)
        {
            btn.RegisterCallback<ClickEvent>(_ =>
            {
                foreach (var b in angles) b.RemoveFromClassList("angle-active");
                btn.AddToClassList("angle-active");
            });
        }
    }

    private void StartIdleAnimations()
    {
        var fill = _productView.Q<VisualElement>("idle-fill");
        var ringOuter = _productView.Q<VisualElement>("idle-ring-outer");
        var ringInner = _productView.Q<VisualElement>("idle-ring-inner");

        if (fill == null || ringOuter == null || ringInner == null) return;

        float progressMs = 0f;
        const float progressDuration = 2500f;

        fill.style.width = Length.Percent(0);
        _progressAnim = fill.schedule.Execute(() =>
        {
            progressMs += 50f;
            if (progressMs <= progressDuration) fill.style.width = Length.Percent(EaseInOut(progressMs / progressDuration) * 100f);
            else if (progressMs >= progressDuration + 600f) progressMs = 0f;
        }).Every(50).StartingIn(100);

        float outerAngle = 0f;
        _ringOuterAnim = ringOuter.schedule.Execute(() => {
            outerAngle = (outerAngle + 1.2f) % 360f;
            ringOuter.style.rotate = new Rotate(Angle.Degrees(outerAngle));
        }).Every(16);

        float innerAngle = 0f;
        _ringInnerAnim = ringInner.schedule.Execute(() => {
            innerAngle = (innerAngle - 0.6f + 360f) % 360f;
            ringInner.style.rotate = new Rotate(Angle.Degrees(innerAngle));
        }).Every(16);
    }

    private void StartProductAnimations()
    {
        var scanLine = _productView.Q<VisualElement>("scan-line");
        var liveDot = _productView.Q<VisualElement>("live-dot");

        if (scanLine != null)
        {
            float ms = 0f;
            _scanAnim = scanLine.schedule.Execute(() =>
            {
                ms = (ms + 16f) % 4000f;
                float frameH = _productView.resolvedStyle.height;
                if (frameH <= 0) frameH = 900f;
                scanLine.style.top = (ms / 4000f) * frameH;
            }).Every(16);
        }

        if (liveDot != null)
        {
            bool vis = true;
            _blinkAnim = liveDot.schedule.Execute(() =>
            {
                vis = !vis;
                liveDot.style.opacity = vis ? 1f : 0.2f;
            }).Every(1000);
        }
    }

    private void SetLabel(string elementName, string value)
    {
        var el = _productView.Q<Label>(elementName);
        if (el != null) el.text = string.IsNullOrEmpty(value) ? "—" : value;
    }

    private void StopAllAnimations()
    {
        _progressAnim?.Pause();
        _blinkAnim?.Pause();
        _scanAnim?.Pause();
        _ringOuterAnim?.Pause();
        _ringInnerAnim?.Pause();
    }

    private static float EaseInOut(float t) => t * t * (3f - 2f * t);
}