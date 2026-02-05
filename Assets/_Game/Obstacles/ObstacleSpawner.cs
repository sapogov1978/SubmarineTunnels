using UnityEngine;

/// <summary>
/// Спавнер препятствий с адаптивным размером
/// В узких туннелях препятствия становятся меньше
/// День 7: Добавлен спавн кислородных баллонов
/// День 8: Добавлен спавн рекламных шариков
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private Rock rockPrefab;
    [SerializeField] private Debris debrisPrefab;
    [SerializeField] private OxygenPickup oxygenPickupPrefab;
    [SerializeField] private AdBoostPickup adBoostPickupPrefab;

    [Header("Spawn Chance")]
    [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.3f;

    [Header("Spawn Spacing")]
    [SerializeField] private float minObstacleSpacingY = 3f;

    [Header("Oxygen Pickup Settings")]
    [SerializeField] private float oxygenSpawnInterval = 12f; // секунды между спавном кислорода
    [SerializeField] private float oxygenAmount = 25f; // сколько % кислорода восстанавливает
    [SerializeField] private float oxygenPickupScale = 0.4f; // размер баллона (0.4 = 40% от оригинала)
    [SerializeField] private bool spawnOxygenEnabled = true;

    [Header("Ad Boost Pickup Settings")]
    [SerializeField] private float adBoostSpawnInterval = 35f; // секунды между спавном (реже чем кислород)
    [SerializeField] private float adBoostPickupScale = 0.5f; // размер шарика
    [SerializeField] private bool spawnAdBoostEnabled = true;

    [Header("Sizes (радиусы)")]
    [SerializeField] private float submarineRadius = 0.125f;
    [SerializeField] private float maxRockRadius = 0.15f;      // Максимальный размер
    [SerializeField] private float maxDebrisRadius = 0.12f;    // Максимальный размер
    [SerializeField] private float minObstacleRadius = 0.08f;  // Минимальный размер
    [SerializeField] private float safetyMargin = 0.1f;

    [Header("Pooling")]
    [SerializeField] private int initialRockPoolSize = 10;
    [SerializeField] private int initialDebrisPoolSize = 15;
    [SerializeField] private int initialOxygenPoolSize = 5;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private ObjectPool<Rock> rockPool;
    private ObjectPool<Debris> debrisPool;
    private ObjectPool<OxygenPickup> oxygenPool;
    private float scrollSpeed = 2f;
    private float lastObstacleY = float.NegativeInfinity;
    private float oxygenSpawnTimer = 0f;
    private float adBoostSpawnTimer = 0f;

    void Start()
    {
        InitializePools();
        // Начальная задержка перед первым кислородом
        oxygenSpawnTimer = oxygenSpawnInterval / 2f;
        // Начальная задержка перед первым рекламным шариком
        adBoostSpawnTimer = adBoostSpawnInterval / 2f;
    }

    void Update()
    {
        // Таймер для спавна кислорода
        if (spawnOxygenEnabled)
        {
            oxygenSpawnTimer += Time.deltaTime;
        }

        // День 8: Таймер для спавна рекламных шариков
        if (spawnAdBoostEnabled)
        {
            adBoostSpawnTimer += Time.deltaTime;
        }
    }

    private void InitializePools()
    {
        rockPool = new ObjectPool<Rock>(rockPrefab, initialRockPoolSize, transform);
        debrisPool = new ObjectPool<Debris>(debrisPrefab, initialDebrisPoolSize, transform);
        
        if (oxygenPickupPrefab != null)
        {
            oxygenPool = new ObjectPool<OxygenPickup>(oxygenPickupPrefab, initialOxygenPoolSize, transform);
        }
    }

    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }

    /// <summary>
    /// Спавн препятствия для сегмента туннеля
    /// День 7: Добавлена возможность спавна кислородных баллонов
    /// День 8: Добавлен спавн рекламных шариков (ПРИОРИТЕТ #1)
    /// </summary>
    public bool SpawnObstacleForSegment(float segmentTopY, float segmentOffset, float segmentWidth, float chanceMultiplier = 1f, float segmentProgressY = float.NaN)
    {
        bool spawned = false;

        // ДЕНЬ 8: Проверяем нужен ли рекламный шарик (ПРИОРИТЕТ #1!)
        if (spawnAdBoostEnabled && adBoostSpawnTimer >= adBoostSpawnInterval)
        {
            // Спавним рекламный шарик в ЦЕНТРЕ туннеля
            SpawnAdBoostPickup(segmentTopY, segmentOffset);
            adBoostSpawnTimer = 0f;
            spawned = true;

            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] 🎯 Ad boost pickup spawned at Y={segmentTopY:F0}");
            
            // Не спавним другие объекты в том же сегменте
            return spawned;
        }

        // ВАЖНО: Проверяем нужен ли кислородный баллон (ПРИОРИТЕТ #2)
        if (spawnOxygenEnabled && oxygenSpawnTimer >= oxygenSpawnInterval)
        {
            // Спавним кислород в ЦЕНТРЕ туннеля (легко собрать)
            SpawnOxygenPickup(segmentTopY, segmentOffset);
            oxygenSpawnTimer = 0f;
            spawned = true;

            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Oxygen pickup spawned at Y={segmentTopY:F0}");
            
            // Не спавним обычное препятствие в том же сегменте
            return spawned;
        }

        // Обычная логика спавна препятствий (Rock/Debris)
        float spacingY = float.IsNaN(segmentProgressY) ? segmentTopY : segmentProgressY;
        if (Mathf.Abs(spacingY - lastObstacleY) < minObstacleSpacingY) return false;

        float finalChance = Mathf.Clamp01(spawnChance * Mathf.Clamp01(chanceMultiplier));
        if (Random.value > finalChance) return false;

        bool spawnRock = Random.value > 0.5f;
        float maxRadius = spawnRock ? maxRockRadius : maxDebrisRadius;

        // Границы туннеля
        float leftWall = segmentOffset - segmentWidth / 2f;
        float rightWall = segmentOffset + segmentWidth / 2f;

        // Проход для submarine
        float passageRadius = submarineRadius + safetyMargin;
        float passageLeft = segmentOffset - passageRadius;
        float passageRight = segmentOffset + passageRadius;

        // Доступное пространство с каждой стороны
        float leftSpace = passageLeft - leftWall;
        float rightSpace = rightWall - passageRight;

        // АДАПТИВНЫЙ РАЗМЕР: вычисляем максимальный радиус для каждой стороны
        float minGap = 0.05f;
        float maxLeftRadius = (leftSpace - minGap) / 2f;
        float maxRightRadius = (rightSpace - minGap) / 2f;
        
        float maxPossibleRadius = Mathf.Max(maxLeftRadius, maxRightRadius);
        float obstacleRadius = Mathf.Clamp(maxPossibleRadius, minObstacleRadius, maxRadius);

        // Пересчитываем зоны с адаптивным радиусом
        float leftZoneStart = leftWall + obstacleRadius;
        float leftZoneEnd = passageLeft - obstacleRadius;
        
        float rightZoneStart = passageRight + obstacleRadius;
        float rightZoneEnd = rightWall - obstacleRadius;

        float leftZoneWidth = leftZoneEnd - leftZoneStart;
        float rightZoneWidth = rightZoneEnd - rightZoneStart;

        if (leftZoneWidth < 0.05f && rightZoneWidth < 0.05f)
        {
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Too narrow even with min size: {segmentWidth:F2}");
            return false;
        }

        // Выбираем сторону
        bool spawnLeft;
        if (leftZoneWidth < 0.05f)
            spawnLeft = false;
        else if (rightZoneWidth < 0.05f)
            spawnLeft = true;
        else
            spawnLeft = Random.value > 0.5f;

        float spawnX = spawnLeft 
            ? Random.Range(leftZoneStart, leftZoneEnd)
            : Random.Range(rightZoneStart, rightZoneEnd);

        float spawnY = segmentTopY;
        Vector3 pos = new Vector3(spawnX, spawnY, 0f);

        // Создаём препятствие с адаптивным размером
        if (spawnRock)
        {
            Rock rock = rockPool.Get(pos, Quaternion.identity);
            rock.SetScrollSpeed(scrollSpeed);
            float scale = obstacleRadius / maxRockRadius;
            rock.transform.localScale = Vector3.one * scale;
        }
        else
        {
            Debris debris = debrisPool.Get(pos, Quaternion.identity);
            debris.SetScrollSpeed(scrollSpeed);
            debris.SetRadius(obstacleRadius);
            float scale = obstacleRadius / maxDebrisRadius;
            debris.transform.localScale = Vector3.one * scale;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[ObstacleSpawner] Spawned {(spawnRock ? "Rock" : "Debris")} at ({spawnX:F2}, {spawnY:F2})");
        }

        lastObstacleY = spacingY;
        return true;
    }

    /// <summary>
    /// Спавн кислородного баллона
    /// Спавнится в ЦЕНТРЕ туннеля для лёгкого сбора
    /// </summary>
    private void SpawnOxygenPickup(float spawnY, float segmentOffset)
    {
        if (oxygenPool == null)
        {
            Debug.LogError("[ObstacleSpawner] Oxygen pool not initialized!");
            return;
        }

        // Спавним в центре туннеля
        Vector3 pos = new Vector3(segmentOffset, spawnY, 0f);

        OxygenPickup oxygen = oxygenPool.Get(pos, Quaternion.identity);
        oxygen.SetScrollSpeed(scrollSpeed);
        oxygen.SetOxygenAmount(oxygenAmount);
        
        // ВАЖНО: Устанавливаем масштаб баллона (маленький, легко собрать)
        oxygen.transform.localScale = Vector3.one * oxygenPickupScale;

        if (showDebugLogs)
            Debug.Log($"[ObstacleSpawner] Oxygen spawned at center ({segmentOffset:F2}, {spawnY:F2}), scale={oxygenPickupScale:F2}");
    }

    /// <summary>
    /// Спавн рекламного шарика
    /// День 8: Создание системы буста
    /// Спавнится в ЦЕНТРЕ туннеля для лёгкого сбора
    /// </summary>
    private void SpawnAdBoostPickup(float spawnY, float segmentOffset)
    {
        if (adBoostPickupPrefab == null)
        {
            Debug.LogError("[ObstacleSpawner] Ad boost pickup prefab not assigned!");
            return;
        }

        // Спавним в центре туннеля
        Vector3 pos = new Vector3(segmentOffset, spawnY, 0f);

        AdBoostPickup adBoost = Instantiate(adBoostPickupPrefab, pos, Quaternion.identity, transform);
        adBoost.SetScrollSpeed(scrollSpeed);
        
        // ВАЖНО: Устанавливаем масштаб шарика
        adBoost.transform.localScale = Vector3.one * adBoostPickupScale;

        if (showDebugLogs)
            Debug.Log($"[ObstacleSpawner] 🎯 Ad boost spawned at center ({segmentOffset:F2}, {spawnY:F2}), scale={adBoostPickupScale:F2}");
    }

    /// <summary>
    /// Возврат препятствия в пул
    /// </summary>
    public void ReturnToPool(Obstacle obstacle)
    {
        obstacle.transform.localScale = Vector3.one;
        
        if (obstacle is Rock rock) 
            rockPool.Return(rock);
        else if (obstacle is Debris debris) 
            debrisPool.Return(debris);
    }

    /// <summary>
    /// Возврат кислородного баллона в пул
    /// ВАЖНО: OxygenPickup НЕ наследуется от Obstacle!
    /// </summary>
    public void ReturnOxygenToPool(OxygenPickup oxygen)
    {
        if (oxygenPool != null)
        {
            oxygenPool.Return(oxygen);
        }
    }

    #if UNITY_EDITOR
    [ContextMenu("Debug: Force Spawn Oxygen")]
    private void DebugForceSpawnOxygen()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            float spawnY = cam.transform.position.y + cam.orthographicSize + 2f;
            SpawnOxygenPickup(spawnY, 0f);
        }
    }

    [ContextMenu("Debug: Force Spawn Ad Boost")]
    private void DebugForceSpawnAdBoost()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            float spawnY = cam.transform.position.y + cam.orthographicSize + 2f;
            SpawnAdBoostPickup(spawnY, 0f);
        }
    }
    #endif
}