using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 6f;
    void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = new Vector3(target.position.x,
                           transform.position.y,
                           target.position.z);
        transform.position = Vector3.Lerp(transform.position, goal,
                           Time.deltaTime * smoothSpeed);
    }
}