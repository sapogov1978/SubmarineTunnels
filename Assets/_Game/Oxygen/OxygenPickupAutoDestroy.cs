using UnityEngine;

/// <summary>
/// Автоматически деактивирует кислородный баллон когда он уходит за пределы экрана
/// Баллон возвращается в Object Pool для переиспользования
/// День 7: Аналог ObstacleAutoDestroy для OxygenPickup
/// </summary>
[RequireComponent(typeof(OxygenPickup))]
public class OxygenPickupAutoDestroy : MonoBehaviour
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
            Debug.LogError("[OxygenPickupAutoDestroy] Main camera not found!");
            enabled = false;
        }
    }

    void Update()
    {
        if (!mainCamera) return;

        // Вычисляем нижнюю границу экрана
        bottomBoundary = mainCamera.transform.position.y - mainCamera.orthographicSize - destroyOffset;

        // Если баллон ушёл ниже границы - деактивируем
        if (transform.position.y < bottomBoundary)
        {
            DeactivatePickup();
        }
    }

    /// <summary>
    /// Деактивация кислородного баллона (возврат в пул)
    /// </summary>
    private void DeactivatePickup()
    {
        // Находим ObstacleSpawner и возвращаем объект в пул
        ObstacleSpawner spawner = FindObjectOfType<ObstacleSpawner>();
        
        if (spawner != null)
        {
            OxygenPickup pickup = GetComponent<OxygenPickup>();
            spawner.ReturnOxygenToPool(pickup);
        }
        else
        {
            // Если spawner не найден - просто деактивируем
            gameObject.SetActive(false);
        }
    }
}