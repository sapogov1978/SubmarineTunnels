using UnityEngine;

/// <summary>
/// Спавнер препятствий с адаптивным размером
/// В узких туннелях препятствия становятся меньше
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private Rock rockPrefab;
    [SerializeField] private Debris debrisPrefab;

    [Header("Spawn Chance")]
    [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.6f;

    [Header("Sizes (радиусы)")]
    [SerializeField] private float submarineRadius = 0.125f;
    [SerializeField] private float maxRockRadius = 0.15f;      // Максимальный размер
    [SerializeField] private float maxDebrisRadius = 0.12f;    // Максимальный размер
    [SerializeField] private float minObstacleRadius = 0.08f;  // Минимальный размер
    [SerializeField] private float safetyMargin = 0.1f;

    [Header("Pooling")]
    [SerializeField] private int initialRockPoolSize = 20;
    [SerializeField] private int initialDebrisPoolSize = 30;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private ObjectPool<Rock> rockPool;
    private ObjectPool<Debris> debrisPool;
    private float scrollSpeed = 2f;

    void Start()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        rockPool = new ObjectPool<Rock>(rockPrefab, initialRockPoolSize, transform);
        debrisPool = new ObjectPool<Debris>(debrisPrefab, initialDebrisPoolSize, transform);
    }

    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }

    public void SpawnObstacleForSegment(float segmentTopY, float segmentOffset, float segmentWidth)
    {
        if (Random.value > spawnChance) return;

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
        // Препятствие должно поместиться: leftWall + radius ... passageLeft - radius
        // Значит нужно: (leftSpace - minGap) / 2, где minGap - минимальный зазор
        float minGap = 0.05f;
        float maxLeftRadius = (leftSpace - minGap) / 2f;
        float maxRightRadius = (rightSpace - minGap) / 2f;
        
        // Берём максимум из доступных
        float maxPossibleRadius = Mathf.Max(maxLeftRadius, maxRightRadius);
        
        // Ограничиваем min/max значениями
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
            return;
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
            // Масштабируем визуально
            float scale = obstacleRadius / maxRockRadius;
            rock.transform.localScale = Vector3.one * scale;
        }
        else
        {
            Debris debris = debrisPool.Get(pos, Quaternion.identity);
            debris.SetScrollSpeed(scrollSpeed);
            debris.SetRadius(obstacleRadius);
            // Масштабируем визуально
            float scale = obstacleRadius / maxDebrisRadius;
            debris.transform.localScale = Vector3.one * scale;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[ObstacleSpawner] Spawned {(spawnRock ? "Rock" : "Debris")} at ({spawnX:F2}, {spawnY:F2})");
            Debug.Log($"  Segment: width={segmentWidth:F2}, offset={segmentOffset:F2}");
            Debug.Log($"  Spaces: left={leftSpace:F2}, right={rightSpace:F2}");
            Debug.Log($"  Max radius: left={maxLeftRadius:F2}, right={maxRightRadius:F2}");
            Debug.Log($"  Final radius: {obstacleRadius:F2} (max: {maxRadius:F2}, scale: {(obstacleRadius/maxRadius):F2})");
        }
    }

    public void ReturnToPool(Obstacle obstacle)
    {
        // Восстанавливаем оригинальный размер
        obstacle.transform.localScale = Vector3.one;
        
        if (obstacle is Rock rock) rockPool.Return(rock);
        else if (obstacle is Debris debris) debrisPool.Return(debris);
    }
}