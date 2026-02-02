using UnityEngine;

/// <summary>
/// ПРОСТОЙ И НАДЁЖНЫЙ спавнер препятствий
/// Спавн по времени, всегда внутри туннеля
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private Rock rockPrefab;
    [SerializeField] private Debris debrisPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnInterval = 1.0f;
    [SerializeField] private float maxSpawnInterval = 2.0f;
    [SerializeField] private float spawnYOffset = 15f;
    [SerializeField] private float initialDelay = 2f;

    [Header("Tunnel Integration")]
    [SerializeField] private TunnelGenerator tunnelGenerator;
    [SerializeField] private float submarineWidth = 0.8f;
    [SerializeField] private float safetyMargin = 0.4f;
    [SerializeField] private float minWidthToSpawn = 1.4f;

    [Header("Safe Spawn Distance (seconds)")]
    [SerializeField] private float minTimeBetweenObstacles = 1.2f;

    [Header("Pooling")]
    [SerializeField] private int initialRockPoolSize = 20;
    [SerializeField] private int initialDebrisPoolSize = 30;

    private ObjectPool<Rock> rockPool;
    private ObjectPool<Debris> debrisPool;

    private Camera mainCamera;

    private float spawnTimer;
    private float nextSpawnInterval;
    private float timeSinceLastSpawn = Mathf.Infinity;

    private float currentTunnelWidth = 2f;
    private float currentTunnelOffset = 0f;

    void Start()
    {
        mainCamera = Camera.main;

        if (!tunnelGenerator)
            tunnelGenerator = FindObjectOfType<TunnelGenerator>();

        InitializePools();

        spawnTimer = -initialDelay;
        SetNextSpawnInterval();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying())
            return;

        UpdateTunnelInfo();

        spawnTimer += Time.deltaTime;
        timeSinceLastSpawn += Time.deltaTime;

        if (spawnTimer >= nextSpawnInterval)
        {
            TrySpawnObstacle();
            spawnTimer = 0f;
            SetNextSpawnInterval();
        }
    }

    private void UpdateTunnelInfo()
    {
        currentTunnelWidth = tunnelGenerator.GetCurrentWidth();
        currentTunnelOffset = tunnelGenerator.GetCurrentOffset();
    }

    // 🔹 используется TunnelGenerator
    public void UpdateTunnelWidth(float width, float offset = 0f)
    {
        currentTunnelWidth = width;
        currentTunnelOffset = offset;
    }

    private void TrySpawnObstacle()
    {
        if (timeSinceLastSpawn < minTimeBetweenObstacles)
            return;

        if (currentTunnelWidth < minWidthToSpawn)
            return;

        SpawnSingleObstacle();
        timeSinceLastSpawn = 0f;
    }

    private void SpawnSingleObstacle()
    {
        float spawnY = tunnelGenerator.GetSpawnY();

        if (!tunnelGenerator.GetWallsAtY(spawnY, out float leftWall, out float rightWall))
        {
            Debug.Log("[ObstacleSpawner] Failed to get tunnel walls at Y: " + spawnY);
            return;
        }

        float passageWidth = submarineWidth + safetyMargin * 2f;
        float passageLeft = currentTunnelOffset - passageWidth / 2f;
        float passageRight = currentTunnelOffset + passageWidth / 2f;

        bool spawnLeft = Random.value > 0.5f;

        float spawnX = spawnLeft
            ? Random.Range(leftWall + 0.2f, passageLeft - 0.15f)
            : Random.Range(passageRight + 0.15f, rightWall - 0.2f);

        Vector3 pos = new Vector3(spawnX, spawnY, 0f);

        if (Random.value > 0.5f)
            rockPool.Get(pos, Quaternion.identity);
        else
            debrisPool.Get(pos, Quaternion.identity);
    }

    private void InitializePools()
    {
        rockPool = new ObjectPool<Rock>(rockPrefab, initialRockPoolSize, transform);
        debrisPool = new ObjectPool<Debris>(debrisPrefab, initialDebrisPoolSize, transform);
    }

    private void SetNextSpawnInterval()
    {
        nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    // 🔹 используется ObstacleAutoDestroy
    public void ReturnToPool(Obstacle obstacle)
    {
        if (obstacle is Rock rock)
            rockPool.Return(rock);
        else if (obstacle is Debris debris)
            debrisPool.Return(debris);
    }
}
