using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image healthBarFill;
    [SerializeField] TMP_Text healthText;
    public Image artifactsParent;
    [SerializeField] Image blessingImage;

    private Health playerHealth;
    private ArtifactPanelItem[] artifactItems;

    void OnEnable()
    {
        {
            var playerController = FindFirstObjectByType<PlayerCharacterController>();
            if (playerController)
            {
                playerHealth = playerController.GetComponent<Health>();
                Debug.Assert(playerHealth, "Player's Health component not found.");
                playerHealth.OnHealthChanged += UpdateHealthBar;
            }
        }

        artifactItems = artifactsParent.GetComponentsInChildren<ArtifactPanelItem>(true);
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

    public void UpdateArtifacts(ArtifactDefinition[] artifacts)
    {
        if (artifactItems == null || artifactItems.Length == 0) return;
        for (int i = 0; i < artifactItems.Length; i++)
        {
            if (i < artifacts.Length && artifacts[i] != null)
            {
                artifactItems[i].SetArtifact(i, artifacts[i], true);
            }
            else
            {
                artifactItems[i].SetArtifact(i, null, false);
            }
        }
    }

    public void UpdateSelectedArtifact(int selectedIndex)
    {
        if (artifactItems == null || artifactItems.Length == 0) return;
        for (int i = 0; i < artifactItems.Length; i++)
        {
            artifactItems[i].SetSelected(i == selectedIndex);
        }
    }

    public void UpdateBlessing(Sprite blessingSprite)
    {
        if (blessingImage) {
            blessingImage.sprite = blessingSprite;
            blessingImage.enabled = blessingSprite != null;
        }
    }
}
