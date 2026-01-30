using UnityEngine;

/// <summary>
/// Базовый класс для всех препятствий в игре
/// Наследуйте от этого класса для создания конкретных типов препятствий
/// </summary>
public abstract class Obstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [SerializeField] protected float damageAmount = 33f; // урон в процентах кислорода
    [SerializeField] protected bool destroyOnHit = true; // уничтожать ли после столкновения

    [Header("Visual Feedback")]
    [SerializeField] protected GameObject hitEffectPrefab; // VFX при столкновении (опционально)
    [SerializeField] protected AudioClip hitSound; // звук при столкновении (опционально)

    [Header("Debug")]
    [SerializeField] protected bool showDebugLogs = false;

    protected bool hasBeenHit = false; // флаг для предотвращения повторных столкновений

    /// <summary>
    /// Вызывается при столкновении с игроком
    /// </summary>
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем что столкнулись с игроком
        if (other.CompareTag("Player") && !hasBeenHit)
        {
            hasBeenHit = true;
            OnHit(other.gameObject);
        }
    }

    /// <summary>
    /// Обработка столкновения - переопределяется в наследниках
    /// </summary>
    /// <param name="player">GameObject игрока</param>
    protected virtual void OnHit(GameObject player)
    {
        if (showDebugLogs)
            Debug.Log($"[{GetType().Name}] Hit player! Damage: {damageAmount}%");

        // Наносим урон
        if (OxygenManager.Instance != null)
        {
            OxygenManager.Instance.RemoveOxygen(damageAmount);
        }

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

    /// <summary>
    /// Публичный метод для получения урона (для балансировки)
    /// </summary>
    public float GetDamage()
    {
        return damageAmount;
    }

    /// <summary>
    /// Публичный метод для изменения урона (для балансировки)
    /// </summary>
    public void SetDamage(float newDamage)
    {
        damageAmount = newDamage;
    }
}