using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Individual tunnel segment with Bezier curve walls
/// Uses EdgeCollider2D that follows Bezier curve shape for accurate collision
/// Provides both physical colliders (blocking) and triggers (damage detection)
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
    [SerializeField] private MeshFilter cylinderGradientMesh;  // Cylinder floor gradient visualization
    [SerializeField] private int resolution = 200;  // High resolution for smooth visual curves
    [SerializeField] private Color centerColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);  // Dark center (cylinder bottom)
    [SerializeField] private Color edgeColor = new Color(0.4f, 0.4f, 0.5f, 0.3f);  // Light edges (near walls)
    [SerializeField] private int gradientResolution = 50;  // Resolution for gradient mesh

    [Header("Collision")]
    [SerializeField] private int collisionResolution = 50;  // Lower than visual but accurate enough

    // Ensure colliders are created only once
    private bool collidersCreated = false;

    // Cache segment height for fast calculations
    private float segmentHeight;
    private EdgeCollider2D leftWallCollider;   // Physical collider - blocks submarine
    private EdgeCollider2D rightWallCollider;  // Physical collider - blocks submarine
    private EdgeCollider2D leftWallTrigger;    // Trigger collider - detects damage
    private EdgeCollider2D rightWallTrigger;   // Trigger collider - detects damage

    void Update()
    {
        // Movement is controlled by TunnelGenerator
    }

    /// <summary>
    /// Build visual line renderers and physical colliders
    /// </summary>
    public void Build()
    {
        BuildWall(leftWall, leftStart, leftControl1, leftControl2, leftEnd);
        BuildWall(rightWall, rightStart, rightControl1, rightControl2, rightEnd);

        // Build cylinder gradient visualization
        if (cylinderGradientMesh != null)
        {
            BuildCylinderGradient();
        }

        // Cache segment height for later calculations
        segmentHeight = Mathf.Max(leftEnd.y, rightEnd.y);

        // Create EdgeCollider2D that follows Bezier curve shape
        // Only create once to avoid duplicates
        if (!collidersCreated)
        {
            CreateEdgeColliders();
            collidersCreated = true;
        }
    }

    /// <summary>
    /// Create EdgeCollider2D for left and right walls
    /// EdgeCollider2D is perfect for curved lines
    /// </summary>
    private void CreateEdgeColliders()
    {
        Debug.Log($"[TunnelSegment] Creating colliders for segment at Y={transform.position.y}");

        // Generate points for left wall
        Vector2[] leftPoints = GenerateBezierPoints(
            leftStart, leftControl1, leftControl2, leftEnd,
            collisionResolution
        );

        // Generate points for right wall
        Vector2[] rightPoints = GenerateBezierPoints(
            rightStart, rightControl1, rightControl2, rightEnd,
            collisionResolution
        );

        Debug.Log($"[TunnelSegment] Generated {leftPoints.Length} points for each wall");

        // Remove any existing colliders
        EdgeCollider2D[] existingColliders = GetComponents<EdgeCollider2D>();
        foreach (EdgeCollider2D ec in existingColliders)
        {
            DestroyImmediate(ec);
        }

        // EdgeCollider2D requires Rigidbody2D component
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            rb.useFullKinematicContacts = true;  // Enable for OnCollisionEnter2D
            Debug.Log("[TunnelSegment] Added Rigidbody2D with Full Kinematic Contacts");
        }

        // ════════════════════════════════════════════════════════════
        // PHYSICAL COLLIDERS (isTrigger = FALSE)
        // Submarine CANNOT pass through walls
        // ════════════════════════════════════════════════════════════
        leftWallCollider = gameObject.AddComponent<EdgeCollider2D>();
        leftWallCollider.points = leftPoints;
        leftWallCollider.isTrigger = false;  // Physical blocking collider
        Debug.Log($"[TunnelSegment] Created LEFT physical collider with {leftPoints.Length} points");

        rightWallCollider = gameObject.AddComponent<EdgeCollider2D>();
        rightWallCollider.points = rightPoints;
        rightWallCollider.isTrigger = false;  // Physical blocking collider
        Debug.Log($"[TunnelSegment] Created RIGHT physical collider with {rightPoints.Length} points");

        // ════════════════════════════════════════════════════════════
        // TRIGGER COLLIDERS (isTrigger = TRUE)
        // For collision detection and damage application
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
    /// Generate points along a cubic Bezier curve
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
    /// Calculate point on cubic Bezier curve
    /// </summary>
    private Vector2 CubicBezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        float u = 1 - t;
        return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
    }

    /// <summary>
    /// Build visual line renderer for one wall
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
    /// Build cylinder gradient showing tunnel depth
    /// Creates gradient mesh: dark in center (bottom), light at edges (walls)
    /// </summary>
    private void BuildCylinderGradient()
    {
        // Calculate segment height
        float height = Mathf.Max(leftEnd.y, rightEnd.y);

        // Create mesh data
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();

        int heightSteps = Mathf.Max(2, Mathf.CeilToInt(height / 0.1f));  // Vertical resolution
        int widthSteps = gradientResolution;  // Horizontal resolution for gradient

        // Build mesh grid
        for (int y = 0; y < heightSteps; y++)
        {
            float tY = y / (float)(heightSteps - 1);
            float posY = height * tY;

            // Get wall positions at this Y
            Vector2 leftPoint = CubicBezier(leftStart, leftControl1, leftControl2, leftEnd, tY);
            Vector2 rightPoint = CubicBezier(rightStart, rightControl1, rightControl2, rightEnd, tY);
            float width = rightPoint.x - leftPoint.x;

            // Create vertices across width with gradient
            for (int x = 0; x < widthSteps; x++)
            {
                float tX = x / (float)(widthSteps - 1);
                float posX = Mathf.Lerp(leftPoint.x, rightPoint.x, tX);

                vertices.Add(new Vector3(posX, posY, 0));

                // Gradient: dark at center, light at edges (cylinder effect)
                float distFromCenter = Mathf.Abs(tX - 0.5f) * 2f;  // 0 at center, 1 at edges
                Color vertexColor = Color.Lerp(centerColor, edgeColor, distFromCenter);
                colors.Add(vertexColor);
            }
        }

        // Build triangles
        for (int y = 0; y < heightSteps - 1; y++)
        {
            for (int x = 0; x < widthSteps - 1; x++)
            {
                int i = y * widthSteps + x;

                // Two triangles per quad
                triangles.Add(i);
                triangles.Add(i + widthSteps);
                triangles.Add(i + 1);

                triangles.Add(i + 1);
                triangles.Add(i + widthSteps);
                triangles.Add(i + widthSteps + 1);
            }
        }

        // Create and assign mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.colors = colors.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        cylinderGradientMesh.mesh = mesh;
    }

    /// <summary>
    /// Get actual wall positions at given Y height
    /// Takes Bezier curve shape into account for precise calculations
    /// Used for accurate obstacle positioning
    /// </summary>
    public bool GetWallPositionsAtY(float worldY, out float leftX, out float rightX)
    {
        // Convert world Y to local segment coordinate
        float localY = worldY - transform.position.y;

        // Check if Y is within this segment's bounds
        if (localY < 0 || localY > segmentHeight)
        {
            leftX = rightX = 0;
            return false;
        }

        // Normalize local Y to Bezier parameter t (0-1)
        float t = localY / segmentHeight;
        t = Mathf.Clamp01(t);

        // Calculate points on Bezier curves in local coordinates
        Vector2 leftPoint = CubicBezier(leftStart, leftControl1, leftControl2, leftEnd, t);
        Vector2 rightPoint = CubicBezier(rightStart, rightControl1, rightControl2, rightEnd, t);

        // Convert back to world coordinates
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
