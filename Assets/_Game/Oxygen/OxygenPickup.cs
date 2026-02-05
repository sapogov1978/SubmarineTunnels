using UnityEngine;

/// <summary>
/// Собираемый кислородный баллон
/// Восстанавливает кислород при столкновении с батискафом
/// День 7: Дать игроку возможность продлевать жизнь
/// </summary>
public class OxygenPickup : MonoBehaviour
{
    [Header("Oxygen Settings")]
    [SerializeField] private float oxygenAmount = 25f; // Сколько % кислорода восстанавливает

    [Header("Movement")]
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private bool rotateWhileMoving = true;
    [SerializeField] private float rotationSpeed = 45f; // градусов в секунду

    [Header("Visual Feedback")]
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private AudioClip pickupSound;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private bool hasBeenCollected = false;

    void Start()
    {
        // DEBUG: Проверяем наличие необходимых компонентов
        if (showDebugLogs)
        {
            ValidateComponents();
        }
    }

    void Update()
    {
        // Движение вниз синхронно с туннелем
        transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime, Space.World);

        // Вращение для визуального эффекта
        if (rotateWhileMoving)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Обработка столкновения с батискафом
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // DEBUG: Всегда логируем столкновения если включены логи
        if (showDebugLogs)
        {
            Debug.Log($"[OxygenPickup] ⚡ OnTriggerEnter2D с объектом: '{other.gameObject.name}', Tag: '{other.tag}', Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        }
        
        // Проверяем что столкнулись с игроком
        if (other.CompareTag("Player") && !hasBeenCollected)
        {
            hasBeenCollected = true;
            CollectPickup();
        }
        else if (showDebugLogs)
        {
            // DEBUG: Объясняем почему НЕ собрали
            if (!other.CompareTag("Player"))
            {
                Debug.LogWarning($"[OxygenPickup] ❌ Тег '{other.tag}' не равен 'Player' - баллон НЕ собран!");
            }
            else if (hasBeenCollected)
            {
                Debug.LogWarning($"[OxygenPickup] ❌ Баллон уже был собран ранее!");
            }
        }
    }
    
    /// <summary>
    /// DEBUG: Обработка физического столкновения (если Is Trigger = FALSE)
    /// Этот метод НЕ должен срабатывать в нормальных условиях!
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (showDebugLogs)
        {
            Debug.LogError($"[OxygenPickup] ⚠️ COLLISION (не триггер!) с: {collision.gameObject.name}");
            Debug.LogError($"[OxygenPickup] ⚠️ ПРОБЛЕМА: Коллайдер должен быть Is Trigger = TRUE!");
        }
    }

    /// <summary>
    /// Сбор кислородного баллона
    /// </summary>
    private void CollectPickup()
    {
        if (showDebugLogs)
            Debug.Log($"[OxygenPickup] Collected! +{oxygenAmount}% oxygen");

        // Восстанавливаем кислород через OxygenManager
        if (OxygenManager.Instance != null)
        {
            float oxygenBefore = OxygenManager.Instance.GetOxygenPercentage();
            OxygenManager.Instance.AddOxygen(oxygenAmount);
            float oxygenAfter = OxygenManager.Instance.GetOxygenPercentage();

            if (showDebugLogs)
                Debug.Log($"[OxygenPickup] Oxygen: {oxygenBefore:F0}% → {oxygenAfter:F0}%");
        }
        else
        {
            Debug.LogError("[OxygenPickup] OxygenManager not found!");
        }

        // Визуальный эффект
        if (pickupEffectPrefab != null)
        {
            Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
        }

        // Звуковой эффект
        if (pickupSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(pickupSound);
        }

        // ВАЖНО: Возвращаем в пул вместо уничтожения (как в Debris.cs)
        ObstacleSpawner spawner = FindObjectOfType<ObstacleSpawner>();
        if (spawner != null)
        {
            spawner.ReturnOxygenToPool(this);
        }
        else
        {
            // Fallback: деактивируем или уничтожаем
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Сброс состояния для переиспользования в Object Pool
    /// </summary>
    public void ResetPickup()
    {
        hasBeenCollected = false;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one; // Восстанавливаем базовый масштаб
    }

    /// <summary>
    /// Установить скорость скроллинга (вызывается из спавнера)
    /// </summary>
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }

    /// <summary>
    /// Установить количество восстанавливаемого кислорода
    /// </summary>
    public void SetOxygenAmount(float amount)
    {
        oxygenAmount = amount;
    }

    // DEBUG метод для тестирования
    #if UNITY_EDITOR
    [ContextMenu("Test: Collect Pickup")]
    private void TestCollectPickup()
    {
        CollectPickup();
    }
    
    /// <summary>
    /// DEBUG: Проверка правильности настройки компонентов
    /// </summary>
    private void ValidateComponents()
    {
        Debug.Log("=== [OxygenPickup] Проверка компонентов ===");
        
        // Проверка Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"✅ Rigidbody2D: ЕСТЬ (Body Type: {rb.bodyType})");
            if (rb.bodyType != RigidbodyType2D.Kinematic)
            {
                Debug.LogWarning($"⚠️ Rigidbody2D должен быть Kinematic, а не {rb.bodyType}!");
            }
        }
        else
        {
            Debug.LogError("❌ Rigidbody2D: НЕТ! Добавьте компонент Rigidbody2D!");
        }
        
        // Проверка Collider2D
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Debug.Log($"✅ Collider2D: ЕСТЬ (Тип: {col.GetType().Name})");
            if (col.isTrigger)
            {
                Debug.Log("✅ Is Trigger: TRUE (правильно)");
            }
            else
            {
                Debug.LogError("❌ Is Trigger: FALSE! Включите Is Trigger в коллайдере!");
            }
        }
        else
        {
            Debug.LogError("❌ Collider2D: НЕТ! Добавьте Circle/Polygon Collider2D!");
        }
        
        // Проверка SpriteRenderer
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Debug.Log($"✅ SpriteRenderer: ЕСТЬ (Sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")})");
        }
        else
        {
            Debug.LogWarning("⚠️ SpriteRenderer: НЕТ (баллон не будет виден)");
        }
        
        Debug.Log("===========================================");
    }
    #endif
}