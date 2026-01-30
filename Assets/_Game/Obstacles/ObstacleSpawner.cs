using UnityEngine;

/// <summary>
/// УЛУЧШЕННЫЙ спавнер препятствий в туннеле
/// Учитывает ширину туннеля и обеспечивает возможность прохода
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private Rock rockPrefab;
    [SerializeField] private Debris debrisPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 1.5f;
    [SerializeField] private float spawnYOffset = 15f;

    [Header("Tunnel Integration")]
    [SerializeField] private TunnelGenerator tunnelGenerator;
    [SerializeField] private float submarineWidth = 0.8f; // ширина батискафа
    [SerializeField] private float safetyMargin = 0.1f; // дополнительный запас для прохода

    [Header("Width-Based Spawning")]
    [SerializeField] private float minWidthToSpawn = 1.8f; // минимальная ширина туннеля для спавна
    [SerializeField] private float wideWidthThreshold = 2.2f; // "широкий" туннель
    [SerializeField] private int maxObstaclesInWideArea = 3; // макс препятствий в широкой части

    [Header("Spawn Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float rockChance = 0.7f; // шанс спавна камня вместо обломков

    [Header("Obstacle Sizing")]
    [SerializeField] private float minObstacleScale = 0.5f; 
    [SerializeField] private float maxObstacleScale = 1.0f; 

    [Header("Object Pooling")]
    [SerializeField] private int initialRockPoolSize = 15; 
    [SerializeField] private int initialDebrisPoolSize = 20;

    [Header("Difficulty Scaling")]
    [SerializeField] private bool increaseDifficulty = true;
    [SerializeField] private float difficultyIncreaseRate = 0.95f;
    [SerializeField] private float minInterval = 0.8f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showGizmos = true;

    private ObjectPool<Rock> rockPool;
    private ObjectPool<Debris> debrisPool;
    private float spawnTimer;
    private float nextSpawnInterval;
    private float difficultyTimer;
    private Camera mainCamera;

    // Данные о текущей ширине туннеля
    private float currentTunnelWidth = 1.5f;
    private float currentTunnelOffset = 0f;

    void Start()
    {
        mainCamera = Camera.main;

        // Находим TunnelGenerator если не назначен
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
        SetNextSpawnInterval();

        if (showDebugLogs)
            Debug.Log($"[ObstacleSpawner] Initialized. Min width to spawn: {minWidthToSpawn}");
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

        // Увеличение сложности
        if (increaseDifficulty)
        {
            difficultyTimer += Time.deltaTime;
            
            if (difficultyTimer >= 10f)
            {
                difficultyTimer = 0f;
                minSpawnInterval = Mathf.Max(minInterval, minSpawnInterval * difficultyIncreaseRate);
                maxSpawnInterval = Mathf.Max(minInterval + 1f, maxSpawnInterval * difficultyIncreaseRate);

                if (showDebugLogs)
                    Debug.Log($"[ObstacleSpawner] Difficulty increased! Intervals: {minSpawnInterval:F1}s - {maxSpawnInterval:F1}s");
            }
        }
    }

    /// <summary>
    /// Получает информацию о текущей ширине туннеля из TunnelGenerator
    /// </summary>
    private void UpdateTunnelInfo()
    {
        if (tunnelGenerator == null) return;

        // Используем рефлексию чтобы получить приватные поля
        var currentWidthField = typeof(TunnelGenerator).GetField("currentWidth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var lastOffsetField = typeof(TunnelGenerator).GetField("lastOffset", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (currentWidthField != null)
            currentTunnelWidth = (float)currentWidthField.GetValue(tunnelGenerator);
        
        if (lastOffsetField != null)
            currentTunnelOffset = (float)lastOffsetField.GetValue(tunnelGenerator);
    }

    /// <summary>
    /// Пытается заспавнить препятствия с учётом ширины туннеля
    /// </summary>
    private void TrySpawnObstacles()
    {
        // Проверка 1: Туннель слишком узкий?
        if (currentTunnelWidth < minWidthToSpawn)
        {
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Tunnel too narrow ({currentTunnelWidth:F2}), skipping spawn");
            return;
        }

        // Проверка 2: Достаточно ли места для прохода?
        float availableSpace = currentTunnelWidth - submarineWidth - (safetyMargin * 2);
        if (availableSpace < 0.3f) // минимум 0.3 единицы для препятствия
        {
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Not enough space for obstacle, skipping");
            return;
        }

        // Определяем сколько препятствий можно заспавнить
        int obstacleCount = CalculateObstacleCount();

        for (int i = 0; i < obstacleCount; i++)
        {
            SpawnSingleObstacle();
        }
    }

    /// <summary>
    /// Вычисляет количество препятствий в зависимости от ширины
    /// </summary>
    private int CalculateObstacleCount()
    {
        if (currentTunnelWidth >= wideWidthThreshold)
        {
            // Широкая часть - может быть несколько препятствий
            return Random.Range(1, maxObstaclesInWideArea + 1);
        }
        else
        {
            // Средняя ширина - одно препятствие
            return 1;
        }
    }

    /// <summary>
    /// Спавнит одно препятствие
    /// </summary>
    private void SpawnSingleObstacle()
    {
        bool spawnRock = Random.value <= rockChance;
        Vector3 spawnPosition = GetSafeSpawnPosition();
        float obstacleScale = CalculateObstacleScale();

        if (spawnRock && rockPrefab != null)
        {
            Rock rock = rockPool.Get(spawnPosition, Quaternion.identity);
            rock.transform.localScale = Vector3.one * obstacleScale;
            
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Spawned Rock at {spawnPosition}, scale: {obstacleScale:F2}");
        }
        else if (!spawnRock && debrisPrefab != null)
        {
            Debris debris = debrisPool.Get(spawnPosition, Quaternion.identity);
            debris.transform.localScale = Vector3.one * obstacleScale;
            
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Spawned Debris at {spawnPosition}, scale: {obstacleScale:F2}");
        }
    }

    /// <summary>
    /// Вычисляет безопасную позицию для спавна (гарантирует проход)
    /// </summary>
    private Vector3 GetSafeSpawnPosition()
    {
        float spawnX;
        float spawnY = mainCamera.transform.position.y + spawnYOffset;

        // Вычисляем границы туннеля с учётом смещения
        float halfWidth = currentTunnelWidth / 2f;
        float leftWall = currentTunnelOffset - halfWidth;
        float rightWall = currentTunnelOffset + halfWidth;

        // Вычисляем зону где могут быть препятствия (с учётом прохода для батискафа)
        float passageWidth = submarineWidth + (safetyMargin * 2);
        float obstacleZoneWidth = currentTunnelWidth - passageWidth;

        if (obstacleZoneWidth <= 0.3f)
        {
            // Если места совсем мало - спавним в центре туннеля (с небольшим смещением)
            spawnX = currentTunnelOffset + Random.Range(-0.2f, 0.2f);
            return new Vector3(spawnX, spawnY, 0f);
        }

        // Случайно выбираем: слева от прохода или справа
        bool spawnLeft = Random.value > 0.5f;
        
        if (spawnLeft)
        {
            // Левая зона (между левой стеной и проходом)
            float leftZoneMin = leftWall + 0.2f; // отступ от стены
            float leftZoneMax = currentTunnelOffset - (passageWidth / 2f) - 0.1f;
            spawnX = Random.Range(leftZoneMin, leftZoneMax);
        }
        else
        {
            // Правая зона (между проходом и правой стеной)
            float rightZoneMin = currentTunnelOffset + (passageWidth / 2f) + 0.1f;
            float rightZoneMax = rightWall - 0.2f; // отступ от стены
            spawnX = Random.Range(rightZoneMin, rightZoneMax);
        }

        return new Vector3(spawnX, spawnY, 0f);
    }

    /// <summary>
    /// Вычисляет размер препятствия в зависимости от ширины туннеля
    /// </summary>
    private float CalculateObstacleScale()
    {
        // В узких местах - мелкие препятствия
        // В широких местах - крупные препятствия
        float normalizedWidth = Mathf.InverseLerp(minWidthToSpawn, wideWidthThreshold, currentTunnelWidth);
        float scale = Mathf.Lerp(minObstacleScale, maxObstacleScale, normalizedWidth);
        
        // Добавляем небольшую случайность
        scale *= Random.Range(0.85f, 1.15f);
        
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

    // Публичный метод для обновления ширины туннеля (опциональный)
    public void UpdateTunnelWidth(float width, float offset = 0f)
    {
        currentTunnelWidth = width;
        currentTunnelOffset = offset;
    }

    // DEBUG: Визуализация зон спавна
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || mainCamera == null) return;

        float spawnY = mainCamera.transform.position.y + spawnYOffset;

        // Границы туннеля
        float halfWidth = currentTunnelWidth / 2f;
        float leftWall = currentTunnelOffset - halfWidth;
        float rightWall = currentTunnelOffset + halfWidth;

        // Зона прохода (безопасная для игрока)
        float passageWidth = submarineWidth + (safetyMargin * 2);
        float passageLeft = currentTunnelOffset - (passageWidth / 2f);
        float passageRight = currentTunnelOffset + (passageWidth / 2f);

        // Рисуем границы туннеля (жёлтый)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(leftWall, spawnY - 2, 0), new Vector3(leftWall, spawnY + 2, 0));
        Gizmos.DrawLine(new Vector3(rightWall, spawnY - 2, 0), new Vector3(rightWall, spawnY + 2, 0));

        // Рисуем зону прохода (зелёный)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(passageLeft, spawnY - 1, 0), new Vector3(passageLeft, spawnY + 1, 0));
        Gizmos.DrawLine(new Vector3(passageRight, spawnY - 1, 0), new Vector3(passageRight, spawnY + 1, 0));
        Gizmos.DrawLine(new Vector3(passageLeft, spawnY, 0), new Vector3(passageRight, spawnY, 0));

        // Рисуем зоны где могут быть препятствия (красный)
        Gizmos.color = Color.red;
        // Левая зона
        Gizmos.DrawLine(new Vector3(leftWall + 0.2f, spawnY, 0), new Vector3(passageLeft - 0.1f, spawnY, 0));
        // Правая зона
        Gizmos.DrawLine(new Vector3(passageRight + 0.1f, spawnY, 0), new Vector3(rightWall - 0.2f, spawnY, 0));
    }

    #if UNITY_EDITOR
    [ContextMenu("Test: Spawn in Current Width")]
    private void TestSpawnInCurrentWidth()
    {
        UpdateTunnelInfo();
        Debug.Log($"Current tunnel width: {currentTunnelWidth:F2}, offset: {currentTunnelOffset:F2}");
        TrySpawnObstacles();
    }

    [ContextMenu("Debug: Show Current Tunnel Info")]
    private void ShowTunnelInfo()
    {
        UpdateTunnelInfo();
        Debug.Log($"[ObstacleSpawner] Width: {currentTunnelWidth:F2}, Offset: {currentTunnelOffset:F2}, Can spawn: {currentTunnelWidth >= minWidthToSpawn}");
    }
    #endif
}