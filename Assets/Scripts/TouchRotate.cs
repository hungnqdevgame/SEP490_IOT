using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    [Header("Cấu hình Vuốt/Chuột")]
    public float rotationSpeed = 0.2f;
    public bool invertDirection = true;

    void Start()
    {
        // 1. ĐĂNG KÝ LẮNG NGHE SỰ KIỆN TỪ RASPBERRY PI (SIGNALR)
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnProductRotatedEvent += OnReceiveRotationFromPi;
            Debug.Log("✅ TouchRotate đã kết nối với SignalR để nhận góc xoay từ Pi!");
        }
    }

    void OnDestroy()
    {
        // Hủy đăng ký khi object (Model) bị xóa để tránh lỗi rò rỉ bộ nhớ
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnProductRotatedEvent -= OnReceiveRotationFromPi;
        }
    }

    void Update()
    {
  
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float x = touch.deltaPosition.x * rotationSpeed;
                if (invertDirection) x = -x;
                transform.Rotate(0, x, 0);
            }
        }

        // --- XỬ LÝ CHUỘT (Giữ nguyên) ---
        if (Input.GetMouseButton(0))
        {
            float x = Input.GetAxis("Mouse X") * rotationSpeed * 10f;
            if (invertDirection) x = -x;
            transform.Rotate(0, x, 0);
        }
    }

 
    private void OnReceiveRotationFromPi(float angle)
    {
        /* * Tùy thuộc vào việc Raspberry Pi của bạn gửi dữ liệu kiểu gì, 
         * bạn hãy BẬT một trong 2 cách dưới đây và XÓA cách còn lại nhé:
         */
        float finalAngle = (transform.rotation.y + angle) * rotationSpeed;
   
 
        transform.Rotate(0, finalAngle, 0);


        Debug.Log($"[TÍN HIỆU PI] Đã nhận góc {angle} độ. Đang xoay model!");
    }

    private void OnRotationFromPi(float angle)
    {

        transform.Rotate(0, angle, 0);
    }
}