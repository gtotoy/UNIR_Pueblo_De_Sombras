using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    public bool Autoplay = true;
    [Header("Artifacts")]
    [SerializeField] ArtifactDefinition[] Artifacts;
    [SerializeField] int SelectedArtifactIndex = 0;

    [Header("Waves")]
    [SerializeField] int TotalWaves = 3;
    [SerializeField] int CurrentWave = 0;

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

        SetState(State.Preparation);
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Preparation:
                break;
            case State.Wave:
                if (WaveManager.Instance.IsFinished)
                {
                    if (CurrentWave < TotalWaves)
                    {
                        SetState(State.Preparation);
                    }
                    else
                    {
                        SetState(State.Boss);
                    }
                }
                break;
        }
    }

    void SetState(State newState)
    {
        var prevState = currentState;
        currentState = newState;
        switch(currentState)
        {
            case State.None:
                break;
            case State.Preparation:
                Debug.Log("Entering Preparation state.");
                foreach (var artifact in Artifacts) {
                    artifact.Reset();
                }
                {
                    var playerController = FindFirstObjectByType<PlayerCharacterController>();
                    var playerInput = playerController.GetComponent<PlayerInput>();
                    playerInput.actions.FindActionMap("Preparation").Enable();
                    playerInput.actions.FindActionMap("Attack").Disable();
                }
                break;
            case State.Wave:
                Debug.Log("Entering Wave state.");
                {
                    var playerController = FindFirstObjectByType<PlayerCharacterController>();
                    var playerInput = playerController.GetComponent<PlayerInput>();
                    playerInput.actions.FindActionMap("Preparation").Disable();
                    playerInput.actions.FindActionMap("Attack").Enable();
                }
                WaveManager.Instance.StartWave();
                CurrentWave += 1;
                break;
            case State.Boss:
                Debug.Log("Entering Boss state.");
                {
                    var playerController = FindFirstObjectByType<PlayerCharacterController>();
                    var playerInput = playerController.GetComponent<PlayerInput>();
                    playerInput.actions.FindActionMap("Preparation").Disable();
                    playerInput.actions.FindActionMap("Attack").Enable();
                }
                break;
        }
    }

    public void TryPlaceArtifact(Vector3 artifactPosition)
    {
        if (currentState == State.Preparation)
        {
            if (SelectedArtifactIndex < Artifacts.Length)
            {
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
