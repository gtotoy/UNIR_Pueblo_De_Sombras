using UnityEngine;

public class EnemyAnimationRelay : MonoBehaviour
{
    EnemyController controller;

    void Awake()
    {
        controller = GetComponentInParent<EnemyController>();
        if (controller == null)
            Debug.LogWarning($"[EnemyAnimationRelay] No se encontró EnemyController en {transform.root.name}");
    }

    public void OnAttackHitFrame()
    {
        controller?.OnAttackHitFrame();
    }
}
