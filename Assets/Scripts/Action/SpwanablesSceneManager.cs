using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SpwanablesSceneManager : MonoBehaviour
{
    public static SpwanablesSceneManager Instance { get; private set; }

    [Header("Collection Settings")]
    [SerializeField] private int minItemsPerType = 30;
    [SerializeField] private float collectionTimeLimit = 600; // 5 minutes in seconds

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI itemCountText;

    [Header("Completion UI")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private Image blackScreen;
    [SerializeField] private TextMeshProUGUI completionText;

    [Header("Fade Settings")]
    [SerializeField] private float blackFadeDuration = 0.8f;
    [SerializeField] private float textFadeDuration = 0.6f;
    [SerializeField] private float textScaleMultiplier = 1.2f;
    [SerializeField] private float postDelay = 2f;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "IsometricScene";

    [Header("Sounds")]
    [SerializeField] private AudioClip collectionCompleteSound;
    [SerializeField] private AudioClip timerExpiredSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Scene Type")]
    [SerializeField] private bool isCollectionScene = true;

    // Item tracking
    private string[] itemTags = { "Bottle", "Pills", "MetalScraps", "Fabric", "Leather", "DuctTape", "Coins", "Herbs", "Zombie" };
    private int totalItemsRemaining;
    private int totalItems;
    private float timeRemaining;
    private bool collectionComplete = false;
    private bool timerExpired = false;

    void Awake()
    {
        // Singleton enforcement
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
        RandomlyDestroyExcessItems();
        CountRemainingItems();
        
        timeRemaining = collectionTimeLimit;
        
        UpdateTimerUI();
        UpdateItemCountUI();

        // Ensure completion panel is hidden
        if (completionPanel != null)
            completionPanel.SetActive(false);
    }

    void Update()
    {
        if (collectionComplete || timerExpired)
            return;

        // Update timer
        timeRemaining -= Time.deltaTime;
        
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerExpired = true;
            StartCoroutine(CompletionSequence(false)); // Time's up
        }

        UpdateTimerUI();
    }

    void RandomlyDestroyExcessItems()
    {
        foreach (string tag in itemTags)
        {
            GameObject[] items = GameObject.FindGameObjectsWithTag(tag);
            
            if (items.Length > minItemsPerType)
            {
                // Calculate how many to destroy
                int excessCount = items.Length - minItemsPerType;
                
                // Shuffle the array
                List<GameObject> itemList = new List<GameObject>(items);
                for (int i = 0; i < itemList.Count; i++)
                {
                    GameObject temp = itemList[i];
                    int randomIndex = Random.Range(i, itemList.Count);
                    itemList[i] = itemList[randomIndex];
                    itemList[randomIndex] = temp;
                }
                
                // Destroy excess items
                for (int i = 0; i < excessCount; i++)
                {
                    Destroy(itemList[i]);
                    // remove their tag to avoid counting them again
                    itemList[i].tag = "Untagged";
                }
                
                Debug.Log($"Destroyed {excessCount} excess {tag} items. Remaining: {minItemsPerType}");
            }
        }
    }

    void CountRemainingItems()
    {
        totalItemsRemaining = 0;
        
        foreach (string tag in itemTags)
        {
            GameObject[] items = GameObject.FindGameObjectsWithTag(tag);
            totalItemsRemaining += items.Length;
        }
        
        totalItems = totalItemsRemaining;

        Debug.Log($"Total collectible items in scene: {totalItemsRemaining}");
    }

    public void OnItemCollected()
    {
        if (collectionComplete || timerExpired)
            return;

        totalItemsRemaining--;
        UpdateItemCountUI();

        if (totalItemsRemaining <= 0)
        {
            collectionComplete = true;
            StartCoroutine(CompletionSequence(true)); // All items collected
        }
    }

    void UpdateTimerUI()
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        
        timerText.text = $"{minutes:00}:{seconds:00}";
        
        // Change color when time is running low (last 30 seconds)
        if (timeRemaining <= 30f && timeRemaining > 0f)
        {
            timerText.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 2f, 1f));
        }
        else
        {
            timerText.color = Color.white;
        }
    }

    void UpdateItemCountUI()
    {
        if (itemCountText == null)
            return;

        itemCountText.text = $"{totalItems - totalItemsRemaining}/{totalItems}";
    }

    IEnumerator CompletionSequence(bool success)
    {
        if (completionPanel == null)
            yield break;

        completionPanel.SetActive(true);

        // Ensure starting values
        SetAlpha(blackScreen, 0f);
        SetAlpha(completionText, 0f);

        Vector3 startScale = completionText.transform.localScale;
        Vector3 targetScale = startScale * textScaleMultiplier;

        // Set completion message
        if (success)
        {
            if (isCollectionScene)
                completionText.text = "<color=white>Congratulations!\nYou have collected all items.</color>";
            else
                completionText.text = "<color=white>Well Done!\nYou have defeated all enemies.</color>";
        }
        else
        {
            completionText.text = "<color=white>Time is up!</color>";
        }

        // Fade in black screen
        yield return FadeGraphic(blackScreen, 0f, 1f, blackFadeDuration);

        // Play sound
        if (audioSource != null)
        {
            AudioClip clipToPlay = success ? collectionCompleteSound : timerExpiredSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        // Fade + scale text simultaneously
        float t = 0f;
        while (t < textFadeDuration)
        {
            t += Time.deltaTime;
            float p = t / textFadeDuration;

            SetAlpha(completionText, p);
            completionText.transform.localScale = Vector3.Lerp(startScale, targetScale, p);

            yield return null;
        }

        SetAlpha(completionText, 1f);
        completionText.transform.localScale = targetScale;

        // Wait
        yield return new WaitForSeconds(postDelay);

        // Change scene
        SceneManager.LoadScene(nextSceneName);
    }

    void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration)
    {
        if (graphic == null)
            yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;

            Color c = graphic.color;
            c.a = Mathf.Lerp(from, to, p);
            graphic.color = c;

            yield return null;
        }

        Color final = graphic.color;
        final.a = to;
        graphic.color = final;
    }

    // Public getter
    public int TotalItemsRemaining => totalItemsRemaining;
}