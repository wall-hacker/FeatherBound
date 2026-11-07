using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BirdShop : MonoBehaviour
{
    public static BirdShop Instance;

    [Header("Bird Setup")]
    [SerializeField] private GameObject[] birds;
    
    [Header("Stage Positions")]
    [SerializeField] private Transform leftPosition;    // Where birds enter from
    [SerializeField] private Transform centerPosition;  // Where current bird sits
    [SerializeField] private Transform rightPosition;   // Where birds exit to

    [Header("Main UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button purchaseButton;

    [Header("Info Panel UI")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TextMeshProUGUI abilityText;

    [Header("Economy (Placeholder)")]
    [SerializeField] private int playerBalance = 500;

    private int currentBirdIndex = 0;
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Disable all birds first
        foreach (var bird in birds)
            bird.SetActive(false);

        // Activate starting bird
        GameObject startingBird = birds[currentBirdIndex];
        startingBird.SetActive(true);

        // Move starting bird to the center stage
        startingBird.transform.position = centerPosition.position;

        // Facing the camera (along -Z axis)
        startingBird.transform.rotation = Quaternion.LookRotation(Vector3.back);

        // Hide info panel initially
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        UpdateUI();
    }

    public void NextBird()
    {
        if (isTransitioning) return;
        StartCoroutine(SwitchBirdNext());
    }

    public void PreviousBird()
    {
        if (isTransitioning) return;
        StartCoroutine(SwitchBirdPrevious());
    }

    IEnumerator SwitchBirdNext()
    {
        isTransitioning = true;

        GameObject currentBird = birds[currentBirdIndex];

        // Calculate next index
        int nextIndex = currentBirdIndex + 1;
        if (nextIndex >= birds.Length) nextIndex = 0;

        GameObject nextBird = birds[nextIndex];

        // Position next bird on the LEFT (entering from left)
        nextBird.SetActive(true);
        nextBird.transform.position = leftPosition.position;

        Bird currentBirdScript = currentBird.GetComponent<Bird>();
        Bird nextBirdScript = nextBird.GetComponent<Bird>();

        // Current bird flies to the RIGHT (exit)
        Coroutine flyOut = StartCoroutine(
            currentBirdScript.FlyToTarget(rightPosition.position)
        );

        // Next bird flies from LEFT to CENTER (enter)
        Coroutine flyIn = StartCoroutine(
            nextBirdScript.FlyToTarget(centerPosition.position)
        );

        // Wait until BOTH finish
        yield return flyOut;
        yield return flyIn;

        // Disable old bird after it's gone
        currentBird.SetActive(false);

        // Commit index change
        currentBirdIndex = nextIndex;

        UpdateUI();

        isTransitioning = false;
    }

    IEnumerator SwitchBirdPrevious()
    {
        isTransitioning = true;

        GameObject currentBird = birds[currentBirdIndex];

        // Calculate previous index
        int prevIndex = currentBirdIndex - 1;
        if (prevIndex < 0) prevIndex = birds.Length - 1;

        GameObject prevBird = birds[prevIndex];

        // Position previous bird on the RIGHT (entering from right)
        prevBird.SetActive(true);
        prevBird.transform.position = rightPosition.position;

        Bird currentBirdScript = currentBird.GetComponent<Bird>();
        Bird prevBirdScript = prevBird.GetComponent<Bird>();

        // Current bird flies to the LEFT (exit)
        Coroutine flyOut = StartCoroutine(
            currentBirdScript.FlyToTarget(leftPosition.position)
        );

        // Previous bird flies from RIGHT to CENTER (enter)
        Coroutine flyIn = StartCoroutine(
            prevBirdScript.FlyToTarget(centerPosition.position)
        );

        // Wait until BOTH finish
        yield return flyOut;
        yield return flyIn;

        // Disable old bird after it's gone
        currentBird.SetActive(false);

        // Commit index change
        currentBirdIndex = prevIndex;

        UpdateUI();

        isTransitioning = false;
    }

    public void UpdateUI()
    {
        BirdShopData data = birds[currentBirdIndex].GetComponent<BirdShopData>();

        // Update name text
        if (data == null || nameText == null) return;
        nameText.text = data.birdName;

        // Update price text
        if (data == null || priceText == null) return;

        // Update info panel texts
        if (data != null && infoPanel != null)
        {
            abilityText.text = $"Passive Ability: <color=#AAAAAA>{data.passiveAbility}</color>\nActive Ability: <color=#AAAAAA>{data.activeAbility}</color>";
        }

        bool affordable = InventorySystem.Instance.GetCoinsCount() >= data.price;

        priceText.text =
            affordable
                ? $"<color=#FFD966>{data.price} Coins</color>"
                : $"<color=#FF5555>{data.price} Coins</color>";

        purchaseButton.interactable = affordable;
        var tmp = purchaseButton.GetComponentInChildren<TextMeshProUGUI>();

        Color c = tmp.color;
        c.a = affordable ? 1f : 0.26f; // 26% alpha when unaffordable
        tmp.color = c;
    }

    public void ToggleInformationPanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(!infoPanel.activeSelf);
        }
    }

    public void PurchaseCurrentBird()
    {
        BirdShopData data = birds[currentBirdIndex].GetComponent<BirdShopData>();
        if (data == null) return;

        int playerCoins = InventorySystem.Instance.GetCoinsCount();
        if (playerCoins >= data.price)
        {
            // Deduct coins
            InventorySystem.Instance.RemoveCoins(data.price);

            // Add bird item to inventory
            InventorySystem.Instance.AddItem(data.birdItem, 1);

            // Optionally, provide feedback to the player
            Debug.Log($"Purchased {data.birdName} for {data.price} coins!");
        }
        else
        {
            // Not enough coins feedback
            Debug.Log("Not enough coins to purchase this bird.");
        }

        // Update UI after purchase attempt
        UpdateUI();
    }
}