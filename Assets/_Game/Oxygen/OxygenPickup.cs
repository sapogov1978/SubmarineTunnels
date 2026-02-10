using UnityEngine;

/// <summary>
/// Collectible oxygen canister pickup
/// Restores oxygen when collected by submarine
/// Uses object pooling for efficient spawning
/// </summary>
public class OxygenPickup : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private float oxygenAmount = 25f;  // Percentage of oxygen restored

    [Header("Movement")]
    [SerializeField] private bool rotateWhileMoving = true;
    [SerializeField] private float rotationSpeed = 22.5f;  // Degrees per second

    [Header("Visual Feedback")]
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private AudioClip pickupSound;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private bool hasBeenCollected = false;

    void Start()
    {
        // Validate components in debug mode
        if (showDebugLogs)
        {
            ValidateComponents();
        }
    }

    void Update()
    {
        // Move downward synchronized with tunnel scroll
        float scrollSpeed = RuntimeGameplayMetrics.ScrollSpeed;
        if (scrollSpeed <= 0f)
            return;

        transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime, Space.World);

        // Rotate for visual effect
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
            Debug.Log($"[OxygenPickup] OnTriggerEnter2D with: '{other.gameObject.name}', Tag: '{other.tag}'");
        }

        // Check if collided with player
        if (other.CompareTag("Player") && !hasBeenCollected)
        {
            hasBeenCollected = true;
            CollectPickup();
        }
        else if (showDebugLogs)
        {
            if (!other.CompareTag("Player"))
            {
                Debug.LogWarning($"[OxygenPickup] Tag '{other.tag}' is not 'Player' - canister NOT collected!");
            }
            else if (hasBeenCollected)
            {
                Debug.LogWarning($"[OxygenPickup] Canister already collected!");
            }
        }
    }

    /// <summary>
    /// DEBUG: Handle physical collision (if Is Trigger = FALSE)
    /// This should NOT trigger in normal conditions!
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (showDebugLogs)
        {
            Debug.LogError($"[OxygenPickup] COLLISION (not trigger!) with: {collision.gameObject.name}");
            Debug.LogError($"[OxygenPickup] PROBLEM: Collider must have Is Trigger = TRUE!");
        }
    }

    /// <summary>
    /// Collect oxygen canister and restore oxygen
    /// </summary>
    private void CollectPickup()
    {
        if (showDebugLogs)
            Debug.Log($"[OxygenPickup] Collected! +{oxygenAmount}% oxygen");

        // Restore oxygen through OxygenManager
        if (OxygenManager.Instance != null)
        {
            float oxygenBefore = OxygenManager.Instance.GetOxygenPercentage();
            OxygenManager.Instance.AddOxygen(oxygenAmount);
            float oxygenAfter = OxygenManager.Instance.GetOxygenPercentage();

            if (showDebugLogs)
                Debug.Log($"[OxygenPickup] Oxygen: {oxygenBefore:F0}% → {oxygenAfter:F0}%");
        }
        else
        {
            Debug.LogError("[OxygenPickup] OxygenManager not found!");
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

        // Return to pool instead of destroying (like Debris.cs)
        ObstacleSpawner spawner = FindObjectOfType<ObstacleSpawner>();
        if (spawner != null)
        {
            spawner.ReturnOxygenToPool(this);
        }
        else
        {
            // Fallback: deactivate or destroy
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Reset state for object pool reuse
    /// </summary>
    public void ResetPickup()
    {
        hasBeenCollected = false;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Set amount of oxygen restored on pickup
    /// </summary>
    public void SetOxygenAmount(float amount)
    {
        oxygenAmount = amount;
    }

    // Test method for debugging
    #if UNITY_EDITOR
    [ContextMenu("Test: Collect Pickup")]
    private void TestCollectPickup()
    {
        CollectPickup();
    }

    /// <summary>
    /// DEBUG: Validate component configuration
    /// </summary>
    private void ValidateComponents()
    {
        Debug.Log("=== [OxygenPickup] Component Validation ===");

        // Check Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"✅ Rigidbody2D: FOUND (Body Type: {rb.bodyType})");
            if (rb.bodyType != RigidbodyType2D.Kinematic)
            {
                Debug.LogWarning($"⚠️ Rigidbody2D should be Kinematic, not {rb.bodyType}!");
            }
        }
        else
        {
            Debug.LogError("❌ Rigidbody2D: NOT FOUND! Add Rigidbody2D component!");
        }

        // Check Collider2D
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Debug.Log($"✅ Collider2D: FOUND (Type: {col.GetType().Name})");
            if (col.isTrigger)
            {
                Debug.Log("✅ Is Trigger: TRUE (correct)");
            }
            else
            {
                Debug.LogError("❌ Is Trigger: FALSE! Enable Is Trigger on collider!");
            }
        }
        else
        {
            Debug.LogError("❌ Collider2D: NOT FOUND! Add Circle/Polygon Collider2D!");
        }

        // Check SpriteRenderer
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Debug.Log($"✅ SpriteRenderer: FOUND (Sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")})");
        }
        else
        {
            Debug.LogWarning("⚠️ SpriteRenderer: NOT FOUND (canister will be invisible)");
        }

        Debug.Log("===========================================");
    }
    #endif
}
