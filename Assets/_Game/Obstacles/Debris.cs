using UnityEngine;

/// <summary>
/// Обломки - более мелкие препятствия
/// Наносят меньше урона чем камни
/// </summary>
public class Debris : Obstacle
{
    [Header("Debris Specific")]
    [SerializeField] private bool driftSideways = true;
    [SerializeField] private float driftSpeed = 0.5f;
    [SerializeField] private float driftAmplitude = 0.3f;

    private float driftTimer = 0f;
    private float startX;

    void Start()
    {
        startX = transform.position.x;
        driftTimer = Random.Range(0f, 2f * Mathf.PI); // случайное начальное смещение
    }

    void Update()
    {
        // Обломки слегка дрейфуют влево-вправо при падении
        if (driftSideways)
        {
            driftTimer += driftSpeed * Time.deltaTime;
            float offsetX = Mathf.Sin(driftTimer) * driftAmplitude;
            
            Vector3 pos = transform.position;
            pos.x = startX + offsetX;
            transform.position = pos;
        }
    }

    protected override void OnHit(GameObject player)
    {
        // Вызываем базовую логику
        base.OnHit(player);

        // Дополнительная логика для обломков (если нужна)
        if (showDebugLogs)
            Debug.Log("[Debris] Small hit, less damage");
    }

    public override void ResetObstacle()
    {
        base.ResetObstacle();
        
        // Сброс дрейфа при переиспользовании
        driftTimer = Random.Range(0f, 2f * Mathf.PI);
        if (transform.position.x != 0)
            startX = transform.position.x;
    }
}