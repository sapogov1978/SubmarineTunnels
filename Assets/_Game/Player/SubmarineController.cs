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

    void Start()
    {
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
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif
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
        float targetX = startSubX + deltaX * moveSpeed;
        targetX = Mathf.Clamp(targetX, -maxX, maxX);

        // FIXED: Only change X position, Y and Z are always fixed
        transform.position = new Vector3(
            targetX,
            fixedY,  // Always fixed Y
            fixedZ   // Always fixed Z
        );
    }
}