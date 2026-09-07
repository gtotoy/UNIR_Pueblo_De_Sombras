using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public class Boss : MonoBehaviour
{
    public enum State { None, Tracking, Attacking, Stunned, Transitioning, Dead }
    public enum Phase { Phase1, Phase2, Phase3 }
    
    [Header("Core")]
    public float FacingAngularSpeed = 180f;
    public float MainBodyMaxHealth = 900f;
    public float MainBodyCurrentHealth;
    public float StunDuration = 4f;

    [Header("Phase 1 Settings (Backpack Intact)")]
    public float P1AttackCooldown = 3f;
    public float P1AttackRange = 3f;
    public float P1AttackDamage = 20f;
    public float P1ParryableAttackDamage = 30f;

    [Header("Phase 2 Settings (Backpack Broken)")]
    public float P2AttackCooldown = 1.5f;
    public float P2MeleeAttackRange = 3f;
    public float P2MeleeAttackDamage = 15f;
    public float P2RangedAttackRange = 12f;
    public float P2RangedAttackDamage = 10f;

    [Header("Phase 3 Settings (Critical)")]
    public float P3AttackCooldown = 2f;
    public float P3Threshold = 0.5f; // 50% max flesh HP triggers minions
    public GameObject MinionPrefab;

    [Header("Audio")]
    [SerializeField] AudioClip SfxAttackHit;

    [Header("State")]
    public State InitialState;
    public State CurrentState;
    public Phase CurrentPhase;

    public Action<Boss> OnHealthChanged;

    public BossBackpack Backpack => backpack;

    private BossBackpack backpack;
    private PlayerCharacterController playerCharacterController;
    private NavMeshAgent agent;
    private Animator animator;
    private float cooldownNextTimeAttack = 0f;
    private CancellationTokenSource parryableAttackCts = null;
    private bool isParryWindowOpen = false;
    private CancellationTokenSource stunCycleCts = null;

    public void Awake()
    {
        MainBodyCurrentHealth = MainBodyMaxHealth;
        backpack = GetComponentInChildren<BossBackpack>();
        animator = GetComponentInChildren<Animator>();
        playerCharacterController = FindFirstObjectByType<PlayerCharacterController>();
        agent = GetComponent<NavMeshAgent>();
        OnHealthChanged += (Boss boss) => {
            if (CurrentPhase == Phase.Phase2 && (TotalCurrentHealth / TotalMaxHealth <= P3Threshold))
            {
                _ = TriggerPhase3TransitionAsync();
            }
        };
        SetState(InitialState);
    }

    public void Update()
    {
        switch (CurrentState)
        {
            case State.Tracking:
                {
                    // Face player
                    var targetDirection = playerCharacterController.transform.position - transform.position;
                    targetDirection.y = 0; // Keep the boss upright
                    if (targetDirection != Vector3.zero)
                    {
                        var targetRotation = Quaternion.LookRotation(targetDirection);
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation,
                            targetRotation,
                            FacingAngularSpeed * Time.deltaTime
                        );
                    }

                    void TryToAttack() {
                        if (cooldownNextTimeAttack > Time.time || !playerCharacterController) { return; }

                        var cooldownDuration = CurrentPhase switch
                        {
                            Phase.Phase1 => P1AttackCooldown,
                            Phase.Phase2 => P2AttackCooldown,
                            Phase.Phase3 => P3AttackCooldown,
                            _ => 2f
                        };

                        var targetDirection = playerCharacterController.transform.position - transform.position;
                        targetDirection.y = 0;
                        var distanceToPlayer = targetDirection.magnitude;

                        switch (CurrentPhase)
                        {
                            case Phase.Phase1:
                                if (distanceToPlayer < P1AttackRange)
                                {
                                    cooldownNextTimeAttack = Time.time + cooldownDuration;

                                    // 50/50 Chance to trigger a normal vs. parryable slash
                                    if (UnityEngine.Random.value > 0.5f) { _ = TriggerRegularAttackAsync(); }
                                    else { _ = TriggerParryableAttackAsync(); }
                                }
                                break;
                            case Phase.Phase2:
                            case Phase.Phase3:
                                if (distanceToPlayer <= P2MeleeAttackRange)
                                {
                                    cooldownNextTimeAttack = Time.time + cooldownDuration;
                                    _ = TriggerP2FastMeleeAsync();
                                } 
                                else if (distanceToPlayer <= P2RangedAttackRange)
                                {
                                    cooldownNextTimeAttack = Time.time + cooldownDuration;
                                    _ = TriggerP2RangedAttackAsync();
                                }
                                break;
                        }
                    }

                    TryToAttack();
                    break;
                }
            case State.Attacking:
                {
                    if (playerCharacterController.PlayerCombatController.IsParrying)
                    {
                        TryGetCurrentAttackParried();
                    }
                    break;
                }
            case State.Stunned:
                {
                    
                    break;
                }
            case State.Transitioning:
                {
                    
                    break;
                }
            case State.Dead:
                {
                    
                    break;
                }
        }
        
    }

    public void SetState(State newState)
    {
        CurrentState = newState;
        switch (CurrentState) {
            case State.Tracking:
                {
                    animator?.SetBool("isStunned", false);
                    break;
                }
                case State.Stunned:
                {
                    animator?.SetBool("isStunned", true);
                    break;
                }
                case State.Dead:
                {
                    animator?.SetTrigger("death");
                    break;
                }
        }
    }

    public void TryGetCurrentAttackParried()
    {
        if (isParryWindowOpen)
        {
            isParryWindowOpen = false;
            parryableAttackCts?.Cancel();
            _ = TriggerStunCycleAsync();
        }
        else
        {
            Debug.Log("Parry failed: No parryable attack in progress.");
        }
    }

    private async Awaitable TriggerStunCycleAsync()
    {
        stunCycleCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        SetState(State.Stunned);
        Debug.Log("Boss Stunned! Backpack vulnerable to attack!");

        try
        {
            // Pass the stun token into the awaitable delay
            await Awaitable.WaitForSecondsAsync(StunDuration, stunCycleCts.Token);

            if (CurrentState == State.Stunned)
            {
                SetState(State.Tracking);
            }
        }
        catch (System.OperationCanceledException)
        {
            // Triggers instantly when the backpack breaks, clearing the background thread smoothly!
            Debug.Log("Stun loop cancelled safely by backpack explosion.");
        }
        finally
        {
            stunCycleCts?.Dispose();
            stunCycleCts = null;
        }
    }

    public void HandleBackpackDestroyed()
    {
        stunCycleCts?.Cancel();
        _ = TriggerPhase2TransitionAsync();
    }

    public async Awaitable TriggerPhase2TransitionAsync()
    {
        SetState(State.Transitioning);
        CurrentPhase = Phase.Phase2;
        Debug.Log("Backpack Exploded! Switching behavior configurations to Phase 2!");

        await Awaitable.WaitForSecondsAsync(2.5f, destroyCancellationToken);
        SetState(State.Tracking);
    }

    public async Awaitable TriggerPhase3TransitionAsync()
    {
        SetState(State.Transitioning);
        CurrentPhase = Phase.Phase3;
        Debug.Log("Enraged! Summoning Minion Squad to protect boss coordinates!");
        // TODO(gus): Spawn minions
        await Awaitable.WaitForSecondsAsync(3.0f, destroyCancellationToken);
        SetState(State.Tracking);
    }

    #region Health
    public float TotalMaxHealth => MainBodyMaxHealth + backpack.MaxHealth;
    public float TotalCurrentHealth => MainBodyCurrentHealth + backpack.CurrentHealth;

    public void MainBodyTakeDamage(float damageAmount)
    {
        if (CurrentState == State.Dead || CurrentState == State.Transitioning) { return; }
        if (CurrentPhase == Phase.Phase1) { return; } // Immune to direct hits while backpack is intact

        MainBodyCurrentHealth = Mathf.Max(0, MainBodyCurrentHealth - damageAmount);
        OnHealthChanged?.Invoke(this);

        if (MainBodyCurrentHealth <= 0)
        {
            SetState(State.Dead);
            Debug.Log("Boss has been completely defeated.");
            return;
        }
    }
    #endregion

    #region Attacks
    private async Awaitable TriggerRegularAttackAsync()
    {
        SetState(State.Attacking);
        animator?.SetTrigger("attackRegular");
        Debug.Log("Executing normal melee attack...");
        {
            playerCharacterController.PlayerHealth.TakeDamage(P1AttackDamage);
            AudioSource.PlayClipAtPoint(SfxAttackHit, transform.position);
        }
        await Awaitable.WaitForSecondsAsync(1.2f, destroyCancellationToken);
        SetState(State.Tracking);
    }

    private async Awaitable TriggerParryableAttackAsync()
    {
        Debug.Assert(parryableAttackCts == null, "Parryable attack already in progress.");
        parryableAttackCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        SetState(State.Attacking);
        animator?.SetTrigger("attackParryable");
        try
        {
            await Awaitable.WaitForSecondsAsync(0.4f, parryableAttackCts.Token);
            isParryWindowOpen = true;
            await Awaitable.WaitForSecondsAsync(0.3f, parryableAttackCts.Token);
            isParryWindowOpen = false;
            {
                playerCharacterController.PlayerHealth.TakeDamage(P1ParryableAttackDamage);
                AudioSource.PlayClipAtPoint(SfxAttackHit, transform.position);
            }
            await Awaitable.WaitForSecondsAsync(0.8f, parryableAttackCts.Token);
            SetState(State.Tracking);
            Debug.Log("Parry attack finished");
        }
        catch (System.OperationCanceledException)
        {
            isParryWindowOpen = false;
            Debug.Log("Parry successful: Attack task aborted safely.");
        }
        finally
        {
            isParryWindowOpen = false;
            parryableAttackCts.Dispose();
            parryableAttackCts = null;
        }
    }

    private async Awaitable TriggerP2FastMeleeAsync()
    {
        SetState(State.Attacking);
        animator?.SetTrigger("attackP2Melee");
        Debug.Log("Executing P2 Fast Combo Strike...");
        {
            playerCharacterController.PlayerHealth.TakeDamage(P2MeleeAttackDamage);
            AudioSource.PlayClipAtPoint(SfxAttackHit, transform.position);
        }
        await Awaitable.WaitForSecondsAsync(0.8f, destroyCancellationToken);
        SetState(State.Tracking);
    }

    private async Awaitable TriggerP2RangedAttackAsync()
    {
        SetState(State.Attacking);
        animator?.SetTrigger("attackP2Ranged");
        Debug.Log("Telegraphing Ranged Energy Blast...");
        await Awaitable.WaitForSecondsAsync(0.4f, destroyCancellationToken);

        // TODO(gus): Instantiate energy blast
        {
            playerCharacterController.PlayerHealth.TakeDamage(P2RangedAttackDamage);
            AudioSource.PlayClipAtPoint(SfxAttackHit, transform.position);
        }

        await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);
        SetState(State.Tracking);
    }
    #endregion
}
