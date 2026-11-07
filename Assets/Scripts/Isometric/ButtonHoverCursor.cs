using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CursorManager cursorManager;

    void Awake()
    {
        if (cursorManager == null)
        {
            cursorManager = FindObjectOfType<CursorManager>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // if gameobject is button and interactable == false return
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null && !button.interactable)
        {
            return;
        }

        cursorManager.SetHoverCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cursorManager.SetDefaultCursor();
    }
}
