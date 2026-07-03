using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] float maxHP = 100f;
    float currentHP;

    public event Action OnDeath;
    public event Action<float, float> OnHealthChanged;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0f;
    public bool IsInvulnerable { get; set; }

    void Awake() => currentHP = maxHP;

    public void TakeDamage(float amount)
    {
        if (IsDead || IsInvulnerable) return;
        currentHP = Mathf.Max(0f, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
        if (currentHP <= 0f) OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }
}
