using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Cooldowns")]
    public float attackCooldown = 0.5f;
    public float specialCooldown = 4f;
    public float rangedCooldown = 2f;

    [Header("Melee")]
    public float meleeDamage = 20f;
    public float meleeRadius = 1.5f;
    public float meleeHitDelay = 0.2f;
    public LayerMask enemyLayers;

    [Header("Special")]
    public float specialDamage = 35f;
    public float specialRadius = 4f;

    [Header("Ranged / Shield")]
    public GameObject shieldProjectilePrefab;
    public Transform shieldThrowOrigin;
    public GameObject shieldMesh;
    public float rangedDamage = 25f;

    [Header("Parry")]
    public float parryWindow = 0.4f;

    [Header("Audio")]
    [SerializeField] AudioClip sfxMeleeHit;
    [SerializeField] AudioClip sfxMeleeSwing;
    [SerializeField] AudioClip sfxBlock;
    [SerializeField] AudioClip sfxParry;
    [SerializeField] AudioClip sfxShieldThrow;
    [SerializeField] AudioClip sfxShieldCatch;

    private Animator anim;
    private AudioSource audioSource;
    private bool isBlocking;
    private bool shieldInFlight;
    private float parryTimer;

    public bool IsBlocking => isBlocking;
    public bool IsParrying => parryTimer > 0f;

    private float atkTimer, spTimer, rngTimer;

    void Awake()
    {
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (atkTimer > 0) atkTimer -= Time.deltaTime;
        if (spTimer > 0) spTimer -= Time.deltaTime;
        if (rngTimer > 0) rngTimer -= Time.deltaTime;
        if (parryTimer > 0) parryTimer -= Time.deltaTime;
    }

    void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null) audioSource.PlayOneShot(clip, volume);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed || atkTimer > 0) return;
        atkTimer = attackCooldown;
        anim.SetTrigger("normalAttack");
        Play(sfxMeleeSwing);
        Invoke(nameof(DealMeleeDamage), meleeHitDelay);
    }

    public void OnSpecialAttack(InputAction.CallbackContext context)
    {
        if (!context.performed || spTimer > 0) return;
        spTimer = specialCooldown;
        anim.SetTrigger("specialAttack");
        Play(sfxMeleeSwing);
        Invoke(nameof(DealSpecialDamage), 0.3f);
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
        sp.Init(transform, this, rangedDamage);

        Play(sfxShieldThrow);
    }

    public void OnShieldCaught()
    {
        shieldInFlight = false;
        if (shieldMesh) shieldMesh.SetActive(true);
        anim.SetTrigger("pickupShield");
        Play(sfxShieldCatch);
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (shieldInFlight) return;
            isBlocking = true;
            anim.SetBool("isBlocking", true);
            Play(sfxBlock);
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
        if (shieldInFlight) return;
        parryTimer = parryWindow;
        anim.SetTrigger("parry");
        Play(sfxParry);
    }

    public bool TryBlock(GameObject attacker)
    {
        if (isBlocking) Play(sfxBlock);
        return isBlocking;
    }

    void DealMeleeDamage()
    {
        Vector3 hitCenter = transform.position + transform.forward * (meleeRadius * 0.5f);
        Collider[] hits = Physics.OverlapSphere(hitCenter, meleeRadius, enemyLayers);
        bool hitSomething = false;
        foreach (var hit in hits)
        {
            hit.GetComponent<Health>()?.TakeDamage(meleeDamage);
            hitSomething = true;
        }
        if (hitSomething) Play(sfxMeleeHit);
    }

    void DealSpecialDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, specialRadius, enemyLayers);
        bool hitSomething = false;
        foreach (var hit in hits)
        {
            hit.GetComponent<Health>()?.TakeDamage(specialDamage);
            hitSomething = true;
        }
        if (hitSomething) Play(sfxMeleeHit);
    }
}
