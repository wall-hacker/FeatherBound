using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] private float baseWalkSpeed = 4f;
    [SerializeField] private float baseRunSpeed = 8f;
    [SerializeField] private float baseSneakSpeed = 2f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float gravity = -55f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Animation Smoothing")]
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private float idleRandomInterval = 5f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;

    [Header("Death UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Image blackScreen;
    [SerializeField] private TMPro.TextMeshProUGUI deathText;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private float blackFadeDuration = 0.8f;
    [SerializeField] private float textFadeDuration = 0.6f;
    [SerializeField] private float textScaleMultiplier = 1.2f;
    [SerializeField] private float postDelay = 0.5f;

    // Components
    private CharacterController controller;
    private PlayerStats playerStats;

    // Movement variables
    private Vector3 velocity;
    private bool isGrounded;
    private bool isSneaking;
    private bool isSprinting;
    private bool hasWeapon;
    private bool isDancing;
    private bool isAiming;

    // Animation hashes (more efficient than strings)
    private int speedHash;
    private int isGroundedHash;
    private int isSneakingHash;
    private int isAimingHash;
    private int hasWeaponHash;
    private int deadHash;
    private int jumpHash;
    private int danceHash;
    private int appearanceHash;

    // Input
    private float horizontal;
    private float vertical;

    // Idle variation timer
    private float idleTimer;

    // Current speeds (can be modified by buffs/debuffs)
    private float walkSpeed = 4f;
    private float runSpeed = 8f;
    private float sneakSpeed = 2f;


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

        controller = GetComponent<CharacterController>();
        playerStats = FindObjectOfType<PlayerStats>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        // Cache animator parameter hashes
        speedHash = Animator.StringToHash("Speed");
        isGroundedHash = Animator.StringToHash("isGrounded");
        isSneakingHash = Animator.StringToHash("isSneaking");
        isAimingHash = Animator.StringToHash("isAiming");
        hasWeaponHash = Animator.StringToHash("hasWeapon");
        deadHash = Animator.StringToHash("Dead");
        jumpHash = Animator.StringToHash("Jump");
        danceHash = Animator.StringToHash("Dance");
        appearanceHash = Animator.StringToHash("Appearance");

        // Initialize idle timer
        idleTimer = idleRandomInterval;
    }

    void Start()
    {
        // Subscribe to death event
        if (playerStats != null)
        {
            playerStats.OnDeath += HandleDeath;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerStats != null)
        {
            playerStats.OnDeath -= HandleDeath;
        }
    }

    void Update()
    {
        if (playerStats != null && playerStats.IsDead) return;

        HandleGroundCheck();
        HandleInput();
        HandleMovement();
        HandleAnimations();
        HandleIdleVariations();
    }

    void HandleGroundCheck()
    {
        // Ground check using spherecast
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            // Fallback: use CharacterController.isGrounded
            isGrounded = controller.isGrounded;
        }

        // Reset velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }

        animator.SetBool(isGroundedHash, isGrounded);
    }

    void HandleInput()
    {
        // Movement input
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        // Sprint (can't sprint while aiming, and need sufficient stamina)
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && !isSneaking && !isAiming;
        
        // Check if can sprint (has enough stamina)
        if (wantsToSprint && playerStats != null)
        {
            isSprinting = playerStats.CanSprint();
        }
        else
        {
            isSprinting = false;
        }

        // Crouch
        isSneaking = Input.GetKey(KeyCode.LeftControl);
        animator.SetBool(isSneakingHash, isSneaking);

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        // Weapon toggle (example: E key)
        // if (Input.GetKeyDown(KeyCode.E))
        // {
        //     WeaponToggle();
        // }

        // Dance (example: T key)
        if (Input.GetKeyDown(KeyCode.T) && isGrounded)
        {
            StartDance();
        }

        // Aiming (Right Click)
        isAiming = Input.GetMouseButton(1) && hasWeapon && isGrounded;

        if (isAiming)
        {
            isSneaking = false; // Disable sneaking when aiming
            animator.SetBool(isSneakingHash, isSneaking);
        }
        animator.SetBool(isAimingHash, isAiming);

        // Appearance flair (example: Y key)
        if (Input.GetKeyDown(KeyCode.Y) && isGrounded)
        {
            animator.SetTrigger(appearanceHash);
        }

        // Check if movement should interrupt dance
        if (isDancing && (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f))
        {
            StopDance();
        }

        // Handle stamina via PlayerStats
        if (playerStats != null)
        {
            bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
            playerStats.HandleStamina(isSprinting, isMoving);
        }
    }

    void HandleMovement()
    {
        if (isDancing) return; // Don't move while dancing

        // Calculate move direction relative to camera
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Flatten directions (remove y component)
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction
        Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;

        // Determine speed based on state
        float currentSpeed = walkSpeed;
        if (isSneaking)
            currentSpeed = sneakSpeed;
        else if (isSprinting)
            currentSpeed = runSpeed;

        if (isAiming)
            currentSpeed *= 0.5f;

        // Move character
        if (moveDirection.magnitude >= 0.1f)
        {
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);

            // Rotate character to face movement direction (unless aiming)
            if (!isAiming)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                // When aiming, rotate to face camera direction
                Vector3 lookDirection = forward;
                if (lookDirection.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        } 
        else if (isAiming)
        {
            // Even when stationary while aiming, face camera direction
            Vector3 lookDirection = forward;
            if (lookDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleAnimations()
    {
        // Calculate speed for animator (0-1 normalized)
        float speed = 0f;

        if (!isDancing && (playerStats == null || !playerStats.IsDead))
        {
            float inputMagnitude = new Vector2(horizontal, vertical).magnitude;

            if (inputMagnitude > 0.1f)
            {
                if (isSneaking)
                    speed = 0.3f; // Sneak speed range: 0.2-0.4
                else if (isSprinting)
                    speed = 1f; // Sprint speed: 0.8-1.0
                else
                    speed = 0.5f; // Walk speed: 0.4-0.6
            }
        }

        // Smooth speed transition
        animator.SetFloat(speedHash, speed, speedDampTime, Time.deltaTime);
    }

    void HandleIdleVariations()
    {
        // Only trigger idle variations when truly idle
        if (isGrounded && !isDancing && (playerStats == null || !playerStats.IsDead))
        {
            float currentSpeed = animator.GetFloat(speedHash);

            if (currentSpeed < 0.01f) // Character is idle
            {
                idleTimer -= Time.deltaTime;

                if (idleTimer <= 0f)
                {
                    // Randomly decide between regular idle and idle chill
                    // This can be handled by animator blend tree or state machine
                    idleTimer = idleRandomInterval + Random.Range(-2f, 2f);
                }
            }
            else
            {
                idleTimer = idleRandomInterval;
            }
        }
    }

    void Jump()
    {
        // Calculate jump velocity using: v = sqrt(2 * jumpHeight * gravity)
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        animator.SetTrigger(jumpHash);
    }

    void StartDance()
    {
        if ((playerStats == null || !playerStats.IsDead) && isGrounded)
        {
            isDancing = true;
            animator.SetTrigger(danceHash);
        }
    }

    void StopDance()
    {
        isDancing = false;
        // The animator will transition out based on speed parameter
    }

    void HandleDeath()
    {
        animator.SetTrigger(deadHash);

        // Disable controller
        controller.enabled = false;
        
        // Untag the player
        gameObject.tag = "Untagged";

        // Additional death logic here (disable input, trigger game over, etc.)
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        deathPanel.SetActive(true);

        // Ensure starting values
        SetAlpha(blackScreen, 0f);
        SetAlpha(deathText, 0f);

        Vector3 startScale = deathText.transform.localScale;
        Vector3 targetScale = startScale * textScaleMultiplier;

        // Fade in black screen
        yield return FadeGraphic(blackScreen, 0f, 1f, blackFadeDuration);

        if (audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Fade + scale text simultaneously
        float t = 0f;
        while (t < textFadeDuration)
        {
            t += Time.deltaTime;
            float p = t / textFadeDuration;

            SetAlpha(deathText, p);
            deathText.transform.localScale = Vector3.Lerp(startScale, targetScale, p);

            yield return null;
        }

        SetAlpha(deathText, 1f);
        deathText.transform.localScale = targetScale;

        // Wait
        yield return new WaitForSeconds(postDelay);

        // Change scene
        SceneManager.LoadScene("IsometricScene");
    }

    void SetAlpha(Graphic graphic, float alpha)
    {
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration)
    {
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

    public void WeaponToggle()
    {
        hasWeapon = !hasWeapon;
        animator.SetBool(hasWeaponHash, hasWeapon);
    }


    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }

    // Buff methods
    public void SetSpeedBuff(float bonus)
    {
        walkSpeed = baseWalkSpeed + bonus;
        runSpeed = baseRunSpeed + bonus;
        sneakSpeed = baseSneakSpeed + bonus;
    }

    // Public getters
    public bool IsGrounded => isGrounded;
    public bool IsAiming => isAiming;
    public bool HasWeapon => hasWeapon;
    public bool IsSneaking => isSneaking;
    public bool IsSprinting => isSprinting;
}