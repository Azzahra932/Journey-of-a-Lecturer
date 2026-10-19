using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject tutorialPanel; // Panel overlay utama
    public RectTransform tutorialBoxRect; // RectTransform dari kotak box tutorial (tutbox)
    public Image tutorialDisplayImage; // Komponen Image untuk ganti gambar

    [System.Serializable]
    public struct TutorialStep
    {
        public Sprite tutorialSprite; // Gambar untuk langkah ini
        public Vector2 anchoredPosition; // Posisi X dan Y di layar untuk langkah ini
        public Vector2 sizeDelta; // Ukuran Width dan Height untuk langkah ini
    }

    [Header("Daftar Step Tutorial Awal")]
    public TutorialStep[] steps;

    [Header("Daftar Step Tutorial Selesai Bimbingan")]
    public TutorialStep[] bimbinganSteps; // Array khusus untuk tutorial bimbingan

    [Header("Pengaturan Player")]
    public PlayerMovement playerMovement;

    private TutorialStep[] currentActiveSteps; // Menyimpan daftar step yang sedang aktif
    private int currentIndex = 0;

    void Start()
    {
        StartTutorial(); // Otomatis jalankan tutorial awal saat game mulai
    }

    void Update()
    {
        if (tutorialPanel != null && tutorialPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                NextStep();
            }
        }
    }

    // Fungsi untuk memanggil Tutorial Awal Game
    public void StartTutorial()
    {
        if (steps != null && steps.Length > 0)
        {
            currentActiveSteps = steps;
            ShowTutorial();
        }
    }

    // Fungsi untuk memanggil Tutorial Selesai Bimbingan
    public void StartBimbinganTutorial()
    {
        if (bimbinganSteps != null && bimbinganSteps.Length > 0)
        {
            currentActiveSteps = bimbinganSteps;
            ShowTutorial();
        }
        else
        {
            Debug.LogWarning("[TutorialManager] Array 'bimbinganSteps' masih kosong di Inspector!");
        }
    }

    void ShowTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            currentIndex = 0;
            ApplyStep(currentIndex);

            if (playerMovement != null) playerMovement.enabled = false;
            Time.timeScale = 0f;
        }
    }

    void ApplyStep(int index)
    {
        if (currentActiveSteps != null && index < currentActiveSteps.Length)
        {
            TutorialStep currentStep = currentActiveSteps[index];

            // Ganti gambar
            if (tutorialDisplayImage != null && currentStep.tutorialSprite != null)
            {
                tutorialDisplayImage.sprite = currentStep.tutorialSprite;
            }

            // Ubah posisi dan ukuran box secara dinamis sesuai step-nya
            if (tutorialBoxRect != null)
            {
                tutorialBoxRect.anchoredPosition = currentStep.anchoredPosition;
                tutorialBoxRect.sizeDelta = currentStep.sizeDelta;
            }
        }
    }

    public void NextStep()
    {
        currentIndex++;

        if (currentActiveSteps != null && currentIndex < currentActiveSteps.Length)
        {
            ApplyStep(currentIndex);
        }
        else
        {
            CloseTutorial();
        }
    }

    public void CloseTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);

            if (playerMovement != null) playerMovement.enabled = true;
            Time.timeScale = 1f;
        }
    }
}