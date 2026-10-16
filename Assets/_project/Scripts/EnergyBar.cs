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

    // ===== TAMBAHAN BARU: Energi habis -> transisi hitam -> kembali ke titik awal =====
    [Header("Energi Habis (Transisi Kembali ke Awal)")]
    public TransisiKembaliKeAwal transisi;      // drag objek ManagerTransisi (kosong = dicari otomatis)
    public bool isiPenuhSetelahTransisi = true; // energi terisi penuh setelah transisi selesai
    public bool pemicuOtomatis = true;          // transisi langsung jalan begitu energi 0 (tanpa jeda)
    // ===================================================================================

    // Status apakah pengurangan energi sedang di-pause
    private bool isPaused = false;

    private void Start()
    {
        currentEnergy = maxEnergy;
        SetMaxEnergy(maxEnergy);

        // TAMBAHAN BARU: sambungkan ke transisi
        if (transisi == null) transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();
        if (transisi != null && isiPenuhSetelahTransisi)
        {
            transisi.saatTransisiSelesai.AddListener(IsiPenuh);
        }
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

    // TAMBAHAN BARU: pemicu otomatis. Begitu energi 0 (dari sebab apa pun), transisi langsung jalan.
    private void LateUpdate()
    {
        if (!pemicuOtomatis || !isiPenuhSetelahTransisi) return;
        if (currentEnergy > 0.01f) return;
        if (transisi == null || transisi.SedangTransisi) return;

        Debug.Log("[EnergyBar] Energi 0, transisi langsung dimulai.");
        transisi.MulaiTransisi();
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

    // ===== TAMBAHAN BARU =====

    // Dipanggil script aktivitas SETELAH aktivitasnya selesai.
    // Kalau energi sudah habis, jalankan transisi hitam dan kembalikan player ke titik awal.
    public void CekEnergiHabis()
    {
        Debug.Log("[EnergyBar] CekEnergiHabis dipanggil. Energi sekarang = " + currentEnergy);

        if (currentEnergy > 0.01f) return;

        if (transisi == null)
        {
            Debug.LogWarning("[EnergyBar] Energi habis, tapi TransisiKembaliKeAwal belum ditemukan di scene!");
            return;
        }

        transisi.MulaiTransisi();
    }

    // Isi energi sampai penuh (dipanggil otomatis setelah transisi selesai)
    public void IsiPenuh()
    {
        RestoreEnergy(maxEnergy);
    }
}