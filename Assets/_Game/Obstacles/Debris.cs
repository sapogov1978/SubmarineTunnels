using UnityEngine;

/// <summary>
/// Debris obstacle with horizontal drift movement
/// Uses object pooling for efficient memory management
/// Drifts side to side while scrolling down with tunnel
/// Constrained by dynamic tunnel walls
/// </summary>
public class Debris : Obstacle
{
    [Header("Drift")]
    [SerializeField] private bool driftSideways = true;
    [SerializeField] private float driftSpeed = 1.5f;
    [SerializeField] private float driftAmplitude = 0.25f;
    [SerializeField] private float wallMargin = 0.1f;  // Safety margin from tunnel walls

    private float driftTimer = 0f;
    private float startX;
    private float debrisRadius = 0.12f;
    private TunnelGenerator tunnelGenerator;
    private float cachedLeftWall;
    private float cachedRightWall;
    private float cachedWallY;
    private bool wallsCached = false;

    void Start()
    {
        startX = transform.position.x;
        driftTimer = Random.Range(0f, 2f * Mathf.PI);

        // Find tunnel generator for wall boundary calculations
        if (tunnelGenerator == null)
        {
            tunnelGenerator = FindObjectOfType<TunnelGenerator>();
        }
    }

    void Update()
    {
        // Move downward with tunnel scroll
        float scrollSpeed = RuntimeGameplayMetrics.ScrollSpeed;
        if (scrollSpeed <= 0f)
            return;

        transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime, Space.World);

        // Drift left and right using sine wave
        if (driftSideways)
        {
            driftTimer += driftSpeed * Time.deltaTime;
            float offsetX = Mathf.Sin(driftTimer) * driftAmplitude;

            Vector3 pos = transform.position;
            pos.x = startX + offsetX;

            // OPTIMIZATION: Cache wall bounds, only update if Y position changed significantly
            bool needsUpdate = !wallsCached || Mathf.Abs(pos.y - cachedWallY) > 1.5f;

            if (needsUpdate && tunnelGenerator != null && TryGetTunnelBounds(pos.y, out float leftWall, out float rightWall))
            {
                cachedLeftWall = leftWall;
                cachedRightWall = rightWall;
                cachedWallY = pos.y;
                wallsCached = true;
            }

            // Constrain to tunnel boundaries
            if (wallsCached)
            {
                // Add margin to prevent debris from touching walls
                float minX = cachedLeftWall + debrisRadius + wallMargin;
                float maxX = cachedRightWall - debrisRadius - wallMargin;
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
            }
            else
            {
                // Fallback: use simple tunnel width from runtime metrics
                float halfWidth = RuntimeGameplayMetrics.CurrentTunnelWidth * 0.5f;
                if (halfWidth > 0f)
                {
                    pos.x = Mathf.Clamp(pos.x, -halfWidth + debrisRadius + wallMargin,
                                              halfWidth - debrisRadius - wallMargin);
                }
            }

            transform.position = pos;
        }
    }

    protected override void OnHit(GameObject player)
    {
        // DO NOT call base.OnHit() - Debris uses pooling!
        // base.OnHit() calls Destroy(gameObject), which breaks pooling

        if (showDebugLogs)
            Debug.Log("[Debris] Hit! Returning to pool");

        // Visual effect (from Obstacle base class)
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Sound effect (from Obstacle base class)
        if (hitSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hitSound);
        }

        // CRITICAL: Return to pool instead of Destroy
        ObstacleSpawner spawner = FindObjectOfType<ObstacleSpawner>();
        if (spawner != null)
        {
            spawner.ReturnToPool(this);
            if (showDebugLogs)
                Debug.Log("[Debris] Returned to pool");
        }
        else
        {
            // Fallback: deactivate if spawner not found
            gameObject.SetActive(false);
            if (showDebugLogs)
                Debug.Log("[Debris] No spawner found, deactivated");
        }
    }

    public override void ResetObstacle()
    {
        base.ResetObstacle();
        driftTimer = Random.Range(0f, 2f * Mathf.PI);
        if (transform.position.x != 0)
            startX = transform.position.x;
        wallsCached = false; // Clear cache on reset
    }

    public void SetRadius(float radius)
    {
        debrisRadius = radius;
    }

    /// <summary>
    /// Get tunnel wall positions at debris's current Y position
    /// </summary>
    private bool TryGetTunnelBounds(float worldY, out float leftX, out float rightX)
    {
        if (tunnelGenerator == null)
        {
            leftX = rightX = 0f;
            return false;
        }

        // Check all tunnel segments to find the one at this Y position
        foreach (var segment in tunnelGenerator.GetSegments())
        {
            if (segment != null && segment.GetWallPositionsAtY(worldY, out leftX, out rightX))
            {
                return true;
            }
        }

        leftX = rightX = 0f;
        return false;
    }
}
