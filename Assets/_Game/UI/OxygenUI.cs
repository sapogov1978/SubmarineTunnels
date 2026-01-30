using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Отображает уровень кислорода в UI
/// Использует Image.fillAmount для плавной анимации
/// Цветовая индикация: зелёный → жёлтый → красный
/// </summary>
public class OxygenUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image oxygenFillImage; // заполняемое изображение
    [SerializeField] private TextMeshProUGUI oxygenText; // текст "87%" (опционально)
    [SerializeField] private Image backgroundImage; // фон (опционально)

    [Header("Colors")]
    [SerializeField] private Color highOxygenColor = new Color(0.2f, 1f, 0.3f); // зелёный
    [SerializeField] private Color mediumOxygenColor = new Color(1f, 0.9f, 0.2f); // жёлтый
    [SerializeField] private Color lowOxygenColor = new Color(1f, 0.2f, 0.2f); // красный

    [Header("Thresholds")]
    [SerializeField] private float mediumThreshold = 50f; // ниже 50% = жёлтый
    [SerializeField] private float lowThreshold = 25f; // ниже 25% = красный

    [Header("Animation")]
    [SerializeField] private float smoothSpeed = 5f; // скорость плавного изменения
    [SerializeField] private bool animateFill = true;

    [Header("Warning Effects")]
    [SerializeField] private bool pulseOnLowOxygen = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScale = 1.1f;

    private float targetFillAmount;
    private float currentFillAmount;
    private bool isLowOxygen;
    private Vector3 originalScale;

    void Start()
    {
        // Сохраняем оригинальный масштаб для пульсации
        if (oxygenFillImage != null)
        {
            originalScale = oxygenFillImage.transform.localScale;
        }

        // Подписываемся на события OxygenManager
        if (OxygenManager.Instance != null)
        {
            OxygenManager.Instance.OnOxygenChanged += UpdateOxygenDisplay;
            
            // Устанавливаем начальное значение
            UpdateOxygenDisplay(OxygenManager.Instance.GetOxygenPercentage());
        }
        else
        {
            Debug.LogError("[OxygenUI] OxygenManager not found! Make sure it exists in the scene.");
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий при уничтожении
        if (OxygenManager.Instance != null)
        {
            OxygenManager.Instance.OnOxygenChanged -= UpdateOxygenDisplay;
        }
    }

    void Update()
    {
        // Плавная анимация заполнения
        if (animateFill && oxygenFillImage != null)
        {
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, smoothSpeed * Time.deltaTime);
            oxygenFillImage.fillAmount = currentFillAmount;
        }

        // Пульсация при низком кислороде
        if (pulseOnLowOxygen && isLowOxygen && oxygenFillImage != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * (pulseScale - 1f);
            oxygenFillImage.transform.localScale = originalScale * pulse;
        }
        else if (oxygenFillImage != null)
        {
            oxygenFillImage.transform.localScale = originalScale;
        }
    }

    /// <summary>
    /// Обновляет отображение кислорода
    /// Вызывается через событие OnOxygenChanged
    /// </summary>
    /// <param name="percentage">Процент кислорода (0-100)</param>
    private void UpdateOxygenDisplay(float percentage)
    {
        // Нормализуем для fillAmount (0-1)
        float normalized = percentage / 100f;
        targetFillAmount = normalized;

        // Если анимация выключена - сразу устанавливаем значение
        if (!animateFill && oxygenFillImage != null)
        {
            currentFillAmount = targetFillAmount;
            oxygenFillImage.fillAmount = currentFillAmount;
        }

        // Обновляем текст (если есть)
        if (oxygenText != null)
        {
            oxygenText.text = $"{Mathf.RoundToInt(percentage)}%";
        }

        // Обновляем цвет в зависимости от уровня
        UpdateColor(percentage);

        // Проверяем порог для пульсации
        isLowOxygen = percentage <= lowThreshold;
    }

    /// <summary>
    /// Обновляет цвет индикатора в зависимости от уровня кислорода
    /// </summary>
    private void UpdateColor(float percentage)
    {
        if (oxygenFillImage == null) return;

        Color targetColor;

        if (percentage <= lowThreshold)
        {
            // Красный (критично)
            targetColor = lowOxygenColor;
        }
        else if (percentage <= mediumThreshold)
        {
            // Интерполяция между жёлтым и красным
            float t = (percentage - lowThreshold) / (mediumThreshold - lowThreshold);
            targetColor = Color.Lerp(lowOxygenColor, mediumOxygenColor, t);
        }
        else
        {
            // Интерполяция между зелёным и жёлтым
            float t = (percentage - mediumThreshold) / (100f - mediumThreshold);
            targetColor = Color.Lerp(mediumOxygenColor, highOxygenColor, t);
        }

        oxygenFillImage.color = targetColor;

        // Опционально: окрашиваем и текст
        if (oxygenText != null)
        {
            oxygenText.color = targetColor;
        }
    }

    /// <summary>
    /// Публичный метод для ручной установки кислорода (для тестирования)
    /// </summary>
    public void SetOxygen(float percentage)
    {
        UpdateOxygenDisplay(percentage);
    }

    // DEBUG: Визуализация цветовых зон в Inspector
    #if UNITY_EDITOR
    void OnValidate()
    {
        // Проверяем что пороги логичны
        if (lowThreshold >= mediumThreshold)
        {
            Debug.LogWarning("[OxygenUI] lowThreshold should be less than mediumThreshold!");
        }
    }
    #endif
}