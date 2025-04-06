using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField]
    Camera _cam;

    Camera cam => _cam == null ? Camera.main : _cam;

    [SerializeField]
    bool lockY = true;

    [SerializeField]
    bool invertTargetDirection = true;

    void LateUpdate()
    {
        var target = cam.transform.position;
        if (lockY)
        {
            target.y = transform.position.y;
        }

        if (invertTargetDirection)
        {
            var offset = target - transform.position;
            offset *= -1;
            target = transform.position + offset;
        }
        transform.LookAt(target);
    }
}
