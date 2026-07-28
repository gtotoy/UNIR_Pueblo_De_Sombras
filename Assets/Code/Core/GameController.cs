using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    public bool Autoplay = true;
    [Header("Artifacts")]
    [SerializeField] ArtifactDefinition[] Artifacts;
    [SerializeField] int SelectedArtifactIndex = 0;

    [SerializeField] State currentState;

    public enum State
    {
        None,
        Preparation,
        Wave,
        Boss,
    }

    public void Start()
    {
        if (!Autoplay) { return; }

        currentState = State.Preparation;
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Preparation:
                break;
            case State.Wave:
                break;
        }
    }

    void SetState(State newState)
    {
        var prevState = currentState;
        currentState = newState;
        switch(currentState)
        {
            case State.Preparation:
                Debug.Log("Entering Preparation state.");
                break;
            case State.Wave:
                Debug.Log("Entering Wave state.");
                WaveManager.Instance.StartWave();
                break;
            case State.Boss:
                Debug.Log("Entering Boss state.");
                break;
        }
    }

    public void OnPlace(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (currentState == State.Preparation)
        {
            var playerController = FindFirstObjectByType<PlayerCharacterController>();
            Debug.Assert(playerController != null, "PlayerCharacterController not found in the scene.");
            if (SelectedArtifactIndex < Artifacts.Length)
            {
                var artifactPosition = playerController.transform.position + 2.0f * playerController.transform.forward;
                var placed = Artifacts[SelectedArtifactIndex].PlaceArtifact(artifactPosition);
                if (placed)
                {
                    bool allRequiredArtifactsPlaced = false;
                    foreach (var artifact in Artifacts)
                    {
                        if (artifact.IsRequired && artifact.GetRemainingCount() > 0)
                        {
                            allRequiredArtifactsPlaced = false;
                            break;
                        }
                        allRequiredArtifactsPlaced = true;
                    }

                    if (allRequiredArtifactsPlaced)
                    {
                        SetState(State.Wave);
                    }
                }
            }
        }
    }
}
