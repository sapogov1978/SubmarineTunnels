using UnityEngine;

/// <summary>
/// Camera controller that follows the submarine on X axis only
/// Y and Z positions remain fixed for consistent view
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Camera Position")]
    [SerializeField] private float cameraY = 0f;  // Fixed Y position (screen center)
    [SerializeField] private float cameraZ = -10f; // Standard 2D camera Z depth

    void Start()
    {
        // Set initial camera position immediately
        if (target)
        {
            transform.position = new Vector3(target.position.x, cameraY, cameraZ);
        }
        else
        {
            transform.position = new Vector3(0f, cameraY, cameraZ);
        }
    }

    private void LateUpdate()
    {
        if (!target) return;

        // Stop following when game is over
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        // Follow only on X axis, Y and Z remain fixed
        Vector3 targetPos = new Vector3(
            target.position.x,
            cameraY,
            cameraZ
        );

        // Smooth camera movement
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );
    }
}
