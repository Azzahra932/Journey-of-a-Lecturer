using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu utama: tombol Play (mulai game), Continue (opsional), About, Settings, dan Keluar (opsional).
// Pasang script ini di satu objek kosong di scene MainMenu (mis. "MainMenuManager").
// Semua tombol dipasang lewat script, jadi JANGAN isi On Click di Inspector.
// Bagian yang tidak diisi di Inspector dilewati.
// Play = game baru: skor dan integritas direset dulu supaya tidak terbawa dari permainan sebelumnya.
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Tujuan")]
    [Tooltip("Nama scene gameplay persis seperti di Build Profiles / Scene List (huruf besar kecil berpengaruh).")]
    public string namaSceneGameplay = "MainGameplay";

    [Header("Tombol Utama")]
    public Button tombolPlay;                   // Mulai game dari awal
    public Button tombolContinue;               // Opsional
    public Button tombolAbout;                  // Membuka panel About
    public Button tombolSettings;               // Membuka panel Settings (ikon gerigi)
    public Button tombolKeluar;                 // Opsional: keluar dari game

    [Header("Continue (opsional)")]
    [Tooltip("Game ini belum punya sistem simpan, jadi Continue dimatikan dulu. Centang kalau nanti sudah ada sistem simpan. Kalau dicentang, Continue memuat scene gameplay.")]
    public bool continueAktif = false;
    public bool sembunyikanContinueSaatNonaktif = false;   // true = tombol Continue disembunyikan, false = tampil tapi redup dan tidak bisa diklik

    [Header("Panel About (opsional)")]
    public GameObject panelAbout;               // Panel berisi info game / pembuat
    public Button tombolTutupAbout;             // Tombol tutup di panel About

    [Header("Panel Settings (opsional)")]
    public GameObject panelSettings;            // Panel pengaturan
    public Button tombolTutupSettings;          // Tombol tutup di panel Settings
    public Slider sliderVolume;                 // Opsional: slider volume (Min 0, Max 1). Nilainya disimpan antar sesi

    [Header("Efek Klik (opsional)")]
    public AudioSource suaraKlik;               // Opsional: suara saat tombol diklik

    private const string KunciVolume = "volume_game";

    void Start()
    {
        // Kalau menu dibuka lagi dari gameplay (mis. lewat tombol Home di pop-up), pastikan waktu berjalan normal
        Time.timeScale = 1f;

        if (panelAbout != null) panelAbout.SetActive(false);
        if (panelSettings != null) panelSettings.SetActive(false);

        Pasang(tombolPlay, Play);
        Pasang(tombolContinue, LanjutGame);
        Pasang(tombolAbout, BukaAbout);
        Pasang(tombolSettings, BukaSettings);
        Pasang(tombolKeluar, Keluar);
        Pasang(tombolTutupAbout, TutupAbout);
        Pasang(tombolTutupSettings, TutupSettings);

        // Continue
        if (tombolContinue != null)
        {
            if (sembunyikanContinueSaatNonaktif)
                tombolContinue.gameObject.SetActive(continueAktif);
            else
                tombolContinue.interactable = continueAktif;
        }

        // Volume
        float volume = PlayerPrefs.GetFloat(KunciVolume, 1f);
        AudioListener.volume = volume;

        if (sliderVolume != null)
        {
            sliderVolume.SetValueWithoutNotify(volume);
            sliderVolume.onValueChanged.AddListener(UbahVolume);
        }
    }

    void OnDestroy()
    {
        Lepas(tombolPlay, Play);
        Lepas(tombolContinue, LanjutGame);
        Lepas(tombolAbout, BukaAbout);
        Lepas(tombolSettings, BukaSettings);
        Lepas(tombolKeluar, Keluar);
        Lepas(tombolTutupAbout, TutupAbout);
        Lepas(tombolTutupSettings, TutupSettings);

        if (sliderVolume != null) sliderVolume.onValueChanged.RemoveListener(UbahVolume);
    }

    private void Pasang(Button b, UnityEngine.Events.UnityAction aksi)
    {
        if (b != null) b.onClick.AddListener(aksi);
    }

    private void Lepas(Button b, UnityEngine.Events.UnityAction aksi)
    {
        if (b != null) b.onClick.RemoveListener(aksi);
    }

    private void Klik()
    {
        if (suaraKlik != null) suaraKlik.Play();
    }

    // ---------- Play / Continue ----------
    // Play = game baru dari awal: skor dan integritas direset
    public void Play()
    {
        Klik();
        MuatGameplay(true);
    }

    // Continue tidak mereset apa pun
    public void LanjutGame()
    {
        if (!continueAktif) return;

        Klik();
        MuatGameplay(false);
    }

    private void MuatGameplay(bool mulaiBaru)
    {
        if (string.IsNullOrEmpty(namaSceneGameplay))
        {
            Debug.LogWarning("[Menu] Nama Scene Gameplay masih kosong di Inspector.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(namaSceneGameplay))
        {
            Debug.LogWarning("[Menu] Scene '" + namaSceneGameplay + "' tidak ditemukan. Cek ejaannya, dan pastikan scene sudah ditambahkan di Build Profiles (Scene List).");
            return;
        }

        // Game baru: progres dari permainan sebelumnya dihapus (skor, target, integritas)
        if (mulaiBaru)
        {
            ScoreManager.ResetProgres();
            IntegritasManager.ResetProgres();
            Debug.Log("[Menu] Game baru: skor dan integritas direset.");
        }

        Debug.Log("[Menu] Memuat scene '" + namaSceneGameplay + "'.");
        SceneManager.LoadScene(namaSceneGameplay);
    }

    // ---------- About ----------
    public void BukaAbout()
    {
        Klik();
        if (panelSettings != null) panelSettings.SetActive(false);
        if (panelAbout != null)
        {
            panelAbout.SetActive(true);
            panelAbout.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogWarning("[Menu] Panel About belum diisi di Inspector.");
        }
    }

    public void TutupAbout()
    {
        Klik();
        if (panelAbout != null) panelAbout.SetActive(false);
    }

    // ---------- Settings ----------
    public void BukaSettings()
    {
        Klik();
        if (panelAbout != null) panelAbout.SetActive(false);
        if (panelSettings != null)
        {
            panelSettings.SetActive(true);
            panelSettings.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogWarning("[Menu] Panel Settings belum diisi di Inspector.");
        }
    }

    public void TutupSettings()
    {
        Klik();
        if (panelSettings != null) panelSettings.SetActive(false);
    }

    private void UbahVolume(float nilai)
    {
        AudioListener.volume = nilai;
        PlayerPrefs.SetFloat(KunciVolume, nilai);
    }

    // ---------- Keluar ----------
    public void Keluar()
    {
        Klik();
        Debug.Log("[Menu] Keluar dari game.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}