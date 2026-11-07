using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CompanionSystem : MonoBehaviour
{
    public static CompanionSystem Instance;

    [Header("UI References")]
    [SerializeField] private Image companionIcon;
    [SerializeField] private Slider abilityCooldownSlider;

    [Header("Companion Data")]
    [SerializeField] private List<CompanionData> companions;

    [Header("Flying Companion Location")]
    [SerializeField] private Transform flyingCompanionSpawnPoint;

    [Header("Use abilities")]
    [SerializeField] private bool useAbilities = false;

    public enum CompanionName { BlueJay, Cardinal, GoldFinch, Crow, Robin, Chickadee, Sparrow }

    [System.Serializable]
    public class CompanionData
    {
        public CompanionSystem.CompanionName companionName;
        public Sprite icon;
        public Item companionItem;
        public GameObject flyingCompanionPrefab;

        [Header("Ability Timing")]
        public float cooldownTime;        // Time to refill slider
        public float activeDuration;       // Time slider drains
    }

    private CompanionData currentCompanion;
    private GameObject currentCompanionInstance;
    private bool abilityActive;
    private bool onCooldown;

    // Ability helpers
    private Coroutine cardinalHealCoroutine;
    private bool chickadeeAttackBuffActive = false;
    private bool chickadeeDefenseDebuffActive = false;
    private PlayerStats playerStats;

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
        companionIcon.gameObject.SetActive(false);
        abilityCooldownSlider.gameObject.SetActive(false);
        abilityCooldownSlider.value = 1f;

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();
    }

    void Update()
    {
        // For testing: activate ability with C key
        if (Input.GetKeyDown(KeyCode.C) && useAbilities)
        {
            ActivateAbility();
        }
    }

    // -------------------------
    // COMPANION HANDLING
    // -------------------------

    public void UpdateCompanionUI()
    {
        Item equipped = EquipmentSystem.Instance.GetCompanion();

        if (equipped == null)
        {
            companionIcon.gameObject.SetActive(false);
            abilityCooldownSlider.gameObject.SetActive(false);
            RemovePassive(currentCompanion.companionName);
            currentCompanion = null;
            Destroy(currentCompanionInstance);
            currentCompanionInstance = null;
            return;
        }

        companionIcon.gameObject.SetActive(true);
        abilityCooldownSlider.gameObject.SetActive(true);

        if (currentCompanion == null || currentCompanion.companionItem != equipped)
        {
            AssignCompanion(equipped);
        }

        // instantiate currentCompanion.flyingCompanionPrefab at flyingCompanionSpawnPoint
        if (currentCompanion != null && currentCompanion.flyingCompanionPrefab != null)
        {
            currentCompanionInstance = Instantiate(currentCompanion.flyingCompanionPrefab, flyingCompanionSpawnPoint.position, flyingCompanionSpawnPoint.rotation);
            currentCompanionInstance.transform.SetParent(flyingCompanionSpawnPoint);
        }
    }

    void AssignCompanion(Item item)
    {
        foreach (var companion in companions)
        {
            if (companion.companionItem == item ||
                companion.companionItem.itemName == item.itemName)
            {
                currentCompanion = companion;
                companionIcon.sprite = companion.icon;
                abilityCooldownSlider.value = 1f;

                ApplyPassive(companion.companionName);
                break;
            }
        }
    }

    // -------------------------
    // ACTIVE ABILITY
    // -------------------------

    public void ActivateAbility()
    {
        if (currentCompanion == null || abilityActive || onCooldown)
            return;

        StartCoroutine(ActiveAbilityRoutine());
    }

    IEnumerator ActiveAbilityRoutine()
    {
        abilityActive = true;

        ApplyActive(currentCompanion.companionName);

        float t = 0f;
        while (t < currentCompanion.activeDuration)
        {
            t += Time.deltaTime;
            abilityCooldownSlider.value = 1f - (t / currentCompanion.activeDuration);
            yield return null;
        }

        RemoveActive(currentCompanion.companionName);

        abilityActive = false;
        StartCoroutine(CooldownRoutine());
    }

    IEnumerator CooldownRoutine()
    {
        onCooldown = true;

        float t = 0f;
        while (t < currentCompanion.cooldownTime)
        {
            t += Time.deltaTime;
            abilityCooldownSlider.value = t / currentCompanion.cooldownTime;
            yield return null;
        }

        abilityCooldownSlider.value = 1f;
        onCooldown = false;
    }

    // -------------------------
    // ABILITIES
    // -------------------------

    void ApplyPassive(CompanionName name)
    {   
        if (!useAbilities)
            return;

        switch (name)
        {
            case CompanionName.BlueJay:
            {
                // Passive: movement speed + stamina regen
                PlayerController.Instance.SetSpeedBuff(2f);
                playerStats.SetStaminaRegenBuff(15f);
                break;
            }
                

            case CompanionName.Cardinal:
            {
                // Passive: health regeneration
                if (cardinalHealCoroutine == null)
                    cardinalHealCoroutine = StartCoroutine(CardinalHealRoutine());
                break;
            }
                

            case CompanionName.GoldFinch: 
            {
                // Passive: increase item highlight range
                RevealSystem.Instance.SetRevealRadiusBuff(50f);
                break;
            }

            case CompanionName.Chickadee:
            {
                // Passive: glass cannon HP-based modifier
                UpdateChickadeePassive();
                playerStats.chickadeeCompanionActive = true;
                break;
            }
                

            case CompanionName.Sparrow:
            {
                // Passive: tiny stamina + HP increase
                playerStats.StaminaAndHealthBuff(10f);
                break;
            }
        }
    }

    void RemovePassive(CompanionName name)
    {
        if (!useAbilities)
            return;

        switch (name)
        {
            case CompanionName.BlueJay:
            {
                // Passive: movement speed + stamina regen
                PlayerController.Instance.SetSpeedBuff(0f);
                playerStats.SetStaminaRegenBuff(0f);
                break;
            }

            case CompanionName.Cardinal:
            {
                // Passive: health regeneration
                if (cardinalHealCoroutine != null)
                {
                    StopCoroutine(cardinalHealCoroutine);
                    cardinalHealCoroutine = null;
                }
                break;
            }

            case CompanionName.GoldFinch:
            {
                // Passive: increase item highlight range
                RevealSystem.Instance.SetRevealRadiusBuff(0f);
                break;
            }

            case CompanionName.Chickadee:
            {
                if (chickadeeAttackBuffActive)
                {
                    playerStats.ModifyAttack(-6);
                    chickadeeAttackBuffActive = false;
                }

                if (chickadeeDefenseDebuffActive)
                {
                    playerStats.ModifyDefense(+3);
                    chickadeeDefenseDebuffActive = false;
                }
                playerStats.chickadeeCompanionActive = false;
                break;
            }

            case CompanionName.Sparrow:
            {
                // Passive: tiny stamina + HP increase
                playerStats.StaminaAndHealthBuff(-10f);
                break;
            }
                
        }
    }

    IEnumerator CardinalHealRoutine()
    {
        while (true)
        {
            // Heal a very small amount continuously
            playerStats.Heal(0.1f);
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void UpdateChickadeePassive()
    {
        float hp = playerStats.HealthPercent;

        // ---- ATTACK BUFF (Above 75%) ----
        if (hp > 75 && !chickadeeAttackBuffActive)
        {
            playerStats.ModifyAttack(6);
            chickadeeAttackBuffActive = true;
        }
        else if (hp <= 75 && chickadeeAttackBuffActive)
        {
            playerStats.ModifyAttack(-6);
            chickadeeAttackBuffActive = false;
        }

        // ---- DEFENSE DEBUFF (Below 50%) ----
        if (hp < 50 && !chickadeeDefenseDebuffActive)
        {
            playerStats.ModifyDefense(-3);
            chickadeeDefenseDebuffActive = true;
        }
    }

    void ApplyActive(CompanionName name)
    {
        switch (name)
        {
            case CompanionName.BlueJay:
            {
                // Active: sprint costs no stamina
                playerStats.SetStaminaDrainBuff(5f);
                break;
            }
                
            case CompanionName.Cardinal:
            {
                // Active: instant heal
                float roll = Random.value; // 0.0 – 1.0

                float healPercent;

                if (roll < 0.6f)
                    healPercent = 0.10f;   // 60%
                else if (roll < 0.9f)
                    healPercent = 0.25f;   // 30%
                else
                    healPercent = 0.50f;   // 10%

                float healAmount = playerStats.MaxHealth * healPercent;
                playerStats.Heal(healAmount);
                break;
            }

            case CompanionName.GoldFinch:
            {
                // Active: double item pickups
                InventorySystem.Instance.SetDoublePickupActive(true);
                break;
            }

            case CompanionName.Crow:
            {
                // Active: stealth-kill charges
                playerStats.SetStealthCoinBuff(true);
                break;
            }

            case CompanionName.Robin:
            {
                // Active: zombies disengage
                ZombieAI[] zombies = FindObjectsOfType<ZombieAI>();
                foreach (var zombie in zombies)
                {
                    zombie.SetLoseTargetDistance(0.001f);
                    zombie.SetState(ZombieAI.ZombieState.Idle);
                }
                break;

            }
                
            case CompanionName.Chickadee:
            {
                // Active: temporary insta-kill buff
                playerStats.SetInstaKillBuff(true);
                break;
            }

            case CompanionName.Sparrow:
                // Active: prevent death once
                playerStats.sparrowAbilityActive = true;
                break;
        }
    }

    void RemoveActive(CompanionName name)
    {
        switch (name)
        {
            case CompanionName.BlueJay:
            {
                // Remove sprint buff
                playerStats.SetStaminaDrainBuff(0f);
                break;
            }
                

            case CompanionName.GoldFinch:
            {
                // Remove pickup multiplier
                InventorySystem.Instance.SetDoublePickupActive(false);
                break;
            }

            case CompanionName.Robin:
            {
                // End disengage effect
                ZombieAI[] zombies = FindObjectsOfType<ZombieAI>();
                foreach (var zombie in zombies)
                {
                    zombie.SetLoseTargetDistance(20f);
                }
                break;

            }

            case CompanionName.Sparrow:
                // Remove active effect
                playerStats.sparrowAbilityActive = false;
                break;
        }
    }
}
