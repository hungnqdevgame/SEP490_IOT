using UnityEngine;

public class ToyAnimator : MonoBehaviour
{
    [SerializeField] private Animator toyAnimator;
    public void OnFootstep()
    {
        // Hiện tại để trống.
        // Sau này nếu muốn thêm tiếng bước chân, bạn viết code play âm thanh vào đây.
        Debug.Log("Cộp cộp (Bước chân)");
    }
    void Start()
    {
        toyAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
       CheckAnimatior();
    }

    private void CheckAnimatior()
    {
        if (toyAnimator == null)
        {
            toyAnimator = FindAnyObjectByType<Animator>();
            Debug.LogError("Animator chưa được gán! Vui lòng kiểm tra lại.");
        }
        else
        {
            Debug.Log("Animator đã được gán thành công.");
        }
    }
    public void ClickToRun()
    {
        toyAnimator.SetTrigger("run_click");
    }

    public void ClickToIdle()
    {
        toyAnimator.SetTrigger("idle_click");
    }

    public void ClickToWalk()
    {
        toyAnimator.SetTrigger("walk_click");
    }


}
