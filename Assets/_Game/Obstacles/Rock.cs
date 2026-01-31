using UnityEngine;

/// <summary>
/// Камень - стандартное препятствие
/// Наносит средний урон при столкновении
/// </summary>
public class Rock : Obstacle
{
    [Header("Movement")]
    [SerializeField] private float fallSpeed = 2f; // должна совпадать со scrollSpeed туннеля

    void Update()
    {
        // Движение вниз (падение)
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);
        
        // Камни больше НЕ вращаются (статичные скалы)
    }

    protected override void OnHit(GameObject player)
    {
        // Вызываем базовую логику
        base.OnHit(player);

        // Дополнительная логика для камней (если нужна)
        if (showDebugLogs)
            Debug.Log("[Rock] Destroyed after hit");
    }

    public override void ResetObstacle()
    {
        base.ResetObstacle();
        
        // Сброс вращения при переиспользовании
        transform.rotation = Quaternion.identity;
    }
}