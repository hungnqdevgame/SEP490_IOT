using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    public static MainThreadDispatcher Instance { get; private set; }

    // 1. Tự động sinh ra ngay khi bật game (Tránh lỗi tạo GameObject ở luồng ngầm)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            var obj = new GameObject("MainThreadDispatcher");
            Instance = obj.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(obj);
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 2. Tự động cởi trói để không bị lỗi DontDestroyOnLoad
            transform.parent = null;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (Instance != this)
        {
            // Xóa bản sao dư thừa nếu lỡ tay kéo 2 cái vào scene
            Destroy(this.gameObject);
        }
    }

    public void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}