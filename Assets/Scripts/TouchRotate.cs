using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    [Header("Cấu hình Vuốt/Chuột")]
    public float rotationSpeed = 0.2f;
    public bool invertDirection = true;

    [Header("Cấu hình Xoay từ Pi")]
    public float smoothRotateSpeed = 5f; // Tốc độ xoay mượt
    private Quaternion targetRotation;   // Lưu góc mục tiêu

    void Start()
    {
        // Khởi tạo góc mục tiêu bằng góc hiện tại của Model
        targetRotation = transform.rotation;

        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnProductRotatedEvent += OnReceiveRotationFromPi;
            Debug.Log("✅ TouchRotate đã kết nối với SignalR!");
        }
    }

    void OnDestroy()
    {
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnProductRotatedEvent -= OnReceiveRotationFromPi;
        }
    }

    void Update()
    {
        // --- 1. XỬ LÝ VUỐT BẰNG TAY TRÊN MÀN HÌNH ---
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float x = touch.deltaPosition.x * rotationSpeed;
                if (invertDirection) x = -x;

                transform.Rotate(0, x, 0);
                // Phải đồng bộ lại targetRotation để khi thả tay ra, model không bị giật lùi về góc cũ
                targetRotation = transform.rotation;
            }
        }

        // --- 2. XỬ LÝ CHUỘT ---
        if (Input.GetMouseButton(0))
        {
            float x = Input.GetAxis("Mouse X") * rotationSpeed * 10f;
            if (invertDirection) x = -x;

            transform.Rotate(0, x, 0);
            targetRotation = transform.rotation;
        }

        // --- 3. THỰC HIỆN XOAY TỪ TỪ TỪ PI ---
        // Lerp giúp model xoay êm ái về phía góc mục tiêu
        if (transform.rotation != targetRotation)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * smoothRotateSpeed);
        }
    }

    private void OnReceiveRotationFromPi(float angle)
    {
        // Cộng dồn góc mới vào góc mục tiêu
        Quaternion newTarget = targetRotation * Quaternion.Euler(0, angle, 0);

        // NẾU LÀ 90 HOẶC -90 -> SNAP (XOAY NGAY LẬP TỨC)
        if (Mathf.Abs(angle) == 90f)
        {
            targetRotation = newTarget;
            transform.rotation = targetRotation; // Ép vào góc mới luôn
            Debug.Log($"[XOAY] Đã bẻ tức thì góc {angle} độ.");
        }
        else
        {
            // CÁC GÓC KHÁC -> CHỈ CẬP NHẬT MỤC TIÊU ĐỂ UPDATE TỰ XOAY TỪ TỪ
            targetRotation = newTarget;
            Debug.Log($"[XOAY] Đang vặn từ từ góc {angle} độ.");
        }
    }
}