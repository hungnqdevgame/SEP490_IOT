using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class SignalRManager : MonoBehaviour
{
    public static SignalRManager Instance;


    // THAY IP NÀY BẰNG IP LAPTOP CỦA BẠN
    public string serverUrl ;

    private HubConnection connection;

    // Sự kiện để báo cho App Hiển Thị biết
    public event Action<string> OnProductReceived;
    public event Action<float> OnProductRotatedEvent;
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
                 // THÊM ĐOẠN NÀY ĐỂ SỬA LỖI "SENDING REQUEST ERROR"
                 options.SkipNegotiation = true;
                 options.Transports = HttpTransportType.WebSockets;
             })
             .WithAutomaticReconnect()
             .Build();

        // --- PHẦN DÀNH CHO MÁY B (Màn hình) ---
        connection.On<string>("OnProductSelected", (skuCode) =>
        {
            // 1. DÒNG NÀY CHẠY NGAY KHI SERVER GỬI TIN (Ở luồng phụ)
            Debug.LogError($"[BƯỚC 1] SignalR đã nhận tín hiệu từ Server! Raw ID: {skuCode}");

            MainThreadDispatcher.Enqueue(() =>
            {
                // 2. DÒNG NÀY CHẠY KHI CHUYỂN VỀ LUỒNG CHÍNH UNITY
                Debug.LogError($"[BƯỚC 2] MainThreadDispatcher đang xử lý: {skuCode}");

                if (OnProductReceived == null)
                    Debug.LogError("[LỖI] Không có ai đăng ký lắng nghe sự kiện OnProductReceived cả!");
                else
                    OnProductReceived.Invoke(skuCode);
            });
        });

        connection.On<float>("OnProductRotated", (angle) =>
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                // Rút gọn cách gọi hàm (Dấu ? thay cho if != null)
                OnProductRotatedEvent?.Invoke(angle);
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

    // --- PHẦN DÀNH CHO MÁY A (Điều khiển) ---
    // Hàm này được gọi khi bấm nút trên điện thoại A
    public async Task SendSelectProduct(string skuCode)
    {
        if (connection.State == HubConnectionState.Connected)
        {
            await connection.InvokeAsync("SelectProduct", skuCode);
            Debug.Log("Máy A đã gửi lệnh: " + skuCode);
        }
    }

    // Hàm này Unity tự gọi khi bạn tắt Game hoặc tắt ứng dụng
    private async void OnApplicationQuit()
    {
        await CloseConnection();
    }

    // Hàm này Unity gọi khi Object bị hủy
    private async void OnDestroy()
    {
        await CloseConnection();
    }

    private async Task CloseConnection()
    {
        if (connection != null)
        {
            try
            {
                // Báo cho Server biết mình thoát
                await connection.StopAsync();
                // Hủy object để giải phóng RAM và Port
                await connection.DisposeAsync();
                connection = null;
                Debug.Log("Đã đóng kết nối SignalR an toàn.");
            }
            catch (Exception ex)
            {
                // Đôi khi tắt nhanh quá nó báo lỗi, nhưng không sao
                Debug.LogWarning($"Lỗi khi đóng kết nối: {ex.Message}");
            }
        }
    }
}
