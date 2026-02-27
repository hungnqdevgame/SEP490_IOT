using UnityEngine;
using UnityEngine.UI;

public class PanelController : MonoBehaviour
{
    [Header("1. Cấu hình Panel Trắng")]
    public RectTransform infoPanel;
    public float panelUpY = 0f;      // Vị trí Y khi hiện hết (ví dụ: 0)
    public float panelDownY = -500f; // Vị trí Y khi thụt xuống (chỉ hở Tên/Giá)

    [Header("2. Cấu hình Nút Bấm (1,2,3,4)")]
    public RectTransform buttonsGroup;
    public float buttonUpY = 100f;    // Vị trí nút khi Panel cao
    public float buttonDownY = -300f; // Vị trí nút khi Panel thấp (hạ theo)
    public Transform cameraStateOut;
    public Transform cameraStateIn;

    [Header("3. Cấu hình Camera (Zoom)")]
    public Camera modelCamera;
    public float camFieldOfViewOut = 60f; // Góc nhìn rộng (Robot nhỏ - khi hiện tin)
    public float camFieldOfViewIn = 40f;  // Góc nhìn hẹp (Robot to - khi ẩn tin)

    [Header("4. Cài đặt chung")]
    public float smoothSpeed = 5f;    // Tốc độ chuyển động
    public Button toggleButton;       // Nút bấm để kích hoạt (Ví dụ nút "Mua" hoặc một nút tàng hình)

    private bool isExpanded = true;   // Trạng thái hiện tại (Đang mở hay đóng?)

    void Start()
    {
        // Tự động gán sự kiện cho nút bấm nếu có
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleState);
        if (cameraStateOut != null && modelCamera != null)
        {
            modelCamera.transform.position = cameraStateOut.position;
            modelCamera.transform.rotation = cameraStateOut.rotation;
            modelCamera.fieldOfView = 60f; // FOV mặc định
        }

    }

    void Update()
    {
        // --- TÍNH TOÁN GIÁ TRỊ MỤC TIÊU ---
        float targetPanelY = isExpanded ? panelUpY : panelDownY;
        float targetButtonY = isExpanded ? buttonUpY : buttonDownY;
       // float targetFOV = isExpanded ? camFieldOfViewOut : camFieldOfViewIn;

        // --- THỰC HIỆN DI CHUYỂN MƯỢT MÀ (LERP) ---

        // 1. Di chuyển Panel
        if (infoPanel != null)
        {
            Vector2 pos = infoPanel.anchoredPosition;
            pos.y = Mathf.Lerp(pos.y, targetPanelY, Time.deltaTime * smoothSpeed);
            infoPanel.anchoredPosition = pos;
        }

        // 2. Di chuyển Nút bấm
        if (buttonsGroup != null)
        {
            Vector2 pos = buttonsGroup.anchoredPosition;
            pos.y = Mathf.Lerp(pos.y, targetButtonY, Time.deltaTime * smoothSpeed);
            buttonsGroup.anchoredPosition = pos;
        }

        // 3. Zoom Camera (Làm to Robot)
        if (modelCamera != null && cameraStateOut != null && cameraStateIn != null)
        {
            // Chọn điểm mốc mục tiêu
            Transform targetTransform = isExpanded ? cameraStateOut : cameraStateIn;

            // Di chuyển vị trí (Position)
            modelCamera.transform.position = Vector3.Lerp(modelCamera.transform.position, targetTransform.position, Time.deltaTime * smoothSpeed);

            // Xoay góc (Rotation) - Nếu bạn muốn camera cúi xuống/ngẩng lên
            modelCamera.transform.rotation = Quaternion.Lerp(modelCamera.transform.rotation, targetTransform.rotation, Time.deltaTime * smoothSpeed);

            // Thay đổi độ Zoom (FOV) - Giả sử State Out là 60, State In là 40
            // Bạn có thể lưu FOV vào biến riêng nếu muốn, ở đây mình ví dụ set cứng hoặc lấy từ scale
            float targetFOV = isExpanded ? 60f : 45f;
            modelCamera.fieldOfView = Mathf.Lerp(modelCamera.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
        }
    }

    // Hàm gọi khi ấn nút để đảo trạng thái
    public void ToggleState()
    {
        isExpanded = !isExpanded;
    }
}
