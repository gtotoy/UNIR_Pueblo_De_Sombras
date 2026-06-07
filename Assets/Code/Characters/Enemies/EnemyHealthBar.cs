using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] Image healthBarFill;
    [SerializeField] bool faceCamera = true;

    Health enemyHealth;
    Transform cam;

    void Awake()
    {
        enemyHealth = GetComponentInParent<Health>();
        if (enemyHealth == null)
        {
            Debug.LogWarning($"[EnemyHealthBar] No se encontró Health en {transform.root.name}");
            return;
        }

        enemyHealth.OnHealthChanged += UpdateBar;
        enemyHealth.OnDeath += HideBar;
    }

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;

        if (enemyHealth != null)
            UpdateBar(enemyHealth.CurrentHP, enemyHealth.MaxHP);
    }

    void LateUpdate()
    {
        if (!faceCamera || cam == null) return;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
    }

    void OnDestroy()
    {
        if (enemyHealth == null) return;
        enemyHealth.OnHealthChanged -= UpdateBar;
        enemyHealth.OnDeath -= HideBar;
    }

    void UpdateBar(float current, float max)
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = max > 0f ? current / max : 0f;
    }

    void HideBar()
    {
        gameObject.SetActive(false);
    }
}
