using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural tunnel generator with Bezier curves
/// Spawns obstacles during segment creation with accurate wall positions
/// Dynamically adjusts width and curvature for varied difficulty
/// </summary>
public class TunnelGenerator : MonoBehaviour
{
    [Header("Prefab & Settings")]
    [SerializeField] private TunnelSegment segmentPrefab;
    [SerializeField] private float segmentHeight = 3f;
    [SerializeField] private float tunnelWidth = 1.5f;
    private float minTunnelWidth = 0f;
    private float maxTunnelWidth = 0f;

    [Header("Screen Settings")]
    [SerializeField] private Camera mainCamera;
    
    [Header("Obstacle Integration")]
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private int safeStartSegments = 10;
    [SerializeField] private int obstacleCooldownSegments = 1;
    [SerializeField] private float narrowWidthChanceMultiplier = 0.3f;
    [SerializeField] private float curveChanceMultiplier = 0.4f;
    [SerializeField] private float curvePenaltyOffset = 0.6f;

    private Queue<TunnelSegment> segments = new Queue<TunnelSegment>();
    private float lastOffset = 0f;
    private float lastTopY = 0f;
    private int segmentsOnScreen;
    
    private float currentWidth;
    private float targetWidth;
    private float prevWidth;
    private int narrowingSegments;
    private int narrowingCounter = 0;
    
    private float screenHalfWidth;
    private float uiMargin = 0.5f;
    private TunnelSegment lastSpawnedSegment;
    private int spawnedSegments = 0;
    private int segmentsSinceObstacle = 0;
    private float spawnedDistanceY = 0f;
    private bool runtimeMetricsApplied = false;

    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;

        screenHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        
        currentWidth = tunnelWidth;
        targetWidth = tunnelWidth;
        prevWidth = tunnelWidth;
        float scrollSpeed = RuntimeGameplayMetrics.ScrollSpeed;
        narrowingSegments = Mathf.Max(1, (int)(scrollSpeed * 2));
        runtimeMetricsApplied = ApplyRuntimeMetricsIfReady();

        float screenHeight = mainCamera.orthographicSize * 2f;
        segmentsOnScreen = Mathf.CeilToInt(screenHeight / segmentHeight) + 2;

        lastTopY = -mainCamera.orthographicSize - segmentHeight;
        lastOffset = 0f;

        for (int i = 0; i < segmentsOnScreen; i++)
        {
            SpawnSegment();
        }

        // ObstacleSpawner reads runtime metrics directly; no scroll speed injection needed.
    }

    void Update()
    {
        // Stop updating tunnel when game is over
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        // Apply runtime metrics once they become available
        if (!runtimeMetricsApplied)
        {
            runtimeMetricsApplied = ApplyRuntimeMetricsIfReady();
        }

        // Scroll all tunnel segments downward
        float scrollSpeed = RuntimeGameplayMetrics.ScrollSpeed;
        if (scrollSpeed > 0f)
        {
            foreach (var seg in segments)
            {
                seg.transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime);
            }
            lastTopY -= scrollSpeed * Time.deltaTime;
        }

        // Spawn new segment when the last segment's top edge enters visible area
        if (segments.Count > 0)
        {
            var lastSeg = lastSpawnedSegment;
            float topScreenBoundary = mainCamera.transform.position.y + mainCamera.orthographicSize;
            if (lastSeg != null && lastSeg.GetTopY() < topScreenBoundary)
            {
                SpawnSegment();
            }
        }

        // Destroy segments that have scrolled off-screen
        float destroyY = mainCamera.transform.position.y - mainCamera.orthographicSize - (segmentHeight * 3);
        while (segments.Count > 0 && segments.Peek().GetBottomY() < destroyY)
        {
            Destroy(segments.Dequeue().gameObject);
        }
    }

    private void SpawnSegment()
    {
        TunnelSegment seg = Instantiate(segmentPrefab, transform);

        float baseY = lastSpawnedSegment != null
            ? lastSpawnedSegment.transform.position.y
            : lastTopY;
        float posY = baseY + segmentHeight - 0.2f;
        seg.transform.position = new Vector3(0, posY, 0);

        narrowingCounter++;
        
        if (narrowingCounter >= narrowingSegments)
        {
            narrowingCounter = 0;
            targetWidth = Random.Range(minTunnelWidth, maxTunnelWidth);
        }
        
        float step = 1f / narrowingSegments;
        currentWidth = Mathf.Lerp(currentWidth, targetWidth, step);
        RuntimeGameplayMetrics.UpdateCurrentTunnelWidth(currentWidth);

        float usableWidth = screenHalfWidth * 2f - (uiMargin * 2f);
        float maxAllowedOffset = (usableWidth - currentWidth) / 2f;
        
        float maxCurveChange = Mathf.Min(
            (currentWidth - 1.5f) * 0.2f,
            maxAllowedOffset * 0.5f
        );
        
        float newOffset = lastOffset + Random.Range(-maxCurveChange, maxCurveChange);
        newOffset = Mathf.Clamp(newOffset, -maxAllowedOffset, maxAllowedOffset);

        float halfStartWidth = prevWidth / 2f;
        float halfEndWidth = currentWidth / 2f;

        seg.leftStart = new Vector2(lastOffset - halfStartWidth, 0);
        seg.leftEnd   = new Vector2(newOffset - halfEndWidth, segmentHeight);
        seg.leftControl1 = new Vector2(seg.leftStart.x, segmentHeight * 0.33f);
        seg.leftControl2 = new Vector2(seg.leftEnd.x, segmentHeight * 0.66f);

        seg.rightStart = new Vector2(lastOffset + halfStartWidth, 0);
        seg.rightEnd   = new Vector2(newOffset + halfEndWidth, segmentHeight);
        seg.rightControl1 = new Vector2(seg.rightStart.x, segmentHeight * 0.33f);
        seg.rightControl2 = new Vector2(seg.rightEnd.x, segmentHeight * 0.66f);

        seg.Build();

        segments.Enqueue(seg);
        lastSpawnedSegment = seg;
        spawnedSegments++;
        segmentsSinceObstacle++;
        spawnedDistanceY += segmentHeight;

        // Spawn obstacle with this segment's parameters
        // Obstacles are spawned immediately to ensure accurate wall alignment
        if (obstacleSpawner != null && spawnedSegments > safeStartSegments && segmentsSinceObstacle > obstacleCooldownSegments)
        {
            // Calculate spawn chance based on tunnel width (narrower = less obstacles)
            float widthT = Mathf.InverseLerp(minTunnelWidth, maxTunnelWidth, currentWidth);
            float widthChance = Mathf.Lerp(narrowWidthChanceMultiplier, 1f, widthT);

            // Calculate spawn chance based on tunnel curvature (sharper curves = less obstacles)
            float curveDelta = Mathf.Abs(newOffset - lastOffset);
            float curveT = Mathf.InverseLerp(0f, curvePenaltyOffset, curveDelta);
            float curveChance = Mathf.Lerp(1f, curveChanceMultiplier, curveT);

            float chanceMultiplier = widthChance * curveChance;

            // Get exact wall positions at spawn height for precise obstacle placement
            float segmentTopY = posY + segmentHeight;
            if (seg.GetWallPositionsAtY(segmentTopY, out float leftWallX, out float rightWallX))
            {
                bool spawned = obstacleSpawner.SpawnObstacleForSegment(
                    segmentTopY,
                    leftWallX,
                    rightWallX,
                    chanceMultiplier,
                    spawnedDistanceY
                );
                if (spawned)
                {
                    segmentsSinceObstacle = 0;
                }
            }
        }

        prevWidth = currentWidth;
        lastOffset = newOffset;
        lastTopY = posY;
    }

    private bool ApplyRuntimeMetricsIfReady()
    {
        if (RuntimeGameplayMetrics.MinTunnelWidth <= 0f || RuntimeGameplayMetrics.MaxTunnelWidth <= 0f)
            return false;

        minTunnelWidth = RuntimeGameplayMetrics.MinTunnelWidth;
        maxTunnelWidth = RuntimeGameplayMetrics.MaxTunnelWidth;
        float scrollSpeed = RuntimeGameplayMetrics.ScrollSpeed;
        if (scrollSpeed > 0f)
            narrowingSegments = Mathf.Max(1, (int)(scrollSpeed * 2));

        tunnelWidth = Mathf.Clamp(tunnelWidth, minTunnelWidth, maxTunnelWidth);
        currentWidth = Mathf.Clamp(currentWidth, minTunnelWidth, maxTunnelWidth);
        targetWidth = Mathf.Clamp(targetWidth, minTunnelWidth, maxTunnelWidth);
        prevWidth = Mathf.Clamp(prevWidth, minTunnelWidth, maxTunnelWidth);

        return true;
    }
    
    public float GetScrollSpeed() { return RuntimeGameplayMetrics.ScrollSpeed; }
    public IEnumerable<TunnelSegment> GetSegments() { return segments; }
}



