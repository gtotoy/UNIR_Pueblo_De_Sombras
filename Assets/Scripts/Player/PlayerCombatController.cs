using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatController : MonoBehaviour
{
    public float attackCooldown = 0.5f;
    public float specialCooldown = 4f;
    public float rangedCooldown = 1f;
    public float parryWindow = 0.25f;

    private Animator anim;
    private bool isBlocking, parryActive;
    private float atkTimer, spTimer, rngTimer, parryTimer;

    void Awake() => anim = GetComponent<Animator>();

    void Update()
    {
        if (atkTimer > 0) atkTimer -= Time.deltaTime;
        if (spTimer > 0) spTimer -= Time.deltaTime;
        if (rngTimer > 0) rngTimer -= Time.deltaTime;
        if (parryTimer > 0)
        {
            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0) parryActive = false;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed || atkTimer > 0) return;
        atkTimer = attackCooldown;
        anim.SetTrigger("normalAttack");
        DealMeleeDamage();
    }

    public void OnSpecialAttack(InputAction.CallbackContext context)
    {
        if (!context.performed || spTimer > 0) return;
        spTimer = specialCooldown;
        anim.SetTrigger("specialAttack");
        TriggerSpecial();
    }

    public void OnRanged(InputAction.CallbackContext context)
    {
        if (!context.performed || rngTimer > 0) return;
        rngTimer = rangedCooldown;
        anim.SetTrigger("rangedAttack");
        SpawnProjectile();
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isBlocking = true;
            parryActive = true;
            parryTimer = parryWindow;
            anim.SetBool("isBlocking", true);
        }
        if (context.canceled)
        {
            isBlocking = false;
            parryActive = false;
            anim.SetBool("isBlocking", false);
        }
    }

    public bool TryBlock(GameObject attacker)
    {
        if (!isBlocking) return false;
        if (parryActive) { TriggerParry(attacker); return true; }
        TriggerBlock();
        return true;
    }

    void TriggerParry(GameObject attacker)
    {
        anim.SetTrigger("parry");
        parryActive = false;
        Debug.Log("PERFECT PARRY!");
    }

    void TriggerBlock() { Debug.Log("Blocked!"); }
    void DealMeleeDamage() { Debug.Log("Melee hit!"); }
    void TriggerSpecial() { Debug.Log("Special cast!"); }
    void SpawnProjectile() { Debug.Log("Projectile fired!"); }
}