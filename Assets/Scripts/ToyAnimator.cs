using UnityEngine;

public class ToyAnimator : MonoBehaviour
{
    [SerializeField] private Animator toyAnimator;

    void Start()
    {
        toyAnimator = GetComponent<Animator>();

        // 1. ĐĂNG KÝ LẮNG NGHE WEBSOCKET
        // Khi WebSocket nhận được tin nhắn, nó sẽ tự động gọi hàm HandleRaspberrySignal
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnMessageReceivedEvent += HandleRaspberrySignal;
        }
    }

    void OnDestroy()
    {
        // 2. HỦY ĐĂNG KÝ KHI OBJECT BỊ XÓA (Chống lỗi tràn RAM)
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnMessageReceivedEvent -= HandleRaspberrySignal;
        }
    }

    // =========================================================
    // HÀM XỬ LÝ TÍN HIỆU TỪ RASPBERRY PI
    // =========================================================
    private void HandleRaspberrySignal(string signalValue)
    {
        Debug.Log($"[ToyAnimator] Raspberry gửi lệnh số: {signalValue}");

        if (toyAnimator == null)
        {
            toyAnimator = FindAnyObjectByType<Animator>();
        }

        // Dùng switch-case để kiểm tra số và bật Animation tương ứng
        switch (signalValue)
        {
            case "2":
                SetAni1();
                break;
            case "3":
                SetAni2();
                break;
            case "4":
                SetAni3();
                break;
            case "5":
                SetAni4();
                break;
            default:
                // Bỏ qua nếu tin nhắn không phải là số 1, 2, 3, 4
                break;
        }
    }

    public void OnFootstep()
    {
        Debug.Log("Cộp cộp (Bước chân)");
    }

    // --- CÁC HÀM ANIMATION GIỮ NGUYÊN ---
    public void SetAni1()
    {
        ResetAllTriggers();
        toyAnimator.SetTrigger("ani1");
    }

    public void SetAni2()
    {
        ResetAllTriggers();
        toyAnimator.SetTrigger("ani2");
    }

    public void SetAni3()
    {
        ResetAllTriggers();
        toyAnimator.SetTrigger("ani3");
    }

    public void SetAni4()
    {
        ResetAllTriggers();
        toyAnimator.SetTrigger("ani4");
    }

    private void ResetAllTriggers()
    {
        if (toyAnimator == null) return;

        toyAnimator.ResetTrigger("ani1");
        toyAnimator.ResetTrigger("ani2");
        toyAnimator.ResetTrigger("ani3");
        toyAnimator.ResetTrigger("ani4");
    }
}