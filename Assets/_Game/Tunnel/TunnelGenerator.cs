using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Генератор туннеля
/// ПРАВИЛЬНО: Препятствия спавнятся ПРИ СОЗДАНИИ сегмента с его параметрами
/// </summary>
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

        lastTopY = -mainCamera.orthographicSize - segmentHeight;
        lastOffset = 0f;

        for (int i = 0; i < segmentsOnScreen; i++)
        {
            SpawnSegment();
        }

        if (obstacleSpawner != null)
        {
            obstacleSpawner.SetScrollSpeed(scrollSpeed);
        }
    }

    void Update()
    {
        // НЕ обновляем туннель если игра закончилась
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        foreach (var seg in segments)
        {
            seg.transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime);
        }
        lastTopY -= scrollSpeed * Time.deltaTime;

        if (segments.Count > 0)
        {
            var lastSeg = lastSpawnedSegment;
            // Спавним сегмент когда верхний край последнего сегмента выходит за верхнюю границу видимой области
            float topScreenBoundary = mainCamera.transform.position.y + mainCamera.orthographicSize;
            if (lastSeg != null && lastSeg.GetTopY() < topScreenBoundary)
            {
                SpawnSegment();
            }
        }

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

        // КЛЮЧЕВОЙ МОМЕНТ: Спавним препятствие СРАЗУ с параметрами этого сегмента
        if (obstacleSpawner != null && spawnedSegments > safeStartSegments && segmentsSinceObstacle > obstacleCooldownSegments)
        {
            float widthT = Mathf.InverseLerp(minTunnelWidth, maxTunnelWidth, currentWidth);
            float widthChance = Mathf.Lerp(narrowWidthChanceMultiplier, 1f, widthT);

            float curveDelta = Mathf.Abs(newOffset - lastOffset);
            float curveT = Mathf.InverseLerp(0f, curvePenaltyOffset, curveDelta);
            float curveChance = Mathf.Lerp(1f, curveChanceMultiplier, curveT);

            float chanceMultiplier = widthChance * curveChance;

            // Передаём КОНЕЧНЫЕ параметры сегмента (его верх)
            bool spawned = obstacleSpawner.SpawnObstacleForSegment(
                posY + segmentHeight,  // Y верха сегмента
                newOffset,             // Offset КОНЦА сегмента
                currentWidth,          // Ширина КОНЦА сегмента
                chanceMultiplier,
                spawnedDistanceY
            );
            if (spawned)
            {
                segmentsSinceObstacle = 0;
            }
        }

        prevWidth = currentWidth;
        lastOffset = newOffset;
        lastTopY = posY;
    }
    
    public float GetScrollSpeed() { return scrollSpeed; }
    public IEnumerable<TunnelSegment> GetSegments() { return segments; }
}



