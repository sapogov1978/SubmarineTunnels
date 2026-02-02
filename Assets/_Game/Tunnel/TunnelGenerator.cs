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
    
    private float screenHalfWidth;
    private float uiMargin = 0.5f;

    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;

        screenHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        
        currentWidth = tunnelWidth;
        targetWidth = tunnelWidth;
        prevWidth = tunnelWidth;
        narrowingSegments = Mathf.Max(1, (int)(scrollSpeed * 2));

        float screenHeight = mainCamera.orthographicSize * 2f;
        segmentsOnScreen = Mathf.CeilToInt(screenHeight / segmentHeight) + 2;

        // ИСПРАВЛЕНИЕ ПРОБЛЕМЫ 1: Начинаем спавн с НИЖНЕЙ части экрана
        lastTopY = -mainCamera.orthographicSize - segmentHeight;

        for (int i = 0; i < segmentsOnScreen; i++)
        {
            SpawnSegment();
        }

        Debug.Log($"[TunnelGenerator] Started. Initial Y: {lastTopY:F2}, Segments: {segmentsOnScreen}");
    }

    void Update()
    {
        foreach (var seg in segments)
        {
            seg.transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime);
        }

        if (segments.Count > 0)
        {
            var lastSeg = segments.Peek();
            if (lastSeg.GetTopY() + lastSeg.transform.position.y < mainCamera.orthographicSize)
            {
                SpawnSegment();
            }
        }

        while (segments.Count > 0 && segments.Peek().GetBottomY() < -mainCamera.orthographicSize)
        {
            Destroy(segments.Dequeue().gameObject);
        }
    }

    private void SpawnSegment()
    {
        TunnelSegment seg = Instantiate(segmentPrefab, transform);

        float posY = lastTopY + segmentHeight - 0.1f;
        seg.transform.position = new Vector3(0, posY, 0);

        narrowingCounter++;
        
        if (narrowingCounter >= narrowingSegments)
        {
            narrowingCounter = 0;
            targetWidth = Random.Range(minTunnelWidth, maxTunnelWidth);
        }
        
        float step = 1f / narrowingSegments;
        currentWidth = Mathf.Lerp(currentWidth, targetWidth, step);

        if (obstacleSpawner != null)
        {
            obstacleSpawner.UpdateTunnelWidth(currentWidth, lastOffset);
        }

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

        prevWidth = currentWidth;
        lastOffset = newOffset;
        lastTopY = posY;
    }

    // ПУБЛИЧНЫЕ МЕТОДЫ (ИСПРАВЛЕНИЕ ПРОБЛЕМЫ 2)
    public float GetCurrentWidth() { return currentWidth; }
    public float GetCurrentOffset() { return lastOffset; }
    
    public void GetTunnelBounds(out float leftWall, out float rightWall)
    {
        float halfWidth = currentWidth / 2f;
        leftWall = lastOffset - halfWidth;
        rightWall = lastOffset + halfWidth;
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || mainCamera == null) return;
        
        Gizmos.color = Color.yellow;
        float screenWidth = mainCamera.orthographicSize * mainCamera.aspect * 2f;
        float usableWidth = screenWidth - (uiMargin * 2f);
        float halfUsable = usableWidth / 2f;
        
        Gizmos.DrawLine(
            new Vector3(-halfUsable, -10, 0),
            new Vector3(-halfUsable, 30, 0)
        );
        Gizmos.DrawLine(
            new Vector3(halfUsable, -10, 0),
            new Vector3(halfUsable, 30, 0)
        );
    }

    public bool GetWallsAtY(float worldY, out float leftX, out float rightX)
    {
        foreach (var seg in segments)
        {
            float bottom = seg.GetBottomY();
            float top = seg.GetTopY();

            if (worldY >= bottom && worldY <= top)
            {
                float t = Mathf.InverseLerp(bottom, top, worldY);

                leftX = Mathf.Lerp(seg.leftStart.x, seg.leftEnd.x, t);
                rightX = Mathf.Lerp(seg.rightStart.x, seg.rightEnd.x, t);
                return true;
            }
        }

        leftX = rightX = 0;
        return false;
    }

    public float GetSpawnY()
    {
        return lastTopY - 1.5f;
    }

    public IEnumerable<TunnelSegment> GetSegments()
    {
        return segments;
    }
}