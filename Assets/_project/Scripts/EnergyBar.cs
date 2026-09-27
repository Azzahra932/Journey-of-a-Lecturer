using UnityEngine;
using UnityEngine.UI;

public class EnergyBar : MonoBehaviour
{
    public Slider slider;
    public Image fillImage;
    public Gradient colorGradient;

    [Header("Energy Values")]
    public float maxEnergy = 100f;
    public float currentEnergy;

    [Header("Passive Drain Rate")]
    public float drainRate = 0.01f;

    // Status apakah pengurangan energi sedang di-pause
    private bool isPaused = false;

    private void Start()
    {
        currentEnergy = maxEnergy;
        SetMaxEnergy(maxEnergy);
    }

    private void Update()
    {
        // Energi HANYA berkurang jika TIDAK dalam kondisi paused
        if (!isPaused && currentEnergy > 0)
        {
            currentEnergy -= drainRate * Time.deltaTime;
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
            UpdateBarVisual();
        }
    }

    // Fungsi untuk mem-pause atau me-resume pengurangan energi
    public void SetEnergyPaused(bool pauseState)
    {
        isPaused = pauseState;
    }

    public void SetMaxEnergy(float max)
    {
        maxEnergy = max;
        slider.maxValue = maxEnergy;
        currentEnergy = maxEnergy;
        UpdateBarVisual();
    }

    public void DecreaseEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        UpdateBarVisual();
    }

    public void RestoreEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        UpdateBarVisual();
    }

    private void UpdateBarVisual()
    {
        slider.value = currentEnergy;
        fillImage.color = colorGradient.Evaluate(slider.normalizedValue);
    }
}