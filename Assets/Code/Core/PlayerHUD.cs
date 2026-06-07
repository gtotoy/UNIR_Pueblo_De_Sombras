using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Health playerHealth;
    [SerializeField] Image healthBarFill;
    [SerializeField] TMP_Text healthText;

    void OnEnable()
    {
        if (playerHealth == null) return;
        playerHealth.OnHealthChanged += UpdateHealthBar;
    }

    void Start()
    {
        if (playerHealth != null)
            UpdateHealthBar(playerHealth.CurrentHP, playerHealth.MaxHP);
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    void UpdateHealthBar(float current, float max)
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = max > 0f ? current / max : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
