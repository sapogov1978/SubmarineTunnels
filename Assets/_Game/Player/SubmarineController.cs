using UnityEngine;

public class SubmarineController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxX = 2.5f;
    
    [Header("Fixed Position")]
    [SerializeField] private float fixedY = -3f;  // Fixed Y position (lower quarter of screen)
    [SerializeField] private float fixedZ = 0f;   // Fixed Z position
    [SerializeField] private bool autoCalculateY = true;
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
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // FIXED: Set submarine position on Start, not in first frame
        if (autoCalculateY)
        {
            if (!mainCamera) mainCamera = Camera.main;
            
            // Position at 25% from bottom of screen
            // Camera is at Y=0, orthographicSize is half screen height
            // So bottom is at -orthographicSize
            // 25% from bottom = -orthographicSize + (orthographicSize * 0.5)
            fixedY = -mainCamera.orthographicSize * 0.7f;
        }
        
        // Set initial position immediately
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

    private void Move(float deltaX)
    {
        if (inputDampTimer > 0f)
        {
            deltaX *= knockbackInputDampFactor;
        }

        float nextX = startSubX + deltaX * moveSpeed;
        desiredX = Mathf.Clamp(nextX, -maxX, maxX);
    }

    public void ApplyKnockback(float pushX)
    {
        desiredX = Mathf.Clamp(desiredX + pushX, -maxX, maxX);
        targetX = desiredX;
        targetXVelocity = 0f;
        startSubX = desiredX;
        inputDampTimer = knockbackInputDampDuration;
    }

    public void ApplyKnockbackClamped(float pushX, float minX, float maxXClamp)
    {
        desiredX = Mathf.Clamp(desiredX + pushX, minX, maxXClamp);
        targetX = desiredX;
        targetXVelocity = 0f;
        startSubX = desiredX;
        inputDampTimer = knockbackInputDampDuration;
    }
}
