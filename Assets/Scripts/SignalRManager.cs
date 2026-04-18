using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SignalRManager — Kết nối SignalR, tương thích cả Windows EXE và Android APK.
///
/// VẤN ĐỀ ANDROID:
///   - Android không cho phép HTTP cleartext (http://) từ Android 9+ theo mặc định.
///   - WebSocket thuần có thể bị chặn bởi một số mạng Android.
///   - SSL certificate tự ký (self-signed) bị từ chối trên Android.
///
/// GIẢI PHÁP ĐÃ ÁP DỤNG:
///   1. Tự động detect platform → chọn transport phù hợp.
///   2. Android dùng LongPolling làm fallback nếu WebSocket fail.
///   3. Bypass SSL certificate validation trên Android (dev only).
///   4. Hỗ trợ đổi URL runtime qua PanelController hoặc PlayerPrefs.
///
/// LƯU Ý THÊM CHO ANDROID BUILD:
///   - Assets/Plugins/Android/AndroidManifest.xml phải có:
///       android:usesCleartextTraffic="true"  (nếu dùng http://)
///   - Hoặc đổi server sang https:// với cert hợp lệ.
/// </summary>
public class SignalRManager : MonoBehaviour
{
    public static SignalRManager Instance;

    public string serverUrl;
    private HubConnection connection;

    public event Action<string> OnProductReceived;
    public event Action<float>  OnProductRotatedEvent;
    public event Action<string> OnMessageReceivedEvent;
    public event Action<bool>   OnConnectionStatusChanged;

    // Retry config
    private int   _retryCount  = 0;
    private const int MaxRetry = 3;
    private const float RetryDelaySeconds = 3f;
	private int _connectSessionId = 0;
    // ══════════════════════════════════════════════════════════════
    #region Unity Lifecycle
    // ══════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            serverUrl = PlayerPrefs.GetString("SignalR_URL", "http://localhost:5035/productHub");
            SetupSignalR();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void OnApplicationQuit() { await CloseConnection(); }
    private async void OnDestroy()         { await CloseConnection(); }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Setup & Connect
    // ══════════════════════════════════════════════════════════════

void SetupSignalR()
    {
_connectSessionId++;
        // 1. BYPASS SSL TOÀN CỤC CHO UNITY (Fix lỗi Build Android)
        // Bỏ qua mọi kiểm tra chứng chỉ SSL (Rất tốt khi test với HTTPS tự ký)
        System.Net.ServicePointManager.ServerCertificateValidationCallback += 
            (sender, certificate, chain, sslPolicyErrors) => true;

        var builder = new HubConnectionBuilder()
            .WithUrl(serverUrl, options =>
            {
                // ── TRANSPORT STRATEGY ──────────────────────────────
                // Windows/PC: WebSocket nhanh, ổn định → ưu tiên.
                // Android   : Thêm LongPolling làm fallback vì một số mạng chặn WebSocket.
#if UNITY_ANDROID && !UNITY_EDITOR
                options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
                options.SkipNegotiation = false;
#else
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
#endif
                // ĐÃ XÓA BỎ ĐOẠN HttpMessageHandlerFactory GÂY LỖI BUILD Ở ĐÂY
            })
            .WithAutomaticReconnect(new[]
            {
                // Retry sau: 0s, 2s, 5s, 10s
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10)
            });

        connection = builder.Build();

        // ── ĐĂNG KÝ CÁC EVENT HANDLER ────────────────────────────

        connection.On<string>("OnProductSelected", barCode =>
        {
            Debug.Log($"[SignalR] Barcode: {barCode}");
            MainThreadDispatcher.Enqueue(() => OnProductReceived?.Invoke(barCode));
        });

        connection.On<float>("OnProductRotated", angle =>
        {
            MainThreadDispatcher.Enqueue(() => OnProductRotatedEvent?.Invoke(angle));
        });

        connection.On<string>("ReceiveMessage", text =>
        {
            MainThreadDispatcher.Enqueue(() => OnMessageReceivedEvent?.Invoke(text));
        });

        connection.On<string>("OnGestureDetect", gesture =>
        {
            MainThreadDispatcher.Enqueue(() => OnMessageReceivedEvent?.Invoke(gesture));
        });

        // ── CONNECTION LIFECYCLE ──────────────────────────────────

        connection.Closed += async error =>
        {
            Debug.LogWarning($"[SignalR] Kết nối đóng: {error?.Message}");
            MainThreadDispatcher.Enqueue(() => OnConnectionStatusChanged?.Invoke(false));
            await Task.CompletedTask;
        };

        connection.Reconnecting += async error =>
        {
            Debug.LogWarning($"[SignalR] Đang kết nối lại: {error?.Message}");
            MainThreadDispatcher.Enqueue(() => OnConnectionStatusChanged?.Invoke(false));
            await Task.CompletedTask;
        };

        connection.Reconnected += async connectionId =>
        {
            Debug.Log($"[SignalR] Kết nối lại thành công: {connectionId}");
            _retryCount = 0;
            MainThreadDispatcher.Enqueue(() => OnConnectionStatusChanged?.Invoke(true));
            await Task.CompletedTask;
        };

        Connect();
    }
    async void Connect()
    {
      int mySession = _connectSessionId; 

        if (connection == null || connection.State != HubConnectionState.Disconnected) return;

        try
        {
            await connection.StartAsync();

            // Nếu trong lúc Start mà người dùng bấm đổi URL -> vứt luôn kết quả này
            if (mySession != _connectSessionId) return; 

            _retryCount = 0;
            Debug.Log($"[SignalR] Kết nối thành công! URL: {serverUrl}");
            MainThreadDispatcher.Enqueue(() => OnConnectionStatusChanged?.Invoke(true));
        }
        catch (Exception ex)
        {
            // BÓNG MA CHẾT TẠI ĐÂY: Nếu ID đã thay đổi, dập tắt vòng lặp ngay lập tức
            if (mySession != _connectSessionId) return; 

            Debug.LogError($"[SignalR] Kết nối thất bại ({_retryCount + 1}/{MaxRetry}): {ex.Message}");
            MainThreadDispatcher.Enqueue(() => OnConnectionStatusChanged?.Invoke(false));

            if (_retryCount < MaxRetry)
            {
                _retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds));
                
                // BÓNG MA CHẾT TẠI ĐÂY (Sau khi ngủ dậy): Kiểm tra lại lần nữa trước khi Retry
                if (mySession != _connectSessionId) return; 
                
                Connect();
            }
        }
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Public API
    // ══════════════════════════════════════════════════════════════

    public async Task SendSelectProduct(string barCode)
    {
        if (connection?.State == HubConnectionState.Connected)
            await connection.InvokeAsync("SelectProduct", barCode);
        else
            Debug.LogWarning("[SignalR] Chưa kết nối — không thể gửi barcode.");
    }

    /// <summary>
    /// Đổi URL và kết nối lại — dùng từ Settings/PanelController.
    /// </summary>
    public async void ReconnectWithNewUrl(string newUrl)
    {
      serverUrl = newUrl;
        PlayerPrefs.SetString("SignalR_URL", newUrl);
        PlayerPrefs.Save();

        Debug.Log($"[SignalR] Đang thiết lập lại kết nối với: {newUrl}");

        // 2. TẮT HOÀN TOÀN CÁI CŨ
        await CloseConnection();

        // 3. Đợi một nhịp nhỏ để thư viện mạng của Unity giải phóng cổng
        await Task.Delay(500);

        // 4. BẬT LẠI CÁI MỚI
        SetupSignalR();
    }

    /// <summary>Kết nối lại với URL hiện tại.</summary>
    public async void Reconnect()
    {
        await CloseConnection();
        SetupSignalR();
    }

    /// <summary>Trạng thái kết nối hiện tại.</summary>
    public bool IsConnected =>
        connection?.State == HubConnectionState.Connected;

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Cleanup
    // ══════════════════════════════════════════════════════════════

    private async Task CloseConnection()
    {
        if (connection == null) return;
        if (connection.State == HubConnectionState.Disconnected) return;

        try
        {
            await connection.StopAsync();
            await connection.DisposeAsync();
            connection = null;
            Debug.Log("[SignalR] Đã đóng kết nối an toàn.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SignalR] Lỗi khi đóng: {ex.Message}");
        }
    }

    #endregion
}
