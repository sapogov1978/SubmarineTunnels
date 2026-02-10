using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    private struct ObstacleGeometry
    {
        public float longRefLength;
        public float shortRefLength;
        public float shortToLongRatio;
        public Vector2 localInwardDir;
    }

    [Header("Prefab References")]
    [SerializeField] private Rock rockPrefab;
    [SerializeField] private Debris debrisPrefab;
    [SerializeField] private OxygenPickup oxygenPickupPrefab;
    [SerializeField] private AdBoostPickup adBoostPickupPrefab;

    [Header("Spawn Chance")]
    [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.3f;

    [Header("Oxygen Pickup Settings")]
    [SerializeField] private float oxygenSpawnInterval = 12f; 
    [SerializeField] private float oxygenAmount = 25f; 
    [SerializeField] private bool spawnOxygenEnabled = true;

    [Header("Ad Boost Pickup Settings")]
    [SerializeField] private float adBoostSpawnInterval = 35f; 
    [SerializeField] private bool spawnAdBoostEnabled = true;

    [Header("Pooling")]
    [SerializeField] private int initialRockPoolSize = 10;
    [SerializeField] private int initialDebrisPoolSize = 15;
    [SerializeField] private int initialOxygenPoolSize = 5;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private ObjectPool<Rock> rockPool;
    private ObjectPool<Debris> debrisPool;
    private ObjectPool<OxygenPickup> oxygenPool;

    private float minObstacleSpacingY = 0f;
    private float oxygenPickupScale = 0f;
    private float adBoostPickupScale = 0f;

    private float lastObstacleY = float.NegativeInfinity;
    private float oxygenSpawnTimer = 0f;
    private float adBoostSpawnTimer = 0f;

    private ObstacleGeometry rockGeometry;
    private ObstacleGeometry debrisGeometry;
    private bool rockGeometryReady;
    private bool debrisGeometryReady;

    void Start()
    {
        ApplyRuntimeMetrics();
        ApplyPickupScalesFromMetrics();
        PrecomputeObstacleGeometry();
        InitializePools();

        oxygenSpawnTimer = oxygenSpawnInterval / 2f;
        adBoostSpawnTimer = adBoostSpawnInterval / 2f;
    }

    void Update()
    {
        if (spawnOxygenEnabled)
            oxygenSpawnTimer += Time.deltaTime;

        if (spawnAdBoostEnabled)
            adBoostSpawnTimer += Time.deltaTime;
    }

    private void ApplyRuntimeMetrics()
    {
        if (RuntimeGameplayMetrics.MinObstacleSpacingY > 0f)
            minObstacleSpacingY = RuntimeGameplayMetrics.MinObstacleSpacingY;
    }

    private void ApplyPickupScalesFromMetrics()
    {
        if (oxygenPickupPrefab != null)
        {
            if (RuntimeGameplayMetrics.TryGetUniformScaleForPickup(oxygenPickupPrefab.gameObject, out float scale))
                oxygenPickupScale = scale;
        }

        if (adBoostPickupPrefab != null)
        {
            if (RuntimeGameplayMetrics.TryGetUniformScaleForPickup(adBoostPickupPrefab.gameObject, out float scale))
                adBoostPickupScale = scale;
        }
    }

    private void PrecomputeObstacleGeometry()
    {
        rockGeometryReady = TryComputePrefabGeometry(rockPrefab != null ? rockPrefab.gameObject : null, out rockGeometry);
        debrisGeometryReady = TryComputePrefabGeometry(debrisPrefab != null ? debrisPrefab.gameObject : null, out debrisGeometry);
    }

    private bool TryComputePrefabGeometry(GameObject prefab, out ObstacleGeometry geometry)
    {
        geometry = new ObstacleGeometry
        {
            longRefLength = 0f,
            shortRefLength = 0f,
            shortToLongRatio = 0f,
            localInwardDir = Vector2.right
        };

        if (prefab == null)
            return false;

        SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
            return false;

        float maxPos = 0f;
        float maxNeg = 0f;
        bool anyShape = false;

        foreach (var sr in renderers)
        {
            if (sr == null || sr.sprite == null)
                continue;

            int shapeCount = sr.sprite.GetPhysicsShapeCount();
            if (shapeCount <= 0)
                continue;

            anyShape = true;
            for (int i = 0; i < shapeCount; i++)
            {
                List<Vector2> points = new List<Vector2>();
                sr.sprite.GetPhysicsShape(i, points);
                for (int p = 0; p < points.Count; p++)
                {
                    Vector3 world = sr.transform.TransformPoint(points[p]);
                    Vector3 rootLocal = prefab.transform.InverseTransformPoint(world);
                    float x = rootLocal.x;

                    if (x > maxPos) maxPos = x;
                    if (-x > maxNeg) maxNeg = -x;
                }
            }
        }

        if (!anyShape)
            return false;

        bool inwardIsPositiveX = maxPos >= maxNeg;
        geometry.localInwardDir = inwardIsPositiveX ? Vector2.right : Vector2.left;
        geometry.longRefLength = inwardIsPositiveX ? maxPos : maxNeg;
        geometry.shortRefLength = inwardIsPositiveX ? maxNeg : maxPos;

        if (geometry.longRefLength <= 0f)
            return false;

        geometry.shortToLongRatio = geometry.shortRefLength / geometry.longRefLength;

        if (showDebugLogs)
        {
            string inward = inwardIsPositiveX ? "+X" : "-X";
            Debug.Log($"[ObstacleSpawner] Geometry '{prefab.name}': maxPos={maxPos:F3}, maxNeg={maxNeg:F3}, inward={inward}, longRef={geometry.longRefLength:F3}, shortRef={geometry.shortRefLength:F3}, ratio={geometry.shortToLongRatio:F3}");
        }

        return true;
    }


    public bool SpawnObstacleForSegment(float segmentTopY, float leftWallX, float rightWallX, float chanceMultiplier = 1f, float segmentProgressY = float.NaN)
    {
        bool spawned = false;

        if (spawnAdBoostEnabled && adBoostSpawnTimer >= adBoostSpawnInterval)
        {
            float segmentCenterX = (leftWallX + rightWallX) * 0.5f;
            SpawnAdBoostPickup(segmentTopY, segmentCenterX);
            adBoostSpawnTimer = 0f;
            spawned = true;

            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Ad boost pickup spawned at Y={segmentTopY:F0}");

            return spawned;
        }

        if (spawnOxygenEnabled && oxygenSpawnTimer >= oxygenSpawnInterval)
        {
            float segmentCenterX = (leftWallX + rightWallX) * 0.5f;
            SpawnOxygenPickup(segmentTopY, segmentCenterX);
            oxygenSpawnTimer = 0f;
            spawned = true;

            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] Oxygen pickup spawned at Y={segmentTopY:F0}");

            return spawned;
        }

        float spacingY = float.IsNaN(segmentProgressY) ? segmentTopY : segmentProgressY;
        if (Mathf.Abs(spacingY - lastObstacleY) < minObstacleSpacingY) return false;

        float finalChance = Mathf.Clamp01(spawnChance * Mathf.Clamp01(chanceMultiplier));
        if (Random.value > finalChance) return false;

        bool spawnRock = Random.value > 0.5f;

        float leftWall = leftWallX;
        float rightWall = rightWallX;
        float tunnelWidth = RuntimeGameplayMetrics.CurrentTunnelWidth;
        if (tunnelWidth <= 0f) tunnelWidth = rightWall - leftWall;

        float safePassage = RuntimeGameplayMetrics.SafeMargin;
        if (safePassage <= 0f)
        {
            if (showDebugLogs)
                Debug.LogWarning("[ObstacleSpawner] Submarine metrics not initialized yet. Skipping obstacle spawn.");
            return false;
        }

        float availableWidth = tunnelWidth - safePassage;
        if (availableWidth <= 0f)
        {
            if (showDebugLogs)
                Debug.Log($"[ObstacleSpawner] No available width: tunnelWidth={tunnelWidth:F3}, safePassage={safePassage:F3}");
            return false;
        }

        bool spawnLeft = Random.value > 0.5f;
        // Wall normal points INWARD to tunnel: left wall → right normal, right wall → left normal
        Vector2 wallNormal = spawnLeft ? Vector2.right : Vector2.left;
        float randomFactor = Random.Range(0.3f, 1.0f);
        float targetLongLength = availableWidth * randomFactor;

        ObstacleGeometry geometry = spawnRock ? rockGeometry : debrisGeometry;
        bool geometryReady = spawnRock ? rockGeometryReady : debrisGeometryReady;
        if (!geometryReady || geometry.longRefLength <= 0f)
        {
            if (showDebugLogs)
                Debug.LogWarning("[ObstacleSpawner] Obstacle geometry not ready. Skipping obstacle spawn.");
            return false;
        }

        float scaleFactor = targetLongLength / geometry.longRefLength;
        float spawnX = spawnLeft ? leftWall : rightWall;
        float spawnY = segmentTopY;
        Vector3 pos = new Vector3(spawnX, spawnY, 0f);
        Quaternion rot = Quaternion.FromToRotation(new Vector3(geometry.localInwardDir.x, geometry.localInwardDir.y, 0f), new Vector3(wallNormal.x, wallNormal.y, 0f));

        if (spawnRock)
        {
            Rock rock = rockPool.Get(pos, rot);
            rock.transform.position = pos;
            rock.transform.rotation = rot;
            rock.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }
        else
        {
            Debris debris = debrisPool.Get(pos, rot);
            debris.transform.position = pos;
            debris.transform.rotation = rot;
            debris.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[ObstacleSpawner] Spawned {(spawnRock ? "Rock" : "Debris")} at ({spawnX:F2}, {spawnY:F2}), tunnelWidth={tunnelWidth:F3}, safePassage={safePassage:F3}, availableWidth={availableWidth:F3}, randomFactor={randomFactor:F2}, targetLongLength={targetLongLength:F3}, scale={scaleFactor:F3}");
        }

        lastObstacleY = spacingY;
        return true;
    }

    private void SpawnOxygenPickup(float spawnY, float segmentOffset)
    {
        if (oxygenPool == null)
        {
            Debug.LogError("[ObstacleSpawner] Oxygen pool not initialized!");
            return;
        }

        Vector3 pos = new Vector3(segmentOffset, spawnY, 0f);

        OxygenPickup oxygen = oxygenPool.Get(pos, Quaternion.identity);
        oxygen.SetOxygenAmount(oxygenAmount);
        oxygen.transform.localScale = Vector3.one * oxygenPickupScale;

        if (showDebugLogs)
            Debug.Log($"[ObstacleSpawner] Oxygen spawned at center ({segmentOffset:F2}, {spawnY:F2}), scale={oxygenPickupScale:F2}");
    }

    private void SpawnAdBoostPickup(float spawnY, float segmentOffset)
    {
        if (adBoostPickupPrefab == null)
        {
            Debug.LogError("[ObstacleSpawner] Ad boost pickup prefab not assigned!");
            return;
        }

        Vector3 pos = new Vector3(segmentOffset, spawnY, 0f);

        AdBoostPickup adBoost = Instantiate(adBoostPickupPrefab, pos, Quaternion.identity, transform);
        adBoost.transform.localScale = Vector3.one * adBoostPickupScale;

        if (showDebugLogs)
            Debug.Log($"[ObstacleSpawner] Ad boost spawned at center ({segmentOffset:F2}, {spawnY:F2}), scale={adBoostPickupScale:F2}");
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

    public void ReturnToPool(Obstacle obstacle)
    {
        obstacle.transform.localScale = Vector3.one;

        if (obstacle is Rock rock)
            rockPool.Return(rock);
        else if (obstacle is Debris debris)
            debrisPool.Return(debris);
    }

    public void ReturnOxygenToPool(OxygenPickup oxygen)
    {
        if (oxygenPool != null)
            oxygenPool.Return(oxygen);
    }
}
