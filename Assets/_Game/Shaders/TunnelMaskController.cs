using UnityEngine;

/// <summary>
/// Управляет shader-маской туннеля
/// Обновляет позиции стен для shader в реальном времени
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TunnelMaskController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TunnelGenerator tunnelGenerator;
    [SerializeField] private Material maskMaterial;
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.05f; // Обновление 20 раз/сек
    [SerializeField] private bool followCamera = true;
    
    private SpriteRenderer spriteRenderer;
    private float updateTimer = 0f;
    
    // Shader property IDs (для оптимизации)
    private static readonly int LeftWallXID = Shader.PropertyToID("_LeftWallX");
    private static readonly int RightWallXID = Shader.PropertyToID("_RightWallX");
    private static readonly int TunnelCenterYID = Shader.PropertyToID("_TunnelCenterY");
    
    void Start()
    {
        if (!mainCamera) mainCamera = Camera.main;
        if (!tunnelGenerator) tunnelGenerator = FindObjectOfType<TunnelGenerator>();
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.material = maskMaterial;
        spriteRenderer.sortingLayerName = "TunnelOverlay";
        spriteRenderer.sortingOrder = 100;
        
        // Создаём белый квадрат для рендеринга shader
        CreateMaskQuad();
        
        // Начальное обновление
        UpdateMaskPositions();
    }
    
    void Update()
    {
        // Следуем за камерой
        if (followCamera && mainCamera != null)
        {
            transform.position = new Vector3(
                mainCamera.transform.position.x,
                mainCamera.transform.position.y,
                5f // Перед всем
            );
        }
        
        // Обновляем позиции стен для shader
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateMaskPositions();
        }
    }
    
    /// <summary>
    /// Обновляет позиции стен туннеля в shader
    /// </summary>
    private void UpdateMaskPositions()
    {
        if (!tunnelGenerator || !mainCamera || !maskMaterial) return;
        
        // Получаем Y позицию батискафа (центр экрана)
        float submarineY = mainCamera.transform.position.y;
        
        // Находим текущий сегмент туннеля на этой высоте
        float leftWallX = -2.5f;  // Fallback значения
        float rightWallX = 2.5f;
        
        foreach (var segment in tunnelGenerator.GetSegments())
        {
            if (segment.GetWallPositionsAtY(submarineY, out float left, out float right))
            {
                leftWallX = left;
                rightWallX = right;
                break;
            }
        }
        
        // Передаём позиции в shader
        maskMaterial.SetFloat(LeftWallXID, leftWallX);
        maskMaterial.SetFloat(RightWallXID, rightWallX);
        maskMaterial.SetFloat(TunnelCenterYID, submarineY);
    }
    
    /// <summary>
    /// Создаёт quad на весь экран для рендеринга shader
    /// </summary>
    private void CreateMaskQuad()
    {
        if (!mainCamera) return;
        
        // Получаем размер экрана в world units
        float screenHeight = mainCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCamera.aspect;
        
        // Создаём белый спрайт 1×1
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        
        Sprite sprite = Sprite.Create(
            tex, 
            new Rect(0, 0, 1, 1), 
            new Vector2(0.5f, 0.5f),
            1f
        );
        
        spriteRenderer.sprite = sprite;
        
        // Масштабируем на весь экран
        transform.localScale = new Vector3(screenWidth, screenHeight, 1f);
        
        Debug.Log($"[TunnelMask] Created fullscreen quad: {screenWidth}×{screenHeight}");
    }
}