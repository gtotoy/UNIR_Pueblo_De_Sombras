using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] InputActionReference pauseIARef;

    public static bool IsPaused = false;

    [SerializeField] GameObject pauseMenuUI;

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

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
    }

    void Pause(InputAction.CallbackContext obj)
    {
        if (WaveManager.IsFinished) return;

        if (IsPaused) Resume();
        else
        {
            pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            IsPaused = true;
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
}