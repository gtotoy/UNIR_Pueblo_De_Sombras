using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Cooldowns")]
    public float attackCooldown = 0.5f;
    public float specialCooldown = 4f;
    public float rangedCooldown = 2f;

    [Header("Ranged / Shield")]
    public GameObject shieldProjectilePrefab;
    public Transform shieldThrowOrigin;
    public GameObject shieldMesh;
    public float rangedDamage = 25f;

    private Animator anim;
    private bool isBlocking;
    private bool shieldInFlight;

    public bool IsBlocking => isBlocking;

    private float atkTimer, spTimer, rngTimer;

    void Awake() => anim = GetComponent<Animator>();

    void Update()
    {
        if (atkTimer > 0) atkTimer -= Time.deltaTime;
        if (spTimer > 0) spTimer -= Time.deltaTime;
        if (rngTimer > 0) rngTimer -= Time.deltaTime;
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
        if (shieldInFlight) return;

        if (isBlocking)
        {
            isBlocking = false;
            anim.SetBool("isBlocking", false);
        }

        rngTimer = rangedCooldown;
        shieldInFlight = true;

        if (shieldMesh) shieldMesh.SetActive(false);

        anim.SetTrigger("rangedAttack");

        Invoke(nameof(ThrowShield), 0.15f);
    }

    void ThrowShield()
    {
        Transform origin = shieldThrowOrigin ? shieldThrowOrigin : transform;
        GameObject proj = Instantiate(shieldProjectilePrefab, origin.position, new Quaternion(180.0f, 0.0f, 0.0f, 0.0f));
        proj.tag = "ShieldProjectile";

        var sp = proj.GetComponent<ShieldProjectile>();
        sp.Init(transform, this);
    }

    public void OnShieldCaught()
    {
        shieldInFlight = false;
        if (shieldMesh) shieldMesh.SetActive(true);
        anim.SetTrigger("pickupShield");
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (shieldInFlight) return;

            isBlocking = true;
            anim.SetBool("isBlocking", true);
        }
        if (context.canceled)
        {
            isBlocking = false;
            anim.SetBool("isBlocking", false);
        }
    }

    public void OnParry(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (shieldInFlight || shieldInFlight) return;

        anim.SetTrigger("parry");
        TriggerParry();
    }

    public bool TryBlock(GameObject attacker)
    {
        if (!isBlocking) return false;
        TriggerBlock();
        return true;
    }

    void TriggerParry() { Debug.Log("PARRY!"); }
    void TriggerBlock() { Debug.Log("Blocked!"); }
    void DealMeleeDamage() { Debug.Log("Melee hit!"); }
    void TriggerSpecial() { Debug.Log("Special cast!"); }
    void SpawnProjectile() { Debug.Log("Projectile fired!"); }
}