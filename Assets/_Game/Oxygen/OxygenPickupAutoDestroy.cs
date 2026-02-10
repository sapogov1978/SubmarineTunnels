using UnityEngine;

/// <summary>
/// Auto-deactivates oxygen pickup when it scrolls off-screen
/// Returns pickup to object pool for reuse
/// Similar to ObstacleAutoDestroy but for OxygenPickup
/// </summary>
[RequireComponent(typeof(OxygenPickup))]
public class OxygenPickupAutoDestroy : MonoBehaviour
{
    private float destroyOffset = 2f;  // Additional distance beyond screen edge

    private Camera mainCamera;
    private float bottomBoundary;

    void Start()
    {
        mainCamera = Camera.main;
        if (!mainCamera)
        {
            Debug.LogError("[OxygenPickupAutoDestroy] Main camera not found!");
            enabled = false;
        }
    }

    void Update()
    {
        if (!mainCamera) return;

        // Dynamic offset based on scroll speed
        if (RuntimeGameplayMetrics.ScrollSpeed > 0f)
            destroyOffset = RuntimeGameplayMetrics.ScrollSpeed;

        // Calculate bottom screen boundary
        bottomBoundary = mainCamera.transform.position.y - mainCamera.orthographicSize - destroyOffset;

        // Deactivate if pickup has scrolled below boundary
        if (transform.position.y < bottomBoundary)
        {
            DeactivatePickup();
        }
    }

    /// <summary>
    /// Deactivate oxygen pickup (return to pool)
    /// </summary>
    private void DeactivatePickup()
    {
        // Find ObstacleSpawner and return object to pool
        ObstacleSpawner spawner = FindObjectOfType<ObstacleSpawner>();

        if (spawner != null)
        {
            OxygenPickup pickup = GetComponent<OxygenPickup>();
            spawner.ReturnOxygenToPool(pickup);
        }
        else
        {
            // Fallback: simply deactivate if spawner not found
            gameObject.SetActive(false);
        }
    }
}
