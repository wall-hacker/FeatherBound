using UnityEngine;

public class WeaponShooting : MonoBehaviour
{
    public static WeaponShooting Instance { get; private set; }

    [Header("Shooting Settings")]
    [SerializeField] private float baseDamage = 0f; // Base weapon damage
    [SerializeField] private float fireRate = 0.15f; // Time between shots (automatic)
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private LayerMask hitMask; // What can be hit
    
    [Header("Weapon Prefabs")]
    [SerializeField] private GameObject pistolPrefab;
    [SerializeField] private GameObject riflePrefab;
    [SerializeField] private Transform gunHoldPositionTransform;
    
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerController playerController;
    
    [Header("Effects")]
    [SerializeField] private GameObject muzzleFlashEffect;
    [SerializeField] private GameObject bulletImpactEffect;
    [SerializeField] private float impactEffectLifetime = 2f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pistolShootSound;
    [SerializeField] private AudioClip rifleShootSound;
    
    // Internal variables
    private float nextFireTime;
    private GameObject currentWeaponInstance;
    private bool isWeaponEquipped = false;
    private PlayerStats playerStats;

    public enum WeaponType
    {
        None,
        Pistol,
        Rifle
    }

    private WeaponType currentWeaponType = WeaponType.None;

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
    }
    
    void Start()
    {
        // Auto-find references if not assigned
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();
    }

    void Update()
    {
        // Handle weapon equip/unequip with E key
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleWeapon();
        }
        
        // Only shoot if weapon is equipped and aiming
        if (!isWeaponEquipped)
            return;
            
        if (playerController != null && !playerController.HasWeapon)
            return;
        
        if (playerController != null && !playerController.IsAiming)
            return;
        
        // Check for shoot input based on weapon type
        bool canFire = Time.time >= nextFireTime;

        switch (currentWeaponType)
        {
            case WeaponType.Pistol:
                // Semi-auto (click once per shot)
                if (Input.GetMouseButtonDown(0) && canFire)
                {
                    Shoot();
                    nextFireTime = Time.time + fireRate;
                }
                break;

            case WeaponType.Rifle:
                // Full-auto (hold to shoot)
                if (Input.GetMouseButton(0) && canFire)
                {
                    Shoot();
                    nextFireTime = Time.time + fireRate;
                }
                break;
        }
    }

    public void ToggleWeapon()
    {
        // If weapon is already equipped, unequip it
        if (isWeaponEquipped)
        {
            UnequipWeapon();
            PlayerController.Instance.WeaponToggle();
            return;
        }
        
        // Check if there's a weapon in the equipment slot
        Item weaponItem = EquipmentSystem.Instance.GetWeapon();
        
        // If no weapon equipped in inventory, do nothing
        if (weaponItem == null)
        {
            Debug.Log("No weapon equipped in inventory slot");
            return;
        }

        PlayerController.Instance.WeaponToggle();
        
        // Equip the appropriate weapon based on item name
        if (weaponItem.itemName.StartsWith("Pistol"))
        {
            EquipWeapon(pistolPrefab);
            currentWeaponType = WeaponType.Pistol;
        }
        else if (weaponItem.itemName.StartsWith("Rifle"))
        {
            EquipWeapon(riflePrefab);
            currentWeaponType = WeaponType.Rifle;
        }
        else
        {
            Debug.LogWarning($"Unknown weapon type: {weaponItem.itemName}");
        }
    }
    
    void EquipWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.LogError("Weapon prefab is null!");
            return;
        }
        
        if (gunHoldPositionTransform == null)
        {
            Debug.LogError("Gun hold position transform is not assigned!");
            return;
        }
        
        // Instantiate the weapon at the hold position
        currentWeaponInstance = Instantiate(weaponPrefab, gunHoldPositionTransform.position, gunHoldPositionTransform.rotation, gunHoldPositionTransform);
        isWeaponEquipped = true;
        
        Debug.Log($"Equipped weapon: {weaponPrefab.name}");
    }
    
    void UnequipWeapon()
    {
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
            currentWeaponInstance = null;
        }
        
        isWeaponEquipped = false;
        currentWeaponType = WeaponType.None;
        Debug.Log("Weapon unequipped");
    }

    void Shoot()
    {
        Debug.Log("Pew Pew!");

        // Calculate total damage: base weapon damage + player attack stat
        float totalDamage = baseDamage + playerStats.currentAttack;
        
        // Check if insta-kill buff is active
        bool isInstaKill = playerStats.IsInstaKillActive;
        
        // Decrease counter for chickadee insta-kill buff
        playerStats.DecreaseInstaKills();

        // Play muzzle flash
        if (muzzleFlashEffect != null)
        {
            muzzleFlashEffect.SetActive(false);
            muzzleFlashEffect.SetActive(true);
        }
        
        // Play shoot sound
        if (audioSource != null)
        {
            if (currentWeaponInstance != null && EquipmentSystem.Instance.GetWeapon().itemName.StartsWith("Pistol"))
                audioSource.PlayOneShot(pistolShootSound);
            else if (currentWeaponInstance != null && EquipmentSystem.Instance.GetWeapon().itemName.StartsWith("Rifle"))
                audioSource.PlayOneShot(rifleShootSound);
        }
        
        // Perform raycast from screen center (crosshair position)
        Vector3 rayOrigin = playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector3 rayDirection = playerCamera.transform.forward;
        
        // Debug ray visualization
        Debug.DrawRay(rayOrigin, rayDirection * maxRange, Color.red, 1f);
        
        // Raycast to detect hit
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxRange, hitMask))
        {
            // Log what was hit
            Debug.Log($"Hit: {hit.collider.gameObject.name}");
            
            // Check if hit an enemy
            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                // Apply damage based on insta-kill status
                if (isInstaKill)
                {
                    // Deal massive damage for insta-kill (or you could call a specific insta-kill method)
                    enemy.TakeDamage(9999f);
                    Debug.Log("INSTA-KILL!");
                }
                else
                {
                    // Deal normal damage (weapon base + player attack stat)
                    enemy.TakeDamage(totalDamage);
                    Debug.Log($"Dealt {totalDamage} damage ({baseDamage} base + {playerStats.currentAttack} attack stat)");
                }
            }
            
            // Spawn impact effect
            if (bulletImpactEffect != null)
            {
                GameObject impact = Instantiate(bulletImpactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, impactEffectLifetime);
            }
        }
    }
    
    // Public method to get current total damage (for UI display, etc.)
    public float GetCurrentDamage()
    {
        return baseDamage + playerStats.currentAttack;
    }
    
    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 origin = playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
            Gizmos.DrawLine(origin, origin + playerCamera.transform.forward * maxRange);
        }
    }
}