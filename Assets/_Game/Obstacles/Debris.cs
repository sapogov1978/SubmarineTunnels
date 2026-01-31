using UnityEngine;

/// <summary>
/// Обломки - более мелкие препятствия
/// Наносят меньше урона чем камни
/// </summary>
public class Debris : Obstacle
{
    [Header("Movement")]
    [SerializeField] private float fallSpeed = 2f; // должна совпадать со scrollSpeed туннеля

    [Header("Debris Specific")]
    [SerializeField] private bool driftSideways = true;
    [SerializeField] private float driftSpeed = 0.5f;
    [SerializeField] private float driftAmplitude = 0.2f; // Уменьшена амплитуда

    private float driftTimer = 0f;
    private float startX;
    private float minX = -2f; // Границы будут обновляться
    private float maxX = 2f;

    void Start()
    {
        startX = transform.position.x;
        driftTimer = Random.Range(0f, 2f * Mathf.PI);
        
        // Устанавливаем границы дрейфа
        UpdateDriftBounds();
    }

    void Update()
    {
        // Движение вниз (падение)
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        // Обломки слегка дрейфуют влево-вправо при падении
        if (driftSideways)
        {
            driftTimer += driftSpeed * Time.deltaTime;
            float offsetX = Mathf.Sin(driftTimer) * driftAmplitude;
            
            Vector3 pos = transform.position;
            pos.x = startX + offsetX;
            
            // ИСПРАВЛЕНИЕ ПРОБЛЕМЫ 2: Clamp чтобы не выходить за границы
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            
            transform.position = pos;
        }
    }

    private void UpdateDriftBounds()
    {
        // Пытаемся получить границы от TunnelGenerator
        TunnelGenerator tunnel = FindObjectOfType<TunnelGenerator>();
        if (tunnel != null)
        {
            float leftWall, rightWall;
            tunnel.GetTunnelBounds(out leftWall, out rightWall);
            
            // Устанавливаем границы с небольшим отступом
            minX = leftWall + 0.3f;
            maxX = rightWall - 0.3f;
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