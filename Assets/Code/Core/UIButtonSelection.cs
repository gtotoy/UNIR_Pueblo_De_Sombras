using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonSelection : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite selectedSprite;

    private Image buttonImage;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage != null && defaultSprite != null)
        {
            buttonImage.sprite = defaultSprite;
        }
    }

    private void Start()
    {
        if (EventSystem.current.currentSelectedGameObject == gameObject)
        {
            buttonImage.sprite = selectedSprite;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (buttonImage != null && selectedSprite != null)
        {
            buttonImage.sprite = selectedSprite;
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (buttonImage != null && defaultSprite != null)
        {
            buttonImage.sprite = defaultSprite;
        }
    }
}
