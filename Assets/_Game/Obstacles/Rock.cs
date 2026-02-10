using UnityEngine;

/// <summary>
/// Rock obstacle - static hazard that scrolls with the tunnel
/// Damages submarine on collision and returns to pool
/// </summary>
public class Rock : Obstacle
{
    void Update()
    {
        // Move downward with tunnel scroll speed
        float scrollSpeed = RuntimeGameplayMetrics.ScrollSpeed;
        if (scrollSpeed <= 0f)
            return;

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
}
