using UnityEngine; 
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
       
    void Update() 
    { 
        // Движение управляется из TunnelGenerator
    } 
    
    // Генерация линий 
    public void Build() 
    { 
        BuildWall(leftWall, leftStart, leftControl1, leftControl2, leftEnd); 
        BuildWall(rightWall, rightStart, rightControl1, rightControl2, rightEnd); 
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
    
    public float GetBottomY() 
    { 
        return Mathf.Min(leftEnd.y, rightEnd.y) + transform.position.y; 
    } 
    
    public float GetTopY() 
    { 
        return Mathf.Max(leftEnd.y, rightEnd.y) + transform.position.y; 
    } 
}