using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public PlayerHUD playerHUD;
    [SerializeField] InputActionReference pauseIARef;

    public static bool IsPaused = false;

    [SerializeField] GameObject pauseMenuUI;
    [SerializeField] private Selectable pauseFirstSelected;

    private void OnEnable()
    {
        pauseIARef.action.Enable();
        pauseIARef.action.performed += Pause;
    }

    private void OnDisable()
    {
        pauseIARef.action.Disable();
        pauseIARef.action.performed -= Pause;
    }

    private void Update()
    {
        if (IsPaused && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            SelectPauseFirstButton();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
    }

    void Pause(InputAction.CallbackContext obj)
    {
        if (WaveManager.Instance.IsFinished) return;

        if (IsPaused) Resume();
        else
        {
            pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            IsPaused = true;
            SelectPauseFirstButton();
        }

    }

    private void SelectPauseFirstButton()
    {
        if (pauseFirstSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(pauseFirstSelected.gameObject);
        }
    }

    public void LoadMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        IsPaused = false;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void EnterPreparation(ArtifactDefinition[] artifacts)
    {
        playerHUD.artifactsParent.gameObject.SetActive(true);
        playerHUD.UpdateArtifacts(artifacts);
    }

    public void EnterWave()
    {
        playerHUD.artifactsParent.gameObject.SetActive(false);
    }
}