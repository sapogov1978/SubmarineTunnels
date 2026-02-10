using UnityEngine;

/// <summary>
/// Controls submarine movement with touch/mouse input
/// Submarine moves horizontally while maintaining fixed Y position
/// Uses smooth damping for responsive yet fluid movement
/// </summary>
public class SubmarineController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [Header("Fixed Position")]
    [SerializeField] private float fixedY = -3f;  // Fixed Y position in world space
    [SerializeField] private float fixedZ = 0f;   // Fixed Z position for 2D rendering
    [SerializeField] private bool autoCalculateY = true;  // Auto-calculate Y based on screen size
    [SerializeField] private Camera mainCamera;

    private Vector2 startTouchPos;
    private float startSubX;
    private bool isTouching;
    private Rigidbody2D rb;
    private float targetX;
    private float desiredX;
    private float targetXVelocity;
    private float inputDampTimer = 0f;
    [SerializeField] private float knockbackInputDampDuration = 0.12f;
    [SerializeField] private float knockbackInputDampFactor = 0.35f;
    [SerializeField] private float inputSmoothTime = 0.06f;

    void Start()
    {
        // Initialize Rigidbody2D with required settings
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Calculate Y position based on screen size
        if (autoCalculateY)
        {
            if (!mainCamera) mainCamera = Camera.main;

            // Position submarine at lower portion of screen (30% from bottom)
            // Camera center is at Y=0, orthographicSize is half screen height
            // Bottom edge is at -orthographicSize
            fixedY = -mainCamera.orthographicSize * 0.7f;
        }

        // Set initial position
        transform.position = new Vector3(0f, fixedY, fixedZ);
        targetX = transform.position.x;
        desiredX = targetX;
    }

    void Update()
    {
        if (inputDampTimer > 0f)
        {
            inputDampTimer -= Time.deltaTime;
        }

#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        float smoothTime = inputSmoothTime;
        if (inputDampTimer > 0f)
        {
            smoothTime = Mathf.Max(0.01f, inputSmoothTime / Mathf.Max(0.1f, knockbackInputDampFactor));
        }
        targetX = Mathf.SmoothDamp(targetX, desiredX, ref targetXVelocity, smoothTime);
        rb.MovePosition(new Vector2(targetX, fixedY));
    }

    /// <summary>
    /// Handle touch input (mobile devices)
    /// </summary>
    private void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            isTouching = true;
            startTouchPos = touch.position;
            startSubX = transform.position.x;
        }
        else if (touch.phase == TouchPhase.Moved && isTouching)
        {
            // Reset reference point during knockback dampening
            if (inputDampTimer > 0f)
            {
                startTouchPos = touch.position;
                startSubX = targetX;
            }
            float deltaX = (touch.position.x - startTouchPos.x) / Screen.width;
            Move(deltaX);
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            isTouching = false;
        }
    }

    /// <summary>
    /// Handle mouse input (editor and desktop builds)
    /// </summary>
    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isTouching = true;
            startTouchPos = Input.mousePosition;
            startSubX = transform.position.x;
        }
        else if (Input.GetMouseButton(0) && isTouching)
        {
            // Reset reference point during knockback dampening
            if (inputDampTimer > 0f)
            {
                startTouchPos = Input.mousePosition;
                startSubX = targetX;
            }
            float deltaX = (Input.mousePosition.x - startTouchPos.x) / Screen.width;
            Move(deltaX);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isTouching = false;
        }
    }

    /// <summary>
    /// Calculate and apply movement based on input delta
    /// </summary>
    private void Move(float deltaX)
    {
        // Reduce input sensitivity during knockback
        if (inputDampTimer > 0f)
        {
            deltaX *= knockbackInputDampFactor;
        }

        float nextX = startSubX + deltaX * moveSpeed;
        desiredX = nextX;
    }

    /// <summary>
    /// Apply knockback force without boundary constraints
    /// </summary>
    public void ApplyKnockback(float pushX)
    {
        desiredX = desiredX + pushX;
        targetX = desiredX;
        targetXVelocity = 0f;
        startSubX = desiredX;
        inputDampTimer = knockbackInputDampDuration;
    }

    /// <summary>
    /// Apply knockback force with boundary clamping to keep submarine within tunnel
    /// </summary>
    public void ApplyKnockbackClamped(float pushX, float minX, float maxXClamp)
    {
        desiredX = Mathf.Clamp(desiredX + pushX, minX, maxXClamp);
        targetX = desiredX;
        targetXVelocity = 0f;
        startSubX = desiredX;
        inputDampTimer = knockbackInputDampDuration;
    }

    
}
