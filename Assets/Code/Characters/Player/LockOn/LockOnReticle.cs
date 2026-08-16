using UnityEngine;

public class LockOnReticle : MonoBehaviour
{
    [SerializeField] float heightOffset = 0.05f;
    [SerializeField] float spinSpeed = 60f;

    Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        gameObject.SetActive(target != null);
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + Vector3.up * heightOffset;
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }
}
