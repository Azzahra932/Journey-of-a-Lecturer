using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    // Singleton Instance agar bisa dipanggil dari mana saja (misal: LaptopInteraction.cs)
    public static ScoreManager Instance;

    [Header("Pengaturan Skor")]
    public int currentScore = 0;
    public int targetScore = 150;

    [Header("Referensi UI")]
    public Slider scoreSlider; // Komponen Slider pada ScoreBar
    public TMP_Text scoreText; // Komponen TextMeshPro angka "0/150"

    private void Awake()
    {
        // Setup Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Jika slot scoreSlider belum di-assign manual di Inspector, otomatis ambil dari objek ini sendiri
        if (scoreSlider == null)
        {
            scoreSlider = GetComponent<Slider>();
        }

        // Inisialisasi nilai Slider
        if (scoreSlider != null)
        {
            scoreSlider.minValue = 0;
            scoreSlider.maxValue = targetScore;
            scoreSlider.value = currentScore;
        }

        UpdateScoreUI();
    }

    // Fungsi utama untuk menambah poin skor
    public void AddScore(int amount)
    {
        currentScore += amount;

        // Membatasi agar skor tidak melebihi target 150
        if (currentScore > targetScore)
        {
            currentScore = targetScore;
        }

        UpdateScoreUI();

        // Pengecekan jika target skor sudah tercapai
        if (currentScore >= targetScore)
        {
            OnTargetReached();
        }
    }

    // Fungsi memperbarui visual Bar dan Teks Skor
    private void UpdateScoreUI()
    {
        if (scoreSlider != null)
        {
            scoreSlider.value = currentScore;
        }

        if (scoreText != null)
        {
            scoreText.text = $"{currentScore}/{targetScore}";
        }
    }

    // Event saat skor mencapai 150
    private void OnTargetReached()
    {
        Debug.Log("Target skor 150 tercapai!");
        // Tambahkan logika kelulusan / UI Victory di sini jika ada
    }
}