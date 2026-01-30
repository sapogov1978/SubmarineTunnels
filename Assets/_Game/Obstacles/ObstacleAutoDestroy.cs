using UnityEngine;

/// <summary>
/// Автоматически деактивирует препятствие когда оно уходит за пределы экрана
/// Препятствие возвращается в Object Pool для переиспользования
/// </summary>
[RequireComponent(typeof(Obstacle))]
public class ObstacleAutoDestroy : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float destroyOffset = 2f; // дополнительное расстояние за пределами экрана

    private Camera mainCamera;
    private float bottomBoundary;

    void Start()
    {
        mainCamera = Camera.main;
        if (!mainCamera)
        {
            Debug.LogError("[ObstacleAutoDestroy] Main camera not found!");
            enabled = false;
        }
    }

    void Update()
    {
        if (!mainCamera) return;

        // Вычисляем нижнюю границу экрана
        bottomBoundary = mainCamera.transform.position.y - mainCamera.orthographicSize - destroyOffset;

        // Если препятствие ушло ниже границы - деактивируем
        if (transform.position.y < bottomBoundary)
        {
            DeactivateObstacle();
        }
    }

    /// <summary>
    /// Деактивация препятствия (возврат в пул)
    /// </summary>
    private void DeactivateObstacle()
    {
        // Находим ObstacleSpawner и возвращаем объект в пул
        ObstacleSpawner spawner = FindObjectOfType<ObstacleSpawner>();
        
        if (spawner != null)
        {
            Obstacle obstacle = GetComponent<Obstacle>();
            spawner.ReturnToPool(obstacle);
        }
        else
        {
            // Если spawner не найден - просто деактивируем
            gameObject.SetActive(false);
        }
    }
}