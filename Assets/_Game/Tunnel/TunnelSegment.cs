using UnityEngine; 
using System.Collections.Generic;

/// <summary>
/// Сегмент туннеля с кривыми Безье
/// ПРАВИЛЬНО: EdgeCollider2D следует форме кривой Безье (не прямолинейно!)
/// День 6: Коллайдеры совпадают с визуальной формой туннеля
/// </summary>
public class TunnelSegment : MonoBehaviour 
{ 
    [Header("Bezier Points")] 
    public Vector2 leftStart; 
    public Vector2 leftEnd; 
    public Vector2 leftControl1; 
    public Vector2 leftControl2; 
    public Vector2 rightStart; 
    public Vector2 rightEnd; 
    public Vector2 rightControl1; 
    public Vector2 rightControl2; 
    
    [Header("Visual")] 
    [SerializeField] private LineRenderer leftWall; 
    [SerializeField] private LineRenderer rightWall; 
    [SerializeField] private int resolution = 200; // высокое разрешение для плавных линий
    
    [Header("Collision")]
    [SerializeField] private int collisionResolution = 30; // меньше чем визуальное разрешение, но достаточно точно
    
    // Флаг чтобы коллайдеры создавались только один раз
    private bool collidersCreated = false;
    
    // Кэшируем высоту сегмента для быстрых расчётов
    private float segmentHeight;
    private EdgeCollider2D leftWallCollider;  // физический - батискаф НЕ проходит
    private EdgeCollider2D rightWallCollider; // физический - батискаф НЕ проходит
    private EdgeCollider2D leftWallTrigger;   // триггер - генерирует события
    private EdgeCollider2D rightWallTrigger;  // триггер - генерирует события
       
    void Update() 
    { 
        // Движение управляется из TunnelGenerator
    } 
    
    /// <summary>
    /// Генерация визуальных линий и физических коллайдеров
    /// </summary>
    public void Build() 
    { 
        BuildWall(leftWall, leftStart, leftControl1, leftControl2, leftEnd); 
        BuildWall(rightWall, rightStart, rightControl1, rightControl2, rightEnd);
        
        // Сохраняем высоту сегмента
        segmentHeight = Mathf.Max(leftEnd.y, rightEnd.y);
        
        // ПРАВИЛЬНО: Создаём EdgeCollider2D которые следуют кривой Безье
        // НО только если они ещё не были созданы!
        if (!collidersCreated)
        {
            CreateEdgeColliders();
            collidersCreated = true;
        }
    } 
    
    /// <summary>
    /// Создание EdgeCollider2D для левой и правой стенок
    /// EdgeCollider2D идеально подходит для кривых линий
    /// </summary>
    private void CreateEdgeColliders()
    {
        Debug.Log($"[TunnelSegment] Creating colliders for segment at Y={transform.position.y}");
        
        // Генерируем точки для левой стены
        Vector2[] leftPoints = GenerateBezierPoints(
            leftStart, leftControl1, leftControl2, leftEnd, 
            collisionResolution
        );
        
        // Генерируем точки для правой стены
        Vector2[] rightPoints = GenerateBezierPoints(
            rightStart, rightControl1, rightControl2, rightEnd, 
            collisionResolution
        );
        
        Debug.Log($"[TunnelSegment] Generated {leftPoints.Length} points for each wall");
        
        // Удаляем старые коллайдеры если они есть
        EdgeCollider2D[] existingColliders = GetComponents<EdgeCollider2D>();
        foreach (EdgeCollider2D ec in existingColliders)
        {
            DestroyImmediate(ec);
        }
        
        // ВАЖНО: EdgeCollider2D требует Rigidbody2D!
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            rb.useFullKinematicContacts = true; // ← ВКЛЮЧИТЬ для OnCollisionEnter2D!
            Debug.Log("[TunnelSegment] Added Rigidbody2D with Full Kinematic Contacts");
        }
        
        // ════════════════════════════════════════════════════════════
        // ФИЗИЧЕСКИЕ КОЛЛАЙДЕРЫ (isTrigger = FALSE)
        // Батискаф НЕ ПРОХОДИТ сквозь стены!
        // ════════════════════════════════════════════════════════════
        leftWallCollider = gameObject.AddComponent<EdgeCollider2D>();
        leftWallCollider.points = leftPoints;
        leftWallCollider.isTrigger = false; // ← ФИЗИЧЕСКИЙ! Батискаф не проходит
        Debug.Log($"[TunnelSegment] Created LEFT physical collider with {leftPoints.Length} points");
        
        rightWallCollider = gameObject.AddComponent<EdgeCollider2D>();
        rightWallCollider.points = rightPoints;
        rightWallCollider.isTrigger = false; // ← ФИЗИЧЕСКИЙ! Батискаф не проходит
        Debug.Log($"[TunnelSegment] Created RIGHT physical collider with {rightPoints.Length} points");
        
        // ════════════════════════════════════════════════════════════
        // ТРИГГЕР-КОЛЛАЙДЕРЫ (isTrigger = TRUE)
        // Для обнаружения столкновения и урона
        // ════════════════════════════════════════════════════════════
        leftWallTrigger = gameObject.AddComponent<EdgeCollider2D>();
        leftWallTrigger.points = leftPoints;
        leftWallTrigger.isTrigger = true;
        gameObject.tag = "TunnelWall";
        Debug.Log($"[TunnelSegment] Created LEFT trigger collider with {leftPoints.Length} points");
        
        rightWallTrigger = gameObject.AddComponent<EdgeCollider2D>();
        rightWallTrigger.points = rightPoints;
        rightWallTrigger.isTrigger = true;
        gameObject.tag = "TunnelWall";
        Debug.Log($"[TunnelSegment] Created RIGHT trigger collider with {rightPoints.Length} points");
        
        Debug.Log($"[TunnelSegment] ✓ Colliders created successfully! Total: 4 EdgeCollider2D");
    }
    
    /// <summary>
    /// Генерирует точки кубической кривой Безье
    /// </summary>
    private Vector2[] GenerateBezierPoints(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int pointCount)
    {
        Vector2[] points = new Vector2[pointCount];
        
        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);
            points[i] = CubicBezier(p0, p1, p2, p3, t);
        }
        
        return points;
    }
    
    /// <summary>
    /// Вычисление точки на кубической кривой Безье
    /// </summary>
    private Vector2 CubicBezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t) 
    { 
        float u = 1 - t; 
        return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d; 
    }
    
    /// <summary>
    /// Визуальная генерация линий для LineRenderer
    /// </summary>
    private void BuildWall(LineRenderer lr, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3) 
    { 
        lr.positionCount = resolution;
        for (int i = 0; i < resolution; i++) 
        { 
            float t = i / (float)(resolution - 1); 
            Vector2 point = CubicBezier(p0, p1, p2, p3, t); 
            lr.SetPosition(i, point); 
        } 
    } 
    
    /// <summary>
    /// Получить реальные координаты стен на заданной высоте Y
    /// Учитывает изгибы Безье кривых!
    /// Используется для точного позиционирования препятствий
    /// </summary>
    public bool GetWallPositionsAtY(float worldY, out float leftX, out float rightX)
    {
        // Конвертируем мировую Y в локальную координату сегмента
        float localY = worldY - transform.position.y;
        
        // Проверяем что Y находится внутри этого сегмента
        if (localY < 0 || localY > segmentHeight)
        {
            leftX = rightX = 0;
            return false;
        }
        
        // Нормализуем локальную Y в параметр t (0-1) для кривой Безье
        float t = localY / segmentHeight;
        t = Mathf.Clamp01(t);
        
        // Вычисляем точки на кривых Безье в локальных координатах
        Vector2 leftPoint = CubicBezier(leftStart, leftControl1, leftControl2, leftEnd, t);
        Vector2 rightPoint = CubicBezier(rightStart, rightControl1, rightControl2, rightEnd, t);
        
        // Конвертируем обратно в мировые координаты
        leftX = leftPoint.x + transform.position.x;
        rightX = rightPoint.x + transform.position.x;
        
        return true;
    }
    
    public float GetBottomY() 
    { 
        return transform.position.y;
    } 
    
    public float GetTopY() 
    { 
        return transform.position.y + segmentHeight;
    }
}