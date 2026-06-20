using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] float healAmount = 30f;
    [SerializeField] float lifetime = 12f;
    [SerializeField] float bobHeight = 0.15f;
    [SerializeField] float bobSpeed = 2f;
    [SerializeField] float spinSpeed = 90f;
    [SerializeField] AudioClip sfxPickup;

    Vector3 startPos;

    void Start()
    {
        Debug.Log("Pickup Spawned.");
        startPos = transform.position;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var health = other.GetComponent<Health>();
        if (health == null) return;
        if (health.IsDead) return;

        health.Heal(healAmount);

        if (sfxPickup != null)
            AudioSource.PlayClipAtPoint(sfxPickup, transform.position);

        Destroy(gameObject);
    }
}
