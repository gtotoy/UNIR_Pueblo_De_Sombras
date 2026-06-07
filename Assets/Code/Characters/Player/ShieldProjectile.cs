using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShieldProjectile : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 22f;
    public float maxDistance = 14f;
    public float catchRadius = 0.8f;
    public LayerMask hitLayers;
    public float spinSpeed = 720f;

    private Rigidbody rb;
    private Transform owner;
    private Vector3 ownerForwardStart;
    private PlayerCombatController combat;
    private Vector3 startPos;
    private bool returning;
    private float damage;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Init(Transform ownerTransform, PlayerCombatController combatController, float projectileDamage)
    {
        owner = ownerTransform;
        ownerForwardStart = owner.forward;
        combat = combatController;
        startPos = transform.position;
        damage = projectileDamage;
    }

    void FixedUpdate()
    {
        if (!returning)
        {
            Vector3 next = rb.position + ownerForwardStart * speed * Time.fixedDeltaTime;
            rb.MovePosition(next);

            if (Vector3.Distance(startPos, rb.position) >= maxDistance)
                StartReturn();
        }
        else
        {
            if (owner == null) { Destroy(gameObject); return; }

            Vector3 target = owner.position + Vector3.up * 1.2f;
            Vector3 dir = target - rb.position;

            rb.MovePosition(rb.position + dir.normalized * speed * Time.fixedDeltaTime);

            if (dir.magnitude <= catchRadius)
                Catch();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (returning) return;
        if (other.transform == owner) return;
        if (other.CompareTag("ShieldProjectile")) return;

        if (hitLayers != 0 && (hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        var h = other.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (h != null) h.TakeDamage(damage);

        StartReturn();
    }

    void StartReturn() => returning = true;

    void Catch()
    {
        combat?.OnShieldCaught();
        Destroy(gameObject);
    }
}
