using NativeWebSocket;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class WebSocketManager : MonoBehaviour
{
    public static WebSocketManager Instance;

    [Header("Network Settings")]
    public string serverUrl;
    private WebSocket websocket;

    // CẤU HÌNH AUTO-RECONNECT
    private bool isReconnecting = false;
    private const int ReconnectDelayMs = 3000;
    private bool isQuitting = false;

    // Các Event giao tiếp với ToyAnimator và UI
    public event Action<string> OnProductReceived;
    public event Action<float> OnProductRotatedEvent;
    public event Action<string> OnMessageReceivedEvent;
    public event Action<bool> OnConnectionStatusChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            string savedUrl = PlayerPrefs.GetString("WebSocket_URL", "ws://192.168.137.194:5035/ws");
            serverUrl = savedUrl.Replace("http://", "ws://").Replace("https://", "wss://");

            ConnectToServer();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    async void ConnectToServer()
    {
        if (isQuitting) return;

        Debug.Log($"[WebSocket] Đang kết nối tới: {serverUrl}");

        // 1. DỌN DẸP KẾT NỐI CŨ (Đã xóa các dòng gán null gây lỗi)
        if (websocket != null)
        {
            if (websocket.State == WebSocketState.Open)
            {
                await websocket.Close();
            }
            websocket = null;
        }

        // 2. KHỞI TẠO KẾT NỐI MỚI
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("[WebSocket] Kết nối thành công!");
            isReconnecting = false;
            OnConnectionStatusChanged?.Invoke(true);
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError($"[WebSocket] Lỗi: {e}");
            OnConnectionStatusChanged?.Invoke(false);
        };

        websocket.OnClose += (c) =>
        {
            Debug.LogWarning("[WebSocket] Đã đóng hoặc mất kết nối.");
            OnConnectionStatusChanged?.Invoke(false);

            if (!isQuitting) TriggerAutoReconnect();
        };

        websocket.OnMessage += (bytes) =>
        {
            string rawMessage = Encoding.UTF8.GetString(bytes);
            try
            {
                // ĐÃ ĐỔI TÊN THÀNH ServerMessage
                var data = JsonConvert.DeserializeObject<ServerMessage>(rawMessage);
                if (data != null && data.arguments != null && data.arguments.Length > 0)
                {
                    ProcessServerMethod(data.target, data.arguments);
                }
            }
            catch { /* Bỏ qua nếu tin nhắn không đúng chuẩn JSON */ }
        };

        // 3. BẮT ĐẦU KẾT NỐI
        try
        {
            await websocket.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebSocket] Không thể với tới Server: {ex.Message}");
            if (!isQuitting) TriggerAutoReconnect();
        }
    }

    // VÒNG LẶP TỰ ĐỘNG KẾT NỐI LẠI
    private async void TriggerAutoReconnect()
    {
        if (isReconnecting || isQuitting) return;

        isReconnecting = true;
        Debug.Log($"[WebSocket] Sẽ thử kết nối lại sau {ReconnectDelayMs / 1000} giây...");

        await Task.Delay(ReconnectDelayMs);

        if (!isQuitting)
        {
            ConnectToServer();
        }
    }

    void ProcessServerMethod(string target, object[] args)
    {
        switch (target)
        {
            case "OnProductSelected":
            case "SelectProduct":
                if (args != null && args.Length > 0)
                {
                    string barcode = args[0].ToString();
                    Debug.Log($"[THÀNH CÔNG] Đã lọt vào case OnProductSelected! Barcode = {barcode}");

                    // Phát tín hiệu cho ProductDisplay.cs
                    OnProductReceived?.Invoke(barcode);
                }
                else
                {
                    Debug.LogError("[LỖI] Server gọi lệnh chọn sản phẩm nhưng KHÔNG gửi kèm Barcode!");
                }
                break;
            case "OnProductRotated":
                OnProductRotatedEvent?.Invoke(Convert.ToSingle(args[0]));
                break;
            case "ReceiveMessage":
            case "OnGestureDetect":
                OnMessageReceivedEvent?.Invoke(args[0].ToString());
                break;
        }
    }

    public void ReconnectWithNewUrl(string newUrl)
    {
        serverUrl = newUrl.Replace("http://", "ws://");
        PlayerPrefs.SetString("WebSocket_URL", serverUrl);

        isReconnecting = false;
        ConnectToServer();
    }

    private async void OnApplicationQuit()
    {
        isQuitting = true;
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
    }
}

// KHUÔN MẪU DỮ LIỆU JSON ĐÃ ĐƯỢC ĐỔI TÊN
public class ServerMessage
{
    public string target { get; set; }
    public object[] arguments { get; set; }
}