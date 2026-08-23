using UnityEngine;

[CreateAssetMenu(fileName = "BlessingDefinition", menuName = "Scriptable Objects/BlessingDefinition")]
public class BlessingDefinition : ScriptableObject
{
    public Sprite Image;
    public string Title;
    public string Description;
    public float ParryHealthRecoveryPercentage = 1.0f;
    public float ArtifactDurationMultiplier = 1.0f;
}
