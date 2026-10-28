using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Pengaturan Skor")]
    public int currentScore = 0;
    public int targetScore = 150;

    [Header("Referensi UI")]
    public Slider scoreSlider;
    public TMP_Text scoreText;

    // Skor dan target disimpan di sini (static) supaya terbawa ke level berikutnya.
    // Tidak memakai DontDestroyOnLoad, jadi objek ini boleh berada di mana saja di Hierarchy (mis. di dalam Canvas).
    private static bool adaTersimpan = false;
    private static int skorTersimpan = 0;
    private static int targetTersimpan = 150;

    // Panggil ini kalau mau mulai permainan baru dari awal (mis. dari menu utama)
    public static void ResetProgres()
    {
        adaTersimpan = false;
    }

    private void Awake()
    {
        Instance = this;

        // Kalau ada skor dari level sebelumnya, lanjutkan dari situ
        if (adaTersimpan)
        {
            currentScore = skorTersimpan;
            targetScore = targetTersimpan;
        }
    }

    private void Start()
    {
        if (scoreSlider == null)
        {
            scoreSlider = GetComponent<Slider>();
        }

        if (scoreSlider != null)
        {
            scoreSlider.minValue = 0;
            scoreSlider.maxValue = targetScore;
            scoreSlider.value = currentScore;
        }

        UpdateScoreUI();
    }

    // Fungsi universal untuk menambah skor dari scene manapun
    public void AddScore(int amount)
    {
        currentScore += amount;

        if (currentScore > targetScore)
        {
            currentScore = targetScore;
        }

        UpdateScoreUI();

        if (currentScore >= targetScore)
        {
            OnTargetReached();
        }
    }

    private void UpdateScoreUI()
    {
        // Simpan supaya terbawa ke level berikutnya
        skorTersimpan = currentScore;
        targetTersimpan = targetScore;
        adaTersimpan = true;

        if (scoreSlider != null)
        {
            scoreSlider.value = currentScore;

            // PENGAMAN TAMBAHAN: Mencegah komponen Fill meregang/panjang melebihi bar saat nilainya berubah
            RectTransform fillRect = scoreSlider.fillRect;
            if (fillRect != null && scoreSlider.maxValue > 0f)
            {
                float persen = Mathf.Clamp01(currentScore / scoreSlider.maxValue);
                fillRect.anchorMin = new Vector2(0, 0);
                fillRect.anchorMax = new Vector2(persen, 1);
                fillRect.offsetMin = new Vector2(0, 0); // Left, Bottom
                fillRect.offsetMax = new Vector2(0, 0); // Right, Top
            }
        }

        if (scoreText != null)
        {
            scoreText.text = $"{currentScore}/{targetScore}";
        }
    }

    private void OnTargetReached()
    {
        Debug.Log("Target skor " + targetScore + " tercapai!");
    }
}