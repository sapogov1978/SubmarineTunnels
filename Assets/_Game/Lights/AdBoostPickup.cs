using UnityEngine;

/// <summary>
/// Рекламный шарик - собираемый объект
/// День 8: Активация буста (пока без рекламы)
/// День 10: Интеграция Unity Ads
/// </summary>
public class AdBoostPickup : MonoBehaviour
{
        private float scrollSpeed = 2f;
    private float destroyOffset = 2f;
    [SerializeField] private bool rotateWhileMoving = true;
    [SerializeField] private float rotationSpeed = 60f; // градусов в секунду

    [Header("Visual Feedback")]
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private AudioClip pickupSound;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool skipAdForTesting = true; // День 8: пропускаем рекламу

    private bool hasBeenCollected = false;
    private Camera mainCamera;
    private float bottomBoundary;

    void Update()
    {
        if (!mainCamera)
            mainCamera = Camera.main;

        if (RuntimeGameplayMetrics.ScrollSpeed > 0f)
        {
            scrollSpeed = RuntimeGameplayMetrics.ScrollSpeed;
            destroyOffset = RuntimeGameplayMetrics.ScrollSpeed;
        }

        // Движение вниз синхронно с туннелем
        transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime, Space.World);

        if (mainCamera)
        {
            bottomBoundary = mainCamera.transform.position.y - mainCamera.orthographicSize - destroyOffset;
            if (transform.position.y < bottomBoundary)
            {
                Destroy(gameObject);
                return;
            }
        }

        // Вращение для привлечения внимания
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
        if (showDebugLogs)
        {
            Debug.Log($"[AdBoostPickup] ⚡ OnTriggerEnter2D с объектом: '{other.gameObject.name}', Tag: '{other.tag}'");
        }

        // Проверяем что столкнулись с игроком
        if (other.CompareTag("Player") && !hasBeenCollected)
        {
            hasBeenCollected = true;
            CollectPickup();
        }
    }

    /// <summary>
    /// Сбор рекламного шарика
    /// </summary>
    private void CollectPickup()
    {
        if (showDebugLogs)
            Debug.Log("[AdBoostPickup] 🎯 Ad boost pickup collected!");

        // День 8: ЗАГЛУШКА - просто активируем буст без рекламы
        // День 10: Здесь будет вызов RewardedAdsManager.ShowAd()
        if (skipAdForTesting)
        {
            if (showDebugLogs)
                Debug.Log("[AdBoostPickup] ⏭️ Skipping ad (test mode), activating boost directly");

            // Сразу активируем буст
            ActivateBoost();
        }
        else
        {
            // День 10: Показ рекламы
            // RewardedAdsManager.Instance.ShowRewardedAd(OnAdWatched, OnAdFailed);
            Debug.LogWarning("[AdBoostPickup] ⚠️ Ad system not implemented yet! Activating boost anyway.");
            ActivateBoost();
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

        // Уничтожаем шарик
        // День 9-10: Возможно переделаем на Object Pooling
        Destroy(gameObject);
    }

    /// <summary>
    /// Активация светового буста
    /// Вызывается после успешного просмотра рекламы (или сразу в тестовом режиме)
    /// </summary>
    private void ActivateBoost()
    {
        if (LightBoostManager.Instance != null)
        {
            LightBoostManager.Instance.ActivateBoost();

            if (showDebugLogs)
                Debug.Log("[AdBoostPickup] ✨ Light boost activated!");
        }
        else
        {
            Debug.LogError("[AdBoostPickup] LightBoostManager not found!");
        }
    }

    /// <summary>
    /// Callback: реклама просмотрена успешно
    /// День 10: Интеграция с Unity Ads
    /// </summary>
    private void OnAdWatched()
    {
        if (showDebugLogs)
            Debug.Log("[AdBoostPickup] ✅ Ad watched! Activating boost...");

        ActivateBoost();
    }

    /// <summary>
    /// Callback: реклама не показалась или была закрыта
    /// День 10: Интеграция с Unity Ads
    /// </summary>
    private void OnAdFailed()
    {
        if (showDebugLogs)
            Debug.Log("[AdBoostPickup] ❌ Ad failed or skipped. No boost.");

        // Ничего не делаем - буст не активируется
    }

    /// <summary>
    /// Установить скорость скроллинга (вызывается из спавнера)
    /// </summary>
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }

    #if UNITY_EDITOR
    [ContextMenu("Test: Collect Pickup")]
    private void TestCollectPickup()
    {
        CollectPickup();
    }
    #endif
}
