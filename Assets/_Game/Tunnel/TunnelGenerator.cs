using System.Collections.Generic;
using UnityEngine;

public class TunnelGenerator : MonoBehaviour
{
    [Header("Prefab & Settings")]
    [SerializeField] private TunnelSegment segmentPrefab;
    [SerializeField] private float segmentHeight = 3f;
    [SerializeField] private float tunnelWidth = 1.5f;
    [SerializeField] private float minTunnelWidth = 1f;
    [SerializeField] private float maxTunnelWidth = 2.5f;
    [SerializeField] private float scrollSpeed = 2f;

    [Header("Screen Settings")]
    [SerializeField] private Camera mainCamera;

    [Header("Obstacle Integration")]
    [SerializeField] private ObstacleSpawner obstacleSpawner;


    private Queue<TunnelSegment> segments = new Queue<TunnelSegment>();
    private float lastOffset = 0f;
    private float lastTopY = 0f;
    private int segmentsOnScreen;
    
    private float currentWidth;
    private float targetWidth;
    private float prevWidth;
    private int narrowingSegments;
    private int narrowingCounter = 0;
    
    // FIXED: Screen boundaries for tunnel positioning
    private float screenHalfWidth;
    private float uiMargin = 0.5f; // 1/4 screen on each side for UI/controls

    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;

        // FIXED: Calculate usable screen area (middle 50% of screen)
        screenHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        
        currentWidth = tunnelWidth;
        targetWidth = tunnelWidth;
        prevWidth = tunnelWidth;
        narrowingSegments = Mathf.Max(1, (int)(scrollSpeed * 2));

        float screenHeight = mainCamera.orthographicSize * 2f;
        segmentsOnScreen = Mathf.CeilToInt(screenHeight / segmentHeight) + 2;

        for (int i = 0; i < segmentsOnScreen; i++)
        {
            SpawnSegment();
        }
    }

    void Update()
    {
        // Movement of all segments downward
        foreach (var seg in segments)
        {
            seg.transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime);
        }

        // Spawn new segment BEFORE the old one leaves the screen
        if (segments.Count > 0)
        {
            var lastSeg = segments.Peek();
            if (lastSeg.GetTopY() + lastSeg.transform.position.y < mainCamera.orthographicSize)
            {
                SpawnSegment();
            }
        }

        // Remove old segments that went below the screen
        while (segments.Count > 0 && segments.Peek().GetBottomY() < -mainCamera.orthographicSize)
        {
            Destroy(segments.Dequeue().gameObject);
        }
    }

    private void SpawnSegment()
    {
        TunnelSegment seg = Instantiate(segmentPrefab, transform);

        // Position exactly above previous segment with overlap to hide seams
        float posY = lastTopY + segmentHeight - 0.1f;
        seg.transform.position = new Vector3(0, posY, 0);

        // Counter for tracking narrowing progress
        narrowingCounter++;
        
        // If counter reaches the number of segments for narrowing - choose new target width
        if (narrowingCounter >= narrowingSegments)
        {
            narrowingCounter = 0;
            targetWidth = Random.Range(minTunnelWidth, maxTunnelWidth);
        }
        
        // Smooth interpolation of current width to target
        float step = 1f / narrowingSegments;
        currentWidth = Mathf.Lerp(currentWidth, targetWidth, step);

        // transfer actual tunnel width to obsticle spawner
        if (obstacleSpawner != null)
        {
            obstacleSpawner.UpdateTunnelWidth(currentWidth);
        }

        // FIXED: Calculate maximum safe offset to keep tunnel on screen
        // Screen layout: [1/4 UI] [2/4 TUNNEL] [1/4 UI]
        float usableWidth = screenHalfWidth * 2f - (uiMargin * 2f); // Middle 50% of screen
        float maxAllowedOffset = (usableWidth - currentWidth) / 2f;
        
        // FIXED: Limit curvature based on current tunnel width and screen bounds
        float maxCurveChange = Mathf.Min(
            (currentWidth - 1.5f) * 0.2f,  // Curve based on width
            maxAllowedOffset * 0.5f         // Don't move too much per segment
        );
        
        float newOffset = lastOffset + Random.Range(-maxCurveChange, maxCurveChange);
        
        // FIXED: Clamp offset to ensure entire tunnel stays visible
        newOffset = Mathf.Clamp(newOffset, -maxAllowedOffset, maxAllowedOffset);

        float halfStartWidth = prevWidth / 2f;
        float halfEndWidth = currentWidth / 2f;

        // Cubic Bezier: start → control1 → control2 → end
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

        prevWidth = currentWidth;
        lastOffset = newOffset;
        lastTopY = posY;
    }
    
    // DEBUG: Visualize tunnel boundaries in Scene view
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || mainCamera == null) return;
        
        Gizmos.color = Color.yellow;
        float screenWidth = mainCamera.orthographicSize * mainCamera.aspect * 2f;
        float usableWidth = screenWidth - (uiMargin * 2f);
        float halfUsable = usableWidth / 2f;
        
        // Draw usable area boundaries (where tunnel should stay)
        Gizmos.DrawLine(
            new Vector3(-halfUsable, -10, 0),
            new Vector3(-halfUsable, 30, 0)
        );
        Gizmos.DrawLine(
            new Vector3(halfUsable, -10, 0),
            new Vector3(halfUsable, 30, 0)
        );
    }
}