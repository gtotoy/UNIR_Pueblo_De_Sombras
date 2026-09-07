using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    enum State { Idle, Chase, Attack, Stunned, Dead }
    State state = State.Idle;

    [Header("Detection")]
    [SerializeField] float detectionRadius = 8f;
    [SerializeField] float attackRange = 1.8f;

    [Header("Wander")]
    [SerializeField] float wanderRadius = 5f;
    [SerializeField] float wanderWaitMin = 2f;
    [SerializeField] float wanderWaitMax = 4f;

    float wanderTimer;

    [Header("Combat")]
    [SerializeField] float attackDamage = 15f;
    [SerializeField] float attackCooldown = 1.5f;
    [SerializeField] float damageDelay = 0.5f;

    [Header("Stun")]
    [SerializeField] float stunDuration = 2f;

    [Header("Audio")]
    [SerializeField] AudioClip sfxAttackHit;
    [SerializeField] AudioClip sfxAttackBlocked;

    NavMeshAgent agent;
    Animator anim;
    Health health;
    Transform target;
    float attackTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();
        health.OnDeath += HandleDeath;
    }

    void Start()
    {
        var roll = UnityEngine.Random.value;
        if (roll < 0.5f)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }
        else
        { 
            var artifacts = FindObjectsByType<Artifact>(FindObjectsSortMode.None);
            var targetArtifacts = artifacts.Where(x => x.IsEnemyTarget).ToList();
            if (targetArtifacts.Count > 0)
            {
                var randomArtifact = targetArtifacts[Random.Range(0, targetArtifacts.Count)];
                target = randomArtifact.transform;
            }
            else
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) target = playerObj.transform;
            }
        }        
    }

    void Update()
    {
        if (state == State.Dead || state == State.Stunned) return;
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        var distance = GetDistanceToTarget();

        switch (state)
        {
            case State.Idle:
                if (distance <= detectionRadius ) { SetState(State.Chase); break; }
                if (target.GetComponent<Artifact>()) { SetState(State.Chase); break; }
                HandleWander();
                break;

            case State.Chase:
                agent.SetDestination(target.position);
                anim?.SetFloat("speed", agent.velocity.magnitude);
                if (distance <= attackRange)
                    SetState(State.Attack);
                break;

            case State.Attack:
                agent.ResetPath();
                anim?.SetFloat("speed", 0f);
                FaceTarget();
                if (distance > attackRange * 1.3f)
                    SetState(State.Chase);
                else if (attackTimer <= 0f)
                    PerformAttack();
                break;
        }
    }

    void SetState(State next)
    {
        state = next;
        bool chasing = next == State.Chase || next == State.Attack;
        anim?.SetBool("isChasing", chasing);
        if (next == State.Idle)
        {
            agent.ResetPath();
            wanderTimer = Random.Range(wanderWaitMin, wanderWaitMax);
        }
    }

    float GetDistanceToTarget()
    {
        if (!target) return float.MaxValue;
        var displacement = target.position - transform.position;
        displacement.y = 0f;
        return displacement.magnitude;
    }

    void FaceTarget()
    {
        if (target == null) return;
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    void PerformAttack()
    {
        attackTimer = attackCooldown;
        anim?.SetTrigger("attack");
        // Fallback por si no hay Animation Event en el clip
        Invoke(nameof(TryDealDamage), damageDelay);
    }

    // Llamar desde Animation Event en el frame exacto del impacto visual
    public void OnAttackHitFrame()
    {
        CancelInvoke(nameof(TryDealDamage)); // cancelar el fallback
        TryDealDamage();
    }

    void TryDealDamage()
    {
        if (state == State.Dead || !target) return;
        if (GetDistanceToTarget() > attackRange * 1.3f) return;

        var playerCombat = target.GetComponent<PlayerCombatController>();
        if (playerCombat)
        {

            // Parry: si el jugador está en ventana de parry, stunearse
            if (playerCombat.IsParrying)
            {
                playerCombat.NotifyParryLanded();
                Stun();
                return;
            }

            // Block: si el jugador está bloqueando, no hacer daño
            if (playerCombat.TryBlock(gameObject))
            {
                AudioSource.PlayClipAtPoint(sfxAttackBlocked, transform.position);
                return;
            }
        }

        target.GetComponent<Health>()?.TakeDamage(attackDamage);
        AudioSource.PlayClipAtPoint(sfxAttackHit, transform.position);
    }

    public void Stun()
    {
        if (state == State.Dead) return;
        CancelInvoke(nameof(TryDealDamage));
        StopAllCoroutines();
        StartCoroutine(StunRoutine());
    }

    IEnumerator StunRoutine()
    {
        SetState(State.Stunned);
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        anim?.ResetTrigger("attack");
        anim?.SetBool("isStunned", true);
        anim?.SetFloat("speed", 0f);

        yield return new WaitForSeconds(stunDuration);

        if (state == State.Dead) yield break;

        anim?.SetBool("isStunned", false);
        attackTimer = attackCooldown;
        SetState(State.Chase);
    }

    void HandleDeath()
    {
        state = State.Dead;
        agent.enabled = false;

        CancelInvoke(nameof(TryDealDamage));
        StopAllCoroutines();

        if (anim != null)
        {
            anim.ResetTrigger("attack");
            anim.SetBool("isStunned", false);
            anim.SetTrigger("death");
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        WaveManager.Instance?.RegisterEnemyDeath();
        Destroy(gameObject, 3f);
    }

    void HandleWander()
    {
        bool isMoving = !agent.pathPending && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

        anim?.SetBool("isChasing", isMoving);
        anim?.SetFloat("speed", isMoving ? agent.velocity.magnitude : 0f);

        if (isMoving) return;

        wanderTimer -= Time.deltaTime;
        if (wanderTimer > 0f) return;

        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir += transform.position;
        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);

        wanderTimer = Random.Range(wanderWaitMin, wanderWaitMax);
    }

    public bool TryApplyArtifact(Artifact artifact)
    {
        if (artifact == null) return false;
        agent.speed *= artifact.MultiplySpeed;
        return false;
    }

    public bool TryRemoveArtifact(Artifact artifact)
    {
        if (artifact == null) return false;
        agent.speed /= artifact.MultiplySpeed;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
