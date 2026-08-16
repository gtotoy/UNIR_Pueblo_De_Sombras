using UnityEngine;

public class BlessingsPanel : MonoBehaviour
{
    [SerializeField] GameObject blessingItemsParent;

    private BlessingPanelItem[] blessingItems;
    private GameController gameController;
    private int selectedBlessingIndex = -1;

    public int GetSelectedBlessingIndex() => selectedBlessingIndex;

    private void OnEnable()
    {
        blessingItems = blessingItemsParent.GetComponentsInChildren<BlessingPanelItem>(true);

        gameController = FindFirstObjectByType<GameController>();
        selectedBlessingIndex = gameController.GetEquippedBlessingIndex();
        if (selectedBlessingIndex < 0 || selectedBlessingIndex >= blessingItems.Length) {
            selectedBlessingIndex = 0;
        }
        UpdateBlessings(gameController.GetBlessings());
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
                item.SetSelected(i == selectedBlessingIndex);
            }
            else
            {
                item.SetBlessing(i, null, false);
            }
        }
    }

    public void SelectNextBlessing(int step)
    {
        if (blessingItems.Length == 0) { return; }
        selectedBlessingIndex += step;
        if (selectedBlessingIndex < 0) {
            selectedBlessingIndex = blessingItems.Length - 1;
        }
        else if (selectedBlessingIndex >= blessingItems.Length) {
            selectedBlessingIndex = 0;
        }
        for (int i = 0; i < blessingItems.Length; i += 1)
        {
            var item = blessingItems[i];
            item.SetSelected(i == selectedBlessingIndex);
        }
    }
}
