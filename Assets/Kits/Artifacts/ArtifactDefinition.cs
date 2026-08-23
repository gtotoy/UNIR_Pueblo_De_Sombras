using UnityEngine;

[CreateAssetMenu(fileName = "ArtifactDefinition", menuName = "Scriptable Objects/ArtifactDefinition")]
public class ArtifactDefinition : ScriptableObject
{
    public Artifact Prefab;
    public int MaxCount;
    public bool IsRequired;
    public Sprite Image;
    public Vector3 PlacementOffset;

    private int Remaining;

    public void OnEnable()
    {
        Reset();
    }

    public bool PlaceArtifact(Vector3 position, Vector3 forward, Vector3 up)
    {
        Debug.Assert(Prefab != null);
        if (Remaining <= 0)
        {
            Debug.LogWarning($"No remaining artifacts of type {name} to place.");
            return false;
        }
        Instantiate(Prefab, position + PlacementOffset.z * forward + PlacementOffset.y * up, Quaternion.identity);
        Remaining -= 1;
        Debug.Log("Artifact placed!");
        return true;
    }

    public void Reset()
    {
        Remaining = MaxCount;
    }

    public int GetRemainingCount()
    {
        return Remaining;
    }
}
