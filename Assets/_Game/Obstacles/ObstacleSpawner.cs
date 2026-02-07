using UnityEngine;

/// <summary>
/// Спавнер препятствий с адаптивным размером
/// В узких туннелях препятствия становятся меньше
/// День 7: Добавлен спавн кислородных баллонов
/// День 8: Добавлен спавн рекламных шариков
/// ДЕНЬ 8 FIX: Автокалибровка размеров после добавления текстур
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
    [SerializeField] private float adBoostPickupScale = 0.4f; // размер шарика
    [SerializeField] private bool spawnAdBoostEnabled = true;

    [Header("References")]

    [Header("Sizes (половина ширины)")]
    [SerializeField] private float maxRockHalfWidth = 0.15f;      // Авто-калибруется из prefab
    [SerializeField] private float maxDebrisHalfWidth = 0.12f;    // Авто-калибруется из prefab
    [SerializeField] private float minObstacleHalfWidth = 0.08f;  // Минимальная половина ширины
    [SerializeField] private float minObstacleWidthFractionOfTunnel = 0.2f; // Min obstacle width as fraction of free space
    [SerializeField] [Range(0.1f, 1f)] private float rockMinFractionOfMax = 0.8f; // Rock must be >= this fraction of max allowed size
    [SerializeField] private float passageWidthMultiplier = 1.5f; // minPassage = SafeMargin (from RuntimeGameplayMetrics)
    
    [Header("Auto-Calibration")]
    [SerializeField] private bool autoCalibrateSizes = true;
    [Tooltip("Автоматически определяет размеры из коллайдеров префабов. Отключите для ручной настройки.")]
    [SerializeField] private bool showCalibrationInfo = true;  // Показывать информацию о калибровке
    

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

    // Day 9: halves for asymmetric pivots/colliders
    private float actualRockLeftHalf;
    private float actualRockRightHalf;
    private float actualDebrisLeftHalf;
    private float actualDebrisRightHalf;
    
    // ДЕНЬ 8 V6: Вычисляемые размеры (не hardcoded!)
    private float actualRockHalfWidth;    // Получаем из префаба Rock
    private float actualDebrisHalfWidth;  // Получаем из префаба Debris

    void Start()
    {
        // ВАЖНО: Калибруем размеры ДО инициализации пулов
        if (autoCalibrateSizes)
        {
            CalibrateObstacleSizes();
        }
        else
        {
            // Используем ручные настройки
            actualRockHalfWidth = maxRockHalfWidth;
            actualDebrisHalfWidth = maxDebrisHalfWidth;
            actualRockLeftHalf = actualRockHalfWidth;
            actualRockRightHalf = actualRockHalfWidth;
            actualDebrisLeftHalf = actualDebrisHalfWidth;
            actualDebrisRightHalf = actualDebrisHalfWidth;

            if (showCalibrationInfo)
            {
                Debug.Log("[ObstacleSpawner] Manual sizes: Rock=" + actualRockHalfWidth + ", Debris=" + actualDebrisHalfWidth);
            }
        }
        
        ApplyRuntimeMetrics();
        ApplyPickupScalesFromMetrics();

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
    
    /// <summary>
    /// ДЕНЬ 8 FIX V5: Калибровка ШИРИНЫ вместо радиуса
    /// + автокалибровка батискафа!
    /// </summary>
    private void CalibrateObstacleSizes()
    {
        Debug.Log("=== [ObstacleSpawner] AUTO-CALIBRATION START (V5: Width-based) ===");
        
        // НОВОЕ: Калибруем батискаф!
        // Prefer scene instance (its scale is the real one); fallback to prefab if not found.
        // Калибруем Rock
        if (rockPrefab != null)
        {
            actualRockHalfWidth = GetPrefabBiggestHalfWidth(rockPrefab.gameObject, "Rock", out actualRockLeftHalf, out actualRockRightHalf);
            
            if (actualRockHalfWidth <= 0f)
            {
                Debug.LogWarning("[ObstacleSpawner] Rock calibration failed! Using manual fallback: " + maxRockHalfWidth);
                actualRockHalfWidth = maxRockHalfWidth;
                actualRockLeftHalf = actualRockHalfWidth;
                actualRockRightHalf = actualRockHalfWidth;
            }
        }
        else
        {
            Debug.LogError("[ObstacleSpawner] Rock prefab is NULL!");
            actualRockHalfWidth = maxRockHalfWidth;
            actualRockLeftHalf = actualRockHalfWidth;
            actualRockRightHalf = actualRockHalfWidth;
        }
        
        // Калибруем Debris
        if (debrisPrefab != null)
        {
            actualDebrisHalfWidth = GetPrefabHalfWidthSymmetric(debrisPrefab.gameObject, "Debris");
            actualDebrisLeftHalf = actualDebrisHalfWidth;
            actualDebrisRightHalf = actualDebrisHalfWidth;
            
            if (actualDebrisHalfWidth <= 0f)
            {
                Debug.LogWarning("[ObstacleSpawner] Debris calibration failed! Using manual fallback: " + maxDebrisHalfWidth);
                actualDebrisHalfWidth = maxDebrisHalfWidth;
                actualDebrisLeftHalf = actualDebrisHalfWidth;
                actualDebrisRightHalf = actualDebrisHalfWidth;
            }
        }
        else
        {
            Debug.LogError("[ObstacleSpawner] Debris prefab is NULL!");
            actualDebrisHalfWidth = maxDebrisHalfWidth;
            actualDebrisLeftHalf = actualDebrisHalfWidth;
            actualDebrisRightHalf = actualDebrisHalfWidth;
        }
        
        if (showCalibrationInfo)
        {
            Debug.Log("[ObstacleSpawner] ✓ Calibration complete (WIDTH-BASED):");
            Debug.Log("  - Rock halfWidth: " + actualRockHalfWidth + " (manual was: " + maxRockHalfWidth + ")");
            Debug.Log("  - Debris halfWidth: " + actualDebrisHalfWidth + " (manual was: " + maxDebrisHalfWidth + ")");
            
            if (Mathf.Abs(actualRockHalfWidth - maxRockHalfWidth) > 0.01f)
            {
                Debug.LogWarning("[ObstacleSpawner] ⚠️ Rock size mismatch! Consider updating maxRockHalfWidth to " + actualRockHalfWidth);
            }
            
            if (Mathf.Abs(actualDebrisHalfWidth - maxDebrisHalfWidth) > 0.01f)
            {
                Debug.LogWarning("[ObstacleSpawner] ⚠️ Debris size mismatch! Consider updating maxDebrisHalfWidth to " + actualDebrisHalfWidth);
            }
        }
        
        Debug.Log("===========================================");
    }

    private void ApplyRuntimeMetrics()
    {
        RuntimeGameplayMetrics.UpdateScrollSpeed(scrollSpeed);
        passageWidthMultiplier = RuntimeGameplayMetrics.SafeMarginWidthMultiplier;
        minObstacleSpacingY = RuntimeGameplayMetrics.MinObstacleSpacingY;
    }

    private void ApplyPickupScalesFromMetrics()
    {
        if (oxygenPickupPrefab != null)
        {
            if (RuntimeGameplayMetrics.TryGetUniformScaleForPickup(oxygenPickupPrefab.gameObject, out float scale))
            {
                oxygenPickupScale = scale;
            }
        }

        if (adBoostPickupPrefab != null)
        {
            if (RuntimeGameplayMetrics.TryGetUniformScaleForPickup(adBoostPickupPrefab.gameObject, out float scale))
            {
                adBoostPickupScale = scale;
            }
        }
    }
    
    /// <summary>
    /// ДЕНЬ 8 FIX V4: Определяет ПОЛОВИНУ ШИРИНЫ (по оси X) из коллайдера
    /// Это правильный подход для неправильных форм в вертикальном туннеле
    /// </summary>
    private float GetPrefabBiggestHalfWidth(GameObject prefab, string obstacleName)
    {
        float leftHalf;
        float rightHalf;
        float biggestHalf = GetPrefabBiggestHalfWidth(prefab, obstacleName, out leftHalf, out rightHalf);
        return biggestHalf;
    }

    private float GetPrefabHalfWidthSymmetric(GameObject prefab, string obstacleName)
    {
        if (TryGetMinMaxXInRoot(prefab, out float minX, out float maxX, out string source))
        {
            float halfWidth = (maxX - minX) / 2f;
            Debug.Log($"[ObstacleSpawner] {obstacleName} has {source}: halfWidth={halfWidth:F3} (minX={minX:F3}, maxX={maxX:F3})");
            return halfWidth;
        }

        Debug.LogError($"[ObstacleSpawner] {obstacleName}: No collider AND no SpriteRenderer found!");
        return 0f;
    }
    private bool TryGetMinMaxXInRoot(GameObject root, out float minX, out float maxX, out string source)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;
        source = "";

        bool found = false;
        int colliderCount = 0;

        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders)
        {
            if (AccumulateColliderMinMaxX(root.transform, col, ref minX, ref maxX))
            {
                found = true;
                colliderCount++;
            }
        }

        if (found)
        {
            source = colliderCount > 1 ? "Collider2D (children)" : colliders[0].GetType().Name;
            return true;
        }

        SpriteRenderer[] srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        int spriteCount = 0;
        foreach (SpriteRenderer sr in srs)
        {
            if (sr.sprite == null)
                continue;

            if (AccumulateSpriteMinMaxX(root.transform, sr, ref minX, ref maxX))
            {
                found = true;
                spriteCount++;
            }
        }

        if (found)
        {
            source = spriteCount > 1 ? "SpriteRenderer (children)" : "SpriteRenderer";
            return true;
        }

        return false;
    }
    private bool AccumulateColliderMinMaxX(Transform root, Collider2D col, ref float minX, ref float maxX)
    {
        switch (col)
        {
            case CircleCollider2D circle:
                return AccumulatePoints(root, circle.transform, new Vector2[]
                {
                    circle.offset + new Vector2(-circle.radius, 0f),
                    circle.offset + new Vector2(circle.radius, 0f)
                }, ref minX, ref maxX);

            case BoxCollider2D box:
                Vector2 half = box.size * 0.5f;
                return AccumulatePoints(root, box.transform, new Vector2[]
                {
                    box.offset + new Vector2(-half.x, -half.y),
                    box.offset + new Vector2(-half.x,  half.y),
                    box.offset + new Vector2( half.x, -half.y),
                    box.offset + new Vector2( half.x,  half.y)
                }, ref minX, ref maxX);

            case PolygonCollider2D poly:
                if (poly.points == null || poly.points.Length == 0)
                    return false;

                Vector2[] pts = new Vector2[poly.points.Length];
                for (int i = 0; i < poly.points.Length; i++)
                    pts[i] = poly.points[i] + poly.offset;

                return AccumulatePoints(root, poly.transform, pts, ref minX, ref maxX);
        }

        return false;
    }

    private bool AccumulateSpriteMinMaxX(Transform root, SpriteRenderer sr, ref float minX, ref float maxX)
    {
        Bounds bounds = sr.sprite.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector2[] pts = new Vector2[]
        {
            new Vector2(min.x, min.y),
            new Vector2(min.x, max.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y)
        };

        return AccumulatePoints(root, sr.transform, pts, ref minX, ref maxX);
    }
    private bool AccumulatePoints(Transform root, Transform child, Vector2[] localPoints, ref float minX, ref float maxX)
    {
        bool any = false;
        foreach (Vector2 p in localPoints)
        {
            Vector3 world = child.TransformPoint(p);
            Vector3 rootLocal = root.InverseTransformPoint(world);
            minX = Mathf.Min(minX, rootLocal.x);
            maxX = Mathf.Max(maxX, rootLocal.x);
            any = true;
        }

        return any;
    }
    private float GetPrefabBiggestHalfWidth(GameObject prefab, string obstacleName, out float leftHalf, out float rightHalf)
    {
        leftHalf = 0f;
        rightHalf = 0f;

        if (TryGetMinMaxXInRoot(prefab, out float minX, out float maxX, out string source))
        {
            leftHalf = Mathf.Abs(minX);
            rightHalf = Mathf.Abs(maxX);
            float biggestHalf = Mathf.Max(leftHalf, rightHalf);
            Debug.Log($"[ObstacleSpawner] {obstacleName} has {source}: biggestHalf={biggestHalf:F3} (minX={minX:F3}, maxX={maxX:F3})");
            return biggestHalf;
        }

        Debug.LogError($"[ObstacleSpawner] {obstacleName}: No collider AND no SpriteRenderer found!");
        return 0f;
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
        ApplyRuntimeMetrics();
    }

    /// <summary>
    /// Спавн препятствия для сегмента туннеля
    /// День 7: Добавлена возможность спавна кислородных баллонов
    /// День 8: Добавлен спавн рекламных шариков (ПРИОРИТЕТ #1)
    /// ДЕНЬ 8 FIX: Используем actualRockHalfWidth и actualDebrisHalfWidth
    /// </summary>
    public bool SpawnObstacleForSegment(float segmentTopY, float leftWallX, float rightWallX, float chanceMultiplier = 1f, float segmentProgressY = float.NaN)
    {
        bool spawned = false;

        // ДЕНЬ 8: Проверяем нужен ли рекламный шарик (ПРИОРИТЕТ #1!)
        if (spawnAdBoostEnabled && adBoostSpawnTimer >= adBoostSpawnInterval)
        {
            // Спавним рекламный шарик в ЦЕНТРЕ туннеля
            float segmentCenterX = (leftWallX + rightWallX) * 0.5f;
            SpawnAdBoostPickup(segmentTopY, segmentCenterX);
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
            float segmentCenterX = (leftWallX + rightWallX) * 0.5f;
            SpawnOxygenPickup(segmentTopY, segmentCenterX);
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
        
        // ДЕНЬ 8 FIX: Используем откалиброванные размеры!
        float maxHalfWidth = spawnRock ? actualRockHalfWidth : actualDebrisHalfWidth;
        float leftHalfBase = spawnRock ? actualRockLeftHalf : actualDebrisLeftHalf;
        float rightHalfBase = spawnRock ? actualRockRightHalf : actualDebrisRightHalf;

        // ════════════════════════════════════════════════════════════
        // ДЕНЬ 8 FIX V5: КАМНИ КАК ОБВАЛЫ ОТ СТЕН
        // Новая концепция: камни ВСЕГДА начинаются от стены
        // Размер случайный (от минимума до максимально возможного)
        // ════════════════════════════════════════════════════════════
        
        // Границы туннеля (точные по кривой Безье)
        float leftWall = leftWallX;
        float rightWall = rightWallX;
        float segmentWidth = rightWall - leftWall;

        // Минимальный проход для submarine
        float minPassageWidth = RuntimeGameplayMetrics.SafeMargin;
        if (minPassageWidth <= 0f)
        {
            if (showDebugLogs)
                Debug.LogWarning("[ObstacleSpawner] Submarine metrics not initialized yet. Skipping obstacle spawn.");
            return false;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[ObstacleSpawner] Walls at Y={segmentTopY:F2}: LEFT={leftWall:F3}, RIGHT={rightWall:F3}, width={segmentWidth:F3}, requiredPassage={minPassageWidth:F3}");
        }
        
        // Максимально возможный размер камня
        float maxPossibleWidth = segmentWidth - minPassageWidth;
        
        // Проверяем что есть место для камня
        if (maxPossibleWidth < minObstacleHalfWidth * 2)
        {
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Tunnel too narrow: width={segmentWidth:F3}, maxObstacle={maxPossibleWidth:F3}");
            return false;
        }
        
        // Выбираем сторону СЛУЧАЙНО
        bool spawnLeft = Random.value > 0.5f;
        
        float obstacleHalfWidth = 0f;
        float rockInwardWidth = 0f;
        float rockScale = 1f;

        if (spawnRock)
        {
            float inwardHalfBase = spawnLeft ? rightHalfBase : leftHalfBase;
            float maxAllowedInward = Mathf.Min(maxPossibleWidth, inwardHalfBase);
            float minAllowedInward = minObstacleHalfWidth;
            if (minObstacleWidthFractionOfTunnel > 0f)
            {
                float minWidthFromFraction = maxPossibleWidth * minObstacleWidthFractionOfTunnel;
                minAllowedInward = Mathf.Max(minAllowedInward, minWidthFromFraction);
            }
            minAllowedInward = Mathf.Max(minAllowedInward, maxAllowedInward * rockMinFractionOfMax);

            if (maxAllowedInward < minAllowedInward)
            {
                if (showDebugLogs)
                    Debug.Log($"[ObstacleSpawner] Rock inward width too small: min={minAllowedInward:F3}, max={maxAllowedInward:F3}, maxPossibleWidth={maxPossibleWidth:F3}");
                return false;
            }

            // ВАЖНО: Размер СЛУЧАЙНЫЙ (не всегда максимум!)
            rockInwardWidth = Random.Range(minAllowedInward, maxAllowedInward);
            rockScale = inwardHalfBase > 0f ? rockInwardWidth / inwardHalfBase : 1f;
            obstacleHalfWidth = rockInwardWidth;
        }
        else
        {
            // Ограничиваем максимальный размер
            float maxAllowedHalf = Mathf.Min(maxPossibleWidth / 2f, maxHalfWidth);
            float minAllowedHalf = minObstacleHalfWidth;
            if (minObstacleWidthFractionOfTunnel > 0f)
            {
                float minWidthFromFraction = maxPossibleWidth * minObstacleWidthFractionOfTunnel;
                minAllowedHalf = Mathf.Max(minAllowedHalf, minWidthFromFraction / 2f);
            }

            if (maxAllowedHalf < minAllowedHalf)
            {
                if (showDebugLogs)
                    Debug.Log($"[ObstacleSpawner] Allowed half too small: min={minAllowedHalf:F3}, max={maxAllowedHalf:F3}, maxPossibleWidth={maxPossibleWidth:F3}");
                return false;
            }
            
            // ВАЖНО: Размер СЛУЧАЙНЫЙ (не всегда максимум!)
            obstacleHalfWidth = Random.Range(minAllowedHalf, maxAllowedHalf);
        }
        
        // Позиция: основание камня У СТЕНЫ
        float spawnX;
        if (spawnLeft)
        {
            // Камень начинается от левой стены
            if (spawnRock)
            {
                spawnX = leftWall;
            }
            else
            {
                float scale = maxHalfWidth > 0f ? obstacleHalfWidth / maxHalfWidth : 1f;
                float leftHalfScaled = leftHalfBase * scale;
                spawnX = leftWall + leftHalfScaled;
            }
        }
        else
        {
            // Камень начинается от правой стены
            if (spawnRock)
            {
                spawnX = rightWall;
            }
            else
            {
                float scale = maxHalfWidth > 0f ? obstacleHalfWidth / maxHalfWidth : 1f;
                float rightHalfScaled = rightHalfBase * scale;
                spawnX = rightWall - rightHalfScaled;
            }
        }
        
        float spawnY = segmentTopY;
        Vector3 pos = new Vector3(spawnX, spawnY, 0f);

        // Создаём препятствие с адаптивным размером
        if (spawnRock)
        {
            Rock rock = rockPool.Get(pos, Quaternion.identity);
            rock.SetScrollSpeed(scrollSpeed);
            
            // Масштаб относительно "внутренней" половины (камень упирается в стену pivot-ом)
            float scale = rockScale;
            rock.transform.localScale = new Vector3(scale, scale, 1f);
            float leftHalfScaled = actualRockLeftHalf * scale;
            float rightHalfScaled = actualRockRightHalf * scale;
            
            // ДЕНЬ 8 FIX V5: Отражаем через ROTATION (не scale!)
            // Rotation автоматически отражает физику и коллайдеры
            // Оригинал смотрит ВПРАВО (для правой стены)
            if (spawnLeft)
            {
                // Левая сторона - поворот на 180° по Y (отражение)
                rock.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            else
            {
                // Правая сторона - без поворота (оригинал)
                rock.transform.rotation = Quaternion.identity;
            }
            
            if (showDebugLogs)
            {
                float passageWidth = minPassageWidth;
                float totalWidth = (leftHalfScaled + rightHalfScaled);
                float inwardWidth = rockInwardWidth;
                
                Debug.Log($"[ObstacleSpawner] ━━━ Rock spawned (LANDSLIDE from wall) ━━━");
                Debug.Log($"  Side: {(spawnLeft ? "LEFT" : "RIGHT")} wall");
                Debug.Log($"  Flipped: {(spawnLeft ? "YES (rotation.y = 180°)" : "NO (rotation.y = 0°)")}");
                Debug.Log($"  Tunnel width: {segmentWidth:F3}");
                Debug.Log($"  Walls: LEFT={leftWall:F3}, RIGHT={rightWall:F3}");
                Debug.Log($"  Required passage: {passageWidth:F3} (submarine * {passageWidthMultiplier:F2})");
                Debug.Log($"  Max possible: {maxPossibleWidth:F3}");
                Debug.Log($"  Total width: {totalWidth:F3} (includes into wall)");
                Debug.Log($"  Inward width: {inwardWidth:F3} (inside tunnel)");
                Debug.Log($"  Scale: {scale:F3} (= {rockInwardWidth:F3} / inwardHalfBase)");
                Debug.Log($"  Position: X={spawnX:F3}, Y={spawnY:F3}");
                Debug.Log($"  Rock edges: LEFT={spawnX - leftHalfScaled:F3}, RIGHT={spawnX + rightHalfScaled:F3}");
                Debug.Log($"  actualRockHalfWidth from calibration: {actualRockHalfWidth:F3}");
            }
        }
        else
        {
            Debris debris = debrisPool.Get(pos, Quaternion.identity);
            debris.SetScrollSpeed(scrollSpeed);
            // Размер управляется через localScale ниже
            
            // ДЕНЬ 8 FIX V5: Масштаб относительно ОТКАЛИБРОВАННОГО размера (halfWidth)
            float scale = obstacleHalfWidth / actualDebrisHalfWidth;
            debris.transform.localScale = new Vector3(scale, scale, 1f);
            float leftHalfScaled = actualDebrisLeftHalf * scale;
            float rightHalfScaled = actualDebrisRightHalf * scale;
            
            // ДЕНЬ 8 FIX V5: Отражаем через ROTATION (не scale!)
            // Rotation автоматически отражает физику и коллайдеры
            // Оригинал смотрит ВПРАВО (для правой стены)
            if (spawnLeft)
            {
                // Левая сторона - поворот на 180° по Y (отражение)
                debris.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            else
            {
                // Правая сторона - без поворота (оригинал)
                debris.transform.rotation = Quaternion.identity;
            }
            
            if (showDebugLogs)
            {
                float passageWidth = minPassageWidth;
                float actualWidth = (leftHalfScaled + rightHalfScaled);
                
                Debug.Log($"[ObstacleSpawner] ━━━ Debris spawned (LANDSLIDE from wall) ━━━");
                Debug.Log($"  Side: {(spawnLeft ? "LEFT" : "RIGHT")} wall");
                Debug.Log($"  Flipped: {(spawnLeft ? "YES (rotation.y = 180°)" : "NO (rotation.y = 0°)")}");
                Debug.Log($"  Tunnel width: {segmentWidth:F3}");
                Debug.Log($"  Required passage: {passageWidth:F3} (submarine * {passageWidthMultiplier:F2})");
                Debug.Log($"  Max possible: {maxPossibleWidth:F3}");
                Debug.Log($"  Actual width: {actualWidth:F3} (biggestHalf: {obstacleHalfWidth:F3})");
                Debug.Log($"  Scale: {scale:F3}");
                Debug.Log($"  Position: X={spawnX:F3}, Y={spawnY:F3}");
            }
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
    
    [ContextMenu("Debug: Recalibrate Sizes")]
    private void DebugRecalibrateSizes()
    {
        CalibrateObstacleSizes();
    }
    
    [ContextMenu("Debug: Show Current Sizes")]
    private void DebugShowCurrentSizes()
    {
        Debug.Log("=== CURRENT OBSTACLE SIZES ===");
        Debug.Log($"Rock: actual={actualRockHalfWidth:F3}, manual={maxRockHalfWidth:F3}");
        Debug.Log($"Debris: actual={actualDebrisHalfWidth:F3}, manual={maxDebrisHalfWidth:F3}");
        Debug.Log($"Auto-calibration: {(autoCalibrateSizes ? "ENABLED" : "DISABLED")}");
        Debug.Log("===============================");
    }
    #endif
}





