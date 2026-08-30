using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TowerHealthBar : MonoBehaviour
{
    public Slider slider;

    private Health health;

    public void OnEnable()
    {
        var artifacts = FindObjectsByType<Artifact>(FindObjectsSortMode.None);
        health = artifacts.First(x => x.GetComponent<Health>()).GetComponent<Health>();
        health.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(health.CurrentHP, health.MaxHP);
    }

    public void OnDisable()
    {
        health.OnHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (slider != null)
            slider.value = max > 0f ? current / max : 0f;
    }
}
