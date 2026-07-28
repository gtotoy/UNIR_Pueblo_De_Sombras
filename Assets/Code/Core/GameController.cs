using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    public bool Autoplay = true;
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
            // Place tower logic here
            Debug.Log("Tower placed!");
            SetState(State.Wave);
        }
    }
}
