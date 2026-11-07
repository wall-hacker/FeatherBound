using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject highlightBorder;
    
    public Item item { get; private set; }
    public int quantity { get; private set; }
    public int slotIndex;
    
    private Canvas canvas;
    private GameObject draggedIcon;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (highlightBorder != null)
            highlightBorder.SetActive(false);
    }

    public void SetItem(Item newItem, int newQuantity = 1)
    {
        item = newItem;
        quantity = newQuantity;
        
        if (item != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
            iconImage.color = Color.white;
            
            if (item.stackable && quantity > 1)
            {
                quantityText.text = quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }
        else
        {
            Clear();
        }
    }

    public void AddQuantity(int amount)
    {
        quantity += amount;

        if (item != null && item.stackable)
        {
            if (quantity > 1)
            {
                quantityText.text = quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
        iconImage.sprite = null;
        iconImage.enabled = false;
        quantityText.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (item == null) return;
        
        // Right click to use/equip
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (item == null) return;

            item.Use();
        }
    }

    // Drag and drop
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;
        
        // Create dragged icon
        draggedIcon = new GameObject("DraggedIcon");
        draggedIcon.transform.SetParent(canvas.transform);
        draggedIcon.transform.SetAsLastSibling();
        
        Image dragImage = draggedIcon.AddComponent<Image>();
        dragImage.sprite = item.icon;
        dragImage.raycastTarget = false;
        
        RectTransform rectTransform = draggedIcon.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150, 150);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            draggedIcon.transform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
            Destroy(draggedIcon);

        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        if (target == null) return;

        // Inventory slot
        InventorySlot targetSlot = target.GetComponentInParent<InventorySlot>();
        if (targetSlot != null && targetSlot != this)
        {
            InventorySystem.Instance.SwapSlots(slotIndex, targetSlot.slotIndex);
            return;
        }

        // Equipment slot
        EquipmentSlot equipSlot = target.GetComponentInParent<EquipmentSlot>();
        if (equipSlot != null)
        {
            equipSlot.TryEquip(item);
        }
    }

    public void SetHighlight(bool active)
    {
        if (highlightBorder != null)
            highlightBorder.SetActive(active);
    }
}