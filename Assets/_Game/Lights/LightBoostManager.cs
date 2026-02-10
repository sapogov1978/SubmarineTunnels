using UnityEngine;
using System;

/// <summary>
/// Manages light boost power-up for submarine
/// Activated after ad viewing (currently placeholder)
/// Provides temporary lighting enhancement
/// </summary>
public class LightBoostManager : MonoBehaviour
{
    public static LightBoostManager Instance { get; private set; }

    [Header("Boost Settings")]
    [SerializeField] private float boostDuration = 30f;  // Seconds
    [SerializeField] private bool startWithBoost = false;  // For testing

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Events for other systems
    public event Action OnBoostActivated;
    public event Action OnBoostEnded;
    public event Action<float> OnBoostTimeChanged;  // Parameter: remaining time

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
        // For testing: can start with active boost
        if (startWithBoost)
        {
            ActivateBoost();
        }
    }

    void Update()
    {
        // Update timer if boost is active
        if (isBoostActive)
        {
            boostTimer -= Time.deltaTime;

            // Notify UI about time change
            OnBoostTimeChanged?.Invoke(boostTimer);

            // Check if boost has ended
            if (boostTimer <= 0f)
            {
                DeactivateBoost();
            }
        }
    }

    /// <summary>
    /// Activate light boost for specified duration
    /// </summary>
    public void ActivateBoost(float? durationOverride = null)
    {
        float duration = (durationOverride.HasValue && durationOverride.Value > 0f)
            ? durationOverride.Value
            : boostDuration;

        if (isBoostActive)
        {
            // If boost is already active - extend duration
            boostTimer = duration;

            if (showDebugLogs)
                Debug.Log($"[LightBoostManager] Boost time extended! New timer: {boostTimer:F0}s");

            return;
        }

        isBoostActive = true;
        boostTimer = duration;

        OnBoostActivated?.Invoke();

        if (showDebugLogs)
            Debug.Log($"[LightBoostManager] Boost ACTIVATED for {duration}s!");
    }

    /// <summary>
    /// Deactivate light boost
    /// </summary>
    private void DeactivateBoost()
    {
        if (!isBoostActive) return;

        isBoostActive = false;
        boostTimer = 0f;

        OnBoostEnded?.Invoke();

        if (showDebugLogs)
            Debug.Log("[LightBoostManager] Boost ENDED");
    }

    /// <summary>
    /// Check if boost is currently active
    /// </summary>
    public bool IsBoostActive()
    {
        return isBoostActive;
    }

    /// <summary>
    /// Get remaining boost time
    /// </summary>
    public float GetRemainingTime()
    {
        return isBoostActive ? boostTimer : 0f;
    }

    /// <summary>
    /// Force stop boost (for testing)
    /// </summary>
    public void StopBoost()
    {
        DeactivateBoost();
    }

    // Test methods for debugging
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
