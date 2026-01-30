using UnityEngine;

/// <summary>
/// Камень - стандартное препятствие
/// Наносит средний урон при столкновении
/// </summary>
public class Rock : Obstacle
{
    [Header("Rock Specific")]
    [SerializeField] private bool rotateWhileFalling = true;
    [SerializeField] private float rotationSpeed = 30f;

    [Header("Movement")]
    [SerializeField] private float fallSpeed = 2f; // должна совпадать со scrollSpeed туннеля

    void Update()
    {
        // Движение вниз (падение)
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        // Вращение камня при падении для визуального эффекта
        if (rotateWhileFalling)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
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