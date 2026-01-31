using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ИСПРАВЛЕННЫЙ спавнер препятствий
/// Решает ВСЕ проблемы со спавном
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private Rock rockPrefab;
    [SerializeField] private Debris debrisPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnInterval = 2.0f;
    [SerializeField] private float maxSpawnInterval = 4.0f;
    [SerializeField] private float spawnYOffset = 15f;
    [SerializeField] private float initialDelay = 3f; // ИСПРАВЛЕНИЕ 3: задержка перед первым спавном

    [Header("Tunnel Integration")]
    [SerializeField] private TunnelGenerator tunnelGenerator;
    [SerializeField] private float submarineWidth = 0.8f;
    [SerializeField] private float safetyMargin = 0.5f; // увеличен для большего запаса

    [Header("Width-Based Spawning")]
    [SerializeField] private float minWidthToSpawn = 2.0f; // увеличено чтобы не спавнить в узких местах
    [SerializeField] private float wideWidthThreshold = 2.5f;
    // maxObstaclesInWideArea удалён - теперь всегда спавнится только 1 препятствие

    [Header("Spawn Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float rockChance = 0.7f;

    [Header("Obstacle Sizing")]
    [SerializeField] private float minObstacleScale = 0.6f;
    [SerializeField] private float maxObstacleScale = 1.0f;

    [Header("Safe Spawn Distance")]
    [SerializeField] private float minDistanceBetweenObstacles = 3f; // ИСПРАВЛЕНИЕ 6 и 7

    [Header("Object Pooling")]
    [SerializeField] private int initialRockPoolSize = 15;
    [SerializeField] private int initialDebrisPoolSize = 20;

    [Header("Difficulty Scaling")]
    [SerializeField] private bool increaseDifficulty = true;
    [SerializeField] private float difficultyIncreaseRate = 0.95f;
    [SerializeField] private float minInterval = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showGizmos = true;

    private ObjectPool<Rock> rockPool;
    private ObjectPool<Debris> debrisPool;
    private float spawnTimer;
    private float nextSpawnInterval;
    private float difficultyTimer;
    private Camera mainCamera;

    private float currentTunnelWidth = 1.5f;
    private float currentTunnelOffset = 0f;
    
    // ИСПРАВЛЕНИЕ 6 и 7: отслеживание последнего спавна
    private float lastSpawnY = float.MaxValue;
    private List<Vector3> activeObstaclePositions = new List<Vector3>();

    void Start()
    {
        mainCamera = Camera.main;

        if (tunnelGenerator == null)
        {
            tunnelGenerator = FindObjectOfType<TunnelGenerator>();
            if (tunnelGenerator == null)
            {
                Debug.LogError("[ObstacleSpawner] TunnelGenerator not found!");
                enabled = false;
                return;
            }
        }

        InitializePools();
        
        // ИСПРАВЛЕНИЕ 3: начинаем с задержки
        spawnTimer = -initialDelay;
        SetNextSpawnInterval();

        if (showDebugLogs)
            Debug.Log($"[ObstacleSpawner] Initialized with {initialDelay}s delay");
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying())
            return;

        // Получаем текущую ширину туннеля
        UpdateTunnelInfo();

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= nextSpawnInterval)
        {
            TrySpawnObstacles();
            spawnTimer = 0f;
            SetNextSpawnInterval();
        }

        // Очистка списка активных препятствий (удаляем те что ушли вниз)
        CleanupActiveObstacles();

        if (increaseDifficulty)
        {
            difficultyTimer += Time.deltaTime;
            
            if (difficultyTimer >= 15f)
            {
                difficultyTimer = 0f;
                minSpawnInterval = Mathf.Max(minInterval, minSpawnInterval * difficultyIncreaseRate);
                maxSpawnInterval = Mathf.Max(minInterval + 1f, maxSpawnInterval * difficultyIncreaseRate);

                if (showDebugLogs)
                    Debug.Log($"[ObstacleSpawner] Difficulty increased! Intervals: {minSpawnInterval:F1}s - {maxSpawnInterval:F1}s");
            }
        }
    }

    private void UpdateTunnelInfo()
    {
        if (tunnelGenerator == null) return;

        // Используем публичные методы вместо рефлексии
        currentTunnelWidth = tunnelGenerator.GetCurrentWidth();
        currentTunnelOffset = tunnelGenerator.GetCurrentOffset();
    }

    /// <summary>
    /// Публичный метод для обновления ширины туннеля (вызывается из TunnelGenerator)
    /// </summary>
    public void UpdateTunnelWidth(float width, float offset = 0f)
    {
        currentTunnelWidth = width;
        currentTunnelOffset = offset;
    }

    private void TrySpawnObstacles()
    {
        // Проверка 1: Туннель слишком узкий?
        if (currentTunnelWidth < minWidthToSpawn)
        {
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Tunnel too narrow ({currentTunnelWidth:F2}), skipping");
            return;
        }

        // Проверка 2: Достаточно ли места для прохода?
        float requiredPassageWidth = submarineWidth + (safetyMargin * 2);
        float availableSpace = currentTunnelWidth - requiredPassageWidth;
        
        if (availableSpace < 0.4f)
        {
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Not enough space for obstacle ({availableSpace:F2}), skipping");
            return;
        }

        // ИСПРАВЛЕНИЕ 6: Проверка расстояния от последнего спавна
        float currentSpawnY = mainCamera.transform.position.y + spawnYOffset;
        if (lastSpawnY - currentSpawnY < minDistanceBetweenObstacles)
        {
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Too close to last spawn, skipping");
            return;
        }

        // Спавним ОДНО препятствие (ИСПРАВЛЕНИЕ 7)
        SpawnSingleObstacle();
        lastSpawnY = currentSpawnY;
    }

    private void SpawnSingleObstacle()
    {
        bool spawnRock = Random.value <= rockChance;
        
        // ИСПРАВЛЕНИЕ 5: Проход может быть СЛЕВА, СПРАВА или В ЦЕНТРЕ
        Vector3 spawnPosition = GetSafeSpawnPosition();
        
        // ИСПРАВЛЕНИЕ 7: Проверяем что нет конфликта с существующими препятствиями
        if (IsPositionBlocked(spawnPosition))
        {
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Position blocked by existing obstacle, skipping");
            return;
        }

        float obstacleScale = CalculateObstacleScale();

        if (spawnRock && rockPrefab != null)
        {
            Rock rock = rockPool.Get(spawnPosition, Quaternion.identity);
            rock.transform.localScale = Vector3.one * obstacleScale;
            activeObstaclePositions.Add(spawnPosition);
            
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Spawned Rock at {spawnPosition}, scale: {obstacleScale:F2}");
        }
        else if (!spawnRock && debrisPrefab != null)
        {
            Debris debris = debrisPool.Get(spawnPosition, Quaternion.identity);
            debris.transform.localScale = Vector3.one * obstacleScale;
            activeObstaclePositions.Add(spawnPosition);
            
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Spawned Debris at {spawnPosition}, scale: {obstacleScale:F2}");
        }
    }

    /// <summary>
    /// ИСПРАВЛЕНИЕ 2, 5: Вычисляет безопасную позицию с учётом реальных границ
    /// </summary>
    private Vector3 GetSafeSpawnPosition()
    {
        float spawnY = mainCamera.transform.position.y + spawnYOffset;

        // Реальные границы туннеля
        float halfWidth = currentTunnelWidth / 2f;
        float leftWall = currentTunnelOffset - halfWidth;
        float rightWall = currentTunnelOffset + halfWidth;

        // Ширина прохода для батискафа
        float passageWidth = submarineWidth + (safetyMargin * 2);

        // ИСПРАВЛЕНИЕ 5: Проход может быть в ЛЮБОМ месте туннеля
        // Вычисляем случайную позицию для прохода
        float passageCenter = Random.Range(
            leftWall + passageWidth / 2f + 0.2f,
            rightWall - passageWidth / 2f - 0.2f
        );

        float passageLeft = passageCenter - passageWidth / 2f;
        float passageRight = passageCenter + passageWidth / 2f;

        // Препятствие может быть слева или справа от прохода
        bool spawnLeft = Random.value > 0.5f;

        float spawnX;
        if (spawnLeft && (passageLeft - leftWall > 0.4f))
        {
            // Слева от прохода
            float leftZoneMin = leftWall + 0.25f;
            float leftZoneMax = passageLeft - 0.2f;
            spawnX = Random.Range(leftZoneMin, leftZoneMax);
        }
        else if (!spawnLeft && (rightWall - passageRight > 0.4f))
        {
            // Справа от прохода
            float rightZoneMin = passageRight + 0.2f;
            float rightZoneMax = rightWall - 0.25f;
            spawnX = Random.Range(rightZoneMin, rightZoneMax);
        }
        else
        {
            // Недостаточно места - пропускаем спавн
            spawnX = currentTunnelOffset; // временная позиция (будет отклонена проверкой)
        }

        // ИСПРАВЛЕНИЕ 2: Убеждаемся что X в пределах туннеля
        spawnX = Mathf.Clamp(spawnX, leftWall + 0.3f, rightWall - 0.3f);

        return new Vector3(spawnX, spawnY, 0f);
    }

    /// <summary>
    /// ИСПРАВЛЕНИЕ 7: Проверяет что позиция не заблокирована другим препятствием
    /// </summary>
    private bool IsPositionBlocked(Vector3 position)
    {
        foreach (var obstaclePos in activeObstaclePositions)
        {
            float horizontalDistance = Mathf.Abs(position.x - obstaclePos.x);
            float verticalDistance = Mathf.Abs(position.y - obstaclePos.y);
            
            // Если препятствия слишком близко по горизонтали И по вертикали
            if (horizontalDistance < 0.8f && verticalDistance < 2f)
            {
                return true; // Позиция заблокирована
            }
        }
        return false;
    }

    /// <summary>
    /// Очищает список активных препятствий (удаляет те что ушли вниз)
    /// </summary>
    private void CleanupActiveObstacles()
    {
        float bottomBoundary = mainCamera.transform.position.y - mainCamera.orthographicSize - 5f;
        activeObstaclePositions.RemoveAll(pos => pos.y < bottomBoundary);
    }

    private float CalculateObstacleScale()
    {
        float normalizedWidth = Mathf.InverseLerp(minWidthToSpawn, wideWidthThreshold, currentTunnelWidth);
        float scale = Mathf.Lerp(minObstacleScale, maxObstacleScale, normalizedWidth);
        scale *= Random.Range(0.9f, 1.1f);
        return Mathf.Clamp(scale, minObstacleScale, maxObstacleScale);
    }

    private void InitializePools()
    {
        Transform rockParent = new GameObject("RockPool").transform;
        rockParent.SetParent(transform);

        Transform debrisParent = new GameObject("DebrisPool").transform;
        debrisParent.SetParent(transform);

        rockPool = new ObjectPool<Rock>(rockPrefab, initialRockPoolSize, rockParent);
        debrisPool = new ObjectPool<Debris>(debrisPrefab, initialDebrisPoolSize, debrisParent);
    }

    private void SetNextSpawnInterval()
    {
        nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    public void ReturnToPool(Obstacle obstacle)
    {
        if (obstacle is Rock rock)
        {
            rockPool.Return(rock);
        }
        else if (obstacle is Debris debris)
        {
            debrisPool.Return(debris);
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || mainCamera == null) return;

        float spawnY = mainCamera.transform.position.y + spawnYOffset;
        float halfWidth = currentTunnelWidth / 2f;
        float leftWall = currentTunnelOffset - halfWidth;
        float rightWall = currentTunnelOffset + halfWidth;

        // Границы туннеля (жёлтый)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(leftWall, spawnY - 3, 0), new Vector3(leftWall, spawnY + 3, 0));
        Gizmos.DrawLine(new Vector3(rightWall, spawnY - 3, 0), new Vector3(rightWall, spawnY + 3, 0));

        // Активные препятствия (красный)
        Gizmos.color = Color.red;
        foreach (var pos in activeObstaclePositions)
        {
            Gizmos.DrawWireSphere(pos, 0.5f);
        }
    }

    #if UNITY_EDITOR
    [ContextMenu("Debug: Show Tunnel Info")]
    private void ShowTunnelInfo()
    {
        UpdateTunnelInfo();
        Debug.Log($"[ObstacleSpawner] Width: {currentTunnelWidth:F2}, Offset: {currentTunnelOffset:F2}");
        Debug.Log($"Left wall: {currentTunnelOffset - currentTunnelWidth / 2f:F2}, Right wall: {currentTunnelOffset + currentTunnelWidth / 2f:F2}");
    }
    #endif
}