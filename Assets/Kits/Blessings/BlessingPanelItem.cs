using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlessingPanelItem : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public Button blessingButton;
    public Image blessingImage;
    public TextMeshProUGUI blessingTitleText;

    private int blessingIndex;

    public void OnEnable()
    {
        blessingButton.onClick.AddListener(OnBlessingButtonClicked);
    }

    public void OnDisable()
    {
        blessingButton.onClick.RemoveListener(OnBlessingButtonClicked);
    }

	public void OnSelect(BaseEventData eventData)
	{
   		var blessingsPanel = GetComponentInParent<BlessingsPanel>();

    	if (blessingsPanel)
        	blessingsPanel.OnBlessingButtonSelected(blessingIndex);
	}

    private void OnBlessingButtonClicked()
    {
        var blessingsPanel = GetComponentInParent<BlessingsPanel>();

		if (blessingsPanel)
			blessingsPanel.SelectBlessingFromMouse(blessingIndex);
    }

	public void OnPointerEnter(PointerEventData eventData)
	{
		var blessingsPanel = GetComponentInParent<BlessingsPanel>();

		if (blessingsPanel)
			blessingsPanel.SelectBlessingFromMouse(blessingIndex);
	}

    public void SetBlessing(int index, BlessingDefinition blessing, bool isActive)
    {
        blessingIndex = index;
        var image = blessingImage;
        if (isActive)
        {
            Debug.Assert(blessing, "BlessingDefinition is null.");
            image.sprite = blessing.Image;
            blessingButton.gameObject.SetActive(true);
            blessingTitleText.text = $"{blessing.Title}";
        }
        else
        {
            blessingButton.gameObject.SetActive(false);
        }
    }
}
