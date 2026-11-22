using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu utama: tombol Play (permainan baru), Continue (lanjut dari checkpoint terakhir), About, Settings, dan Keluar (opsional).
// Pasang script ini di satu objek kosong di scene MainMenu (mis. "MainMenuManager").
// Semua tombol dipasang lewat script, jadi JANGAN isi On Click di Inspector.
// Bagian yang tidak diisi di Inspector dilewati.
//
// Continue aktif otomatis kalau sudah ada data simpanan (ProgresPermainan.AdaSave), yaitu setelah pemain mencapai awal Level 2.
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Tujuan")]
    [Tooltip("Nama scene gameplay persis seperti di Build Profiles / Scene List (huruf besar kecil berpengaruh).")]
    public string namaSceneGameplay = "MainGameplay";

    [Header("Tombol Utama")]
    public Button tombolPlay;                   // Mulai permainan baru dari awal
    public Button tombolContinue;               // Lanjut dari checkpoint terakhir
    public Button tombolAbout;                  // Membuka panel About
    public Button tombolSettings;               // Membuka panel Settings (ikon gerigi)
    public Button tombolKeluar;                 // Opsional: keluar dari game

    [Header("Continue")]
    [Tooltip("true = tombol Continue disembunyikan kalau belum ada data simpanan. false = tampil tapi redup dan tidak bisa diklik.")]
    public bool sembunyikanContinueSaatNonaktif = false;

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

        // Continue aktif kalau ada data simpanan
        if (tombolContinue != null)
        {
            bool ada = ProgresPermainan.AdaSave();

            if (sembunyikanContinueSaatNonaktif)
                tombolContinue.gameObject.SetActive(ada);
            else
                tombolContinue.interactable = ada;
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

    // ---------- Play (permainan baru) / Continue ----------
    public void Play()
    {
        Klik();

        // Permainan baru: skor, integritas, dan penanda Continue dikembalikan ke awal
        ProgresPermainan.MulaiBaru();

        MuatGameplay();
    }

    public void LanjutGame()
    {
        if (!ProgresPermainan.AdaSave())
        {
            Debug.Log("[Menu] Continue ditekan, tapi belum ada data simpanan.");
            return;
        }

        Klik();

        // Skor dan integritas tersimpan dipasang, lalu penanda Continue dinyalakan (dibaca PemulihProgres di scene gameplay)
        ProgresPermainan.SiapkanContinue();

        MuatGameplay();
    }

    private void MuatGameplay()
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