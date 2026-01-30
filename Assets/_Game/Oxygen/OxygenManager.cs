using UnityEngine;
using System;

/// <summary>
/// Управляет системой кислорода - главным ресурсом игры
/// Singleton паттерн для глобального доступа
/// </summary>
public class OxygenManager : MonoBehaviour
{
    public static OxygenManager Instance { get; private set; }

    [Header("Oxygen Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float currentOxygen = 100f;
    [SerializeField] private float depletionRate = 1f; // -0.5% каждые 2 секунды
    [SerializeField] private float depletionInterval = 1f; // интервал в секундах

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // События для UI и других систем
    public event Action<float> OnOxygenChanged; // параметр: текущий % (0-100)
    public event Action OnOxygenDepleted; // кислород закончился

    private float depletionTimer;
    private bool isGameActive;

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
        currentOxygen = maxOxygen;
        isGameActive = true;
        depletionTimer = 0f;

        // Уведомляем UI о начальном значении
        OnOxygenChanged?.Invoke(GetOxygenPercentage());

        if (showDebugLogs)
            Debug.Log($"[OxygenManager] Initialized. Max Oxygen: {maxOxygen}, Depletion: -{depletionRate}% every {depletionInterval}s");
    }

    void Update()
    {
        if (!isGameActive) return;

        // Таймер для уменьшения кислорода
        depletionTimer += Time.deltaTime;

        if (depletionTimer >= depletionInterval)
        {
            depletionTimer = 0f;
            RemoveOxygen(depletionRate);
        }
    }

    /// <summary>
    /// Добавить кислород (например, при подборе баллона)
    /// </summary>
    /// <param name="amount">Количество кислорода для добавления (в процентах)</param>
    public void AddOxygen(float amount)
    {
        if (!isGameActive) return;

        float previousOxygen = currentOxygen;
        currentOxygen = Mathf.Clamp(currentOxygen + amount, 0f, maxOxygen);

        if (showDebugLogs)
            Debug.Log($"[OxygenManager] +{amount:F1}% oxygen | {previousOxygen:F1}% → {currentOxygen:F1}%");

        OnOxygenChanged?.Invoke(GetOxygenPercentage());
    }

    /// <summary>
    /// Убрать кислород (например, при столкновении или автоматическом уменьшении)
    /// </summary>
    /// <param name="amount">Количество кислорода для удаления (в процентах)</param>
    public void RemoveOxygen(float amount)
    {
        if (!isGameActive) return;

        float previousOxygen = currentOxygen;
        currentOxygen = Mathf.Clamp(currentOxygen - amount, 0f, maxOxygen);

        if (showDebugLogs)
            Debug.Log($"[OxygenManager] -{amount:F1}% oxygen | {previousOxygen:F1}% → {currentOxygen:F1}%");

        OnOxygenChanged?.Invoke(GetOxygenPercentage());

        // Проверка смерти
        if (currentOxygen <= 0f)
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// Получить текущий процент кислорода (0-100)
    /// </summary>
    public float GetOxygenPercentage()
    {
        return (currentOxygen / maxOxygen) * 100f;
    }

    /// <summary>
    /// Получить нормализованное значение кислорода (0-1) для UI Slider/fillAmount
    /// </summary>
    public float GetOxygenNormalized()
    {
        return currentOxygen / maxOxygen;
    }

    /// <summary>
    /// Проверка, жив ли игрок
    /// </summary>
    public bool IsAlive()
    {
        return currentOxygen > 0f;
    }

    /// <summary>
    /// Остановить уменьшение кислорода (при паузе или Game Over)
    /// </summary>
    public void StopDepletion()
    {
        isGameActive = false;
        if (showDebugLogs)
            Debug.Log("[OxygenManager] Oxygen depletion stopped");
    }

    /// <summary>
    /// Возобновить уменьшение кислорода
    /// </summary>
    public void ResumeDepletion()
    {
        isGameActive = true;
        if (showDebugLogs)
            Debug.Log("[OxygenManager] Oxygen depletion resumed");
    }

    /// <summary>
    /// Сброс кислорода для рестарта игры
    /// </summary>
    public void ResetOxygen()
    {
        currentOxygen = maxOxygen;
        depletionTimer = 0f;
        isGameActive = true;

        OnOxygenChanged?.Invoke(GetOxygenPercentage());

        if (showDebugLogs)
            Debug.Log("[OxygenManager] Oxygen reset to 100%");
    }

    /// <summary>
    /// Вызывается когда кислород заканчивается
    /// </summary>
    private void TriggerGameOver()
    {
        if (!isGameActive) return; // Предотвращаем повторный вызов

        isGameActive = false;

        if (showDebugLogs)
            Debug.Log("[OxygenManager] OXYGEN DEPLETED! Game Over triggered.");

        OnOxygenDepleted?.Invoke();
    }

    // DEBUG методы для тестирования
    #if UNITY_EDITOR
    [ContextMenu("Test: Add 25% Oxygen")]
    private void TestAddOxygen()
    {
        AddOxygen(25f);
    }

    [ContextMenu("Test: Remove 33% Oxygen")]
    private void TestRemoveOxygen()
    {
        RemoveOxygen(33f);
    }

    [ContextMenu("Test: Reset Oxygen")]
    private void TestResetOxygen()
    {
        ResetOxygen();
    }
    #endif
}