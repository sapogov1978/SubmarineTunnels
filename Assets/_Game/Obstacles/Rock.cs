using UnityEngine;

/// <summary>
/// Камень - препятствие которое двигается вниз синхронно с туннелем
/// </summary>
public class Rock : Obstacle
{
    [Header("Movement")]
    [SerializeField] private float scrollSpeed = 2f;

    void Update()
    {
        // Движемся вниз синхронно с туннелем
        transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime, Space.World);
    }

    protected override void OnHit(GameObject player)
    {
        base.OnHit(player);

        if (showDebugLogs)
            Debug.Log("[Rock] Hit and destroyed");
    }

    public override void ResetObstacle()
    {
        base.ResetObstacle();
        transform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Устанавливает скорость скроллинга (вызывается из ObstacleSpawner)
    /// </summary>
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }
}