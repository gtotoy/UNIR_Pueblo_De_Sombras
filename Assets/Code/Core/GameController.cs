using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    public bool Autoplay = true;
    [Header("Artifacts")]
    [SerializeField] ArtifactDefinition[] Artifacts;
    [SerializeField] int SelectedArtifactIndex = 0;
    [Header("Blessings")]
    [SerializeField] BlessingDefinition[] Blessings;
    [SerializeField] int EquippedBlessingIndex = -1;

    public BlessingDefinition[] GetBlessings() => Blessings;
    public int GetEquippedBlessingIndex() => EquippedBlessingIndex;

    [Header("Waves")]
    [SerializeField] int TotalWaves = 3;
    [SerializeField] int CurrentWave = 0;

    [SerializeField] State currentState;
    private GameUIManager gameUIManager;
    private float parryHealthRecoveryPercentage = 1.0f;
    private float artifactDurationMultiplier = 1.0f;

    public float GetParryHealthRecoveryPercentage() => parryHealthRecoveryPercentage;
    public float GetArtifactDurationMultiplier() => artifactDurationMultiplier;

    public enum State
    {
        None,
        Preparation,
        BlessingSelection,
        Wave,
        Boss,
    }

    public void Awake()
    {
        gameUIManager = FindFirstObjectByType<GameUIManager>();
        if (gameUIManager == null)
        {
            Debug.LogError($"{nameof(GameUIManager)} not found in the scene.");
        }
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
                gameUIManager.EnterPreparation(Artifacts);
                gameUIManager.playerHUD.UpdateSelectedArtifact(SelectedArtifactIndex);
                break;
            case State.BlessingSelection:
                gameUIManager.playerHUD.artifactsParent.gameObject.SetActive(false);
                gameUIManager.blessingsPanel.gameObject.SetActive(true);
                break;
            case State.Wave:
                Debug.Log("Entering Wave state.");
                gameUIManager.blessingsPanel.gameObject.SetActive(false);
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

    public void TryPlaceArtifact(Vector3 artifactPosition, Vector3 forward, Vector3 up)
    {
        switch (currentState)
        {
            case State.Preparation:
                if (SelectedArtifactIndex < Artifacts.Length)
                {
                    var placed = Artifacts[SelectedArtifactIndex].PlaceArtifact(artifactPosition, forward, up);
                    if (placed)
                    {
                        gameUIManager.playerHUD.UpdateArtifacts(Artifacts);
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
                            SetState(State.BlessingSelection);
                        }
                    }
                }
                break;
            case State.BlessingSelection:
                EquipBlessingAt(gameUIManager.blessingsPanel.GetSelectedBlessingIndex());
                break;
        }
    }

    public void SelectNextArtifact(int step)
    {
        var index = SelectedArtifactIndex + step;
        SelectArtifactAt(index);
    }

    public void SelectArtifactAt(int index)
    {
        if (index < 0) {
            index = Artifacts.Length + index;
        }
        else if (index >= Artifacts.Length) {
            index = index - Artifacts.Length;
        }
        SelectedArtifactIndex = index;
        gameUIManager.playerHUD.UpdateSelectedArtifact(SelectedArtifactIndex);
        //Debug.Log($"Selected artifact: {Artifacts[SelectedArtifactIndex].name}");
    }

    public void EquipBlessingAt(int blessingIndex)
    {
        if (blessingIndex < 0 || blessingIndex >= Blessings.Length) {
            Debug.LogError($"Invalid blessing index: {blessingIndex}");
            return;
        }

        if (blessingIndex != EquippedBlessingIndex)
        {
            Debug.Log($"Equipping blessing: {Blessings[blessingIndex].Title}");
            parryHealthRecoveryPercentage = Blessings[blessingIndex].ParryHealthRecoveryPercentage;
            artifactDurationMultiplier = Blessings[blessingIndex].ArtifactDurationMultiplier;
            EquippedBlessingIndex = blessingIndex;
            gameUIManager.playerHUD.UpdateBlessing(Blessings[blessingIndex].Image);
            SetState(State.Wave);
        }
    }
}
