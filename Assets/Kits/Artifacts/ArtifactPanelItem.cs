using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactPanelItem : MonoBehaviour
{
    public Button artifactButton;
    public Image artifactImage;
    public TextMeshProUGUI artifactCountText;
    public Color DefaultColor = Color.white;
    public Color SelectedColor = Color.yellow;

    private int artifactIndex;

    public void OnEnable()
    {
        artifactButton.onClick.AddListener(OnArtifactButtonClicked);
    }

    public void OnDisable()
    {
        artifactButton.onClick.RemoveListener(OnArtifactButtonClicked);
    }

    public void SetArtifact(int index, ArtifactDefinition artifact, bool isActive)
    {
        artifactIndex = index;
        var image = artifactImage;
        if (isActive)
        {
            Debug.Assert(artifact != null, "ArtifactDefinition is null.");
            image.sprite = artifact.Image;
            artifactButton.gameObject.SetActive(true);
            artifactCountText.text = $"x{artifact.GetRemainingCount()}";
        }
        else
        {
            artifactButton.gameObject.SetActive(false);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            artifactButton.image.color = SelectedColor;
        }
        else
        {
            artifactButton.image.color = DefaultColor;
        }
    }

    private void OnArtifactButtonClicked()
    {
        var gameController = FindFirstObjectByType<GameController>();
        if (gameController) {
            gameController.SelectArtifactAt(artifactIndex);
        }
    }
}
