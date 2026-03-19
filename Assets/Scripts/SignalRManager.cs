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

    // [THÊM MỚI] Sự kiện dành riêng cho chữ Hello
    public event Action<string> OnMessageReceivedEvent;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

        Connect();
    }

    async void Connect()
    {
        try
        {
            await connection.StartAsync();
            Debug.Log("Kết nối Server thành công!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Kết nối thất bại: {ex.Message}");
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
}