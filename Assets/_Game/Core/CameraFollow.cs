using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    
    [Header("Camera Position")]
    [SerializeField] private float cameraY = 0f;  // Camera stays at Y=0 (center)
    [SerializeField] private float cameraZ = -10f; // Standard 2D camera Z

    void Start()
    {
        // FIXED: Set camera position immediately on Start
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

        // FIXED: Only follow X position, Y and Z are fixed
        Vector3 targetPos = new Vector3(
            target.position.x,
            cameraY,   // Camera Y never changes
            cameraZ    // Camera Z never changes
        );

        // Smooth follow on X axis only
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );
    }
}