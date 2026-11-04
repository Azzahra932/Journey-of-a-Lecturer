using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Pop-up "Level 2 selesai" (popuplv2): muncul saat poin mencapai target level 2 (300), menampilkan XP dan Integritas.
// Sama seperti pop-up level 1 (PopupLevelSelesai), tapi hanya aktif setelah target skor naik menjadi 300.
// Kalau poin tercapai saat sebuah aktivitas masih berjalan (laptop, podium, tutorial), pop-up menunggu sampai aktivitas itu selesai.
//
// Pasang script ini di satu objek kosong (mis. "ManagerLevel2Selesai").
public class PopupLevel2Selesai : MonoBehaviour
{
    [Header("Syarat Muncul")]
    public int targetSkorLevel2 = 300;          // Pop-up muncul saat skor mencapai target ini. Pop-up tidak muncul selama target skor masih milik level 1 (150)
    public float jedaSebelumPopup = 1.0f;       // Jeda (detik) setelah poin tercapai

    [Header("Panel Pop-up (popuplv2)")]
    public GameObject panelPopup;               // Panel pop-up level 2
    public GameObject overlayHitam;             // Panel hitam transparan satu layar di belakang pop-up (opsional)
    public TMP_Text teksXP;                     // Teks angka di sebelah ikon bintang
    public TMP_Text teksIntegritas;             // Teks angka di sebelah ikon perisai
    public Button tombolLanjut;                 // Tombol "Lanjut" (jangan isi On Click di Inspector, dipasang lewat script)
    public bool hentikanWaktuSaatTampil = true; // Game berhenti selama pop-up tampil

    [Header("Menunggu Aktivitas Selesai (kosong = dicari otomatis)")]
    public LaptopKelas laptopKelas;
    public LaptopInteraction laptopDosen;
    public LaptopPerpusInteraction laptopPerpus;

    [Header("Saat Tombol Lanjut Ditekan")]
    public string namaSceneBerikutnya = "";     // Opsional: nama scene yang dimuat setelah Lanjut (harus ada di Build Profiles). Kosong = tidak pindah scene
    public UnityEvent saatLanjut;               // Opsional: hal lain yang dijalankan saat Lanjut (mis. menampilkan ending)

    private bool sudahTampil = false;
    private bool sudahLanjut = false;
    private float waktuTercapai = -1f;
    private bool sudahDilaporkanMenunggu = false;
    private PlayerMovement pm;

    void Start()
    {
        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (tombolLanjut != null) tombolLanjut.onClick.AddListener(Lanjut);

        if (laptopKelas == null) laptopKelas = FindFirstObjectByType<LaptopKelas>();
        if (laptopDosen == null) laptopDosen = FindFirstObjectByType<LaptopInteraction>();
        if (laptopPerpus == null) laptopPerpus = FindFirstObjectByType<LaptopPerpusInteraction>(FindObjectsInactive.Include);
    }

    void OnDestroy()
    {
        if (tombolLanjut != null) tombolLanjut.onClick.RemoveListener(Lanjut);
    }

    void Update()
    {
        if (sudahTampil || ScoreManager.Instance == null) return;

        // Pop-up level 2 hanya aktif setelah target skor naik ke level 2 (supaya tidak ikut muncul di poin 150 level 1)
        bool level2Aktif = ScoreManager.Instance.targetScore >= targetSkorLevel2;
        bool tercapai = level2Aktif && ScoreManager.Instance.currentScore >= ScoreManager.Instance.targetScore;

        if (!tercapai)
        {
            waktuTercapai = -1f;
            sudahDilaporkanMenunggu = false;
            return;
        }

        if (waktuTercapai < 0f) waktuTercapai = Time.unscaledTime;
        if (Time.unscaledTime - waktuTercapai < jedaSebelumPopup) return;

        // Tunggu aktivitas yang sedang berjalan selesai (supaya poin, reward, dan integritas akhir sudah terhitung)
        string alasan = AlasanMasihSibuk();
        if (alasan != null)
        {
            if (!sudahDilaporkanMenunggu && Time.unscaledTime - waktuTercapai > 5f)
            {
                sudahDilaporkanMenunggu = true;
                Debug.Log("[Level 2] Poin sudah tercapai, pop-up menunggu: " + alasan);
            }
            return;
        }

        Tampilkan();
    }

    // Mengembalikan alasan pop-up harus menunggu, atau null kalau tidak ada aktivitas yang berjalan
    private string AlasanMasihSibuk()
    {
        if (Time.timeScale == 0f) return "game sedang berhenti (tutorial atau pop-up lain sedang tampil)";
        if (laptopKelas != null && laptopKelas.SedangSibuk) return "laptop kelas sedang dipakai";
        if (laptopDosen != null && laptopDosen.SedangSibuk) return "laptop dosen sedang dipakai";
        if (laptopPerpus != null && laptopPerpus.SedangSibuk) return "laptop perpustakaan sedang dipakai";

        // MC dikunci (adegan atau aktivitas di podium sedang berjalan)
        if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null && !pm.enabled) return "MC sedang dikunci oleh adegan yang berjalan";

        return null;
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Tampilkan Pop-up (tanpa menunggu 300 poin)")]
    private void TesTampilkan()
    {
        if (sudahTampil) return;
        Tampilkan();
    }

    private void Tampilkan()
    {
        sudahTampil = true;

        if (teksXP != null && ScoreManager.Instance != null)
            teksXP.text = ScoreManager.Instance.currentScore.ToString();

        if (teksIntegritas != null)
        {
            if (IntegritasManager.Instance != null)
                teksIntegritas.text = Mathf.RoundToInt(IntegritasManager.Instance.CurrentIntegritas) + "%";
            else
                teksIntegritas.text = "-";
        }

        if (panelPopup == null)
        {
            Debug.LogWarning("[Level 2] Slot Panel Popup masih kosong di Inspector! Pop-up tidak bisa tampil.");
            return;
        }

        // Urutan penting: overlay dulu, baru panel (panel harus tampil di atas overlay)
        if (overlayHitam != null)
        {
            overlayHitam.SetActive(true);
            overlayHitam.transform.SetAsLastSibling();
        }

        panelPopup.SetActive(true);
        panelPopup.transform.SetAsLastSibling();

        if (!panelPopup.activeInHierarchy)
            Debug.LogWarning("[Level 2] Panel '" + panelPopup.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil.");

        if (hentikanWaktuSaatTampil) Time.timeScale = 0f;

        Debug.Log("[Level 2] Target poin level 2 tercapai. Pop-up level 2 ditampilkan.");
    }

    public void Lanjut()
    {
        if (sudahLanjut) return;   // cegah klik ganda
        sudahLanjut = true;

        Time.timeScale = 1f;

        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        saatLanjut.Invoke();

        if (!string.IsNullOrEmpty(namaSceneBerikutnya))
            SceneManager.LoadScene(namaSceneBerikutnya);

        Debug.Log("[Level 2] Tombol Lanjut ditekan.");
    }
}