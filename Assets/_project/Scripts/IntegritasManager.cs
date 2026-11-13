using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntegritasManager : MonoBehaviour
{
    public static IntegritasManager Instance;

    [Header("Pengaturan Integritas")]
    public float maxIntegritas = 100f;
    private float currentIntegritas;

    // TAMBAHAN BARU: nilai integritas disimpan di sini supaya terbawa ke level berikutnya (tidak kembali 100%)
    private static float integritasTersimpan = -1f;

    // Panggil ini kalau mau mulai permainan baru dari awal (mis. dari menu utama)
    public static void ResetProgres()
    {
        integritasTersimpan = -1f;
    }

    // TAMBAHAN BARU: supaya script lain (pop-up level selesai) bisa membaca nilai integritas
    public float CurrentIntegritas { get { return currentIntegritas; } }

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
        // Kalau ada nilai dari level sebelumnya, lanjutkan dari situ. Kalau tidak, mulai dari penuh.
        currentIntegritas = (integritasTersimpan >= 0f)
            ? Mathf.Clamp(integritasTersimpan, 0f, maxIntegritas)
            : maxIntegritas;

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
        integritasTersimpan = currentIntegritas;

        Debug.Log($"[IntegritasManager] Integritas berkurang {amount}%. Sisa: {currentIntegritas}%");
        UpdateUI();
    }

    // TAMBAHAN BARU: mengatur integritas ke angka tertentu (dipakai pop-up Game Over, mis. dipulihkan ke 50%)
    public void SetIntegritas(float nilai)
    {
        currentIntegritas = Mathf.Clamp(nilai, 0f, maxIntegritas);
        integritasTersimpan = currentIntegritas;   // supaya terbawa ke level berikutnya

        Debug.Log($"[IntegritasManager] Integritas diatur ke {currentIntegritas}%.");
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