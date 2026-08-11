using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private InputAction continueAction;
    private Vector2 titleMenuAnchoredPosition;
    private bool menuShown;
    private bool controlsShown;
    private Coroutine pressStartRoutine;
    private Coroutine titleMoveRoutine;

    private void Awake()
    {
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
        if (menuShown && !controlsShown && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            SelectFirstButton();
        }
    }

    private void OnContinuePerformed(InputAction.CallbackContext ctx)
    {
        if (!menuShown)
        {
            ShowMenu();
            return;
        }

        if (controlsShown)
        {
            HideControls();
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
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void LoadCredits()
    {
        SceneManager.LoadScene("Credits");
    }

    public void ShowControls()
    {
        if (!menuShown || controlsShown) return;

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
