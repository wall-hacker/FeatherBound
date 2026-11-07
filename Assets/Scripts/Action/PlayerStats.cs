using UnityEngine;
using UnityEngine.UI;
using System;

// Player Stats Component
public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseAttack = 0f;
    [SerializeField] private float baseDefense = 0f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxStamina = 100f;
    
    [Header("Stamina System")]
    [SerializeField] private float baseStaminaDrainRate = 10f; // Stamina drained per second while sprinting
    [SerializeField] private float baseStaminaRegenRate = 30f; // Stamina regenerated per second when not sprinting
    [SerializeField] private float staminaRegenDelay = 1f; // Delay before stamina starts regenerating after sprinting
    [SerializeField] private float minStaminaToSprint = 10f; // Minimum stamina required to start sprinting
    
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Image lowHealthVignette;
    
    [Header("Warning Effects")]
    [SerializeField] private float vignetteFlashSpeed = 2f;
    [SerializeField] private float lowHealthThreshold = 30f;
    
    [Header("Current Stats")]
    public float currentAttack;
    public float currentDefense;
    private float currentHealth;
    private float currentStamina;
    private float staminaDrainRate = 10f; // is modified by buffs/debuffs
    private float staminaRegenRate = 30f; // is modified by buffs/debuffs
    private bool stealthCoinActive = false; // is modified by buffs/debuffs
    public bool chickadeeCompanionActive = false; // is modified by buffs/debuffs
    private int buffedKillsRemaining = 0; // is modified by buffs/debuffs
    private int instaKillKillsRemaining = 0; // is modified by buffs/debuffs
    public bool sparrowAbilityActive = false; // is modified by buffs/debuffs
    private float staminaRegenTimer;
    private bool canSprint;
    private bool isDead;
    
    private float attackBonus = 0;
    private float defenseBonus = 0;

    // Events for other systems to subscribe to
    public event Action OnDeath;
    public event Action<float> OnHealthChanged;
    public event Action<float> OnStaminaChanged;

    // Surplus Stamina Feature Added
    private float surplusStamina = 0f;

    void Start()
    {
        ResetStats();
        InitializeUI();
    }

    void Update()
    {
        if (!isDead)
        {
            UpdateWarningEffects();
        }
    }

    void ResetStats()
    {
        currentAttack = baseAttack + attackBonus;
        currentDefense = baseDefense + defenseBonus;
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        canSprint = true;
        isDead = false;
        
        UpdateHealthUI();
        UpdateStaminaUI();
    }

    void InitializeUI()
    {
        UpdateHealthUI();
        UpdateStaminaUI();

        // Initialize vignette effect
        if (lowHealthVignette != null)
        {
            lowHealthVignette.enabled = true;
            Color c = lowHealthVignette.color;
            c.a = 0f;
            lowHealthVignette.color = c;
        }
    }

    // === STAMINA MANAGEMENT ===
    
    public void HandleStamina(bool isSprinting, bool isMoving)
    {
        Debug.Log($"Sprint:{isSprinting} Move:{isMoving} Surplus:{surplusStamina}");
        if (isSprinting && isMoving)
        {

            if (surplusStamina > 0f)
            {
                surplusStamina -= staminaDrainRate * Time.deltaTime;
                surplusStamina = Mathf.Max(surplusStamina, 0f);
                Debug.Log($"Using surplus stamina. Remaining surplus: {surplusStamina}");
            } else
            {
                // Drain stamina while sprinting
                currentStamina -= staminaDrainRate * Time.deltaTime;
                currentStamina = Mathf.Max(currentStamina, 0f);
            }
            
            // Reset regen timer
            staminaRegenTimer = staminaRegenDelay;
            
            // Update UI and trigger event
            UpdateStaminaUI();
            OnStaminaChanged?.Invoke(currentStamina);
        }
        else
        {
            // Count down regen delay
            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= Time.deltaTime;
            }
            else
            {
                // Regenerate stamina
                float oldStamina = currentStamina;
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
                
                // Allow sprinting again once stamina is sufficient
                if (currentStamina >= minStaminaToSprint)
                {
                    canSprint = true;
                }
                
                // Only update if stamina changed
                if (oldStamina != currentStamina)
                {
                    UpdateStaminaUI();
                    OnStaminaChanged?.Invoke(currentStamina);
                }
            }
        }
    }

    public bool CanSprint()
    {
        if (currentStamina >= minStaminaToSprint)
        {
            canSprint = true;
            return true;
        }
        
        if (currentStamina <= 0f)
        {
            canSprint = false;
            return false;
        }
        
        return canSprint;
    }

    public void RestoreStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        
        if (currentStamina >= minStaminaToSprint)
        {
            canSprint = true;
        }
        
        UpdateStaminaUI();
        OnStaminaChanged?.Invoke(currentStamina);
    }

    // New method for surplus stamina (can exceed max)
    public void RestoreSurplusStamina(float amount)
    {
        surplusStamina += amount;
        Debug.Log($"Using surplus stamina. Remaining surplus: {surplusStamina}");
    }

    public void SetStaminaToMax()
    {
        currentStamina = maxStamina;
        canSprint = true;
        UpdateStaminaUI();
        OnStaminaChanged?.Invoke(currentStamina);
    }

    // === HEALTH MANAGEMENT ===
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // Apply defense reduction
        float actualDamage = Mathf.Max(damage - currentDefense, 0);
        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(currentHealth, 0f);
        
        // Update UI and trigger event
        UpdateHealthUI();
        if (chickadeeCompanionActive)
            CompanionSystem.Instance.UpdateChickadeePassive();
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        Debug.Log($"Healing for {amount} health.");
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        UpdateHealthUI();
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void SetHealthToMax()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        OnHealthChanged?.Invoke(currentHealth);
    }

    void Die()
    {
        if (sparrowAbilityActive)
        {
            // heal player to half health instead of dying
            currentHealth = 55f;
            UpdateHealthUI();
            OnHealthChanged?.Invoke(currentHealth);
            return;
        }

        if (isDead) return;
        
        isDead = true;
        OnDeath?.Invoke();
    }

    public void Revive()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        canSprint = true;
        
        UpdateHealthUI();
        UpdateStaminaUI();
        OnHealthChanged?.Invoke(currentHealth);
        OnStaminaChanged?.Invoke(currentStamina);
    }

    // === STAT MODIFICATION ===
    
    public void ModifyAttack(float amount)
    {
        attackBonus += amount;
        currentAttack = baseAttack + attackBonus;
        Debug.Log($"Attack: {currentAttack} (Base: {baseAttack} + Bonus: {attackBonus})");
    }

    public void ModifyDefense(float amount)
    {
        defenseBonus += amount;
        currentDefense = baseDefense + defenseBonus;
        Debug.Log($"Defense: {currentDefense} (Base: {baseDefense} + Bonus: {defenseBonus})");
    }

    // === UI UPDATES ===
    
    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }
    
    void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void UpdateWarningEffects()
    {
        if (lowHealthVignette == null) return;
        
        float healthPercent = currentHealth / maxHealth * 100f;
        
        if (healthPercent <= lowHealthThreshold)
        {
            // Flash red vignette when low health
            float flash = Mathf.PingPong(Time.time * vignetteFlashSpeed, 1f);
            float alpha = Mathf.Lerp(0.1f, 1f, flash);
            
            // More intense at lower health
            alpha *= (1f - (healthPercent / lowHealthThreshold));
            
            Color c = lowHealthVignette.color;
            c.a = alpha;
            lowHealthVignette.color = c;
        }
        else
        {
            // Fade out vignette
            Color c = lowHealthVignette.color;
            c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * 5f);
            lowHealthVignette.color = c;
        }
    }

    // === Buff/Debuff Management ===

    public void SetStaminaRegenBuff(float bonus)
    {
        staminaRegenRate = baseStaminaRegenRate + bonus;
    }

    public void SetStaminaDrainBuff(float bonus)
    {
        staminaDrainRate = baseStaminaDrainRate - bonus;
    }

    public void SetStealthCoinBuff(bool isActive)
    {
        stealthCoinActive = isActive;
        if (isActive)
            buffedKillsRemaining = 5;
        if (!isActive)
            buffedKillsRemaining = 0;
    }

    public void SetInstaKillBuff(bool isActive)
    {
        if (isActive)
            instaKillKillsRemaining = 3;
    }

    public void DecreaseBuffedKills()
    {
        Debug.Log($"Buffed kills remaining before decrease: {buffedKillsRemaining}");
        if (buffedKillsRemaining > 0)
        {
            buffedKillsRemaining--;
            if (buffedKillsRemaining == 0)
            {
                stealthCoinActive = false;
            }
        }
    }

    public void DecreaseInstaKills()
    {
        if (instaKillKillsRemaining > 0)
        {
            instaKillKillsRemaining--;
        }
    }

    public void StaminaAndHealthBuff(float bonus)
    {
        maxHealth += bonus;
        maxStamina += bonus;
        currentHealth += bonus;
        currentStamina += bonus;
        UpdateHealthUI();
        UpdateStaminaUI();
    }

    // === PUBLIC GETTERS ===
    
    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float HealthPercent => (currentHealth / maxHealth) * 100f;
    public float StaminaPercent => (currentStamina / maxStamina) * 100f;
    public bool IsStealthCoinActive => stealthCoinActive;
    public int BuffedKillsRemaining => buffedKillsRemaining;
    public bool IsInstaKillActive => instaKillKillsRemaining > 0;
}