using UnityEngine;
using System;

/// <summary>
/// Управляет световым бустом для батискафа
/// Активируется после просмотра рекламы (пока заглушка)
/// День 8: Создание системы буста
/// </summary>
public class LightBoostManager : MonoBehaviour
{
    public static LightBoostManager Instance { get; private set; }

    [Header("Boost Settings")]
    [SerializeField] private float boostDuration = 30f; // секунды
    [SerializeField] private bool startWithBoost = false; // для тестирования

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // События для других систем
    public event Action OnBoostActivated;
    public event Action OnBoostEnded;
    public event Action<float> OnBoostTimeChanged; // параметр: оставшееся время

    private bool isBoostActive = false;
    private float boostTimer = 0f;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Для тестирования можно начать с активным бустом
        if (startWithBoost)
        {
            ActivateBoost();
        }
    }

    void Update()
    {
        // Обновляем таймер если буст активен
        if (isBoostActive)
        {
            boostTimer -= Time.deltaTime;

            // Уведомляем UI о времени
            OnBoostTimeChanged?.Invoke(boostTimer);

            // Проверяем окончание буста
            if (boostTimer <= 0f)
            {
                DeactivateBoost();
            }
        }
    }

    /// <summary>
    /// Активировать световой буст на заданное время
    /// </summary>
    public void ActivateBoost()
    {
        if (isBoostActive)
        {
            // Если буст уже активен - продлеваем время
            boostTimer = boostDuration;
            
            if (showDebugLogs)
                Debug.Log($"[LightBoostManager] Boost time extended! New timer: {boostTimer:F0}s");
            
            return;
        }

        isBoostActive = true;
        boostTimer = boostDuration;

        OnBoostActivated?.Invoke();

        if (showDebugLogs)
            Debug.Log($"[LightBoostManager] ✨ Boost ACTIVATED for {boostDuration}s!");
    }

    /// <summary>
    /// Деактивировать световой буст
    /// </summary>
    private void DeactivateBoost()
    {
        if (!isBoostActive) return;

        isBoostActive = false;
        boostTimer = 0f;

        OnBoostEnded?.Invoke();

        if (showDebugLogs)
            Debug.Log("[LightBoostManager] 🌑 Boost ENDED");
    }

    /// <summary>
    /// Проверка активен ли буст
    /// </summary>
    public bool IsBoostActive()
    {
        return isBoostActive;
    }

    /// <summary>
    /// Получить оставшееся время буста
    /// </summary>
    public float GetRemainingTime()
    {
        return isBoostActive ? boostTimer : 0f;
    }

    /// <summary>
    /// Принудительно остановить буст (для тестирования)
    /// </summary>
    public void StopBoost()
    {
        DeactivateBoost();
    }

    // DEBUG методы для тестирования
    #if UNITY_EDITOR
    [ContextMenu("Test: Activate Boost")]
    private void TestActivateBoost()
    {
        ActivateBoost();
    }

    [ContextMenu("Test: Stop Boost")]
    private void TestStopBoost()
    {
        StopBoost();
    }
    #endif
}