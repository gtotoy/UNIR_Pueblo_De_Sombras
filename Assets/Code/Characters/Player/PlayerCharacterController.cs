using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCharacterController : MonoBehaviour
{
    private enum PlayerState { Idle, Moving, Dashing, Paused }
    private PlayerState currentState = PlayerState.Idle;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float blockMoveSpeed = 2f;
    public float rotateSpeed = 12f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;
    public float runSpeedMultiplier = 1.6f;

    [Header("Lock-On")]
    public float lockOnRange = 15f;
    public float cycleTriggerThreshold = 0.6f;
    public float cycleReleaseThreshold = 0.3f;
    [SerializeField] LockOnReticle lockOnReticlePrefab;

    [Header("Audio")]
    [SerializeField] AudioClip sfxDash;

    [Header("Camera")]
    [SerializeField] Transform cameraTransform;

    private Rigidbody rb;
    private Transform trans;
    private Animator anim;
    private AudioSource audioSource;
    private PlayerCombatController combat;
    private Health health;
    private Collider col;

    private Vector2 moveInput;
    private float dashTimer, dashCooldownTimer;
    private bool dashHeld;

    private Transform lockedTarget;
    private Vector2 cycleInput;
    private bool cycleStickReleased = true;
    private LockOnReticle activeReticle;

    public bool IsLockedOn => lockedTarget != null;
    public Transform LockedTarget => lockedTarget;

    private readonly List<Collider> ignoredEnemyColliders = new List<Collider>();

    private int combatLayerIndex;

    public PlayerCombatController PlayerCombatController => combat;
    public Health PlayerHealth => health;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        trans = GetComponent<Transform>();
        combat = GetComponent<PlayerCombatController>();
        health = GetComponent<Health>();
        col = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        combatLayerIndex = anim.GetLayerIndex("CombatLayer");

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (lockOnReticlePrefab != null)
        {
            activeReticle = Instantiate(lockOnReticlePrefab);
            activeReticle.SetTarget(null);
        }
    }

    private bool IsPaused => currentState == PlayerState.Paused;
    private bool IsDashing => currentState == PlayerState.Dashing;

    private void TransitionTo(PlayerState next)
    {
        currentState = next;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (IsPaused) return;
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started) dashHeld = true;
        if (context.canceled) dashHeld = false;

        if (!context.performed) return;
        if (IsPaused || IsDashing || dashCooldownTimer > 0) return;

        TransitionTo(PlayerState.Dashing);
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        rb.linearVelocity = trans.forward * dashSpeed;
        if (health != null) health.IsInvulnerable = true;
        SetEnemyCollisionsIgnored(true);

        anim.SetLayerWeight(combatLayerIndex, 0f);
        anim.SetTrigger("dash");
        if (sfxDash != null) audioSource.PlayOneShot(sfxDash);
    }

    public void OnPlaceArtifact(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var gameController = FindFirstObjectByType<GameController>();
        if (gameController) {
            gameController.TryPlaceArtifact(transform.position, transform.forward, transform.up);
        }
    }

    public void OnSelectArtifactLeft(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var gameController = FindFirstObjectByType<GameController>();
        if (gameController) {
            gameController.SelectNextArtifact(-1);
        }
        var gameUIManager = FindFirstObjectByType<GameUIManager>();
        if (gameUIManager && gameUIManager.blessingsPanel.gameObject.activeSelf) {
            gameUIManager.blessingsPanel.SelectNextBlessing(-1);
        }
    }

    public void OnSelectArtifactRight(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var gameController = FindFirstObjectByType<GameController>();
        if (gameController) {
            gameController.SelectNextArtifact(1);
        }
        var gameUIManager = FindFirstObjectByType<GameUIManager>();
        if (gameUIManager && gameUIManager.blessingsPanel.gameObject.activeSelf) {
            gameUIManager.blessingsPanel.SelectNextBlessing(1);
        }
    }

    void SetEnemyCollisionsIgnored(bool ignore)
    {
        if (col == null) return;

        if (ignore)
        {
            foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            {
                Collider enemyCol = enemy.GetComponent<Collider>();
                if (enemyCol == null) continue;
                Physics.IgnoreCollision(col, enemyCol, true);
                ignoredEnemyColliders.Add(enemyCol);
            }
        }
        else
        {
            foreach (var enemyCol in ignoredEnemyColliders)
            {
                if (enemyCol != null) Physics.IgnoreCollision(col, enemyCol, false);
            }
            ignoredEnemyColliders.Clear();
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (IsPaused)
        {
            TransitionTo(moveInput.sqrMagnitude > 0.01f ? PlayerState.Moving : PlayerState.Idle);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            anim.SetFloat("speed", 0f);
            TransitionTo(PlayerState.Paused);
        }
    }

    public void OnLockOn(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (IsPaused) return;

        lockedTarget = lockedTarget != null ? null : FindClosestEnemy();
        UpdateReticle();
    }

    public void OnCycleTarget(InputAction.CallbackContext context)
    {
        cycleInput = new Vector2(context.ReadValue<float>(), 0f);
    }

    Transform FindClosestEnemy()
    {
        Transform closest = null;
        float closestDist = lockOnRange;
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            var enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null && enemyHealth.IsDead) continue;

            float dist = Vector3.Distance(trans.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy.transform;
            }
        }
        return closest;
    }

    void HandleTargetCycle()
    {
        if (lockedTarget == null)
        {
            cycleStickReleased = true;
            return;
        }

        if (Mathf.Abs(cycleInput.x) < cycleReleaseThreshold)
        {
            cycleStickReleased = true;
            return;
        }

        if (!cycleStickReleased) return;
        if (Mathf.Abs(cycleInput.x) < cycleTriggerThreshold) return;

        CycleTarget(cycleInput.x > 0f ? 1 : -1);
        UpdateReticle();
        cycleStickReleased = false;
    }

    void CycleTarget(int direction)
    {
        var enemies = new List<Transform>();
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            var enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null && enemyHealth.IsDead) continue;
            if (Vector3.Distance(trans.position, enemy.transform.position) > lockOnRange) continue;
            enemies.Add(enemy.transform);
        }

        if (enemies.Count == 0)
        {
            lockedTarget = null;
            return;
        }

        enemies.Sort((a, b) => Vector3.Distance(trans.position, a.position).CompareTo(Vector3.Distance(trans.position, b.position)));

        int currentIndex = enemies.IndexOf(lockedTarget);
        if (currentIndex < 0)
        {
            lockedTarget = enemies[0];
            return;
        }

        int nextIndex = (currentIndex + direction + enemies.Count) % enemies.Count;
        lockedTarget = enemies[nextIndex];
    }

    void HandleLockOn()
    {
        if (lockedTarget == null) return;

        var targetHealth = lockedTarget.GetComponent<Health>();
        bool dead = targetHealth != null && targetHealth.IsDead;
        float dist = Vector3.Distance(trans.position, lockedTarget.position);

        if (dead || dist > lockOnRange)
        {
            lockedTarget = null;
            UpdateReticle();
        }
    }

    void UpdateReticle()
    {
        if (activeReticle != null) activeReticle.SetTarget(lockedTarget);
    }

    void FaceLockedTarget()
    {
        Vector3 lookDir = lockedTarget.position - trans.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * rotateSpeed);
    }

    void Update()
    {
        HandleTargetCycle();
    }

    void FixedUpdate()
    {
        if (IsPaused) return;
        HandleLockOn();
        HandleMovement();
        HandleTimers();
    }

    void HandleMovement()
    {
        if (IsDashing) return;

        bool blocking = combat != null && combat.IsBlocking;
        bool running = dashHeld;
        float activeSpeed = blocking ? blockMoveSpeed : (running ? moveSpeed * runSpeedMultiplier : moveSpeed);

        Vector3 dir;
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();
            dir = camRight * moveInput.x + camForward * moveInput.y;
        }
        else
        {
            dir = new Vector3(moveInput.x, 0, moveInput.y);
        }
        bool hasMoveInput = dir.sqrMagnitude > 0.01f;

        if (hasMoveInput)
        {
            dir = dir.normalized;
            rb.linearVelocity = new Vector3(dir.x * activeSpeed, rb.linearVelocity.y, dir.z * activeSpeed);
            TransitionTo(PlayerState.Moving);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            TransitionTo(PlayerState.Idle);
        }

        if (IsLockedOn)
            FaceLockedTarget();
        else if (hasMoveInput)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * rotateSpeed);
        }

        anim.SetFloat("speed", dir.magnitude);
        anim.SetBool("isRunning", running);
    }

    void HandleTimers()
    {
        if (IsDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0)
            {
                anim.SetLayerWeight(combatLayerIndex, 1f);
                if (health != null) health.IsInvulnerable = false;
                SetEnemyCollisionsIgnored(false);
                TransitionTo(PlayerState.Idle);
            }
        }

        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.fixedDeltaTime;
    }
}