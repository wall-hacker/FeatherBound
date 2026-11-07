using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    // AFTER load
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedLoad());
    }

    // BEFORE unload
    void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"[SaveLoadManager] Scene unloading: {scene.name}");
        SaveGame();
    }

    // On application quit
    void OnApplicationQuit()
    {
        Debug.Log("[SaveLoadManager] Application quitting, saving game...");
        SaveGame();
    }

    IEnumerator DelayedLoad()
    {
        yield return null;
        LoadGame();
        Debug.Log($"[SaveLoadManager] Scene loaded: {SceneManager.GetActiveScene().name}");
    }

    // ================= SAVE =================
    public void SaveGame()
    {
        SaveData data = new SaveData();

        SaveInventory(data);
        SaveEquipment(data);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        Debug.Log("Game saved to " + SavePath);
    }

    // ================= LOAD =================
    public void LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("No save file found");
            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        LoadInventory(data);
        LoadEquipment(data);

        Debug.Log("Game loaded");
    }

        void SaveInventory(SaveData data)
    {
        var inventory = InventorySystem.Instance;

        for (int i = 0; i < inventory.MaxSlots; i++)
        {
            var entry = inventory.GetEntry(i);
            if (entry.IsEmpty) continue;

            data.inventory.Add(new InventorySlotSave
            {
                slotIndex = i,
                itemName = entry.item.itemName,
                quantity = entry.quantity
            });
        }
    }

    void LoadInventory(SaveData data)
    {
        var inventory = InventorySystem.Instance;
        inventory.ClearAll();

        foreach (var slot in data.inventory)
        {
            Item item = ItemDatabase.Instance.GetItem(slot.itemName);
            if (item == null) continue;

            inventory.SetSlot(slot.slotIndex, item, slot.quantity);
        }

        inventory.RefreshUI();
    }

    void SaveEquipment(SaveData data)
    {
        var equipment = EquipmentSystem.Instance;

        foreach (EquipmentSlotType type in System.Enum.GetValues(typeof(EquipmentSlotType)))
        {
            Item item = equipment.GetEquippedItem(type);
            if (item == null) continue;

            data.equipment.Add(new EquipmentSlotSave
            {
                slotType = type,
                itemName = item.itemName
            });
        }
    }

    void LoadEquipment(SaveData data)
    {
        var equipment = EquipmentSystem.Instance;

        equipment.UnequipAll();

        foreach (var slot in data.equipment)
        {
            Item item = ItemDatabase.Instance.GetItem(slot.itemName);
            if (item == null) continue;

            equipment.Equip(item, true);
        }
    }
}


