using UnityEngine;

/// <summary>
/// Базовый класс для всех препятствий в игре
/// Наследуйте от этого класса для создания конкретных типов препятствий
/// </summary>
public abstract class Obstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [SerializeField] protected float damageAmount = 33f;
    [SerializeField] protected bool destroyOnHit = true;

    [Header("Visual Feedback")]
    [SerializeField] protected GameObject hitEffectPrefab;
    [SerializeField] protected AudioClip hitSound;

    [Header("Debug")]
    [SerializeField] protected bool showDebugLogs = false;

    protected bool hasBeenHit = false;

    /// <summary>
    /// Вызывается при столкновении с игроком
    /// </summary>
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasBeenHit)
        {
            hasBeenHit = true;
            OnHit(other.gameObject);
        }
    }

    /// <summary>
    /// Обработка столкновения
    /// ВАЖНО: Урон наносится CollisionDetector.cs, а не здесь!
    /// Это предотвращает дублирование урона
    /// </summary>
    protected virtual void OnHit(GameObject player)
    {
        if (showDebugLogs)
            Debug.Log($"[{GetType().Name}] Hit player! destroyOnHit={destroyOnHit}");

        // Визуальный эффект
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Звуковой эффект
        if (hitSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hitSound);
        }

        // Уничтожаем препятствие если нужно
        if (destroyOnHit)
        {
            if (showDebugLogs)
                Debug.Log($"[{GetType().Name}] Destroying obstacle at {transform.position}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Сброс состояния для переиспользования (Object Pooling)
    /// </summary>
    public virtual void ResetObstacle()
    {
        hasBeenHit = false;
    }

    public float GetDamage()
    {
        return damageAmount;
    }

    public void SetDamage(float newDamage)
    {
        damageAmount = newDamage;
    }
}