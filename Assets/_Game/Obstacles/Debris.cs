using UnityEngine;

/// <summary>
/// Обломки - препятствие с дрейфом
/// УПРОЩЕНО: Дрейф с фиксированными границами (ширина экрана)
/// ВАЖНО: Использует Object Pooling, поэтому вернули в пул вместо Destroy
/// </summary>
public class Debris : Obstacle
{
    [Header("Movement")]
    [SerializeField] private float scrollSpeed = 2f;

    [Header("Drift")]
    [SerializeField] private bool driftSideways = true;
    [SerializeField] private float driftSpeed = 1.5f;
    [SerializeField] private float driftAmplitude = 0.25f;

    private float driftTimer = 0f;
    private float startX;
    private float debrisRadius = 0.12f;

    void Start()
    {
        startX = transform.position.x;
        driftTimer = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        // Движение вниз
        transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime, Space.World);

        // Дрейф влево-вправо
        if (driftSideways)
        {
            driftTimer += driftSpeed * Time.deltaTime;
            float offsetX = Mathf.Sin(driftTimer) * driftAmplitude;
            
            Vector3 pos = transform.position;
            pos.x = startX + offsetX;
            
            // Простое ограничение по ширине экрана
            pos.x = Mathf.Clamp(pos.x, -2.5f, 2.5f);
            
            transform.position = pos;
        }
    }

    protected override void OnHit(GameObject player)
    {
        // НЕ вызываем base.OnHit() - Debris использует pooling!
        // base.OnHit() содержит Destroy(gameObject), что нарушает pooling
        
        if (showDebugLogs)
            Debug.Log("[Debris] Hit! Returning to pool");

        // Визуальный эффект (из Obstacle)
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Звуковой эффект (из Obstacle)
        if (hitSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hitSound);
        }

        // ГЛАВНОЕ: Возвращаем в пул вместо Destroy
        ObstacleSpawner spawner = FindObjectOfType<ObstacleSpawner>();
        if (spawner != null)
        {
            spawner.ReturnToPool(this);
            if (showDebugLogs)
                Debug.Log("[Debris] Returned to pool");
        }
        else
        {
            // Fallback: просто деактивируем
            gameObject.SetActive(false);
            if (showDebugLogs)
                Debug.Log("[Debris] No spawner found, deactivated");
        }
    }

    public override void ResetObstacle()
    {
        base.ResetObstacle();
        driftTimer = Random.Range(0f, 2f * Mathf.PI);
        if (transform.position.x != 0)
            startX = transform.position.x;
    }

    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }

    public void SetRadius(float radius)
    {
        debrisRadius = radius;
    }
}