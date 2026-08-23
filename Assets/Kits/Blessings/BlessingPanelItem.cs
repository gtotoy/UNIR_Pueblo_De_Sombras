using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlessingPanelItem : MonoBehaviour
{
    public Button blessingButton;
    public Image blessingImage;
    public TextMeshProUGUI blessingTitleText;
    public Color DefaultColor = Color.white;
    public Color SelectedColor = Color.yellow;

    private int blessingIndex;

    public void OnEnable()
    {
        blessingButton.onClick.AddListener(OnBlessingButtonClicked);
    }

    public void OnDisable()
    {
        blessingButton.onClick.RemoveListener(OnBlessingButtonClicked);
    }

    private void OnBlessingButtonClicked()
    {
        var gameController = FindFirstObjectByType<GameController>();
        if (gameController)
        {
            gameController.EquipBlessingAt(blessingIndex);
        }
    }

    public void SetBlessing(int index, BlessingDefinition blessing, bool isActive)
    {
        blessingIndex = index;
        var image = blessingImage;
        if (isActive)
        {
            Debug.Assert(blessing, "BlessingDefinition is null.");
            image.sprite = blessing.Image;
            blessingButton.gameObject.SetActive(true);
            blessingTitleText.text = $"{blessing.Title}";
        }
        else
        {
            blessingButton.gameObject.SetActive(false);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            blessingButton.image.color = SelectedColor;
        }
        else
        {
            blessingButton.image.color = DefaultColor;
        }
    }
}
