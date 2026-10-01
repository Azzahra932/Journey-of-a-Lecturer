using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntegritasManager : MonoBehaviour
{
    public static IntegritasManager Instance;

    [Header("Pengaturan Integritas")]
    public float maxIntegritas = 100f;
    private float currentIntegritas;

    [Header("Referensi UI (Slider)")]
    public Slider integritasSlider;
    public TMP_Text integritasText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentIntegritas = maxIntegritas;

        if (integritasText != null)
        {
            integritasText.text = Mathf.RoundToInt(currentIntegritas) + "%";
        }

        UpdateUI();
    }

    public void ReduceIntegritas(float amount)
    {
        currentIntegritas -= amount;
        currentIntegritas = Mathf.Clamp(currentIntegritas, 0f, maxIntegritas);

        Debug.Log($"[IntegritasManager] Integritas berkurang {amount}%. Sisa: {currentIntegritas}%");
        UpdateUI();
    }

    void UpdateUI()
    {
        if (integritasSlider != null)
        {
            integritasSlider.maxValue = maxIntegritas;
            integritasSlider.value = currentIntegritas;

            // PENGAMAN TAMBAHAN: Mencegah komponen Fill meregang/panjang sendiri saat nilainya berubah
            RectTransform fillRect = integritasSlider.fillRect;
            if (fillRect != null)
            {
                fillRect.anchorMin = new Vector2(0, 0);
                fillRect.anchorMax = new Vector2(currentIntegritas / maxIntegritas, 1);
                fillRect.offsetMin = new Vector2(0, 0); // Left, Bottom
                fillRect.offsetMax = new Vector2(0, 0); // Right, Top
            }
        }

        if (integritasText != null)
        {
            integritasText.text = Mathf.RoundToInt(currentIntegritas) + "%";
        }
    }
}