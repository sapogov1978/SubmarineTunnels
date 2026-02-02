using UnityEngine;

/// <summary>
/// Обломки - препятствие с дрейфом
/// УПРОЩЕНО: Дрейф с фиксированными границами (ширина экрана)
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
        base.OnHit(player);

        if (showDebugLogs)
            Debug.Log("[Debris] Small hit");
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