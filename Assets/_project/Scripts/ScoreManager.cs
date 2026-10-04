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

    private void Awake()
    {
        // Singleton agar ScoreManager hanya ada satu dan bertahan antar scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Agar skor terbawa dari Scene 1 ke Scene 2 tanpa reset
        }
        else
        {
            Destroy(gameObject);
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
        if (scoreSlider != null)
        {
            scoreSlider.value = currentScore;
        }

        if (scoreText != null)
        {
            scoreText.text = $"{currentScore}/{targetScore}";
        }
    }

    private void OnTargetReached()
    {
        Debug.Log("Target skor 150 tercapai!");
    }
}