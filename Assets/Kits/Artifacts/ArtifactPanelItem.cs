using UnityEngine;

public class ArtifactPanelItem : MonoBehaviour
{
    public void SetArtifact(ArtifactDefinition artifact, bool isActive)
    {
        var image = GetComponent<UnityEngine.UI.Image>();
        if (isActive)
        {
            Debug.Assert(artifact != null, "ArtifactDefinition is null.");
            image.sprite = artifact.Image;
            image.gameObject.SetActive(true);
        }
        else
        {
            image.gameObject.SetActive(false);
        }
    }

    public void SetSelected(bool isSelected)
    {
        var image = GetComponent<UnityEngine.UI.Image>();
        if (isSelected)
        {
            image.color = Color.yellow; // Highlight color
        }
        else
        {
            image.color = Color.white; // Default color
        }
    }
}
