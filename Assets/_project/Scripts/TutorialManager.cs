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

    [Header("Daftar Step Tutorial")]
    public TutorialStep[] steps;

    [Header("Pengaturan Player")]
    public PlayerMovement playerMovement;

    private int currentIndex = 0;

    void Start()
    {
        StartTutorial();
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

    public void StartTutorial()
    {
        if (tutorialPanel != null && steps.Length > 0)
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
        if (index < steps.Length)
        {
            TutorialStep currentStep = steps[index];

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

        if (currentIndex < steps.Length)
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