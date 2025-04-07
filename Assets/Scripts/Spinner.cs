using UnityEngine;

public class Spinner : MonoBehaviour
{
    [SerializeField]
    bool local;

    [SerializeField]
    bool zAxis;

    [SerializeField]
    float speed = 5;

    float a;

    private void Update()
    {
        a += speed * Time.deltaTime;
        a %= 360;

        var euler = new Vector3(!zAxis ? a : 0f, 0f, zAxis ? a : 0);
        if (local)
        {
            transform.localEulerAngles = euler;
        } else
        {
            transform.eulerAngles = euler;
        }
    }
}
