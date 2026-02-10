using UnityEngine;

/// <summary>
/// Base class for all obstacles in the game
/// Inherit from this class to create specific obstacle types
/// Handles collision detection, effects, and pooling reset
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
    /// Called when colliding with player
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
    /// Handle collision impact
    /// NOTE: Damage is applied by CollisionDetector.cs, not here!
    /// This prevents damage duplication
    /// </summary>
    protected virtual void OnHit(GameObject player)
    {
        if (showDebugLogs)
            Debug.Log($"[{GetType().Name}] Hit player! destroyOnHit={destroyOnHit}");

        // Visual effect
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Sound effect
        if (hitSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hitSound);
        }

        // Destroy obstacle if configured to do so
        if (destroyOnHit)
        {
            if (showDebugLogs)
                Debug.Log($"[{GetType().Name}] Destroying obstacle at {transform.position}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Reset state for object pooling reuse
    /// Resets rotation for mirrored debris and collision flag
    /// </summary>
    public virtual void ResetObstacle()
    {
        hasBeenHit = false;
        transform.rotation = Quaternion.identity;
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
