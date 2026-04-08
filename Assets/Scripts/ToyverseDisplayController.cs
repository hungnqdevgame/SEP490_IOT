using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;

/// ═══════════════════════════════════════════════════════════════════
/// ToyverseDisplayController.cs
///
/// NHIỆM VỤ: Điều phối toàn bộ UI Toolkit (UXML/USS) cho màn hình
///           hiển thị sản phẩm kiosk — thay thế phần UI của
///           ProductDisplay.cs, GIỮ NGUYÊN toàn bộ logic API và
///           LoadModel, không đụng vào các script scene khác.
///
/// LUỒNG:
///   SignalRManager.OnProductReceived
///       └─► HandleProductReceived(barCode)
///               └─► FetchUrlAndLoadModel(barCode)   [Coroutine, Web API]
///                       └─► SetupUI(ProductBarcodeData)
///                               ├─► PopulateTextFields()
///                               ├─► FetchCategoryName()       [Coroutine]
///                               └─► BuildColorSwatches()
///                                       └─► OnColorSelected(sku)
///                                               └─► LoadModel.DownloadAndShow()
///
/// PHỤ THUỘC (không thay đổi):
///   SignalRManager.cs, LoadModel.cs, DataModel.cs,
///   MainThreadDispatcher.cs (dispatch đã được SignalR lo)
/// ═══════════════════════════════════════════════════════════════════

public class ToyverseDisplayController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────
    // INSPECTOR
    // ──────────────────────────────────────────────────────────────
    [Header("── UI Toolkit ──")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset idleTemplate;
    [SerializeField] private VisualTreeAsset productTemplate;
    [SerializeField] private StyleSheet toyverseStyles;

    [Header("── Kết nối Script Hiện Có ──")]
    [Tooltip("Kéo GameObject chứa LoadModel.cs vào đây")]
    [SerializeField] private LoadModel modelLoader;

    [Header("── API Config (giống ProductDisplay.cs) ──")]
    [SerializeField] private string apiUrlGetBySku = "http://localhost:5035/api/Product/barcode/";
    [SerializeField] private string apiUrlCategory = "http://localhost:5035/api/ProductCategory/";
    [SerializeField] private string bundleServerUrl = "http://localhost:5035/";

    // ──────────────────────────────────────────────────────────────
    // RUNTIME STATE
    // ──────────────────────────────────────────────────────────────

    private VisualElement _root;
    private VisualElement _idleView;
    private VisualElement _productView;
    private Coroutine _helloCoroutine;

    // Swatches được spawn động — lưu lại để toggle active-state
    private readonly List<(VisualElement element, string sku)> _swatches = new();

    // Scheduler handles để cancel animation khi chuyển màn
    private IVisualElementScheduledItem _progressAnim;
    private IVisualElementScheduledItem _blinkAnim;
    private IVisualElementScheduledItem _scanAnim;
    private IVisualElementScheduledItem _floatAnim;
    private IVisualElementScheduledItem _ringOuterAnim;
    private IVisualElementScheduledItem _ringInnerAnim;

    // ══════════════════════════════════════════════════════════════
    #region Unity Lifecycle
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        _root = uiDocument.rootVisualElement;
        _root.styleSheets.Add(toyverseStyles);

        // VŨ KHÍ TỐI THƯỢNG: Ép gốc UI phải tàng hình 100%
        _root.style.backgroundColor = new StyleColor(Color.clear);

        BuildViews();

        // Ép vỏ bọc Template phải tàng hình (ĐÃ XÓA _idleView)
        if (_productView != null) _productView.style.backgroundColor = new StyleColor(Color.clear);
    }

    private void Start()
    {
        _root.style.flexGrow = 1;
        _root.style.backgroundColor = new StyleColor(Color.clear);

        // HIỆN MÀN CHỜ NGAY KHI VỪA MỞ GAME
        ShowIdle();

        // Đăng ký SignalR
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnMessageReceivedEvent += HandleMessageReceived;
            SignalRManager.Instance.OnProductReceived += HandleProductReceived;
            SignalRManager.Instance.OnConnectionStatusChanged += UpdateConnectionUI;
            UpdateConnectionUI(false);  
        }
           
        else
            Debug.LogWarning("[ToyverseUI] Không tìm thấy SignalRManager!");

        var btnSettings = _productView?.Q<Button>("btn-settings");
        var settingsPanel = _productView?.Q<VisualElement>("settings-panel");
        var btnCloseSettings = _productView?.Q<Button>("btn-close-settings");
        var btnSaveSettings = _productView?.Q<Button>("btn-save-settings");
        var inputUrl = _productView?.Q<TextField>("input-server-url");

        // Bấm bánh răng -> Mở bảng, điền sẵn URL hiện tại
        btnSettings?.RegisterCallback<ClickEvent>(e => {
            if (settingsPanel != null)
            {
                settingsPanel.style.display = DisplayStyle.Flex;
                if (inputUrl != null)
                    inputUrl.value = PlayerPrefs.GetString("SignalR_URL", "http://localhost:5035/productHub");
            }
        });

        // Bấm Hủy -> Tắt bảng
        btnCloseSettings?.RegisterCallback<ClickEvent>(e => {
            if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.None;
        });

        // Bấm Lưu -> Gọi SignalR kết nối lại và tắt bảng
        btnSaveSettings?.RegisterCallback<ClickEvent>(e => {
            if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.None;
            if (inputUrl != null && SignalRManager.Instance != null)
            {
                SignalRManager.Instance.ReconnectWithNewUrl(inputUrl.value);
            }
        });
    }
    private void OnDestroy()
    {
        StopAllAnimations();
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnProductReceived -= HandleProductReceived;
            // Hủy đăng ký nhận lời chào
            SignalRManager.Instance.OnMessageReceivedEvent -= HandleMessageReceived;
            SignalRManager.Instance.OnConnectionStatusChanged -= UpdateConnectionUI;
        }
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Build Views — khởi tạo UXML 1 lần duy nhất
    // ══════════════════════════════════════════════════════════════

    private void BuildViews()
    {
        _root.Clear();
        // Giờ ta chỉ load mỗi file Product thôi
        _productView = productTemplate.CloneTree();
        _productView.styleSheets.Add(toyverseStyles);
        _productView.style.flexGrow = 1;
        _productView.style.backgroundColor = new StyleColor(Color.clear);
        _root.Add(_productView);

        WireTabs();
        WireAngleButtons();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region SignalR → API → UI  (giữ nguyên logic ProductDisplay.cs)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Callback từ SignalRManager — giống HandleProductReceived() trong ProductDisplay.cs.
    /// MainThreadDispatcher đã dispatch về main thread bên trong SignalRManager rồi.
    /// </summary>
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
            // Bật Panel và in chữ ra
            helloPanel.style.display = DisplayStyle.Flex;
            helloText.text = text;

            // Reset và bắt đầu đếm 10 giây
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
        barCode = barCode.Trim();
        Debug.Log($"[ToyverseUI][TÍN HIỆU] Barcode nhận được: {barCode}. Đang gọi API...");
        StartCoroutine(FetchUrlAndLoadModel(barCode));
    }

    /// <summary>
    /// Fetch API theo barcode — logic 100% giống FetchUrlAndLoadModel() trong ProductDisplay.cs.
    /// </summary>
    private IEnumerator FetchUrlAndLoadModel(string barCode)
    {
        string finalUrl = apiUrlGetBySku + barCode;

        using var request = UnityWebRequest.Get(finalUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ToyverseUI][API LỖI] Không tìm thấy barcode {barCode}: {request.error}");
            ShowIdle(); // API lỗi → quay về idle
            yield break;
        }

        string rawJson = request.downloadHandler.text;
        Debug.Log($"[ToyverseUI][RAW JSON TỪ API]:\n{rawJson}");

        ProductBarcodeResponse response = null;
        try
        {
            response = JsonUtility.FromJson<ProductBarcodeResponse>(rawJson);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ToyverseUI][NGOẠI LỆ PARSE JSON] {e.Message}");
            yield break;
        }

        if (response?.data == null || string.IsNullOrEmpty(response.data.id))
        {
            Debug.LogError("[ToyverseUI][LỖI PARSE] Data null hoặc sai cấu trúc JSON.");
            yield break;
        }

        Debug.Log($"[ToyverseUI][THÀNH CÔNG] Sản phẩm: {response.data.name}");
        SetupUI(response.data);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region SetupUI — điểm vào chính khi có data
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tương đương SetupUI(ProductBarcodeData) trong ProductDisplay.cs
    /// nhưng target là UI Toolkit thay vì TextMeshPro + Carousel.
    /// </summary>
    private void SetupUI(ProductBarcodeData data)
    {
        // 1. Hiện màn sản phẩm (animation bắt đầu)
        ShowProduct();

        // 2. Điền các label tĩnh
        PopulateTextFields(data);

        // 3. Fetch tên loại sản phẩm — async, giống ProductDisplay.cs
        if (!string.IsNullOrEmpty(data.productCategoryId))
        {
            SetLabel("spec-type", "Đang tải...");
            StartCoroutine(FetchCategoryName(data.productCategoryId));
        }
        else
        {
            SetLabel("spec-type", "Chưa phân loại");
        }

        // 4. Spawn swatch màu từ data.colors — thay thế Carousel Prefab
        BuildColorSwatches(data);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Populate Text Fields
    // ══════════════════════════════════════════════════════════════

    /// <summary>Điền toàn bộ label tĩnh — ánh xạ 1-1 với ProductBarcodeData.</summary>
    private void PopulateTextFields(ProductBarcodeData data)
    {
        // ── Header ──
        SetLabel("prod-name", data.name);
        SetLabel("prod-sub", BuildSubtitle(data));
        SetLabel("prod-id", data.sku);

        // ── Spec chips (tab TỔNG QUAN) ──
        // spec-type: sẽ được điền sau FetchCategoryName
        SetLabel("spec-material", data.material);
        SetLabel("spec-brand", data.brand);
        SetLabel("spec-age", data.ageRange);
        SetLabel("spec-price", data.basePrice.ToString("N0") + " ₫");

        SetLabel("spec-length", data.length > 0 ? $"{data.length} cm" : "—");
        SetLabel("spec-width", data.width > 0 ? $"{data.width} cm" : "—");
        SetLabel("spec-height", data.height > 0 ? $"{data.height} cm" : "—");
        SetLabel("spec-weight", data.weight > 0 ? $"{data.weight} kg" : "—");
        // spec-battery dùng lại cho originCountry vì ProductBarcodeData không có field pin
        SetLabel("spec-battery",
            string.IsNullOrEmpty(data.originCountry) ? "—" : data.originCountry);
    }

    /// <summary>Ghép subtitle từ brand + ageRange giống ProductDisplay.</summary>
    private string BuildSubtitle(ProductBarcodeData data)
    {
        string brand = string.IsNullOrEmpty(data.brand) ? "" : data.brand;
        string age = string.IsNullOrEmpty(data.ageRange) ? "" : $"· {data.ageRange}";
        return $"{brand} {age}".Trim();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Fetch Category Name (giống ProductDisplay.FetchCategoryName)
    // ══════════════════════════════════════════════════════════════

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
        else
        {
            SetLabel("spec-type", "Lỗi kết nối");
            Debug.LogWarning($"[ToyverseUI] FetchCategoryName lỗi: {request.error}");
        }
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Build Color Swatches (thay thế Carousel Prefab)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tương đương phần "TẠO THẺ MÀU SẮC CHO CAROUSEL" trong ProductDisplay.cs.
    /// Thay vì Instantiate Prefab vào carouselContent, ta tạo VisualElement vào #swatches.
    /// </summary>
    private void BuildColorSwatches(ProductBarcodeData data)
    {
        var swatchContainer = _productView.Q<VisualElement>("swatches");
        if (swatchContainer == null)
        {
            Debug.LogWarning("[ToyverseUI] Không tìm thấy element 'swatches' trong UXML.");
            return;
        }

        // Xóa swatch cũ (nếu load sản phẩm mới)
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

            // Tự động chọn màu đầu tiên — giống ProductDisplay.cs: OnColorSelected(data.colors[0].sku)
            SelectSwatch(_swatches[0].element, _swatches[0].sku);
        }
        else
        {
            // Sản phẩm không chia màu → load thẳng SKU gốc (giống else trong ProductDisplay)
            OnColorSelected(data.sku);
        }
    }

    /// <summary>
    /// Tạo 1 VisualElement swatch từ ProductBarcodeColor.
    /// Tương đương 1 vòng lặp trong SetupUI() của ProductDisplay.cs
    /// (Instantiate colorCardPrefab + ColorCardUI.SetupCard + Button.onClick).
    /// </summary>
    private (VisualElement element, string sku) CreateSwatchElement(ProductBarcodeColor colorData)
    {
        var sw = new VisualElement();
        sw.AddToClassList("swatch");

        // Tooltip = tên màu (giống cName trong ProductDisplay.cs)
        string displayName = !string.IsNullOrEmpty(colorData.colorName)
                                ? colorData.colorName
                                : colorData.sku;
        sw.tooltip = displayName;

        // Màu nền từ hexcode API
        if (!string.IsNullOrEmpty(colorData.hexcode))
        {
            string hex = colorData.hexcode.StartsWith("#")
                            ? colorData.hexcode
                            : "#" + colorData.hexcode;

            if (ColorUtility.TryParseHtmlString(hex, out Color c))
                sw.style.backgroundColor = c;
            else
                sw.style.backgroundColor = Color.white;
        }
        else
        {
            sw.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f); // fallback xám nhạt
        }

        // Dấu ✓ ẩn ban đầu
        var check = new Label { text = "✓" };
        check.AddToClassList("swatch-check");
        check.style.opacity = 0;
        sw.Add(check);

        // Click → SelectSwatch (giống cardBtn.onClick trong ProductDisplay.cs)
        string skuCapture = colorData.sku;
        sw.RegisterCallback<ClickEvent>(_ => SelectSwatch(sw, skuCapture));

        return (sw, colorData.sku);
    }

    /// <summary>
    /// Cập nhật trạng thái active của swatches và gọi tải model.
    /// Tương đương logic onClick trong vòng lặp màu của ProductDisplay.cs.
    /// </summary>
    private void SelectSwatch(VisualElement selected, string sku)
    {
        // Bỏ active tất cả
        foreach (var (el, _) in _swatches)
        {
            el.RemoveFromClassList("swatch-active");
            var chk = el.Q<Label>(className: "swatch-check");
            if (chk != null) chk.style.opacity = 0;
        }

        // Active swatch được chọn
        selected.AddToClassList("swatch-active");
        var activeCheck = selected.Q<Label>(className: "swatch-check");
        if (activeCheck != null) activeCheck.style.opacity = 1;

        // Gọi tải model — giống cardBtn.onClick → OnColorSelected(skuToLoad)
        OnColorSelected(sku);
    }

    /// <summary>
    /// Gọi modelLoader.DownloadAndShow() — 100% giống OnColorSelected() trong ProductDisplay.cs.
    /// </summary>
    private void OnColorSelected(string modelSku)
    {
        if (string.IsNullOrEmpty(modelSku)) return;

        Debug.Log($"[ToyverseUI][HIỂN THỊ] Chọn màu SKU: {modelSku} — đang tải Model 3D...");

        string fullBundleUrl = bundleServerUrl + modelSku.ToLower();

        if (modelLoader != null)
            modelLoader.DownloadAndShow(fullBundleUrl, modelSku.ToLower());
        else
            Debug.LogError("[ToyverseUI] modelLoader CHƯA ĐƯỢC GÁN trong Inspector!");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Show Idle / Show Product
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Hiện màn chờ. Gọi khi: Start(), API lỗi, hoặc từ hệ thống bên ngoài.
    /// </summary>
    public void ShowIdle()
    {
        StopAllAnimations();

        var overlay = _productView?.Q<VisualElement>("idle-overlay");
        if (overlay != null) overlay.style.display = DisplayStyle.Flex;

        // KHÔNG ẨN TOPBAR VÀ INFO-PANEL NỮA
        var topbar = _productView?.Q<VisualElement>("topbar");
        if (topbar != null) topbar.style.display = DisplayStyle.Flex;

        var infoPanel = _productView?.Q<VisualElement>("info-panel");
        if (infoPanel != null) infoPanel.style.display = DisplayStyle.Flex;

        // Gọi hàm xóa trắng thông tin
        ClearTextFields();

        StartIdleAnimations();
    }

    /// <summary>Hiện màn sản phẩm — chỉ gọi từ SetupUI() sau khi đã có data.</summary>
    private void ShowProduct()
    {
        StopAllAnimations();
        // Ẩn lớp phủ báo chờ đi
        var overlay = _productView?.Q<VisualElement>("idle-overlay");
        if (overlay != null) overlay.style.display = DisplayStyle.None;

        // Hiện bảng thông số và chữ TOYVERSE
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

        // Xóa các ô màu 3D
        var swatchContainer = _productView?.Q<VisualElement>("swatches");
        if (swatchContainer != null) swatchContainer.Clear();
        _swatches.Clear();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Tab & Angle Button Wiring
    // ══════════════════════════════════════════════════════════════

    private void WireTabs()
    {
        var tabOverview = _productView.Q<Button>("tab-overview");
        var tabSpecs = _productView.Q<Button>("tab-specs");
        var contentOverview = _productView.Q<VisualElement>("tab-content-overview");
        var contentSpecs = _productView.Q<VisualElement>("tab-content-specs");

        tabOverview?.RegisterCallback<ClickEvent>(_ =>
            SwitchTab(tabOverview, tabSpecs, contentOverview, contentSpecs));

        tabSpecs?.RegisterCallback<ClickEvent>(_ =>
            SwitchTab(tabSpecs, tabOverview, contentSpecs, contentOverview));
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
                // Nếu có CameraRig: CameraRig.Instance?.SetView(btn.name);
                Debug.Log($"[ToyverseUI] Góc nhìn đổi sang: {btn.name}");
            });
        }
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Idle Animations
    // ══════════════════════════════════════════════════════════════

    private void StartIdleAnimations()
    {
        // Thay vì tìm trong _idleView, giờ ta tìm trong _productView
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
            if (progressMs <= progressDuration)
                fill.style.width = Length.Percent(EaseInOut(progressMs / progressDuration) * 100f);
            else if (progressMs >= progressDuration + 600f)
                progressMs = 0f;
        }).Every(50).StartingIn(100);

        float outerAngle = 0f;
        _ringOuterAnim = ringOuter.schedule.Execute(() =>
        {
            outerAngle = (outerAngle + 1.2f) % 360f;
            ringOuter.style.rotate = new Rotate(Angle.Degrees(outerAngle));
        }).Every(16);

        float innerAngle = 0f;
        _ringInnerAnim = ringInner.schedule.Execute(() =>
        {
            innerAngle = (innerAngle - 0.6f + 360f) % 360f;
            ringInner.style.rotate = new Rotate(Angle.Degrees(innerAngle));
        }).Every(16);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Product Animations
    // ══════════════════════════════════════════════════════════════

    private void StartProductAnimations()
    {
        var scanLine = _productView.Q<VisualElement>("scan-line");
        var liveDot = _productView.Q<VisualElement>("live-dot");
        // product-3d và product-shadow đã xóa khỏi UXML (Canvas 3D mode)

        // Scan line trôi từ top → bottom toàn bộ frame trong 4s
        if (scanLine != null)
        {
            float ms = 0f;
            _scanAnim = scanLine.schedule.Execute(() =>
            {
                ms = (ms + 16f) % 4000f;
                // Lấy chiều cao frame thực tế thay vì hardcode 260px
                float frameH = _productView.resolvedStyle.height;
                if (frameH <= 0) frameH = 900f; // fallback nếu chưa layout
                scanLine.style.top = (ms / 4000f) * frameH;
            }).Every(16);
        }

        // product-3d không còn trong UXML — float animation đã được xóa.
        // Nếu muốn float model 3D, dùng animation trên Camera hoặc model transform.

        // Live dot blink mỗi 1 giây
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

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Helpers
    // ══════════════════════════════════════════════════════════════

    /// <summary>Tìm Label theo name trong _productView và set text an toàn.</summary>
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
        _floatAnim?.Pause();
        _ringOuterAnim?.Pause();
        _ringInnerAnim?.Pause();
    }

    private static float EaseInOut(float t) => t * t * (3f - 2f * t);

    #endregion

    private void UpdateConnectionUI(bool isConnected)
    {
        var liveDot = _productView?.Q<VisualElement>("live-dot");
        var liveText = _productView?.Q<Label>("live-text");

        if (liveDot == null || liveText == null) return;

        if (isConnected)
        {
            liveText.text = "ĐÃ KẾT NỐI";
            // Đổi chữ và chấm sang màu Xanh Neon
            liveDot.style.backgroundColor = new StyleColor(new Color32(0, 255, 136, 255));
            liveText.style.color = new StyleColor(new Color32(0, 255, 136, 255));
        }
        else
        {
            liveText.text = "CHƯA KẾT NỐI";
            // Đổi chữ và chấm sang màu Đỏ
            liveDot.style.backgroundColor = new StyleColor(new Color32(255, 60, 60, 255));
            liveText.style.color = new StyleColor(new Color32(255, 60, 60, 255));
        }
    }
}