using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Selectable firstSelected;

    [Header("Splash Phase")]
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private RectTransform titleSplashPosition;
    [SerializeField] private CanvasGroup pressStartCanvasGroup;
    [SerializeField] private GameObject buttonsRoot;
    [SerializeField] private float titleMoveDuration = 0.5f;
    [SerializeField] private float pressStartFadeDuration = 1f;

    [Header("Controls Overlay")]
    [SerializeField] private GameObject controlSchemeRoot;

    [Header("Audio")]
    [SerializeField] private AudioClip buttonSelectSfx;
    [SerializeField] private AudioClip buttonConfirmSfx;

    private InputAction continueAction;
    private Vector2 titleMenuAnchoredPosition;
    private bool menuShown;
    private bool controlsShown;
    private Coroutine pressStartRoutine;
    private Coroutine titleMoveRoutine;
    private AudioSource audioSource;
    private GameObject lastSelectedGameObject;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        continueAction = new InputAction(type: InputActionType.Button);
        continueAction.AddBinding("<Keyboard>/enter");
        continueAction.AddBinding("<Gamepad>/buttonSouth");
        continueAction.AddBinding("<Gamepad>/start");

        titleMenuAnchoredPosition = titleRect.anchoredPosition;

        if (titleSplashPosition != null)
        {
            titleRect.anchoredPosition = titleSplashPosition.anchoredPosition;
        }

        buttonsRoot.SetActive(false);
    }

    private void OnEnable()
    {
        continueAction.Enable();
        continueAction.performed += OnContinuePerformed;
        pressStartRoutine = StartCoroutine(PulsePressStart());
    }

    private void OnDisable()
    {
        continueAction.performed -= OnContinuePerformed;
        continueAction.Disable();

        if (pressStartRoutine != null)
        {
            StopCoroutine(pressStartRoutine);
        }
    }

    private void Update()
    {
        if (!menuShown || controlsShown || EventSystem.current == null)
        {
            return;
        }

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null)
        {
            SelectFirstButton();
            return;
        }

        if (currentSelected != lastSelectedGameObject)
        {
            PlaySfx(buttonSelectSfx);
            lastSelectedGameObject = currentSelected;
        }
    }

    private void OnContinuePerformed(InputAction.CallbackContext ctx)
    {
        if (!menuShown)
        {
            PlaySfx(buttonConfirmSfx);
            ShowMenu();
            return;
        }

        if (controlsShown)
        {
            PlaySfx(buttonConfirmSfx);
            HideControls();
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void ShowMenu()
    {
        menuShown = true;

        if (pressStartRoutine != null)
        {
            StopCoroutine(pressStartRoutine);
        }
        pressStartCanvasGroup.gameObject.SetActive(false);

        if (titleMoveRoutine != null)
        {
            StopCoroutine(titleMoveRoutine);
        }
        titleMoveRoutine = StartCoroutine(MoveTitleToMenuPosition());
    }

    private IEnumerator MoveTitleToMenuPosition()
    {
        Vector2 start = titleRect.anchoredPosition;
        float t = 0f;

        while (t < titleMoveDuration)
        {
            t += Time.deltaTime;
            titleRect.anchoredPosition = Vector2.Lerp(start, titleMenuAnchoredPosition, t / titleMoveDuration);
            yield return null;
        }

        titleRect.anchoredPosition = titleMenuAnchoredPosition;

        buttonsRoot.SetActive(true);
        SelectFirstButton();
    }

    private IEnumerator PulsePressStart()
    {
        while (true)
        {
            float t = 0f;
            while (t < pressStartFadeDuration)
            {
                t += Time.deltaTime;
                pressStartCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t / pressStartFadeDuration);
                yield return null;
            }

            t = 0f;
            while (t < pressStartFadeDuration)
            {
                t += Time.deltaTime;
                pressStartCanvasGroup.alpha = Mathf.SmoothStep(1f, 0f, t / pressStartFadeDuration);
                yield return null;
            }
        }
    }

    private void SelectFirstButton()
    {
        if (firstSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
            lastSelectedGameObject = firstSelected.gameObject;
        }
    }

    public void StartGame()
    {
        PlaySfx(buttonConfirmSfx);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void ExitGame()
    {
        PlaySfx(buttonConfirmSfx);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void LoadCredits()
    {
        PlaySfx(buttonConfirmSfx);
        SceneManager.LoadScene("Credits");
    }

    public void ShowControls()
    {
        if (!menuShown || controlsShown) return;

        PlaySfx(buttonConfirmSfx);
        controlsShown = true;
        controlSchemeRoot.SetActive(true);
        buttonsRoot.SetActive(false);
        titleRect.gameObject.SetActive(false);
    }

    private void HideControls()
    {
        controlsShown = false;
        controlSchemeRoot.SetActive(false);
        buttonsRoot.SetActive(true);
        titleRect.gameObject.SetActive(true);
        SelectFirstButton();
    }
}
