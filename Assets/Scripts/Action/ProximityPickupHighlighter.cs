using UnityEngine;
using TMPro;

public class ProximityPickupHighlighter : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;

    [Header("Tooltip UI")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Header("Item Data")]
    [SerializeField] private Item item;

    // References
    private Transform player;
    private HighlightableGroupAuthoring highlightable;

    // State
    private bool playerInRange;

    void Awake()
    {
        highlightable = GetComponentInParent<HighlightableGroupAuthoring>();

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= interactionRange)
        {
            if (!playerInRange)
            {
                OnEnterRange();
            }

            if (Input.GetKeyDown(pickupKey))
            {
                PickupItem();
            }
        }
        else
        {
            if (playerInRange)
            {
                OnExitRange();
            }
        }
    }

    void OnEnterRange()
    {
        playerInRange = true;

        // Enable highlight
        if (highlightable != null)
            highlightable.EnableHighlight();

        // Show tooltip
        ShowTooltip();
    }

    void OnExitRange()
    {
        playerInRange = false;

        // Disable highlight
        if (highlightable != null)
            highlightable.DisableHighlight();

        // Hide tooltip
        HideTooltip();
    }

    void ShowTooltip()
    {
        if (tooltipPanel == null)
            return;

        tooltipText.text = $"{item.itemName}\n<color=#AAAAAA>{item.itemTooltip}</color>";

        tooltipPanel.SetActive(true);
    }

    void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    void PickupItem()
    {
        // Store item in inventory
        InventorySystem.Instance.AddItem(item);

        // Notify SpwanablesSceneManager that an item was collected
        if (SpwanablesSceneManager.Instance != null)
        {
            SpwanablesSceneManager.Instance.OnItemCollected();
        }

        // Cleanup
        HideTooltip();

        if (highlightable != null)
            highlightable.DisableHighlight();

        Destroy(gameObject);
    }


    void OnDisable()
    {
        HideTooltip();

        if (highlightable != null)
            highlightable.DisableHighlight();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
