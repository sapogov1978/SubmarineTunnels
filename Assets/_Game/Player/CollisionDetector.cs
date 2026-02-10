using UnityEngine;

/// <summary>
/// Submarine collision handler
/// Manages damage, visual feedback, and knockback from obstacles and tunnel walls
/// Works with EdgeCollider2D for Bezier curve tunnels
/// </summary>
public class CollisionDetector : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float obstacleDamage = 33f;
    [SerializeField] private float wallDamage = 33f;

    [Header("Visual Feedback")]
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioClip collisionSound;

    [Header("Wall Knockback")]
    [SerializeField] private float wallKnockbackForce = 2f;
    [SerializeField] private float wallKnockbackDistance = 0.4f;
    [SerializeField] private float narrowKnockbackScale = 0.4f;
    [SerializeField] private float narrowWidthThreshold = 1.2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float flashTimer = 0f;
    private bool isFlashing = false;
    private Camera mainCamera;
    private Collider2D submarineCollider;
    private TunnelGenerator tunnelGenerator;

    // Cooldown to prevent multiple collisions per frame
    private float collisionCooldown = 0.5f;  // Increased from 0.1f to 0.5f
    private float lastCollisionTime = -1f;
    private GameObject lastCollidedObject = null;  // Track previous collision object

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        mainCamera = Camera.main;
        submarineCollider = GetComponent<Collider2D>();
        tunnelGenerator = FindObjectOfType<TunnelGenerator>();

        // Component diagnostics
        Debug.Log($"[CollisionDetector] Initialized on {gameObject.name}");
        Debug.Log($"[CollisionDetector] SpriteRenderer: {(spriteRenderer != null ? "FOUND" : "NOT FOUND")}");
        Debug.Log($"[CollisionDetector] Collider2D: {(submarineCollider != null ? "FOUND" : "NOT FOUND")}");
        Debug.Log($"[CollisionDetector] Rigidbody2D: {GetComponent<Rigidbody2D>() != null}");
    }

    void Update()
    {
        // Update damage flash effect
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;

            if (flashTimer <= 0f)
            {
                isFlashing = false;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = originalColor;
                }
            }
        }
    }

    /// <summary>
    /// Handle trigger collisions (non-blocking obstacles)
    /// For obstacles that DON'T block submarine movement
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[CollisionDetector] OnTriggerEnter2D: {collision.gameObject.name}");

        // Ignore if same object as last collision (entry-exit flicker prevention)
        if (collision.gameObject == lastCollidedObject)
            return;

        // Check cooldown to avoid multiple collisions per frame
        if (Time.time - lastCollisionTime < collisionCooldown)
            return;

        lastCollidedObject = collision.gameObject;
        lastCollisionTime = Time.time;

        // Collision with obstacle (Rock, Debris)
        if (collision.CompareTag("Obstacle"))
        {
            HandleObstacleCollision(collision.gameObject);
            return;
        }

        // Handle walls configured as triggers
        if (collision.CompareTag("TunnelWall"))
        {
            HandleWallCollision(collision);
        }
    }

    /// <summary>
    /// Handle physical collision with tunnel walls
    /// Required for submarine with Kinematic Rigidbody2D
    /// OnTriggerEnter2D doesn't fire with Kinematic, so we use OnCollisionEnter2D
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[CollisionDetector] OnCollisionEnter2D: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");

        // Check if collided with tunnel wall
        if (collision.gameObject.CompareTag("TunnelWall"))
        {
            // Ignore if same object as last collision
            if (collision.gameObject == lastCollidedObject)
                return;

            // Check cooldown
            if (Time.time - lastCollisionTime < collisionCooldown)
                return;

            lastCollidedObject = collision.gameObject;
            lastCollisionTime = Time.time;

            HandleWallCollision(collision);
        }
    }

    /// <summary>
    /// Handle collision with obstacle
    /// </summary>
    private void HandleObstacleCollision(GameObject obstacle)
    {
        if (showDebugLogs)
            Debug.Log($"[CollisionDetector] ▲ HIT OBSTACLE: {obstacle.name}");

        // Наносим урон
        if (OxygenManager.Instance != null)
        {
            float oxygenBefore = OxygenManager.Instance.GetOxygenPercentage();
            OxygenManager.Instance.RemoveOxygen(obstacleDamage);
            float oxygenAfter = OxygenManager.Instance.GetOxygenPercentage();
            
            if (showDebugLogs)
                Debug.Log($"[CollisionDetector] Damage: -{obstacleDamage}% | {oxygenBefore:F0}% → {oxygenAfter:F0}%");
        }

        // Visual feedback
        PlayDamageFeedback();

        // Sound effect
        PlayCollisionSound();
    }

    /// <summary>
    /// Handle collision with tunnel wall.
    /// </summary>
    private void HandleWallCollision(Collision2D collision)
    {
        if (showDebugLogs)
            Debug.Log("[CollisionDetector] HIT WALL!");

        if (OxygenManager.Instance != null)
        {
            float oxygenBefore = OxygenManager.Instance.GetOxygenPercentage();
            OxygenManager.Instance.RemoveOxygen(wallDamage);
            float oxygenAfter = OxygenManager.Instance.GetOxygenPercentage();
            
            if (showDebugLogs)
                Debug.Log($"[CollisionDetector] Damage: -{wallDamage}% | {oxygenBefore:F0}% -> {oxygenAfter:F0}%");
        }

        PlayDamageFeedback();
        PlayCollisionSound();
        ApplyWallKnockback(collision);
    }

    private void HandleWallCollision(Collider2D collision)
    {
        if (showDebugLogs)
            Debug.Log("[CollisionDetector] HIT WALL (trigger)!");

        if (OxygenManager.Instance != null)
        {
            float oxygenBefore = OxygenManager.Instance.GetOxygenPercentage();
            OxygenManager.Instance.RemoveOxygen(wallDamage);
            float oxygenAfter = OxygenManager.Instance.GetOxygenPercentage();
            
            if (showDebugLogs)
                Debug.Log($"[CollisionDetector] Damage: -{wallDamage}% | {oxygenBefore:F0}% -> {oxygenAfter:F0}%");
        }

        PlayDamageFeedback();
        PlayCollisionSound();
        ApplyWallKnockbackFromTrigger(collision);
    }

    private void ApplyWallKnockback(Collision2D collision)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        ContactPoint2D contact = collision.GetContact(0);
        Vector2 pushDir = contact.normal;
        ApplyKnockback(pushDir);
        rb.AddForce(pushDir * wallKnockbackForce, ForceMode2D.Impulse);
    }

    private void ApplyWallKnockbackFromTrigger(Collider2D collision)
    {
        Vector2 closest = collision.ClosestPoint(transform.position);
        Vector2 pushDir = ((Vector2)transform.position - closest).normalized;
        if (pushDir == Vector2.zero)
        {
            pushDir = transform.position.x >= collision.transform.position.x ? Vector2.right : Vector2.left;
        }

        ApplyKnockback(pushDir);
    }

    private void ApplyKnockback(Vector2 pushDir)
    {
        SubmarineController controller = GetComponent<SubmarineController>();
        if (controller == null) return;

        float pushScale = 1f;
        if (TryGetTunnelBoundsAtY(transform.position.y, out float leftX, out float rightX))
        {
            float radius = 0.1f;
            if (submarineCollider != null)
            {
                radius = submarineCollider.bounds.extents.x;
            }
            float minX = leftX + radius;
            float maxX = rightX - radius;

            float tunnelWidth = Mathf.Max(0.01f, rightX - leftX);
            if (tunnelWidth < narrowWidthThreshold)
            {
                pushScale = narrowKnockbackScale;
            }

            float pushX = pushDir.x * wallKnockbackDistance * pushScale;
            controller.ApplyKnockbackClamped(pushX, minX, maxX);
        }
        else
        {
            float pushX = pushDir.x * wallKnockbackDistance * pushScale;
            controller.ApplyKnockback(pushX);
        }
    }

    private bool TryGetTunnelBoundsAtY(float worldY, out float leftX, out float rightX)
    {
        if (tunnelGenerator == null)
        {
            leftX = rightX = 0f;
            return false;
        }

        foreach (var seg in tunnelGenerator.GetSegments())
        {
            if (seg != null && seg.GetWallPositionsAtY(worldY, out leftX, out rightX))
            {
                return true;
            }
        }

        leftX = rightX = 0f;
        return false;
    }

    /// <summary>
    /// Visual feedback on damage
    /// </summary>
    private void PlayDamageFeedback()
    {
        // Red flash effect
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageFlashColor;
            isFlashing = true;
            flashTimer = damageFlashDuration;
        }

        // Camera shake
        ShakeCamera();
    }

    /// <summary>
    /// Shake camera on collision
    /// </summary>
    private void ShakeCamera()
    {
        if (mainCamera == null) return;
        StartCoroutine(CameraShakeRoutine());
    }

    /// <summary>
    /// Camera shake coroutine
    /// </summary>
    private System.Collections.IEnumerator CameraShakeRoutine()
    {
        Vector3 originalPos = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Check if not paused (Time.timeScale == 0)
            if (Time.timeScale > 0)
            {
                elapsed += Time.deltaTime;

                // Random offset within magnitude bounds
                float offsetX = Random.Range(-shakeMagnitude, shakeMagnitude);
                float offsetY = Random.Range(-shakeMagnitude, shakeMagnitude);

                mainCamera.transform.position = originalPos + new Vector3(offsetX, offsetY, 0f);
            }

            yield return null;
        }

        // IMPORTANT: Return camera to original position after shake
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalPos;
        }
    }

    /// <summary>
    /// Play collision sound effect
    /// </summary>
    private void PlayCollisionSound()
    {
        if (collisionSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(collisionSound);
        }
    }

    /// <summary>
    /// Set obstacle damage (for balancing)
    /// </summary>
    public void SetObstacleDamage(float damage)
    {
        obstacleDamage = damage;
    }

    /// <summary>
    /// Set wall damage (for balancing)
    /// </summary>
    public void SetWallDamage(float damage)
    {
        wallDamage = damage;
    }

    // Test methods for debugging
    #if UNITY_EDITOR
    [ContextMenu("Test: Simulate Obstacle Hit")]
    private void TestObstacleHit()
    {
        HandleObstacleCollision(gameObject);
    }

    [ContextMenu("Test: Simulate Wall Hit")]
    private void TestWallHit()
    {
        if (OxygenManager.Instance != null)
        {
            OxygenManager.Instance.RemoveOxygen(wallDamage);
        }

        PlayDamageFeedback();
        PlayCollisionSound();
    }
    #endif
}

