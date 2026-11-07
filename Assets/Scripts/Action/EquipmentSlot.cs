using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EquipmentSlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Slot Settings")]
    [SerializeField] private EquipmentSlotType slotType;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image slotTypeIcon; // Shows what can go here
    
    public Item equippedItem { get; private set; }

    private Canvas canvas;    
    private GameObject draggedIcon;
    private Item draggedItem;   

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        Clear();
    }

    public bool TryEquip(Item item, bool force = false)
    {
        if (item == null) return false;
        
        // Check if item can go in this slot
        if (item.equipSlot != slotType)
        {
            Debug.Log($"Cannot equip {item.itemName} in {slotType} slot!");
            return false;
        }

        // Special check for companion slot during zombie and collection scenes
        if (item.equipSlot == EquipmentSlotType.Companion && EquipmentSystem.Instance.isCompanionSlotLocked && !force)
        {
            return false;
        }
        
        // Unequip current item if any
        if (equippedItem != null)
        {
            Unequip();
        }
        
        // Equip new item
        equippedItem = item;
        iconImage.sprite = item.icon;
        iconImage.enabled = true;
        iconImage.color = Color.white;
        
        // Apply stats
        EquipmentSystem.Instance.ApplyItemStats(item, true);
        
        // Remove from inventory
        InventorySystem.Instance.RemoveItem(item);

        // Hide slot type icon
        if (slotTypeIcon != null)
            slotTypeIcon.enabled = false;

        // Special handling for companion slot
        if (item.equipSlot == EquipmentSlotType.Companion)
        {
            CompanionSystem.Instance.UpdateCompanionUI();
        }

        if (item.equipSlot == EquipmentSlotType.Weapon && WeaponShooting.Instance != null)
        {
            WeaponShooting.Instance.ToggleWeapon();
            WeaponShooting.Instance.ToggleWeapon();
        }
        
        return true;
    }

    public void Unequip()
    {
        if (equippedItem == null) return;

        if (equippedItem.equipSlot == EquipmentSlotType.Companion && EquipmentSystem.Instance.isCompanionSlotLocked)
            return;
        
        // Return to inventory
        InventorySystem.Instance.AddItem(equippedItem);
        
        // Remove stats
        EquipmentSystem.Instance.ApplyItemStats(equippedItem, false);

        bool isCompanion = false;
        if (equippedItem.equipSlot == EquipmentSlotType.Companion)
            isCompanion = true;

        Clear();

        if (isCompanion)
        {
            CompanionSystem.Instance.UpdateCompanionUI();
        }
    }

    void Clear()
    {
        equippedItem = null;
        iconImage.enabled = false;
        
        // Show slot type icon
        if (slotTypeIcon != null)
            slotTypeIcon.enabled = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Get the dragged item
        InventorySlot draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlot>();
        if (draggedSlot != null && draggedSlot.item != null)
        {
            TryEquip(draggedSlot.item);
        }
    }

    // Drag and drop
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (equippedItem == null) return;

        Debug.Log($"Started dragging {equippedItem.itemName} from {slotType} slot.");
        
        // Create dragged icon
        draggedIcon = new GameObject("DraggedIcon");
        draggedIcon.transform.SetParent(canvas.transform);
        draggedIcon.transform.SetAsLastSibling();
        
        Image dragImage = draggedIcon.AddComponent<Image>();
        dragImage.sprite = equippedItem.icon;
        dragImage.raycastTarget = false;
        
        RectTransform rectTransform = draggedIcon.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150, 150);

        draggedItem = equippedItem;
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
            if ((targetSlot.item != null && targetSlot.item == equippedItem && equippedItem.stackable) || targetSlot.item == null)
            {
                Unequip();
                return;
            }
            if (TryEquip(targetSlot.item))
            {
                Unequip();
            }
            return;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Right click to unequip
        if (eventData.button == PointerEventData.InputButton.Right && equippedItem != null)
        {
            Unequip();
        }
    }
}