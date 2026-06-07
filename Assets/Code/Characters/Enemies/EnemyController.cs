using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    enum State { Idle, Chase, Attack, Dead }
    State state = State.Idle;

    [Header("Detection")]
    [SerializeField] float detectionRadius = 8f;
    [SerializeField] float attackRange = 1.8f;

    [Header("Combat")]
    [SerializeField] float attackDamage = 15f;
    [SerializeField] float attackCooldown = 1.5f;
    [SerializeField] float damageDelay = 0.5f;

    NavMeshAgent agent;
    Animator anim;
    Health health;
    Transform player;
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
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        if (state == State.Dead) return;
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        float dist = player != null
            ? Vector3.Distance(transform.position, player.position)
            : float.MaxValue;

        switch (state)
        {
            case State.Idle:
                anim?.SetFloat("speed", 0f);
                if (dist <= detectionRadius)
                    SetState(State.Chase);
                break;

            case State.Chase:
                agent.SetDestination(player.position);
                anim?.SetFloat("speed", agent.velocity.magnitude);
                if (dist <= attackRange)
                    SetState(State.Attack);
                break;

            case State.Attack:
                agent.ResetPath();
                anim?.SetFloat("speed", 0f);
                FacePlayer();
                if (dist > attackRange * 1.3f)
                    SetState(State.Chase);
                else if (attackTimer <= 0f)
                    PerformAttack();
                break;
        }
    }

    void SetState(State next)
    {
        state = next;
        if (next == State.Chase || next == State.Attack)
            anim?.SetBool("isChasing", true);
        else
            anim?.SetBool("isChasing", false);
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    void PerformAttack()
    {
        attackTimer = attackCooldown;
        anim?.SetTrigger("attack");
        Invoke(nameof(TryDealDamage), damageDelay);
    }

    void TryDealDamage()
    {
        if (state == State.Dead || player == null) return;
        if (Vector3.Distance(transform.position, player.position) > attackRange * 1.3f) return;

        var playerCombat = player.GetComponent<PlayerCombatController>();
        if (playerCombat != null && playerCombat.TryBlock(gameObject)) return;

        player.GetComponent<Health>()?.TakeDamage(attackDamage);
    }

    void HandleDeath()
    {
        state = State.Dead;
        agent.enabled = false;

        CancelInvoke(nameof(TryDealDamage));

        if (anim != null)
        {
            anim.ResetTrigger("attack");
            anim.SetTrigger("death");
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        WaveManager.Instance?.RegisterEnemyDeath();
        Destroy(gameObject, 3f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
