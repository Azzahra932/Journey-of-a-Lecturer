using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Pop-up "Game Over" (integritas turun di bawah 30%): berlaku di semua level (Level 1 sampai 4).
//   1. Integritas turun di bawah batas -> pop-up menunggu aktivitas yang sedang berjalan selesai (laptop, adegan, transisi, pop-up lain)
//   2. Pop-up "Game Over" tampil, game berhenti
//   3. Klik "Mulai Ulang": pop-up ditutup, integritas dipulihkan jadi 50%, layar menghitam, MC kembali ke titik awal (ruang dosen)
//   4. Energi dan jam tetap seperti sebelumnya (bisa dinyalakan di Inspector kalau mau energi penuh / jam 08.00)
// Alur scene, poin, level, dan ruangan yang sudah terbuka TIDAK direset: permainan tetap berlanjut dari level yang sedang berjalan.
//
// Pasang script ini di satu objek kosong (mis. "ManagerGameOver"), SATU saja untuk seluruh game.
// Butuh method SetIntegritas di IntegritasManager (lihat contoh kode di penjelasan).
public class GameOverIntegritas : MonoBehaviour
{
    [Header("Syarat Muncul")]
    public float batasIntegritas = 30f;             // Pop-up muncul kalau integritas di bawah angka ini
    public float jedaSebelumPopup = 0.5f;           // Jeda (detik) setelah integritas turun

    [Header("Panel Pop-up Game Over")]
    public GameObject panelGameOver;                // Panel pop-up "Game Over"
    public GameObject overlayHitam;                 // Panel hitam transparan satu layar di belakang pop-up (opsional)
    public Button tombolMulaiUlang;                 // Tombol "Mulai Ulang" (jangan isi On Click di Inspector, dipasang lewat script)
    public bool hentikanWaktuSaatTampil = true;     // Game berhenti selama pop-up tampil

    [Header("Setelah Mulai Ulang")]
    public float integritasSetelahMulaiUlang = 50f; // Integritas dipulihkan ke angka ini (bukan 100%)
    public bool isiEnergiPenuh = false;             // false = energi tetap seperti sebelumnya. true = energi diisi penuh setelah MC kembali ke titik awal
    public bool resetJamKePagi = false;             // false = jam tetap seperti sebelumnya. true = jam dikembalikan ke 08.00 setelah MC kembali ke titik awal
    public TransisiKembaliKeAwal transisi;          // Kosong = dicari otomatis
    public EnergyBar energyBar;                     // Kosong = dicari otomatis
    public GameTimeManager timeManager;             // Kosong = dicari otomatis

    [Header("Menunggu Aktivitas Selesai (kosong = dicari otomatis)")]
    public LaptopKelas laptopKelas;
    public LaptopInteraction laptopDosen;
    public LaptopPerpusInteraction laptopPerpus;

    [Header("Event (opsional)")]
    public UnityEvent saatMulaiUlang;               // Dipanggil begitu tombol Mulai Ulang ditekan
    public UnityEvent saatSelesaiMulaiUlang;        // Dipanggil setelah MC kembali ke titik awal dan semuanya dipulihkan

    private bool sedangGameOver = false;
    private bool sudahKlik = false;
    private float waktuTurun = -1f;
    private bool sudahDilaporkanMenunggu = false;
    private PlayerMovement pm;

    // Semua kursi di scene: selama adegan atau aktivitas berjalan, kursi MC dikunci (TerkunciEksternal)
    private InteractiveDesk[] semuaKursiDesk;
    private KursiPerpusInteraction[] semuaKursiPerpus;

    void Start()
    {
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        if (tombolMulaiUlang != null) tombolMulaiUlang.onClick.AddListener(MulaiUlang);

        if (laptopKelas == null) laptopKelas = FindFirstObjectByType<LaptopKelas>();
        if (laptopDosen == null) laptopDosen = FindFirstObjectByType<LaptopInteraction>();
        if (laptopPerpus == null) laptopPerpus = FindFirstObjectByType<LaptopPerpusInteraction>(FindObjectsInactive.Include);
        semuaKursiDesk = FindObjectsByType<InteractiveDesk>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        semuaKursiPerpus = FindObjectsByType<KursiPerpusInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (transisi == null) transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();
        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();
        if (timeManager == null) timeManager = FindFirstObjectByType<GameTimeManager>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        GameOverIntegritas[] semua = FindObjectsByType<GameOverIntegritas>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Game Over] Ada " + semua.Length + " objek yang memasang script GameOverIntegritas. Pakai satu saja.");
    }

    void OnDestroy()
    {
        if (tombolMulaiUlang != null) tombolMulaiUlang.onClick.RemoveListener(MulaiUlang);
    }

    void Update()
    {
        if (sedangGameOver || IntegritasManager.Instance == null) return;

        bool dibawahBatas = IntegritasManager.Instance.CurrentIntegritas < batasIntegritas;
        if (!dibawahBatas)
        {
            waktuTurun = -1f;
            sudahDilaporkanMenunggu = false;
            return;
        }

        if (waktuTurun < 0f) waktuTurun = Time.unscaledTime;
        if (Time.unscaledTime - waktuTurun < jedaSebelumPopup) return;

        // Tunggu aktivitas yang sedang berjalan selesai (supaya adegan, poin, dan reward tidak terpotong)
        string alasan = AlasanMasihSibuk();
        if (alasan != null)
        {
            if (!sudahDilaporkanMenunggu && Time.unscaledTime - waktuTurun > 5f)
            {
                sudahDilaporkanMenunggu = true;
                Debug.Log("[Game Over] Integritas sudah di bawah batas, pop-up menunggu: " + alasan);
            }
            return;
        }

        Tampilkan();
    }

    // Mengembalikan alasan pop-up harus menunggu, atau null kalau tidak ada aktivitas yang berjalan
    private string AlasanMasihSibuk()
    {
        if (Time.timeScale == 0f) return "game sedang berhenti (tutorial atau pop-up lain sedang tampil)";
        if (transisi != null && transisi.SedangTransisi) return "transisi layar sedang berjalan";
        if (laptopKelas != null && laptopKelas.SedangSibuk) return "laptop kelas sedang dipakai";
        if (laptopDosen != null && laptopDosen.SedangSibuk) return "laptop dosen sedang dipakai";
        if (laptopPerpus != null && laptopPerpus.SedangSibuk) return "laptop perpustakaan sedang dipakai";

        if (semuaKursiDesk != null)
        {
            foreach (InteractiveDesk d in semuaKursiDesk)
            {
                if (d != null && d.TerkunciEksternal) return "MC sedang dikunci di kursi '" + d.name + "' oleh adegan yang berjalan";
            }
        }

        if (semuaKursiPerpus != null)
        {
            foreach (KursiPerpusInteraction k in semuaKursiPerpus)
            {
                if (k != null && k.TerkunciEksternal) return "MC sedang dikunci di kursi '" + k.name + "' oleh adegan yang berjalan";
            }
        }

        // MC dikunci (adegan atau aktivitas sedang berjalan)
        if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null && !pm.enabled) return "MC sedang dikunci oleh adegan yang berjalan";

        return null;
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Tampilkan Game Over (tanpa menunggu integritas)")]
    private void TesTampilkan()
    {
        if (sedangGameOver) return;
        Tampilkan();
    }

    private void Tampilkan()
    {
        sedangGameOver = true;
        sudahKlik = false;

        if (panelGameOver == null)
        {
            Debug.LogWarning("[Game Over] Slot Panel Game Over masih kosong di Inspector! Pop-up tidak bisa tampil.");
            sedangGameOver = false;
            return;
        }

        if (tombolMulaiUlang != null) tombolMulaiUlang.interactable = true;

        // Urutan penting: overlay dulu, baru panel (panel harus tampil di atas overlay)
        if (overlayHitam != null)
        {
            overlayHitam.SetActive(true);
            overlayHitam.transform.SetAsLastSibling();
        }

        panelGameOver.SetActive(true);
        panelGameOver.transform.SetAsLastSibling();

        if (!panelGameOver.activeInHierarchy)
            Debug.LogWarning("[Game Over] Panel '" + panelGameOver.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil.");

        if (hentikanWaktuSaatTampil) Time.timeScale = 0f;

        Debug.Log("[Game Over] Integritas di bawah " + batasIntegritas + "%. Pop-up Game Over ditampilkan.");
    }

    // Dipanggil tombol Mulai Ulang
    public void MulaiUlang()
    {
        if (!sedangGameOver || sudahKlik) return;   // cegah klik ganda
        sudahKlik = true;

        if (tombolMulaiUlang != null) tombolMulaiUlang.interactable = false;

        // Pop-up dan overlay ditutup, waktu berjalan lagi supaya transisi layar hitam bisa jalan
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        Time.timeScale = 1f;

        // Integritas dipulihkan ke 50% (tersimpan juga untuk level berikutnya)
        if (IntegritasManager.Instance != null)
            IntegritasManager.Instance.SetIntegritas(integritasSetelahMulaiUlang);
        else
            Debug.LogWarning("[Game Over] IntegritasManager.Instance tidak ditemukan di Scene!");

        saatMulaiUlang.Invoke();

        // Transisi layar hitam: MC kembali ke titik awal (alur scene tidak diulang, permainan tetap lanjut)
        if (transisi != null && !transisi.SedangTransisi)
        {
            transisi.saatTransisiSelesai.AddListener(SetelahTransisi);
            transisi.MulaiTransisi();
        }
        else
        {
            if (transisi == null)
                Debug.LogWarning("[Game Over] TransisiKembaliKeAwal tidak ditemukan, MC tidak dipindah ke titik awal.");

            SetelahTransisi();
        }

        Debug.Log("[Game Over] Mulai Ulang ditekan, integritas jadi " + integritasSetelahMulaiUlang + "%.");
    }

    // Dijalankan setelah transisi selesai dan MC berada di titik awal
    private void SetelahTransisi()
    {
        if (transisi != null) transisi.saatTransisiSelesai.RemoveListener(SetelahTransisi);

        if (isiEnergiPenuh && energyBar != null) energyBar.IsiPenuh();
        if (resetJamKePagi && timeManager != null) timeManager.ResetKeJamPagi();

        sedangGameOver = false;
        waktuTurun = -1f;
        sudahDilaporkanMenunggu = false;

        saatSelesaiMulaiUlang.Invoke();

        Debug.Log("[Game Over] MC kembali ke titik awal, permainan dilanjutkan.");
    }
}