using UnityEngine;

public class PlayerAnimationRelay : MonoBehaviour
{
    PlayerCombatController controller;

    void Awake()
    {
        controller = GetComponentInParent<PlayerCombatController>();
        if (controller == null)
            Debug.LogWarning($"[PlayerAnimationRelay] No se encontró PlayerCombatController en {transform.root.name}");
    }

    public void OnMeleeHitFrame()
    {
        controller?.OnMeleeHitFrame();
    }

    public void OnSpecialHitFrame()
    {
        controller?.OnSpecialHitFrame();
    }
}
