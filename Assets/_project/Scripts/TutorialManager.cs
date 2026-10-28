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

        [Tooltip("Centang kalau step ini hanya untuk Level 1 (misalnya penjelasan top bar). Di Level 2 step ini dilewati.")]
        public bool hanyaLevel1;
    }

    [Header("Daftar Step Tutorial Awal")]
    public TutorialStep[] steps;

    [Header("Daftar Step Tutorial Selesai Bimbingan")]
    public TutorialStep[] bimbinganSteps; // Array khusus untuk tutorial bimbingan

    [Header("Daftar Step Tutorial Selesai Dosen Senior")]
    public TutorialStep[] dosenSeniorSteps; // Array khusus untuk tutorial dosen senior

    [Header("Daftar Step Tutorial Selesai Kaprodi (Scene 5)")]
    public TutorialStep[] kaprodiSteps; // Array khusus untuk tutorial kaprodi (3 box)

    [Header("Pengaturan Player")]
    public PlayerMovement playerMovement;

    // True setelah pemain masuk Level 2 (diset oleh PopupLevelSelesai lewat MasukLevel2)
    public bool SudahLevel2 { get; private set; } = false;

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

    // Dipanggil saat pemain menekan Lanjutkan di pop-up Level Selesai.
    // Setelah ini, semua step yang bertanda "Hanya Level 1" dilewati.
    public void MasukLevel2()
    {
        SudahLevel2 = true;
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

    // Fungsi untuk memanggil Tutorial Selesai Dosen Senior
    public void StartDosenSeniorTutorial()
    {
        if (dosenSeniorSteps != null && dosenSeniorSteps.Length > 0)
        {
            currentActiveSteps = dosenSeniorSteps;
            ShowTutorial();
        }
        else
        {
            Debug.LogWarning("[TutorialManager] Array 'dosenSeniorSteps' masih kosong di Inspector!");
        }
    }

    // Fungsi untuk memanggil Tutorial Selesai Kaprodi (Scene 5)
    public void StartKaprodiTutorial()
    {
        if (kaprodiSteps != null && kaprodiSteps.Length > 0)
        {
            currentActiveSteps = kaprodiSteps;
            ShowTutorial();
        }
        else
        {
            Debug.LogWarning("[TutorialManager] Array 'kaprodiSteps' masih kosong di Inspector!");
        }
    }

    // Cari step berikutnya (mulai dari index 'dari') yang tidak dilewati. -1 = tidak ada lagi.
    private int CariStep(int dari)
    {
        if (currentActiveSteps == null) return -1;

        for (int i = dari; i < currentActiveSteps.Length; i++)
        {
            bool dilewati = SudahLevel2 && currentActiveSteps[i].hanyaLevel1;
            if (!dilewati) return i;
        }
        return -1;
    }

    void ShowTutorial()
    {
        if (tutorialPanel == null) return;

        int awal = CariStep(0);
        if (awal < 0) return;   // semua step dilewati: tutorial tidak ditampilkan, game tidak dihentikan

        tutorialPanel.SetActive(true);
        currentIndex = awal;
        ApplyStep(currentIndex);

        if (playerMovement != null) playerMovement.enabled = false;
        Time.timeScale = 0f;
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
        int berikut = CariStep(currentIndex + 1);

        if (berikut >= 0)
        {
            currentIndex = berikut;
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