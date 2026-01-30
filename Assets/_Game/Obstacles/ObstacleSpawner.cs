using UnityEngine;

/// <summary>
/// Спавнер препятствий в туннеле
/// Управляет созданием камней и обломков с использованием Object Pooling
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private Rock rockPrefab;
    [SerializeField] private Debris debrisPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnInterval = 1.5f; // минимальный интервал между спавнами
    [SerializeField] private float maxSpawnInterval = 3.5f; // максимальный интервал
    [SerializeField] private float spawnYOffset = 15f; // высота над камерой для спавна

    [Header("Spawn Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float rockChance = 0.6f; // 60% камни, 40% обломки

    [Header("Positioning")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float minDistanceFromWalls = 0.3f; // минимальное расстояние от стенок туннеля
    [SerializeField] private float tunnelWidth = 1.5f; // текущая ширина туннеля (обновляется из TunnelGenerator)

    [Header("Object Pooling")]
    [SerializeField] private int initialRockPoolSize = 15;
    [SerializeField] private int initialDebrisPoolSize = 20;

    [Header("Difficulty Scaling")]
    [SerializeField] private bool increaseDifficulty = true;
    [SerializeField] private float difficultyIncreaseRate = 0.95f; // множитель для интервалов (меньше = чаще спавн)
    [SerializeField] private float minInterval = 0.8f; // минимально возможный интервал

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private bool showGizmos = true;

    private ObjectPool<Rock> rockPool;
    private ObjectPool<Debris> debrisPool;
    private float spawnTimer;
    private float nextSpawnInterval;
    private float difficultyTimer;

    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;

        // Создаём пулы объектов
        InitializePools();

        // Устанавливаем первый интервал
        SetNextSpawnInterval();

        if (showDebugLogs)
            Debug.Log($"[ObstacleSpawner] Initialized. Pools: Rocks={initialRockPoolSize}, Debris={initialDebrisPoolSize}");
    }

    void Update()
    {
        // Не спавним если игра не активна
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying())
            return;

        // Таймер спавна
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= nextSpawnInterval)
        {
            SpawnObstacle();
            spawnTimer = 0f;
            SetNextSpawnInterval();
        }

        // Увеличение сложности со временем
        if (increaseDifficulty)
        {
            difficultyTimer += Time.deltaTime;
            
            // Каждые 10 секунд уменьшаем интервалы
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
    /// Инициализация пулов объектов
    /// </summary>
    private void InitializePools()
    {
        // Создаём родительские объекты для организации иерархии
        Transform rockParent = new GameObject("RockPool").transform;
        rockParent.SetParent(transform);

        Transform debrisParent = new GameObject("DebrisPool").transform;
        debrisParent.SetParent(transform);

        // Инициализируем пулы
        rockPool = new ObjectPool<Rock>(rockPrefab, initialRockPoolSize, rockParent);
        debrisPool = new ObjectPool<Debris>(debrisPrefab, initialDebrisPoolSize, debrisParent);
    }

    /// <summary>
    /// Спавн одного препятствия
    /// </summary>
    private void SpawnObstacle()
    {
        // Определяем тип препятствия случайным образом
        bool spawnRock = Random.value <= rockChance;

        // Получаем позицию спавна
        Vector3 spawnPosition = GetSpawnPosition();

        // Спавним из пула
        if (spawnRock && rockPrefab != null)
        {
            Rock rock = rockPool.Get(spawnPosition, Quaternion.identity);
            
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Spawned Rock at {spawnPosition}");
        }
        else if (!spawnRock && debrisPrefab != null)
        {
            Debris debris = debrisPool.Get(spawnPosition, Quaternion.identity);
            
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Spawned Debris at {spawnPosition}");
        }
    }

    /// <summary>
    /// Получить случайную позицию спавна внутри туннеля
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        if (!mainCamera)
        {
            Debug.LogError("[ObstacleSpawner] Main camera not found!");
            return Vector3.zero;
        }

        // Y позиция - выше камеры
        float spawnY = mainCamera.transform.position.y + spawnYOffset;

        // X позиция - внутри туннеля с учётом отступов от стенок
        float halfWidth = tunnelWidth / 2f;
        float minX = -halfWidth + minDistanceFromWalls;
        float maxX = halfWidth - minDistanceFromWalls;
        float spawnX = Random.Range(minX, maxX);

        return new Vector3(spawnX, spawnY, 0f);
    }

    /// <summary>
    /// Установить следующий интервал спавна
    /// </summary>
    private void SetNextSpawnInterval()
    {
        nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    /// <summary>
    /// Обновить ширину туннеля (вызывается из TunnelGenerator)
    /// </summary>
    public void UpdateTunnelWidth(float newWidth)
    {
        tunnelWidth = newWidth;
    }

    /// <summary>
    /// Вернуть препятствие в пул (вызывается когда препятствие уходит за пределы экрана)
    /// </summary>
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

    // DEBUG: Визуализация зоны спавна
    void OnDrawGizmos()
    {
        if (!showGizmos || !mainCamera) return;

        Gizmos.color = Color.yellow;
        
        float spawnY = mainCamera.transform.position.y + spawnYOffset;
        float halfWidth = tunnelWidth / 2f;
        float minX = -halfWidth + minDistanceFromWalls;
        float maxX = halfWidth - minDistanceFromWalls;

        // Линия зоны спавна
        Gizmos.DrawLine(
            new Vector3(minX, spawnY, 0),
            new Vector3(maxX, spawnY, 0)
        );

        // Маркеры границ
        Gizmos.DrawWireSphere(new Vector3(minX, spawnY, 0), 0.2f);
        Gizmos.DrawWireSphere(new Vector3(maxX, spawnY, 0), 0.2f);
    }

    // DEBUG методы для тестирования
    #if UNITY_EDITOR
    [ContextMenu("Test: Spawn Rock")]
    private void TestSpawnRock()
    {
        if (rockPrefab != null)
        {
            Rock rock = rockPool.Get(GetSpawnPosition(), Quaternion.identity);
            Debug.Log("[ObstacleSpawner] Test rock spawned");
        }
    }

    [ContextMenu("Test: Spawn Debris")]
    private void TestSpawnDebris()
    {
        if (debrisPrefab != null)
        {
            Debris debris = debrisPool.Get(GetSpawnPosition(), Quaternion.identity);
            Debug.Log("[ObstacleSpawner] Test debris spawned");
        }
    }

    [ContextMenu("Debug: Show Pool Sizes")]
    private void ShowPoolSizes()
    {
        Debug.Log($"[ObstacleSpawner] Rock Pool: {rockPool.GetPoolSize()}, Debris Pool: {debrisPool.GetPoolSize()}");
    }
    #endif
}