using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Управляет экраном Game Over
/// Показывает финальную статистику и кнопки управления
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel; // главная панель
    [SerializeField] private TextMeshProUGUI titleText; // "GAME OVER"
    [SerializeField] private TextMeshProUGUI finalDistanceText; // "Distance: 1523m"
    [SerializeField] private TextMeshProUGUI bestDistanceText; // "Best: 2341m"
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton; // опционально

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool animateOnShow = true;

    [Header("Audio")]
    [SerializeField] private AudioClip gameOverSound;

    private CanvasGroup canvasGroup;
    private bool isShowing;

    void Awake()
    {
        // Получаем или добавляем CanvasGroup для fade эффектов
        canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null && animateOnShow)
        {
            canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }

        // Настраиваем кнопки
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    void Start()
    {
        // Скрываем панель при старте
        Hide();

        // Подписываемся на событие смерти
        if (OxygenManager.Instance != null)
        {
            OxygenManager.Instance.OnOxygenDepleted += OnPlayerDied;
        }
        else
        {
            Debug.LogError("[GameOverUI] OxygenManager not found!");
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (OxygenManager.Instance != null)
        {
            OxygenManager.Instance.OnOxygenDepleted -= OnPlayerDied;
        }

        // Очищаем слушатели кнопок
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }
    }

    /// <summary>
    /// Вызывается когда игрок умирает
    /// </summary>
    private void OnPlayerDied()
    {
        // Получаем финальную статистику (пока заглушка, будет реализовано в День 12)
        float finalDistance = GetFinalDistance();
        float bestDistance = GetBestDistance();

        Show(finalDistance, bestDistance);
    }

    /// <summary>
    /// Показывает экран Game Over с анимацией
    /// </summary>
    public void Show(float distance = 0f, float bestDistance = 0f)
    {
        if (isShowing) return;

        isShowing = true;
        gameOverPanel.SetActive(true);

        // Обновляем текст статистики
        if (finalDistanceText != null)
        {
            finalDistanceText.text = $"Distance: {Mathf.RoundToInt(distance)}m";
        }

        if (bestDistanceText != null)
        {
            // Проверяем новый рекорд
            bool isNewRecord = distance > bestDistance;
            
            if (isNewRecord && distance > 0)
            {
                bestDistanceText.text = $"<color=yellow>NEW BEST: {Mathf.RoundToInt(distance)}m!</color>";
                SaveBestDistance(distance);
            }
            else
            {
                bestDistanceText.text = $"Best: {Mathf.RoundToInt(bestDistance)}m";
            }
        }

        // Воспроизводим звук
        if (gameOverSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(gameOverSound);
        }

        // Анимация появления
        if (animateOnShow && canvasGroup != null)
        {
            StartCoroutine(FadeInRoutine());
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // Останавливаем игру
        Time.timeScale = 0f;

        Debug.Log($"[GameOverUI] Game Over! Distance: {distance:F0}m, Best: {bestDistance:F0}m");
    }

    /// <summary>
    /// Скрывает экран Game Over
    /// </summary>
    public void Hide()
    {
        isShowing = false;
        gameOverPanel.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// Анимация плавного появления
    /// </summary>
    private System.Collections.IEnumerator FadeInRoutine()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime; // используем unscaled для работы при Time.timeScale = 0
            float t = fadeInCurve.Evaluate(elapsed / fadeInDuration);
            canvasGroup.alpha = t;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Обработчик кнопки Restart
    /// </summary>
    private void OnRestartClicked()
    {
        Debug.Log("[GameOverUI] Restart clicked");

        // Восстанавливаем время
        Time.timeScale = 1f;

        // Перезагружаем сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Обработчик кнопки Main Menu (опционально)
    /// </summary>
    private void OnMainMenuClicked()
    {
        Debug.Log("[GameOverUI] Main Menu clicked");

        // Восстанавливаем время
        Time.timeScale = 1f;

        // Переход в главное меню (если есть отдельная сцена)
        // SceneManager.LoadScene("MainMenu");
        
        // Пока просто рестарт
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Получить финальную дистанцию (заглушка для Дня 12)
    /// </summary>
    private float GetFinalDistance()
    {
        // TODO День 12: Интегрировать с ProgressUI
        // Пока возвращаем рандомное значение для демонстрации
        return Random.Range(100f, 2000f);
    }

    /// <summary>
    /// Получить лучшую дистанцию из PlayerPrefs
    /// </summary>
    private float GetBestDistance()
    {
        return PlayerPrefs.GetFloat("BestDistance", 0f);
    }

    /// <summary>
    /// Сохранить лучшую дистанцию в PlayerPrefs
    /// </summary>
    private void SaveBestDistance(float distance)
    {
        float currentBest = GetBestDistance();
        if (distance > currentBest)
        {
            PlayerPrefs.SetFloat("BestDistance", distance);
            PlayerPrefs.Save();
            Debug.Log($"[GameOverUI] New best distance saved: {distance:F0}m");
        }
    }

    // DEBUG методы для тестирования
    #if UNITY_EDITOR
    [ContextMenu("Test: Show Game Over")]
    private void TestShowGameOver()
    {
        Show(1234f, 2000f);
    }

    [ContextMenu("Test: Hide Game Over")]
    private void TestHideGameOver()
    {
        Hide();
        Time.timeScale = 1f;
    }
    #endif
}