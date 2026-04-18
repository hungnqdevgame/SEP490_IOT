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

            string savedUrl = PlayerPrefs.GetString("WebSocket_URL", "ws://192.168.137.194:8765/ws");
            serverUrl = savedUrl.Replace("http://", "ws://").Replace("https://", "wss://");

            InitWebSocket();
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

    async void InitWebSocket()
    {
        Debug.Log($"[WebSocket] Đang kết nối tới: {serverUrl}");
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () =>
        {
            Debug.Log("[WebSocket] Kết nối thành công!");
            OnConnectionStatusChanged?.Invoke(true);
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError($"[WebSocket] Lỗi: {e}");
            OnConnectionStatusChanged?.Invoke(false);
        };

        websocket.OnClose += (c) =>
        {
            Debug.LogWarning("[WebSocket] Đã đóng kết nối.");
            OnConnectionStatusChanged?.Invoke(false);
        };

        websocket.OnMessage += (bytes) =>
        {
            string rawMessage = Encoding.UTF8.GetString(bytes);
            try
            {
                var data = JsonConvert.DeserializeObject<SignalRMessage>(rawMessage);
                if (data != null && data.arguments != null && data.arguments.Length > 0)
                {
                    ProcessServerMethod(data.target, data.arguments);
                }
            }
            catch { /* Bỏ qua nếu tin nhắn không đúng chuẩn JSON */ }
        };

        await websocket.Connect();
    }

    void ProcessServerMethod(string target, object[] args)
    {
        switch (target)
        {
            case "OnProductSelected":
                OnProductReceived?.Invoke(args[0].ToString());
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

    public async void ReconnectWithNewUrl(string newUrl)
    {
        serverUrl = newUrl.Replace("http://", "ws://");
        PlayerPrefs.SetString("WebSocket_URL", serverUrl);

        if (websocket != null) await websocket.Close();
        InitWebSocket();
    }

    private async void OnApplicationQuit() { if (websocket != null) await websocket.Close(); }
}

public class SignalRMessage
{
    public string target { get; set; }
    public object[] arguments { get; set; }
}