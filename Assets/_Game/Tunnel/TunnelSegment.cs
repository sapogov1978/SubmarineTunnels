using UnityEngine; 

/// <summary>
/// Сегмент туннеля с кривыми Безье
/// ИСПРАВЛЕНО: Добавлен метод GetWallPositionsAtY() для точного расчёта координат стен
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
    [SerializeField] private int resolution = 200; // высокое разрешение для плавных переходов
    
    // ИСПРАВЛЕНИЕ: Кэшируем высоту сегмента для быстрых расчётов
    private float segmentHeight;
       
    void Update() 
    { 
        // Движение управляется из TunnelGenerator
    } 
    
    // Генерация линий 
    public void Build() 
    { 
        BuildWall(leftWall, leftStart, leftControl1, leftControl2, leftEnd); 
        BuildWall(rightWall, rightStart, rightControl1, rightControl2, rightEnd);
        
        // ИСПРАВЛЕНИЕ: Сохраняем высоту для GetWallPositionsAtY()
        segmentHeight = Mathf.Max(leftEnd.y, rightEnd.y);
    } 
    
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
    
    private Vector2 CubicBezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t) 
    { 
        // кубическая Безье 
        float u = 1 - t; 
        return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d; 
    }
    
    /// <summary>
    /// НОВЫЙ МЕТОД: Получить РЕАЛЬНЫЕ координаты стен на заданной высоте Y
    /// Учитывает изгибы кривых Безье!
    /// Используется ObstacleSpawner для точного размещения препятствий
    /// </summary>
    /// <param name="worldY">Мировая координата Y</param>
    /// <param name="leftX">Выход: X координата левой стены</param>
    /// <param name="rightX">Выход: X координата правой стены</param>
    /// <returns>true если worldY находится внутри этого сегмента</returns>
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
    
    // ИСПРАВЛЕНИЕ: Используем кэшированную высоту вместо расчёта каждый раз
    public float GetBottomY() 
    { 
        return transform.position.y;
    } 
    
    public float GetTopY() 
    { 
        return transform.position.y + segmentHeight;
    }
}