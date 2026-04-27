using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float runSpeed = 3.0f;
    [SerializeField] private float smoothTime = 0.025f;
    [SerializeField] private float interpolationRate = 10.0f;
    [SerializeField] private float normalAttackDuration = 0.6f;
    [SerializeField] private float specialAttackDuration = 1.2f;
    [SerializeField] private float dashDuration = 1.2f;
    [SerializeField] private float dashCooldown = 5.0f;
    [SerializeField] private float dashSpeed = 10.0f;

    private Rigidbody rb;
    private Animator animator;
    private PlayerInput playerInput;
    private bool isAttacking = false;
    private bool isDashing = false;
    private bool canDash = true;
    private Vector3 dashDirection = Vector3.zero;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerInput = new PlayerInput();
        playerInput.Enable();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Vector3 rawMove = Vector3.zero;

        if (!isAttacking)
            rawMove = MovementInput(rawMove);

        rb.linearVelocity = rawMove;

        if (rawMove != Vector3.zero)
        {
            transform.rotation = Rotation(rawMove);
            animator.SetFloat("speed", runSpeed, smoothTime, Time.deltaTime);
        }
        else
            animator.SetFloat("speed", 0f, smoothTime, Time.deltaTime);

        if (!isAttacking && !isDashing)
            AttackInput();
        
    }

    private Vector3 MovementInput(Vector3 rawMove)
    {

        if (!isDashing)
        {
            if (playerInput.Movement.Forward.IsPressed())
                rawMove = Vector3.forward;
            else if (playerInput.Movement.Back.IsPressed())
                rawMove = Vector3.back;

            if (playerInput.Movement.Right.IsPressed())
                rawMove += Vector3.right;
            else if (playerInput.Movement.Left.IsPressed())
                rawMove += Vector3.left;

            rawMove = Vector3.ClampMagnitude(rawMove, 1f);
            rawMove *= runSpeed;

            if (playerInput.Movement.Dash.WasPressedThisFrame() && canDash)
            {
                dashDirection = transform.forward;
                dashDirection.Normalize();
                StartCoroutine(Dash(dashDuration, dashCooldown));
            }
        }
        else
            rawMove = dashDirection * dashSpeed;

        return rawMove;
    }

    private void AttackInput()
    {
        if (playerInput.Attack.NormalAttack.WasPressedThisFrame() && !isAttacking && !isDashing)
            StartCoroutine(Attack(normalAttackDuration, "normalAttack"));

        if (playerInput.Attack.SpecialAttack.WasPressedThisFrame() && !isAttacking && !isDashing)
            StartCoroutine(Attack(specialAttackDuration, "specialAttack"));
    }

    private Quaternion Rotation(Vector3 rawMove)
    {
        Vector3 direction = new(rawMove.x, 0f, rawMove.z);
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        targetRotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * interpolationRate);
        return targetRotation;
    }

    private IEnumerator Dash(float dashDuration, float dashCooldown)
    {
        isDashing = true;
        canDash = false;
        animator.SetTrigger("dash");

        yield return new WaitForSeconds(dashDuration);
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown - dashDuration);
        canDash = true;
    }

    private IEnumerator Attack(float attackDuration, string attackType)
    {
        isAttacking = true;
        animator.SetTrigger(attackType);
        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
    }

}
