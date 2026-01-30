using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Главный менеджер игры
/// Управляет состояниями игры, настройками и глобальными параметрами
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Header("Settings")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool preventSleep = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // События состояний игры
    public event System.Action OnGameStart;
    public event System.Action OnGamePause;
    public event System.Action OnGameResume;
    public event System.Action OnGameOver;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // опционально, если нужна персистентность

        // Применяем настройки
        ApplySettings();
    }

    void Start()
    {
        StartGame();
    }

    /// <summary>
    /// Применяет глобальные настройки приложения
    /// </summary>
    private void ApplySettings()
    {
        // Устанавливаем целевой FPS
        Application.targetFrameRate = targetFrameRate;

        // Предотвращаем засыпание экрана
        if (preventSleep)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[GameManager] Settings applied: FPS={targetFrameRate}, PreventSleep={preventSleep}");
        }
    }

    /// <summary>
    /// Запускает новую игру
    /// </summary>
    public void StartGame()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;

        // Сбрасываем кислород
        if (OxygenManager.Instance != null)
        {
            OxygenManager.Instance.ResetOxygen();
        }

        OnGameStart?.Invoke();

        if (showDebugLogs)
            Debug.Log("[GameManager] Game Started");
    }

    /// <summary>
    /// Ставит игру на паузу
    /// </summary>
    public void PauseGame()
    {
        if (currentState == GameState.GameOver) return;

        currentState = GameState.Paused;
        Time.timeScale = 0f;

        // Останавливаем уменьшение кислорода
        if (OxygenManager.Instance != null)
        {
            OxygenManager.Instance.StopDepletion();
        }

        OnGamePause?.Invoke();

        if (showDebugLogs)
            Debug.Log("[GameManager] Game Paused");
    }

    /// <summary>
    /// Возобновляет игру после паузы
    /// </summary>
    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;

        // Возобновляем уменьшение кислорода
        if (OxygenManager.Instance != null)
        {
            OxygenManager.Instance.ResumeDepletion();
        }

        OnGameResume?.Invoke();

        if (showDebugLogs)
            Debug.Log("[GameManager] Game Resumed");
    }

    /// <summary>
    /// Завершает игру (Game Over)
    /// </summary>
    public void EndGame()
    {
        if (currentState == GameState.GameOver) return;

        currentState = GameState.GameOver;
        // Time.timeScale будет установлен в 0 в GameOverUI

        OnGameOver?.Invoke();

        if (showDebugLogs)
            Debug.Log("[GameManager] Game Over");
    }

    /// <summary>
    /// Перезапускает игру
    /// </summary>
    public void RestartGame()
    {
        if (showDebugLogs)
            Debug.Log("[GameManager] Restarting game...");

        // Восстанавливаем время
        Time.timeScale = 1f;

        // Перезагружаем сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Получить текущее состояние игры
    /// </summary>
    public GameState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Проверка, идёт ли игра
    /// </summary>
    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }

    /// <summary>
    /// Проверка, на паузе ли игра
    /// </summary>
    public bool IsPaused()
    {
        return currentState == GameState.Paused;
    }

    /// <summary>
    /// Проверка, закончена ли игра
    /// </summary>
    public bool IsGameOver()
    {
        return currentState == GameState.GameOver;
    }

    // DEBUG методы для тестирования состояний
    #if UNITY_EDITOR
    void Update()
    {
        // Горячие клавиши для отладки (только в редакторе)
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (currentState == GameState.Playing)
                PauseGame();
            else if (currentState == GameState.Paused)
                ResumeGame();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            // Принудительный Game Over для тестирования
            if (OxygenManager.Instance != null)
            {
                OxygenManager.Instance.RemoveOxygen(100f);
            }
        }
    }

    [ContextMenu("Debug: Pause Game")]
    private void DebugPauseGame()
    {
        PauseGame();
    }

    [ContextMenu("Debug: Resume Game")]
    private void DebugResumeGame()
    {
        ResumeGame();
    }

    [ContextMenu("Debug: Restart Game")]
    private void DebugRestartGame()
    {
        RestartGame();
    }
    #endif
}

/// <summary>
/// Возможные состояния игры
/// </summary>
public enum GameState
{
    Playing,    // Игра идёт
    Paused,     // Игра на паузе
    GameOver    // Игра окончена
}