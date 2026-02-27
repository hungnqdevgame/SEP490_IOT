using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    public static MainThreadDispatcher Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
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
        if (action == null)
        {
            return;
        }

        if (Instance == null)
        {
            var obj = new GameObject("MainThreadDispatcher");
            Instance = obj.AddComponent<MainThreadDispatcher>();
        }

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}
