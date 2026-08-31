using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class GameUIManager : MonoBehaviour
{
    public static bool IsPaused = false;

    [Header("Player HUD")]
    public PlayerHUD playerHUD;
    [SerializeField] InputActionReference pauseIARef;
    [Header("Blessing Selection")]
    public BlessingsPanel blessingsPanel;
    [Header("Pause Menu")]
    [SerializeField] GameObject pauseMenuUI;
    [SerializeField] private Selectable pauseFirstSelected;

    [Header("Audio")]
    [SerializeField] private AudioClip pauseToggleSfx;
    [SerializeField] private AudioClip buttonSelectSfx;
    [SerializeField] private AudioClip buttonConfirmSfx;

    private AudioSource audioSource;
    private GameObject lastSelectedGameObject;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

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
        if (!IsPaused || EventSystem.current == null)
        {
            return;
        }

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null)
        {
            SelectPauseFirstButton();
            return;
        }

        if (currentSelected != lastSelectedGameObject)
        {
            PlaySfx(buttonSelectSfx);
            lastSelectedGameObject = currentSelected;
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void Resume()
    {
        PlaySfx(pauseToggleSfx);
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
            PlaySfx(pauseToggleSfx);
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
            lastSelectedGameObject = pauseFirstSelected.gameObject;
        }
    }

    public void LoadMenu()
    {
        PlaySfx(buttonConfirmSfx);
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        PlaySfx(buttonConfirmSfx);
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
}