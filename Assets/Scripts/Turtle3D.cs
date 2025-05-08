using System.Collections;
using UnityEngine;

public class Turtle3D : MonoBehaviour
{
    private Transform tr;
    [SerializeField] private float moveSpeed;
    private Animation anim;

    public string TurtleName { get; private set; }

    void Awake()
    {
        tr   = transform;
        anim = GetComponent<Animation>();
    }

    public void Initialize(string name, Vector3 pos, Quaternion rot)
    {
        TurtleName = name;
        gameObject.name = name;
        tr.SetPositionAndRotation(pos, rot);
        anim.Play("Idle");
    }

    public Vector3 Position => tr.position;

    /// <summary>
    /// 부드러운 전진을 담당하는 코루틴.
    /// 외부에서 StartCoroutine(TurtleForward(...)) 으로 호출하거나,
    /// yield return TurtleForward(...) 으로 완료를 기다릴 수 있다.
    /// </summary>
    public IEnumerator TurtleForward(float distance)
    {
        float remaining = Mathf.Abs(distance);
        Vector3 dir    = tr.forward * Mathf.Sign(distance);

        while (remaining > 0f)
        {
            float step = moveSpeed * Time.deltaTime;
            tr.Translate(dir * step, Space.World);
            remaining -= step;
            yield return null;              // 다음 프레임까지 대기
        }
    }
}
