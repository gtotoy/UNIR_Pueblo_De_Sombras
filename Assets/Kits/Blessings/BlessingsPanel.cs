using UnityEngine;
using UnityEngine.EventSystems;

public class BlessingsPanel : MonoBehaviour
{
    [SerializeField] GameObject blessingItemsParent;

    private BlessingPanelItem[] blessingItems;
    private GameController gameController;
    private int selectedBlessingIndex = -1;
    private int navigationBlessingIndex;
    private int mouseBlessingIndex;

    public int GetSelectedBlessingIndex() => selectedBlessingIndex;

    private void OnEnable()
    {
        blessingItems = blessingItemsParent.GetComponentsInChildren<BlessingPanelItem>(true);

        gameController = FindFirstObjectByType<GameController>();
        selectedBlessingIndex = gameController.GetEquippedBlessingIndex();

        if (selectedBlessingIndex < 0 || selectedBlessingIndex >= blessingItems.Length)
            selectedBlessingIndex = 0;

        UpdateBlessings(gameController.GetBlessings());

        navigationBlessingIndex = selectedBlessingIndex;
        mouseBlessingIndex = navigationBlessingIndex;

        if (InputDeviceManager.Instance != null)
            InputDeviceManager.Instance.OnInputDeviceChanged += OnInputDeviceChanged;

        UpdateBlessings(gameController.GetBlessings());
        SelectCurrentDeviceButton();
    }

    private void OnDisable()
    {
        if (InputDeviceManager.Instance != null)
            InputDeviceManager.Instance.OnInputDeviceChanged -= OnInputDeviceChanged;
    }

    public void UpdateBlessings(BlessingDefinition[] blessings)
    {
        for (int i = 0; i < blessingItems.Length; i += 1)
        {
            var item = blessingItems[i];

            if (i < blessings.Length)
            {
                var blessing = blessings[i];
                item.SetBlessing(i, blessing, true);
            }
            else
                item.SetBlessing(i, null, false);
        }
    }

    public void SelectNextBlessing(int step)
    {
        navigationBlessingIndex += step;

        if (navigationBlessingIndex < 0)
        {
            navigationBlessingIndex = blessingItems.Length - 1;
        }
        else if (navigationBlessingIndex >= blessingItems.Length)
        {
            navigationBlessingIndex = 0;
        }

        SelectButton(navigationBlessingIndex);
    }

    private void SelectButton(int index)
    {
        if (blessingItems == null || index < 0 || index >= blessingItems.Length)
            return;

        selectedBlessingIndex = index;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(blessingItems[index].blessingButton.gameObject);
    }

    public void SelectBlessingFromMouse(int index)
    {
        if (index < 0 || index >= blessingItems.Length)
            return;

        mouseBlessingIndex = index;

        if (InputDeviceManager.Instance == null)
            return;

        if (InputDeviceManager.Instance.CurrentDevice == InputDeviceType.Mouse)
            SelectButton(mouseBlessingIndex);
    }

    private void OnInputDeviceChanged(InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputDeviceType.Gamepad:
            case InputDeviceType.Keyboard:
                SelectButton(navigationBlessingIndex);
                break;
            case InputDeviceType.Mouse:
                SelectButton(mouseBlessingIndex);
                break;
        }
    }

    private void SelectCurrentDeviceButton()
    {
        if (InputDeviceManager.Instance == null)
            return;

        switch (InputDeviceManager.Instance.CurrentDevice)
        {
            case InputDeviceType.Gamepad:
            case InputDeviceType.Keyboard:
                SelectButton(navigationBlessingIndex);
                break;
            
            case InputDeviceType.Mouse:
                SelectButton(mouseBlessingIndex);
                break;
        }
    }
    
    public void OnBlessingButtonSelected(int index)
    {
        if (index < 0 || index >= blessingItems.Length)
        {
            return;
        }

        switch (InputDeviceManager.Instance.CurrentDevice)
        {
            case InputDeviceType.Gamepad:
            case InputDeviceType.Keyboard:
                navigationBlessingIndex = index;
                selectedBlessingIndex = index;
                break;

            case InputDeviceType.Mouse:
                mouseBlessingIndex = index;
                selectedBlessingIndex = index;
                break;
        }
    }
}
