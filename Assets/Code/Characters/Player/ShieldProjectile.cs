using UnityEngine;

public class ShieldProjectile : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 22f;
    public float maxDistance = 14f;
    public float catchRadius = 0.8f;
    public LayerMask hitLayers;

    private Transform owner;
    private Vector3 ownerForwardStart;
    private PlayerCombatController combat;
    private Vector3 startPos;
    private bool returning;

    public float spinSpeed = 720f;

    public void Init(Transform ownerTransform, PlayerCombatController combatController)
    {
        owner = ownerTransform;
        ownerForwardStart = owner.forward;
        combat = combatController;
        startPos = transform.position;
    }

    void Update()
    {

        if (!returning)
        {
            transform.position += ownerForwardStart * speed * Time.deltaTime;

            if (Vector3.Distance(startPos, transform.position) >= maxDistance)
                StartReturn();
        }
        else
        {
            Vector3 target = owner.position + Vector3.up * 1.2f;
            Vector3 dir = target - transform.position;

            transform.position += dir.normalized * speed * Time.deltaTime;

            if (dir.magnitude <= catchRadius)
                Catch();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (returning) return;
        if (other.transform == owner) return;
        if (other.CompareTag("ShieldProjectile")) return;

        //TODO: Here I need to do damage later

        StartReturn();
    }

    void StartReturn() => returning = true;

    void Catch()
    {
        combat.OnShieldCaught();
        Destroy(gameObject);
    }
}