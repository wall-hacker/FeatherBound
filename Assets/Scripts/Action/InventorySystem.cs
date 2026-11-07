using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 30;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;
    
    InventoryEntry[] inventorySlots;
    private List<InventorySlot> slots = new List<InventorySlot>();
    
    private bool isInventoryOpen = false;

    // Buff
    private bool doublePickupActive = false; // is modified by buffs/debuffs

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        inventorySlots = new InventoryEntry[maxSlots];
        for (int i = 0; i < maxSlots; i++)
            inventorySlots[i] = new InventoryEntry(null, 0);

        InitializeSlots();
    }


    void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    void Update()
    {
        // Toggle inventory with I key
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (isInventoryOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleInventory();
            }
        }
    }

    void InitializeSlots()
    {
        if (slotsContainer == null || slotPrefab == null) return;
        
        // Create slots
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            slot.slotIndex = i;
            slots.Add(slot);
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        PlayerController playerController = FindObjectOfType<PlayerController>();
        WeaponShooting weaponShooting = FindObjectOfType<WeaponShooting>();
        PointClickController pointClickController = FindObjectOfType<PointClickController>();
        MouseHighlighter mouseHighlighter = FindObjectOfType<MouseHighlighter>();
        if (weaponShooting != null)
        {
            weaponShooting.enabled = !isInventoryOpen;
        }
        if (playerController != null)
        {
            playerController.enabled = !isInventoryOpen;
        }
        if (pointClickController != null)
        {
            pointClickController.enabled = !isInventoryOpen;
        }
        if (mouseHighlighter != null)
        {
            mouseHighlighter.enabled = !isInventoryOpen;
        }
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
            
        
        if (inventoryPanel != null)
            inventoryPanel.SetActive(isInventoryOpen);
        
        // Update cursor state
        Cursor.visible = isInventoryOpen;
        Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        
        if (isInventoryOpen)
            RefreshUI();
    }

    public bool AddItem(Item item, int quantity = 1)
    {
        if (item == null) return false;

        if (doublePickupActive)
        {
            quantity *= 2;
        }
            
        // Try stacking first
        if (item.stackable)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i].item == item)
                {
                    inventorySlots[i].quantity += quantity;
                    RefreshUI();
                    return true;
                }
            }
        }

        // Find empty slot
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].IsEmpty)
            {
                inventorySlots[i].item = item;
                inventorySlots[i].quantity = quantity;
                RefreshUI();
                return true;
            }
        }

        Debug.Log("Inventory full!");
        return false;
    }



    public void RemoveItem(Item item, int quantity = 1)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].item == item)
            {
                inventorySlots[i].quantity -= quantity;

                if (inventorySlots[i].quantity <= 0)
                    inventorySlots[i].Clear();

                RefreshUI();
                return;
            }
        }
    }


    public int GetItemCount(Item item)
    {
        int count = 0;
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].item == item)
            {
                count += inventorySlots[i].quantity;
            }
        }
        return count;
    }

    public int GetCoinsCount()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].item != null && inventorySlots[i].item.itemName == "Coins")
            {
                return inventorySlots[i].quantity;
            }
        }
        return 0;
    }

    public void RemoveCoins(int amount)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].item != null && inventorySlots[i].item.itemName == "Coins")
            {
                inventorySlots[i].quantity -= amount;
                if (inventorySlots[i].quantity <= 0)
                    inventorySlots[i].Clear();
                RefreshUI();
                return;
            }
        }
    }

    public void SwapSlots(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;

        InventoryEntry temp = inventorySlots[fromIndex];
        inventorySlots[fromIndex] = inventorySlots[toIndex];
        inventorySlots[toIndex] = temp;

        RefreshUI();
    }

    public void RefreshUI()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            InventoryEntry entry = inventorySlots[i];

            if (entry.IsEmpty)
                slots[i].Clear();
            else
                slots[i].SetItem(entry.item, entry.quantity);
        }
    }

    public bool IsInventoryOpen => isInventoryOpen;
    public int MaxSlots => inventorySlots.Length;

    public InventoryEntry GetEntry(int index) => inventorySlots[index];

    public void SetSlot(int index, Item item, int quantity)
    {
        inventorySlots[index].item = item;
        inventorySlots[index].quantity = quantity;
    }

    public void ClearAll()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
            inventorySlots[i].Clear();
    }

    // Buff
    public void SetDoublePickupActive(bool isActive)
    {
        doublePickupActive = isActive;
    }
}