using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic Object Pool для переиспользования объектов
/// Уменьшает нагрузку на GC и улучшает производительность
/// </summary>
/// <typeparam name="T">Тип компонента (должен быть MonoBehaviour)</typeparam>
public class ObjectPool<T> where T : MonoBehaviour
{
    private T prefab;
    private Queue<T> pool = new Queue<T>();
    private Transform parent;
    private int initialSize;

    /// <summary>
    /// Конструктор пула
    /// </summary>
    /// <param name="prefab">Префаб для создания объектов</param>
    /// <param name="initialSize">Начальный размер пула</param>
    /// <param name="parent">Родительский Transform (для организации иерархии)</param>
    public ObjectPool(T prefab, int initialSize = 10, Transform parent = null)
    {
        this.prefab = prefab;
        this.initialSize = initialSize;
        this.parent = parent;

        // Предварительное создание объектов
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    /// <summary>
    /// Получить объект из пула
    /// </summary>
    public T Get()
    {
        T obj;

        // Если в пуле есть объекты - берём оттуда
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        // Иначе создаём новый
        else
        {
            obj = CreateNewObject();
        }

        obj.gameObject.SetActive(true);
        return obj;
    }

    /// <summary>
    /// Получить объект с заданной позицией и вращением
    /// </summary>
    public T Get(Vector3 position, Quaternion rotation)
    {
        T obj = Get();
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        return obj;
    }

    /// <summary>
    /// Вернуть объект в пул
    /// </summary>
    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        
        // Сброс состояния если объект - Obstacle
        if (obj is Obstacle obstacle)
        {
            obstacle.ResetObstacle();
        }
        
        pool.Enqueue(obj);
    }

    /// <summary>
    /// Создать новый объект
    /// </summary>
    private T CreateNewObject()
    {
        T obj = Object.Instantiate(prefab, parent);
        obj.gameObject.SetActive(false);
        return obj;
    }

    /// <summary>
    /// Получить текущий размер пула
    /// </summary>
    public int GetPoolSize()
    {
        return pool.Count;
    }

    /// <summary>
    /// Очистить весь пул
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
/// Простой компонент для автоматического возврата объекта в пул через заданное время
/// Используется для VFX и временных эффектов
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
            // Объект деактивируется и может быть возвращён в пул
            gameObject.SetActive(false);
        }
    }
}