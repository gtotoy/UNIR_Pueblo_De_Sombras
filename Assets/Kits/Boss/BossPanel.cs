using UnityEngine;
using UnityEngine.UI;

public class BossPanel : MonoBehaviour
{
    public Image FillImage;
    private Boss boss;

    public void OnEnable()
    {
        boss = FindFirstObjectByType<Boss>();
        Debug.Assert(boss, "Boss not found in the scene.");
        boss.OnHealthChanged += UpdateHealthBar;
    }

    public void OnDisable()
    {
        if (boss)
        {
            boss.OnHealthChanged -= UpdateHealthBar;
        }
    }

    private void UpdateHealthBar(Boss boss)
    {
        FillImage.fillAmount = boss.TotalCurrentHealth / boss.TotalMaxHealth;
    }
}
