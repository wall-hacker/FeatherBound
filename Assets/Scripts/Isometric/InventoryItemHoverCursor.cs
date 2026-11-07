using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InventoryItemHoverCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    private GameObject tooltipPanel;
    private TextMeshProUGUI tooltipText;

    void Awake()
    {
        // seach whole scene for object with name TooltipPanel
        Debug.LogWarning("TooltipPanel BBBBBBBBBBB.");
        tooltipPanel = GetComponentInParent<Canvas>(true)
    .transform.Find("TooltipPanel")?.gameObject;
        if (tooltipPanel != null)
        {
            Debug.LogWarning("TooltipPanel AAAAAAAAAAAa.");
            tooltipText = tooltipPanel.GetComponentInChildren<TextMeshProUGUI>();
            tooltipPanel.SetActive(false);
        } else
        {
            Debug.LogWarning("TooltipPanel not found in scene.");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // get parent InventorySlot
        InventorySlot slot = GetComponentInParent<InventorySlot>();
        if (slot != null && slot.item != null)
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(true);
                if (tooltipText != null)
                {
                    tooltipText.text = $"{slot.item.itemName}\n<color=#AAAAAA><size=75>{slot.item.itemDescription}</size></color>";
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}
