using UnityEngine;

public class BossBackpack : MonoBehaviour
{
    public float MaxHealth = 100f;
    public float CurrentHealth;

    private Boss boss;

    public void Awake()
    {
        CurrentHealth = MaxHealth;
        boss = GetComponentInParent<Boss>();
    }

    public void TakeDamage(float damageAmount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);
        boss.OnHealthChanged?.Invoke(boss);
        if (CurrentHealth <= 0)
        {
            boss.HandleBackpackDestroyed();
            gameObject.SetActive(false);
        }
    }
}
