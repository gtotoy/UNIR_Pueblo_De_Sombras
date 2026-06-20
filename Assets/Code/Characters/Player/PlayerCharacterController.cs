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

    [Header("Audio")]
    [SerializeField] AudioClip sfxDash;

    private Rigidbody rb;
    private Transform trans;
    private Animator anim;
    private AudioSource audioSource;
    private PlayerCombatController combat;

    private Vector2 moveInput;
    private float dashTimer, dashCooldownTimer;

    private int combatLayerIndex;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        trans = GetComponent<Transform>();
        combat = GetComponent<PlayerCombatController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        combatLayerIndex = anim.GetLayerIndex("CombatLayer");
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
        if (!context.performed) return;
        if (IsPaused || IsDashing || dashCooldownTimer > 0) return;

        TransitionTo(PlayerState.Dashing);
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        rb.linearVelocity = trans.forward * dashSpeed;

        anim.SetLayerWeight(combatLayerIndex, 0f);
        anim.SetTrigger("dash");
        if (sfxDash != null) audioSource.PlayOneShot(sfxDash);
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

    void FixedUpdate()
    {
        if (IsPaused) return;
        HandleMovement();
        HandleTimers();
    }

    void HandleMovement()
    {
        if (IsDashing) return;

        bool blocking = combat != null && combat.IsBlocking;
        float activeSpeed = blocking ? blockMoveSpeed : moveSpeed;

        Vector3 dir = new Vector3(moveInput.x, 0, moveInput.y);
        if (dir.sqrMagnitude > 0.01f)
        {
            dir = dir.normalized;
            rb.linearVelocity = new Vector3(dir.x * activeSpeed, rb.linearVelocity.y, dir.z * activeSpeed);
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * rotateSpeed);
            TransitionTo(PlayerState.Moving);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            TransitionTo(PlayerState.Idle);
        }

        anim.SetFloat("speed", dir.magnitude);
    }

    void HandleTimers()
    {
        if (IsDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0)
            {
                anim.SetLayerWeight(combatLayerIndex, 1f);
                TransitionTo(PlayerState.Idle);
            }
        }

        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.fixedDeltaTime;
    }
}