using UnityEngine;

public class LookTowardsPlayer : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 50f;

    private Transform cam;

    void Start()
    {
        // Cache the camera transform (faster + safer)
        if (Camera.main != null)
            cam = Camera.main.transform;
        else
            Debug.LogWarning("No camera tagged MainCamera found in scene.");
    }

    void Update()
    {
        if (!cam) return;

        Vector3 direction = cam.position - transform.position;
        direction.y = 0f; // keep upright (no tilt)

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
