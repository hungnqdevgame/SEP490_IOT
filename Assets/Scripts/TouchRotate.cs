using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    [Header("Cấu hình")]
    public float rotationSpeed = 0.2f;
    public bool invertDirection = true;

    [Header("Trạng thái")]
    public bool isLeft = false;
    private bool _lastIsLeft; // Biến dùng để kiểm tra sự thay đổi

    void Start()
    {
        // Khởi tạo trạng thái ban đầu để tránh lỗi khi vừa vào game
        _lastIsLeft = isLeft;
    }

    void Update()
    {
        // --- 1. XỬ LÝ CẢM ỨNG (Giữ nguyên) ---
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

        // --- 2. XỬ LÝ CHUỘT (Giữ nguyên) ---
        if (Input.GetMouseButton(0))
        {
            float x = Input.GetAxis("Mouse X") * rotationSpeed * 10f;
            if (invertDirection) x = -x;
            transform.Rotate(0, x, 0);
        }

        // --- 3. KIỂM TRA SỰ THAY ĐỔI CỦA BIẾN isLeft ---
        // Nếu giá trị hiện tại (isLeft) KHÁC với giá trị cũ (_lastIsLeft)
        if (isLeft != _lastIsLeft)
        {
            CheckRotate(isLeft); // Thực hiện xoay 1 lần duy nhất
            _lastIsLeft = isLeft; // Cập nhật lại giá trị cũ để chờ lần thay đổi sau
        }
    }

    void CheckRotate(bool isLeftStatus)
    {
        // Lưu ý: rotationSpeed của bạn đang là 0.2
        // 90 * 0.2 = 18 độ.
        // Nếu bạn muốn xoay hẳn 90 độ thì nên bỏ nhân với rotationSpeed ở đây

        float rotateAmount = 90f; // Xoay 90 độ

        if (isLeftStatus)
        {
            // Xoay sang trái (hoặc hướng dương tùy trục)
            transform.Rotate(0, rotateAmount, 0);
        }
        else
        {
            // Xoay sang phải (hoặc hướng âm)
            transform.Rotate(0, -rotateAmount, 0);
        }
    }
}
