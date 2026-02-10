using UnityEngine;

public static class RuntimeGameplayMetrics
{
    public const float SafeMarginWidthMultiplier = 1.5f;
    public const float MinTunnelWidthDiagonalMultiplier = 2f;
    public const float MaxTunnelScreenFraction = 0.7f;
    public const float MinObstacleSpacingScrollSpeedMultiplier = 1.5f;
    public const float PickupSizeMultiplier = 1.5f;

    public static float SubmarineWidth { get; private set; }
    public static float SubmarineLength { get; private set; }
    public static float SubmarineDiagonal { get; private set; }
    public static float SafeMargin { get; private set; }
    public static float MinTunnelWidth { get; private set; }
    public static float MaxTunnelWidth { get; private set; }
    public static float CurrentTunnelWidth { get; private set; }
    public static float ScreenSafeWidthWorld { get; private set; }
    public static float ScrollSpeed { get; private set; }
    public static float MinObstacleSpacingY { get; private set; }
    public static float TargetPickupMaxSize { get; private set; }
    public static bool Initialized { get; private set; }
    public static bool EnableDebugLogs { get; set; } = true;

    public static void Initialize(Camera mainCamera, GameObject submarinePrefab, float scrollSpeed)
    {
        if (Initialized) return;

        UpdateScrollSpeed(scrollSpeed);
        UpdateScreenSafeWidth(CalcSafeWidthWorld(mainCamera));

        GameObject submarineScene = GameObject.FindGameObjectWithTag("Player");
        GameObject submarineSource = submarineScene != null ? submarineScene : submarinePrefab;
        if (submarineSource != null && TryGetColliderSize(submarineSource, out float width, out float length))
        {
            UpdateSubmarineSize(width, length);
            if (EnableDebugLogs)
                Debug.Log($"[RuntimeGameplayMetrics] Submarine collider size: width={width:F3}, length={length:F3}");
        }
        else if (EnableDebugLogs)
        {
            Debug.LogWarning("[RuntimeGameplayMetrics] Submarine collider not found or invalid.");
        }

        Initialized = true;
        if (EnableDebugLogs)
        {
            Debug.Log($"[RuntimeGameplayMetrics] Init: SafeMargin={SafeMargin:F3}, MinTunnelWidth={MinTunnelWidth:F3}, MaxTunnelWidth={MaxTunnelWidth:F3}, TargetPickupMaxSize={TargetPickupMaxSize:F3}, ScrollSpeed={ScrollSpeed:F3}");
        }
    }

    public static void UpdateSubmarineSize(float width, float length)
    {
        if (width <= 0f || length <= 0f) return;

        SubmarineWidth = width;
        SubmarineLength = length;
        SubmarineDiagonal = Mathf.Sqrt(width * width + length * length);
        SafeMargin = SubmarineWidth * SafeMarginWidthMultiplier;
        MinTunnelWidth = SubmarineDiagonal * MinTunnelWidthDiagonalMultiplier;
        float submarineMaxSize = Mathf.Max(SubmarineWidth, SubmarineLength);
        TargetPickupMaxSize = submarineMaxSize * PickupSizeMultiplier;
    }

    public static void UpdateScreenSafeWidth(float safeWidthWorld)
    {
        if (safeWidthWorld <= 0f) return;

        ScreenSafeWidthWorld = safeWidthWorld;
        MaxTunnelWidth = ScreenSafeWidthWorld * MaxTunnelScreenFraction;
    }

    public static void UpdateScrollSpeed(float speed)
    {
        if (speed <= 0f) return;

        ScrollSpeed = speed;
        MinObstacleSpacingY = speed * MinObstacleSpacingScrollSpeedMultiplier;
    }

    public static void UpdateCurrentTunnelWidth(float width)
    {
        if (width <= 0f) return;
        CurrentTunnelWidth = width;
        if (EnableDebugLogs)
            Debug.Log($"[RuntimeGameplayMetrics] CurrentTunnelWidth={CurrentTunnelWidth:F3}");
    }

    private static float CalcSafeWidthWorld(Camera mainCamera)
    {
        if (mainCamera == null) return 0f;

        float screenWidthWorld = mainCamera.orthographicSize * 2f * mainCamera.aspect;
        float pixelWidth = mainCamera.pixelWidth;
        float safeAreaRatio = pixelWidth > 0f ? Screen.safeArea.width / pixelWidth : 1f;
        return screenWidthWorld * safeAreaRatio;
    }

    public static bool TryGetColliderSize(GameObject root, out float width, out float length)
    {
        if (TryGetMinMaxXYInRoot(root, out float minX, out float maxX, out float minY, out float maxY))
        {
            width = maxX - minX;
            length = maxY - minY;
            return true;
        }

        width = 0f;
        length = 0f;
        return false;
    }

    private static bool TryGetMinMaxXYInRoot(GameObject root, out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;
        minY = float.MaxValue;
        maxY = float.MinValue;

        bool found = false;

        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders)
        {
            if (AccumulateColliderMinMaxXY(root.transform, col, ref minX, ref maxX, ref minY, ref maxY))
            {
                found = true;
            }
        }

        if (found) return true;
        return false;
    }

    private static bool AccumulateColliderMinMaxXY(Transform root, Collider2D col, ref float minX, ref float maxX, ref float minY, ref float maxY)
    {
        switch (col)
        {
            case CircleCollider2D circle:
                return AccumulatePointsXY(root, circle.transform, new Vector2[]
                {
                    circle.offset + new Vector2(-circle.radius, 0f),
                    circle.offset + new Vector2(circle.radius, 0f),
                    circle.offset + new Vector2(0f, -circle.radius),
                    circle.offset + new Vector2(0f, circle.radius)
                }, ref minX, ref maxX, ref minY, ref maxY);

            case BoxCollider2D box:
                Vector2 half = box.size * 0.5f;
                return AccumulatePointsXY(root, box.transform, new Vector2[]
                {
                    box.offset + new Vector2(-half.x, -half.y),
                    box.offset + new Vector2(-half.x,  half.y),
                    box.offset + new Vector2( half.x, -half.y),
                    box.offset + new Vector2( half.x,  half.y)
                }, ref minX, ref maxX, ref minY, ref maxY);

            case PolygonCollider2D poly:
                if (poly.points == null || poly.points.Length == 0)
                    return false;

                Vector2[] pts = new Vector2[poly.points.Length];
                for (int i = 0; i < poly.points.Length; i++)
                    pts[i] = poly.points[i] + poly.offset;

                return AccumulatePointsXY(root, poly.transform, pts, ref minX, ref maxX, ref minY, ref maxY);
        }

        return false;
    }

    private static bool AccumulatePointsXY(Transform root, Transform child, Vector2[] localPoints, ref float minX, ref float maxX, ref float minY, ref float maxY)
    {
        bool any = false;
        foreach (Vector2 p in localPoints)
        {
            Vector3 world = child.TransformPoint(p);
            Vector3 rootLocal = root.InverseTransformPoint(world);
            minX = Mathf.Min(minX, rootLocal.x);
            maxX = Mathf.Max(maxX, rootLocal.x);
            minY = Mathf.Min(minY, rootLocal.y);
            maxY = Mathf.Max(maxY, rootLocal.y);
            any = true;
        }

        return any;
    }

    public static bool TryGetUniformScaleForPickup(GameObject pickupPrefab, out float scale)
    {
        scale = 1f;
        if (TargetPickupMaxSize <= 0f) return false;

        if (!TryGetColliderSize(pickupPrefab, out float width, out float length))
            return false;

        float pickupMaxSize = Mathf.Max(width, length);
        if (pickupMaxSize <= 0f) return false;

        scale = TargetPickupMaxSize / pickupMaxSize;
        return true;
    }
}
