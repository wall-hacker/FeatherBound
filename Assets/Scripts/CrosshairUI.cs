using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private GameObject crosshairObject;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color aimColor = new Color(1f, 0f, 0f, 0.8f);

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera playerCamera;

    [Header("Raycast Settings")]
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private LayerMask enemyMask;

    [Header("Animation Settings")]
    [SerializeField] private float fadeSpeed = 10f;

    private CanvasGroup canvasGroup;

    void Start()
    {
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        canvasGroup = crosshairObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = crosshairObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        crosshairObject.SetActive(true);
    }

    void Update()
    {
        if (playerController == null || crosshairObject == null) return;

        bool isAiming = playerController.IsAiming && playerController.HasWeapon;

        // Fade in/out
        float targetAlpha = isAiming ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

        if (!isAiming)
        {
            crosshairImage.color = normalColor;
            return;
        }

        // Enemy detection raycast
        bool aimingAtEnemy = IsAimingAtEnemy();

        crosshairImage.color = aimingAtEnemy ? aimColor : normalColor;
    }

    bool IsAimingAtEnemy()
    {
        Vector3 rayOrigin = playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector3 rayDirection = playerCamera.transform.forward;

        Debug.DrawRay(rayOrigin, rayDirection * maxRange, Color.red);

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxRange, enemyMask))
        {
            return hit.collider.GetComponent<EnemyHealth>() != null;
        }

        return false;
    }
}
