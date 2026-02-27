using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ModelSlideshow : MonoBehaviour
{
    public List<ModelPlaylistItem> playlist;
    private GameObject currentModel;
    private Coroutine currentSequence;

    void Start()
    {
        // Ví dụ: Bắt đầu phát playlist khi nhấn nút hoặc nhận lệnh từ SignalR
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        foreach (var item in playlist)
        {
            // 1. Xóa model cũ để tiết kiệm RAM
            if (currentModel != null)
            {
                Destroy(currentModel);
                // Rất quan trọng: Giải phóng bộ nhớ AssetBundle cũ
                Resources.UnloadUnusedAssets();
            }

            // 2. Tải model từ Server (Dùng lại logic của bạn)
            yield return StartCoroutine(DownloadAndDisplay(item));

            // 3. Đợi đúng khoảng thời gian quy định
            yield return new WaitForSeconds(item.displayDuration);
        }

        Debug.Log("Đã phát xong toàn bộ playlist!");
    }

    IEnumerator DownloadAndDisplay(ModelPlaylistItem item)
    {
        string finalUrl = item.modelUrl + "?t=" + System.DateTime.Now.Ticks;
        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(finalUrl))
        {
            uwr.certificateHandler = new BypassCertificate(); // Luôn giữ để tránh lỗi SSL trên Render
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                GameObject prefab = bundle.LoadAsset<GameObject>(item.assetName);
                if (prefab != null)
                {
                    currentModel = Instantiate(prefab);
                    Debug.Log("Đang hiển thị: " + item.assetName);
                }
                bundle.Unload(false); // Giải nén xong thì đóng gói nhưng giữ lại Object
            }
        }
    }

    public void StartNewPlaylist(List<ModelPlaylistItem> newPlaylist)
    {
        // 1. Dừng chuỗi đang phát cũ (nếu có)
        if (currentSequence != null) StopCoroutine(currentSequence);

        // 2. Cập nhật danh sách mới
        playlist = new List<ModelPlaylistItem>(newPlaylist);

        // 3. Bắt đầu phát từ đầu
        currentSequence = StartCoroutine(PlaySequence());
    }
}
