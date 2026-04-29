using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCharacterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotateSpeed = 12f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;

    [Header("Block / Parry")]
    public float parryWindow = 0.2f;

    private Rigidbody rb;
    private Animator anim;
    private Vector2 moveInput;
    private bool isDashing, isBlocking, isParrying;
    private float dashTimer, dashCooldownTimer, parryTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleTimers();
    }

    void HandleMovement()
    {
        if (isDashing)
        {
            return;
        }

        Vector3 dir = new Vector3(moveInput.x, 0, moveInput.y);

        if (dir.sqrMagnitude > 0.01f)
        {
            dir = dir.normalized;
            rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * rotateSpeed);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        anim.SetFloat("speed", dir.magnitude);
    }

    void HandleTimers()
    {
        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0) isDashing = false;
        }
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.fixedDeltaTime;
        if (parryTimer > 0)
        {
            parryTimer -= Time.fixedDeltaTime;
            if (parryTimer <= 0) isParrying = false;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (isDashing || dashCooldownTimer > 0) return;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        Vector3 dashDir = Vector3.forward;
        if (moveInput.sqrMagnitude > 0.01f)
            dashDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        rb.linearVelocity = dashDir * dashSpeed;
        anim.SetTrigger("dash");
    }
}