using UnityEngine;
using UnityEngine.UI;
using System;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private Slider healthBar;
    
    [Header("Death Settings")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 3f;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private float hitFlashDuration = 0.1f;

    [Header("Drop Items")]
    [SerializeField] Item coinItem;

    private Color originalColor;
    private bool isFlashing;
    private bool isDead;
    private PlayerStats playerStats;

    // Event that ZombieAI can subscribe to
    public event Action OnDamageTaken;

    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
        
        // Cache original color for hit feedback
        if (meshRenderer != null)
        {
            originalColor = meshRenderer.material.color;
        }

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);
        if (healthBar != null)
            healthBar.value = currentHealth;
        
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Notify subscribers (ZombieAI) that damage was taken
        OnDamageTaken?.Invoke();
        
        // Visual feedback
        ShowDamageEffect();
        
        if (currentHealth <= 0f)
        {
            if (healthBar != null)
                healthBar.gameObject.SetActive(false);
            Die();
        }
    }

    void ShowDamageEffect()
    {
        // Spawn damage particle effect
        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }
        
        // Flash red
        if (meshRenderer != null && !isFlashing)
        {
            StartCoroutine(HitFlash());
        }
    }

    System.Collections.IEnumerator HitFlash()
    {
        isFlashing = true;
        meshRenderer.material.color = Color.red;
        
        yield return new WaitForSeconds(hitFlashDuration);
        
        meshRenderer.material.color = originalColor;
        isFlashing = false;
    }

    void Die()
    {
        if (isDead) return;
        
        isDead = true;

        ZombieAI zombieAI = GetComponent<ZombieAI>();

        zombieAI.SetState(ZombieAI.ZombieState.Dead);
        
        Debug.Log($"{gameObject.name} died!");

        // Notify SpwanablesSceneManager that an enemy was killed
        if (SpwanablesSceneManager.Instance != null)
        {
            SpwanablesSceneManager.Instance.OnItemCollected();
        }
        
        // Disable components
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Disable AI/movement scripts if any
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this)
                script.enabled = false;
        }
        
        // Destroy or disable after delay
        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            // Just disable the GameObject
            Invoke(nameof(DisableEnemy), destroyDelay);
        }

        // Reward player with random coins amount
        int randomizedCoinAmount = UnityEngine.Random.Range(1, 3);

        InventorySystem.Instance.AddItem(coinItem, randomizedCoinAmount);
        playerStats.DecreaseBuffedKills();
    }

    void DisableEnemy()
    {
        gameObject.SetActive(false);
    }

    // Public getters
    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    
    // Optional: Healing method
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        if (healthBar != null)
            healthBar.value = currentHealth;
    }
}