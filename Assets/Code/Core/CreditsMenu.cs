using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CreditsMenu : MonoBehaviour
{
    private InputAction backAction;

    private void Awake()
    {
        backAction = new InputAction(type: InputActionType.Button);
        backAction.AddBinding("<Keyboard>/escape");
        backAction.AddBinding("<Gamepad>/buttonEast");
    }

    private void OnEnable()
    {
        backAction.Enable();
        backAction.performed += OnBackPerformed;
    }

    private void OnDisable()
    {
        backAction.performed -= OnBackPerformed;
        backAction.Disable();
    }

    private void OnBackPerformed(InputAction.CallbackContext ctx)
    {
        BackToMainMenu();
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
