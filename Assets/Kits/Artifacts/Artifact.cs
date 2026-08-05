using UnityEngine;

public class Artifact : MonoBehaviour
{
    public float MultiplySpeed = 1f;

    public void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponent<EnemyController>();
        if (enemy)
        {
            enemy.TryApplyArtifact(this);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        var enemy = other.GetComponent<EnemyController>();
        if (enemy)
        {
            enemy.TryRemoveArtifact(this);
        }
    }
}
