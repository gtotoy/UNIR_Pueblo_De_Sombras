using UnityEngine;

public class Artifact : MonoBehaviour
{
    public float LifetimeInSeconds = Mathf.Infinity;
    public float MultiplySpeed = 1f;

    public void StartLifetime(float lifetimeMultiplier)
    {
        if (LifetimeInSeconds == Mathf.Infinity) return;
        var lifetime = lifetimeMultiplier * LifetimeInSeconds;
        Destroy(gameObject, lifetime);
    }

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
