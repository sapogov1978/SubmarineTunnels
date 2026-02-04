using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Управляет экраном Game Over
/// Показывает финальную статистику и кнопки управления
/// День 6 FIX: Останавливает камеру shake перед паузой
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI finalDistanceText;
    [SerializeField] private TextMeshProUGUI bestDistanceText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool animateOnShow = true;

    [Header("Audio")]
    [SerializeField] private AudioClip gameOverSound;

    private CanvasGroup canvasGroup;
    private bool isShowing;
    private Camera mainCamera;

    void Awake()
    {
        canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null && animateOnShow)
        {
            canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        mainCamera = Camera.main;
    }

    void Start()
    {
        Hide();

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
        if (OxygenManager.Instance != null)
        {
            OxygenManager.Instance.OnOxygenDepleted -= OnPlayerDied;
        }

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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndGame();
        }

        float finalDistance = GetFinalDistance();
        float bestDistance = GetBestDistance();
        Show(finalDistance, bestDistance);
    }

    /// <summary>
    /// Показывает экран Game Over
    /// ДЕНЬ 6 FIX: Останавливает camera shake перед паузой
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

        // ════════════════════════════════════════════════════════════
        // ДЕНЬ 6 FIX: КРИТИЧНО!
        // Останавливаем camera shake ПЕРЕД паузой
        // Иначе камера будет дрожать как судорога
        // ════════════════════════════════════════════════════════════
        if (mainCamera != null)
        {
            // Прерываем все корутины камеры (включая CameraShake)
            StopAllCameraCoroutines();
        }

        // Анимация появления (используем unscaledDeltaTime чтобы работало при pause)
        if (animateOnShow && canvasGroup != null)
        {
            StartCoroutine(FadeInRoutine());
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // Теперь можно ставить паузу - камера уже стабильна
        Time.timeScale = 0f;

        Debug.Log($"[GameOverUI] Game Over! Distance: {distance:F0}m, Best: {bestDistance:F0}m");
    }

    /// <summary>
    /// Останавливает все корутины связанные с камерой
    /// Это требуется чтобы остановить camera shake перед паузой
    /// </summary>
    private void StopAllCameraCoroutines()
    {
        if (mainCamera == null) return;
        
        // Находим все компоненты на камере
        MonoBehaviour[] behaviours = mainCamera.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            behaviour.StopAllCoroutines();
        }
        
        // Возвращаем камеру в исходное положение
        mainCamera.transform.position = new Vector3(
            mainCamera.transform.position.x,
            0f, // Y = 0 (так как камера привязана к батискафу)
            -10f // Z = -10 (стандартное Z для 2D)
        );
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
            // ВАЖНО: используем unscaledDeltaTime чтобы работало при Time.timeScale = 0
            elapsed += Time.unscaledDeltaTime;
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

        // Восстанавливаем время перед рестартом
        Time.timeScale = 1f;

        // Перезагружаем сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Обработчик кнопки Main Menu
    /// </summary>
    private void OnMainMenuClicked()
    {
        Debug.Log("[GameOverUI] Main Menu clicked");

        // Восстанавливаем время
        Time.timeScale = 1f;

        // SceneManager.LoadScene("MainMenu");
        
        // Пока просто рестарт
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Получить финальную дистанцию
    /// </summary>
    private float GetFinalDistance()
    {
        // TODO День 12: Интегрировать с ProgressUI
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
