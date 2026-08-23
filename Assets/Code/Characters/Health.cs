using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] float maxHP = 100f;
    float currentHP;

    [Header("Health Regain")]
    [Tooltip("Fraction of each hit's damage that becomes recoverable, e.g. 0.5 = 50%.")]
    [Range(0f, 1f)]
    [SerializeField] float regainPercent = 0.5f;
    [Tooltip("Seconds the player has to land a hit and regain the recoverable health before it's lost.")]
    [SerializeField] float regainWindow = 3f;

    float recoverableHP;
    float regainTimer;

    public event Action OnDeath;
    public event Action<float, float> OnHealthChanged;
    public event Action<float> OnRecoverableChanged;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public float RecoverableHP => recoverableHP;
    public bool IsDead => currentHP <= 0f;
    public bool IsInvulnerable { get; set; }

    void Awake() => currentHP = maxHP;

    void Update()
    {
        if (regainTimer <= 0f) return;
        regainTimer -= Time.deltaTime;
        if (regainTimer <= 0f) ClearRecoverable();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || IsInvulnerable) return;
        currentHP = Mathf.Max(0f, currentHP - amount);
        recoverableHP = Mathf.Min(maxHP - currentHP, recoverableHP + amount * regainPercent);
        regainTimer = regainWindow;
        OnHealthChanged?.Invoke(currentHP, maxHP);
        OnRecoverableChanged?.Invoke(recoverableHP);
        if (currentHP <= 0f) OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }
    public void RegainHealth(float? healthAmount = null)
    {
        if (healthAmount.HasValue)
        {
            if (healthAmount.Value <= 0f) return;
            currentHP = Mathf.Min(maxHP, currentHP + healthAmount.Value);
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }
        else
        {
            if (recoverableHP <= 0f) return;
            currentHP = Mathf.Min(maxHP, currentHP + recoverableHP);
            ClearRecoverable();
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }
    }

    void ClearRecoverable()
    {
        regainTimer = 0f;
        if (recoverableHP <= 0f) return;
        recoverableHP = 0f;
        OnRecoverableChanged?.Invoke(recoverableHP);
    }
}
