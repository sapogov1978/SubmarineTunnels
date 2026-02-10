using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool for reusing objects
/// Reduces GC pressure and improves performance
/// Supports Obstacles, OxygenPickup, and other MonoBehaviour types
/// </summary>
/// <typeparam name="T">Component type (must be MonoBehaviour)</typeparam>
public class ObjectPool<T> where T : MonoBehaviour
{
    private T prefab;
    private Queue<T> pool = new Queue<T>();
    private Transform parent;
    private int initialSize;

    /// <summary>
    /// Object pool constructor
    /// </summary>
    /// <param name="prefab">Prefab to create objects from</param>
    /// <param name="initialSize">Initial pool size</param>
    /// <param name="parent">Parent transform for hierarchy organization</param>
    public ObjectPool(T prefab, int initialSize = 10, Transform parent = null)
    {
        this.prefab = prefab;
        this.initialSize = initialSize;
        this.parent = parent;

        // Pre-create objects
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    /// <summary>
    /// Get object from pool
    /// </summary>
    public T Get()
    {
        T obj;

        // If pool has objects - get from pool
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        // Otherwise create new object
        else
        {
            obj = CreateNewObject();
        }

        obj.gameObject.SetActive(true);
        return obj;
    }

    /// <summary>
    /// Get object with specified position and rotation
    /// </summary>
    public T Get(Vector3 position, Quaternion rotation)
    {
        T obj = Get();
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        return obj;
    }

    /// <summary>
    /// Return object to pool
    /// </summary>
    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);

        // Reset state if object is Obstacle
        if (obj is Obstacle obstacle)
        {
            obstacle.ResetObstacle();
        }
        // Reset state if object is OxygenPickup
        else if (obj is OxygenPickup pickup)
        {
            pickup.ResetPickup();
        }

        pool.Enqueue(obj);
    }

    /// <summary>
    /// Create new object instance
    /// </summary>
    private T CreateNewObject()
    {
        T obj = Object.Instantiate(prefab, parent);
        obj.gameObject.SetActive(false);
        return obj;
    }

    /// <summary>
    /// Get current pool size
    /// </summary>
    public int GetPoolSize()
    {
        return pool.Count;
    }

    /// <summary>
    /// Clear entire pool
    /// </summary>
    public void Clear()
    {
        while (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            if (obj != null)
            {
                Object.Destroy(obj.gameObject);
            }
        }
    }
}

/// <summary>
/// Simple component for automatic pool return after specified time
/// Used for VFX and temporary effects
/// </summary>
public class PooledObject : MonoBehaviour
{
    [SerializeField] private float lifetimeSeconds = 2f;

    private float timer;

    void OnEnable()
    {
        timer = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= lifetimeSeconds)
        {
            // Object deactivates and can be returned to pool
            gameObject.SetActive(false);
        }
    }
}