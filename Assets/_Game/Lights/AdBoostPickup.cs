using UnityEngine;

/// <summary>
/// Ad-based light boost pickup
/// Activates light boost after collecting (ad integration placeholder)
/// TODO: Integrate with rewarded ads system
/// </summary>
public class AdBoostPickup : MonoBehaviour
{
    private float destroyOffset = 2f;
    [SerializeField] private bool rotateWhileMoving = true;
    [SerializeField] private float rotationSpeed = 0f;  // Degrees per second

    [Header("Visual Feedback")]
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private AudioClip pickupSound;

    [Header("Boost Settings")]
    [SerializeField] private float boostDuration = 25f;  // Seconds

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool skipAdForTesting = true;  // Skip ad in test mode

    private bool hasBeenCollected = false;
    private Camera mainCamera;
    private float bottomBoundary;

    void Update()
    {
        if (!mainCamera)
            mainCamera = Camera.main;

        // Move downward synchronized with tunnel
        float scrollSpeed = RuntimeGameplayMetrics.ScrollSpeed;
        if (scrollSpeed <= 0f)
            return;

        transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime, Space.World);

        // Auto-destroy when off-screen
        if (mainCamera)
        {
            bottomBoundary = mainCamera.transform.position.y - mainCamera.orthographicSize - destroyOffset;
            if (transform.position.y < bottomBoundary)
            {
                Destroy(gameObject);
                return;
            }
        }

        // Rotate to attract attention
        if (rotateWhileMoving)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Handle collision with submarine
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[AdBoostPickup] OnTriggerEnter2D with: '{other.gameObject.name}', Tag: '{other.tag}'");
        }

        // Check if collided with player
        if (other.CompareTag("Player") && !hasBeenCollected)
        {
            hasBeenCollected = true;
            CollectPickup();
        }
    }

    /// <summary>
    /// Collect ad boost pickup
    /// </summary>
    private void CollectPickup()
    {
        if (showDebugLogs)
            Debug.Log("[AdBoostPickup] Ad boost pickup collected!");

        // Placeholder: activate boost without ad in test mode
        // TODO: Integrate with RewardedAdsManager.ShowAd()
        if (skipAdForTesting)
        {
            if (showDebugLogs)
                Debug.Log("[AdBoostPickup] Skipping ad (test mode), activating boost directly");

            ActivateBoost();
        }
        else
        {
            // TODO: Show rewarded ad
            // RewardedAdsManager.Instance.ShowRewardedAd(OnAdWatched, OnAdFailed);
            Debug.LogWarning("[AdBoostPickup] Ad system not implemented yet! Activating boost anyway.");
            ActivateBoost();
        }

        // Visual effect
        if (pickupEffectPrefab != null)
        {
            Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
        }

        // Sound effect
        if (pickupSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(pickupSound);
        }

        // Destroy pickup
        // TODO: Consider using object pooling for better performance
        Destroy(gameObject);
    }

    /// <summary>
    /// Activate light boost
    /// Called after successful ad view (or immediately in test mode)
    /// </summary>
    private void ActivateBoost()
    {
        if (LightBoostManager.Instance != null)
        {
            LightBoostManager.Instance.ActivateBoost(boostDuration);

            if (showDebugLogs)
                Debug.Log("[AdBoostPickup] Light boost activated!");
        }
        else
        {
            Debug.LogError("[AdBoostPickup] LightBoostManager not found!");
        }
    }

    /// <summary>
    /// Callback: ad watched successfully
    /// TODO: Integrate with Unity Ads or similar rewarded ad system
    /// </summary>
    private void OnAdWatched()
    {
        if (showDebugLogs)
            Debug.Log("[AdBoostPickup] Ad watched! Activating boost...");

        ActivateBoost();
    }

    /// <summary>
    /// Callback: ad failed to show or was closed
    /// TODO: Integrate with Unity Ads or similar rewarded ad system
    /// </summary>
    private void OnAdFailed()
    {
        if (showDebugLogs)
            Debug.Log("[AdBoostPickup] Ad failed or skipped. No boost.");

        // Do nothing - boost is not activated
    }

    #if UNITY_EDITOR
    [ContextMenu("Test: Collect Pickup")]
    private void TestCollectPickup()
    {
        CollectPickup();
    }
    #endif
}
