using TMPro;
using UnityEngine;

public class ProductDisplay : MonoBehaviour
{
    public TextMeshPro productNameText;
    void Start()
    {
        if (SignalRManager.Instance != null)
        {
            Debug.Log("✅ Đã tìm thấy SignalRManager! Đang đăng ký sự kiện...");
            SignalRManager.Instance.OnProductReceived += ShowProduct;
        }
        else
        {
            // NẾU BẠN THẤY DÒNG NÀY TRONG CONSOLE -> ĐÂY LÀ LỖI
            Debug.LogError("❌ LỖI TO: Không tìm thấy SignalRManager.Instance! Script này chạy sớm hơn Manager hoặc Manager chưa được tạo.");
        }
    }

    void OnDestroy()
    {
        // Nhớ hủy đăng ký khi chuyển màn chơi để tránh lỗi
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnProductReceived -= ShowProduct;
        }
    }
    // Update is called once per frame
    void ShowProduct(string productId)
    {
        Debug.LogError($"[TEST] Đang cố tải ID: '{productId}' (Độ dài: {productId.Length})");

        // Xử lý khoảng trắng thừa (Rất quan trọng!)
        productId = productId.Trim();
        // Xóa model cũ nếu có
        foreach (Transform child in transform) Destroy(child.gameObject);

        // Load model mới từ thư mục Resources
        GameObject prefab = Resources.Load<GameObject>(productId); // Tên file phải trùng productId
        if (prefab == null)
        {
            Debug.LogError($"[LỖI] Không tìm thấy file nào tên là '{productId}' trong thư mục Resources!");
            return; // Dừng lại luôn
        }
        if (prefab != null)
        {
            GameObject newObj = Instantiate(prefab, transform);
            newObj.transform.localPosition = new Vector3(0, 0, -7);
            newObj.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError("Không tìm thấy model có tên: " + productId);
        }

    }
}
