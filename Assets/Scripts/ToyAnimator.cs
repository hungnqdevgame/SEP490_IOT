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
         //   Debug.LogError("Animator chưa được gán! Vui lòng kiểm tra lại.");
        }
        else
        {
       //     Debug.Log("Animator đã được gán thành công.");
        }
    }
    public void SetAni1()
    {
        ResetAllTriggers();
        toyAnimator.SetTrigger("ani1");
        
    }

    public void SetAni2()
    {
        ResetAllTriggers();
        toyAnimator.SetTrigger("ani2");
    }

    public void SetAni3()
    {
        ResetAllTriggers();
        toyAnimator.SetTrigger("ani3");
    }

    public void SetAni4()
    {
        ResetAllTriggers();
        toyAnimator.SetTrigger("ani4");
    }

    private void ResetAllTriggers()
    {
        if (toyAnimator == null) return;

        toyAnimator.ResetTrigger("ani1");
        toyAnimator.ResetTrigger("ani2");
        toyAnimator.ResetTrigger("ani3");
        toyAnimator.ResetTrigger("ani4");
    }


}
