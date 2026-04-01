using UnityEngine;

public class ToyAnimator : MonoBehaviour
{
    [SerializeField] private Animator toyAnimator;

    void Start()
    {
        toyAnimator = GetComponent<Animator>();

        // 1. ĐĂNG KÝ LẮNG NGHE SIGNALR
        // Khi SignalR nhận được tin nhắn, nó sẽ tự động gọi hàm HandleRaspberrySignal
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnMessageReceivedEvent += HandleRaspberrySignal;
        }
    }

    void OnDestroy()
    {
        // 2. HỦY ĐĂNG KÝ KHI OBJECT BỊ XÓA (Chống lỗi tràn RAM)
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnMessageReceivedEvent -= HandleRaspberrySignal;
        }
    }

    // =========================================================
    // HÀM XỬ LÝ TÍN HIỆU TỪ RASPBERRY PI
    // =========================================================
    private void HandleRaspberrySignal(string signalValue)
    {
        Debug.Log($"Raspberry gửi lệnh số: {signalValue}");

        // Dùng switch-case để kiểm tra số và bật Animation tương ứng
        switch (signalValue)
        {
            case "1":
                SetAni1();
                break;
            case "2":
                SetAni2();
                break;
            case "3":
                SetAni3();
                break;
            case "4":
                SetAni4();
                break;
            default:
                // Bỏ qua nếu tin nhắn không phải là số 1, 2, 3, 4 (ví dụ: tin nhắn HelloGuest)
                break;
        }
    }

    public void OnFootstep()
    {
        Debug.Log("Cộp cộp (Bước chân)");
    }

    // =========================================================
    // LỜI KHUYÊN: TỐI ƯU HIỆU SUẤT (RẤT QUAN TRỌNG)
    // =========================================================
    void Update()
    {
        // TÔI ĐÃ TẮT HÀM CheckAnimatior() TRONG UPDATE ĐI!
        // Lý do: Lệnh FindAnyObjectByType rất nặng. Nếu không tìm thấy Animator, 
        // nó sẽ quét toàn bộ game 60 lần/giây, làm game của bạn cực kỳ giật lag (tụt FPS).
        // CheckAnimatior(); 
    }

    private void CheckAnimatior()
    {
        if (toyAnimator == null)
        {
            toyAnimator = FindAnyObjectByType<Animator>();
        }
    }

    // --- CÁC HÀM ANIMATION GIỮ NGUYÊN CỦA BẠN ---
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