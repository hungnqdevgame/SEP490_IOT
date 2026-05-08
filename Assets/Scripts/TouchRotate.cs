using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    [Header("Cấu hình Vuốt / Chuột (1 ngón - Ngang)")]
    public float rotationSpeed = 0.2f;
    public bool invertRotation = true; // [Lưu ý] Đổi tên biến direction cũ cho rõ nghĩa

    [Header("Cấu hình Di chuyển (1 ngón - Dọc) & Giới hạn")]
    public float moveSpeed = 0.005f;  // Độ nhạy di chuyển lên/xuống
    public float minYOffset = -2f;    // Phạm vi di chuyển xuống tối đa (số âm) so với ban đầu
    public float maxYOffset = 2f;     // Phạm vi di chuyển lên tối đa (số dương) so với ban đầu
    private float initialY;          // Vị trí Y ban đầu để làm chuẩn giới hạn

    [Header("Cấu hình Phóng to / Thu nhỏ (2 ngón)")]
    public float zoomSpeed = 0.005f;
    public float minScale = 0.5f;
    public float maxScale = 3f;

    [Header("Cấu hình Xoay từ Pi (WebSocket)")]
    public float smoothRotateSpeed = 5f; // Tốc độ xoay mượt
    private Quaternion targetRotation;   // Lưu góc mục tiêu

    void Start()
    {
        // 1. Lưu lại vị trí Y ban đầu để làm chuẩn cho 'khung' di chuyển dọc
        initialY = transform.position.y;

        // 2. Khởi tạo góc mục tiêu bằng góc hiện tại của Model
        targetRotation = transform.rotation;

        // 3. Kết nối WebSocketManager
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnProductRotatedEvent += OnReceiveRotationFromPi;
            Debug.Log("✅ TouchRotate đã kết nối với WebSocket!");
        }
    }

    void OnDestroy()
    {
        // Hủy đăng ký sự kiện để tránh lỗi RAM
        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnProductRotatedEvent -= OnReceiveRotationFromPi;
        }
    }

    void Update()
    {
        // --- 1. XỬ LÝ CHẠM BẰNG TAY (MOBILE TOUCH) ---
        if (Input.touchCount == 1) // CHẠM 1 NGÓN -> XOAY & DI CHUYỂN LÊN/XUỐNG
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                // === 1a. XỬ LÝ XOAY (Ngang - touch.deltaPosition.x) ===
                float x = touch.deltaPosition.x * rotationSpeed;
                if (invertRotation) x = -x;

                // Sử dụng Space.World cho trục Up để xoay nhất quán trên màn hình
                transform.Rotate(Vector3.up, x, Space.World);
                targetRotation = transform.rotation; // Cập nhật cho Pi

                // === 1b. XỬ LÝ DI CHUYỂN & GIỚI HẠN (Dọc - touch.deltaPosition.y) ===
                // Tính toán lượng di chuyển dựa trên delta y
                float yMovement = touch.deltaPosition.y * moveSpeed;

                // Tính vị trí Y mục tiêu mới
                float targetY = transform.position.y + yMovement;

                // Khóa (Clamp) vị trí Y mục tiêu trong khung [initialY + minY, initialY + maxY]
                // Dùng relative offset từ initialY giúp file hoạt động bất kể vị trí gốc của object
                float clampedY = Mathf.Clamp(targetY, initialY + minYOffset, initialY + maxYOffset);

                // Cập nhật vị trí mới (chỉ thay đổi Y, giữ nguyên X và Z)
                transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
            }
        }
        else if (Input.touchCount == 2) // CHẠM 2 NGÓN -> PHÓNG TO / THU NHỎ
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Vị trí frame trước
            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            // Khoảng cách giữa 2 ngón (Hiện tại vs Trước đó)
            float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
            float touchDeltaMag = (touch0.position - touch1.position).magnitude;

            // Chênh lệch khoảng cách (>0 là vuốt ra, <0 là vuốt vào)
            float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

            // Tính tỷ lệ mới
            Vector3 newScale = transform.localScale + Vector3.one * (deltaMagnitudeDiff * zoomSpeed);

            // Khóa (Clamp) kích thước
            newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
            newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
            newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);

            // Cập nhật kích thước
            transform.localScale = newScale;
        }

        // --- 2. XỬ LÝ CHUỘT (Test trên PC) ---
        // Thêm điều kiện touchCount == 0 để chuột không tranh giành với cảm ứng
        if (Input.GetMouseButton(0) && Input.touchCount == 0)
        {
            // [Ngang] Xoay
            float x = Input.GetAxis("Mouse X") * rotationSpeed * 10f;
            if (invertRotation) x = -x;
            transform.Rotate(Vector3.up, x, Space.World);
            targetRotation = transform.rotation;

            // [Dọc] Di chuyển chuột dọc
            float y = Input.GetAxis("Mouse Y") * moveSpeed * 50f; // Mouse Y delta nhạy thấp hơn cảm ứng
            float targetY = transform.position.y + y;
            float clampedY = Mathf.Clamp(targetY, initialY + minYOffset, initialY + maxYOffset);
            transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
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
            Debug.Log($"[XOAY SNAP] Pi gửi: {angle}");
        }
        else
        {
            targetRotation = newTarget;
            Debug.Log($"[XOAY MƯỢT] Pi gửi: {angle}");
        }
    }
}