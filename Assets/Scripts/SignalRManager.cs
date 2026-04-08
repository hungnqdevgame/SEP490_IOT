using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class SignalRManager : MonoBehaviour
{
    public static SignalRManager Instance;

    public string serverUrl;
    private HubConnection connection;

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
            serverUrl = PlayerPrefs.GetString("SignalR_URL", "http://localhost:5035/productHub");
            SetupSignalR();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SetupSignalR()
    {
        connection = new HubConnectionBuilder()
             .WithUrl(serverUrl, options =>
             {
                 options.SkipNegotiation = true;
                 options.Transports = HttpTransportType.WebSockets;
             })
             .WithAutomaticReconnect()
             .Build();

        connection.On<string>("OnProductSelected", (barCode) =>
        {
            Debug.Log(barCode);
            MainThreadDispatcher.Enqueue(() =>
            {
                OnProductReceived?.Invoke(barCode);
            });
        });

        connection.On<float>("OnProductRotated", (angle) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                OnProductRotatedEvent?.Invoke(angle);
            });
        });

        // [THÊM MỚI] Nhận tín hiệu HelloGuest từ Server
        connection.On<string>("ReceiveMessage", (text) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                OnMessageReceivedEvent?.Invoke(text);
            });
        });

        connection.On<string>("OnGestureDetect", (gestureNumber) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                OnMessageReceivedEvent?.Invoke(gestureNumber);
            });
        });
        connection.Closed += async (error) =>
        {
            MainThreadDispatcher.Enqueue(() => OnConnectionStatusChanged?.Invoke(false));
            await Task.CompletedTask;
        };
        connection.Reconnecting += async (error) =>
        {
            MainThreadDispatcher.Enqueue(() => OnConnectionStatusChanged?.Invoke(false));
            await Task.CompletedTask;
        };
        connection.Reconnected += async (connectionId) =>
        {
            MainThreadDispatcher.Enqueue(() => OnConnectionStatusChanged?.Invoke(true));
            await Task.CompletedTask;
        };
        Connect();
    }

    async void Connect()
    {
        try
        {
            await connection.StartAsync();
            Debug.Log("Kết nối Server thành công!");
            // BÁO CÁO: ĐÃ KẾT NỐI
            MainThreadDispatcher.Enqueue(() => OnConnectionStatusChanged?.Invoke(true));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Kết nối thất bại: {ex.Message}");
            // BÁO CÁO: LỖI MẠNG
            MainThreadDispatcher.Enqueue(() => OnConnectionStatusChanged?.Invoke(false));
        }
    }

    public async Task SendSelectProduct(string barCode)
    {
        if (connection.State == HubConnectionState.Connected)
        {
            await connection.InvokeAsync("SelectProduct", barCode);
        }
    }

    private async void OnApplicationQuit() { await CloseConnection(); }
    private async void OnDestroy() { await CloseConnection(); }

    private async Task CloseConnection()
    {
        if (connection != null && connection.State != HubConnectionState.Disconnected)
        {
            try
            {
                await connection.StopAsync();
                await connection.DisposeAsync();
                connection = null;
                Debug.Log("Đã đóng kết nối SignalR an toàn.");
            }
            catch (Exception ex) { Debug.LogWarning($"Lỗi khi đóng kết nối: {ex.Message}"); }
        }
    }

    public async void ReconnectWithNewUrl(string newUrl)
    {
        serverUrl = newUrl;
        PlayerPrefs.SetString("SignalR_URL", newUrl); // Lưu vào máy
        PlayerPrefs.Save();

        Debug.Log("[SignalR] Đang thử kết nối lại với: " + newUrl);
        await CloseConnection(); // Ngắt kết nối cũ
        SetupSignalR();          // Thiết lập lại với URL mới
    }
}