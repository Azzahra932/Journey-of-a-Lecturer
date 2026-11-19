using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Pop-up penutup Level 5 (level terakhir): jam game mencapai 20.00, lalu:
//   1. Box penutup tampil, Space (atau klik kiri) untuk lanjut. Box yang muncul bergantung pada pilihan MC di Scene 3 Level 5
//      (tawaran Dewan Profesor): Ya (menerima) memunculkan Box Penutup Ya, Tidak (menolak) memunculkan Box Penutup Tidak
//      Khusus pilihan Tidak: setelah box penutup, muncul popup "Achievement Academic Legacy terbuka!" (Space untuk lanjut)
//   2. Setelah Space terakhir, langsung muncul pop-up hasil: XP (angka), Integritas (angka), dan Rank (huruf S, A, B, C, atau F dari integritas: S = 100%, A >= 90%, B >= 85%, C >= 80%, F >= 75%), dengan SATU tombol saja: Home
//   3. Home memunculkan konfirmasi keluar (Ya / Tidak). Ya = pindah ke scene menu utama, Tidak = kembali ke pop-up hasil
// Level 5 hanya satu hari (08.00 sampai 20.00) dan tidak punya target poin: poin lanjut dari level sebelumnya.
//
// Mode level terakhir diaktifkan oleh PopupLevel4Selesai (tombol Lanjut). Di jam 20.00 GameTimeManager TIDAK memulai tidur malam,
// jam berhenti, dan pop-up ini yang berjalan. Pop-up menunggu aktivitas yang sedang berjalan selesai dulu
// (laptop, adegan yang mengunci MC di kursi, tutorial, reward, pop-up lain).
//
// Pasang script ini di satu objek kosong (mis. "ManagerLevel5Selesai").
public class PopupLevel5Selesai : MonoBehaviour
{
    [Header("Syarat Muncul")]
    public float jedaSebelumPopup = 1.0f;       // Jeda (detik) setelah jam 20.00 tercapai

    [Header("1. Box Penutup (muncul dulu, sebelum pop-up hasil, sesuai pilihan di Scene 3)")]
    [Tooltip("Scene 3 Level 5 (tawaran Dewan Profesor). Kosong = dicari otomatis. Pilihan MC di sana menentukan box penutup yang muncul.")]
    public AdeganLevel5Scene3 adeganScene3;
    [Tooltip("Box penutup kalau MC MENERIMA tawaran di Scene 3 (memilih Ya). Tampil berurutan, Space untuk lanjut. Kosong = langsung ke pop-up hasil.")]
    public GameObject[] boxPenutupYa;
    [Tooltip("Box penutup kalau MC MENOLAK tawaran di Scene 3 (memilih Tidak). Dipakai juga kalau MC belum memilih (mis. Scene 3 dilewati saat tes). Kosong = langsung ke pop-up hasil.")]
    public GameObject[] boxPenutupTidak;
    [Header("1b. Popup Achievement (hanya kalau MC menolak tawaran)")]
    [Tooltip("Popup \"Achievement Academic Legacy terbuka!\". Muncul setelah Box Penutup Tidak selesai, sebelum pop-up hasil. Hanya untuk pilihan Tidak (menolak). Kosong = tidak ada popup achievement.")]
    public GameObject popupAchievement;
    [Tooltip("0 = popup achievement ditutup dengan Space atau klik. Lebih dari 0 = otomatis tertutup setelah sekian detik (Space atau klik tetap bisa menutup lebih cepat).")]
    public float lamaPopupAchievementOtomatis = 0f;
    public KeyCode tombolLanjutBox = KeyCode.Space;   // Menutup box dan lanjut ke box berikutnya (setelah box terakhir: pop-up hasil)
    public bool bolehKlikMouse = true;                // Klik kiri mouse juga bisa

    [Header("2. Panel Pop-up Hasil (popuplv5)")]
    public GameObject panelPopup;               // Panel pop-up hasil (poin dan integritas), nonaktif di awal
    public GameObject overlayHitam;             // Panel hitam transparan satu layar di belakang box penutup dan pop-up (opsional)
    public TMP_Text teksXP;                     // Teks angka di sebelah ikon bintang
    public TMP_Text teksIntegritas;             // Teks angka di sebelah ikon perisai
    public bool hentikanWaktuSaatTampil = true; // Game berhenti selama box penutup dan pop-up tampil

    [Header("Badge Rank (berdasarkan integritas, Guru Besar; dari Hierarchy, satu objek per rank)")]
    [Tooltip("Buat satu objek badge per rank di dalam pop-up hasil, lalu drag ke slot di bawah. Hanya objek yang sesuai rank yang dinyalakan, yang lain dimatikan.")]
    public GameObject objekRankS;               // Objek badge Rank S
    public GameObject objekRankA;               // Objek badge Rank A
    public GameObject objekRankB;               // Objek badge Rank B
    public GameObject objekRankC;               // Objek badge Rank C
    public GameObject objekRankF;               // Objek badge Rank F

    [Header("Batas Rank (integritas dalam persen)")]
    public int batasRankS = 100;                // Integritas >= angka ini: Rank S
    public int batasRankA = 90;                 // Integritas >= angka ini (dan di bawah batas S): Rank A
    public int batasRankB = 85;                 // Integritas >= angka ini (dan di bawah batas A): Rank B
    public int batasRankC = 80;                 // Integritas >= angka ini (dan di bawah batas B): Rank C
    public int batasRankF = 75;                 // Integritas >= angka ini (dan di bawah batas C): Rank F. Di bawah ini pun tetap Rank F (rank terendah)

    // Rank akhir (S, A, B, C, atau F), diisi saat pop-up hasil ditampilkan. Bisa dibaca script lain (mis. untuk achievement)
    public string RankAkhir { get; private set; }

    [Header("Tombol Home (satu-satunya tombol di pop-up hasil)")]
    public Button tombolHome;                   // Tombol "Home" (jangan isi On Click di Inspector, dipasang lewat script)

    [Header("Konfirmasi Keluar (muncul saat tombol Home ditekan)")]
    public GameObject boxKonfirmasi;            // Box kalimat: "Apakah Anda ingin keluar dari permainan?" Kosong = tombol Home langsung pindah scene tanpa konfirmasi
    public GameObject boxYa;                    // Box tombol "Ya" (di bawah box kalimat)
    public GameObject boxTidak;                 // Box tombol "Tidak" (di bawah box kalimat)
    public Button tombolYa;                     // Button di box Ya (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolTidak;                  // Button di box Tidak (jangan isi On Click di Inspector, dipasang lewat script)
    [Tooltip("Nama scene menu utama persis seperti di Build Profiles / Scene List (huruf besar kecil berpengaruh). Kosong = tidak pindah scene, hanya event Saat Tombol Home yang jalan.")]
    public string namaSceneMenuUtama = "";
    public UnityEvent saatTombolHome;           // Dipanggil begitu tombol Ya ditekan, sebelum pindah scene

    [Header("Setelah Pindah Scene")]
    public bool resetProgresSaatPindahScene = true;   // true = skor dan integritas direset sebelum pindah scene (permainan sudah tamat, game baru mulai dari awal)

    [Header("Menunggu Aktivitas Selesai (kosong = dicari otomatis)")]
    public GameTimeManager timeManager;
    public LaptopKelas laptopKelas;
    public LaptopInteraction laptopDosen;
    public LaptopPerpusInteraction laptopPerpus;

    private bool sudahTampil = false;
    private bool sudahKeluar = false;
    private bool menerimaTawaran = false;   // pilihan MC di Scene 3 (true = Ya), diisi saat box penutup dipilih
    private float waktuTercapai = -1f;
    private bool sudahDilaporkanMenunggu = false;
    private PlayerMovement pm;

    // Semua kursi di scene: selama sebuah adegan berjalan, kursi MC dikunci (TerkunciEksternal)
    private InteractiveDesk[] semuaKursiDesk;
    private KursiPerpusInteraction[] semuaKursiPerpus;

    void Start()
    {
        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        SembunyikanBoxPenutup();

        TampilkanKonfirmasi(false);

        if (tombolHome != null) tombolHome.onClick.AddListener(BukaKonfirmasiHome);
        if (tombolYa != null) tombolYa.onClick.AddListener(KeHome);
        if (tombolTidak != null) tombolTidak.onClick.AddListener(TutupKonfirmasiHome);

        if (timeManager == null) timeManager = FindFirstObjectByType<GameTimeManager>();
        if (laptopKelas == null) laptopKelas = FindFirstObjectByType<LaptopKelas>();
        if (laptopDosen == null) laptopDosen = FindFirstObjectByType<LaptopInteraction>();
        if (laptopPerpus == null) laptopPerpus = FindFirstObjectByType<LaptopPerpusInteraction>(FindObjectsInactive.Include);
        semuaKursiDesk = FindObjectsByType<InteractiveDesk>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        semuaKursiPerpus = FindObjectsByType<KursiPerpusInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    void OnDestroy()
    {
        if (tombolHome != null) tombolHome.onClick.RemoveListener(BukaKonfirmasiHome);
        if (tombolYa != null) tombolYa.onClick.RemoveListener(KeHome);
        if (tombolTidak != null) tombolTidak.onClick.RemoveListener(TutupKonfirmasiHome);
    }

    private void SembunyikanBoxPenutup()
    {
        SembunyikanDaftar(boxPenutupYa);
        SembunyikanDaftar(boxPenutupTidak);
        if (popupAchievement != null) popupAchievement.SetActive(false);
    }

    private void SembunyikanDaftar(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    // Menyalakan objek badge yang sesuai rank dan mematikan yang lain. Mengembalikan false kalau tidak ada slot objek rank yang diisi
    private bool TampilkanObjekRank(string rank)
    {
        bool adaIsi = objekRankS != null || objekRankA != null || objekRankB != null || objekRankC != null || objekRankF != null;
        if (!adaIsi)
        {
            Debug.LogWarning("[Level 5] Slot Objek Rank S/A/B/C/F belum diisi di Inspector, tidak ada badge yang tampil.");
            return false;
        }

        if (objekRankS != null) objekRankS.SetActive(rank == "S");
        if (objekRankA != null) objekRankA.SetActive(rank == "A");
        if (objekRankB != null) objekRankB.SetActive(rank == "B");
        if (objekRankC != null) objekRankC.SetActive(rank == "C");
        if (objekRankF != null) objekRankF.SetActive(rank == "F");

        GameObject dipakai = null;
        switch (rank)
        {
            case "S": dipakai = objekRankS; break;
            case "A": dipakai = objekRankA; break;
            case "B": dipakai = objekRankB; break;
            case "C": dipakai = objekRankC; break;
            case "F": dipakai = objekRankF; break;
        }

        if (dipakai == null)
            Debug.LogWarning("[Level 5] Objek badge untuk Rank " + rank + " belum diisi di Inspector, tidak ada badge yang tampil.");
        else if (!dipakai.activeInHierarchy)
            Debug.LogWarning("[Level 5] Objek badge '" + dipakai.name + "' sudah dinyalakan tapi induknya nonaktif, jadi tidak tampil. Pastikan objek badge ada di dalam pop-up hasil.");

        return true;
    }

    // Rank akhir dari integritas (dibulatkan seperti angka di top bar): S = 100, A >= 90, B >= 85, C >= 80, F >= 75 (di bawah 75 tetap F)
    private string HitungRank(int persenIntegritas)
    {
        if (persenIntegritas >= batasRankS) return "S";
        if (persenIntegritas >= batasRankA) return "A";
        if (persenIntegritas >= batasRankB) return "B";
        if (persenIntegritas >= batasRankC) return "C";
        return "F";   // batasRankF (75) dan di bawahnya: Rank F
    }

    // Memilih daftar box penutup sesuai pilihan MC di Scene 3 (Ya = menerima tawaran, Tidak = menolak)
    private GameObject[] PilihBoxPenutup()
    {
        if (adeganScene3 == null) adeganScene3 = FindFirstObjectByType<AdeganLevel5Scene3>(FindObjectsInactive.Include);

        if (adeganScene3 == null)
        {
            Debug.LogWarning("[Level 5] AdeganLevel5Scene3 tidak ditemukan, dipakai Box Penutup Tidak.");
            menerimaTawaran = false;
            return boxPenutupTidak;
        }

        if (!adeganScene3.SudahMemilih)
        {
            Debug.LogWarning("[Level 5] MC belum memilih di Scene 3, dipakai Box Penutup Tidak.");
            menerimaTawaran = false;
            return boxPenutupTidak;
        }

        bool menerima = adeganScene3.MenerimaTawaran;
        menerimaTawaran = menerima;
        Debug.Log("[Level 5] MC " + (menerima ? "menerima" : "menolak") + " tawaran di Scene 3, dipakai Box Penutup " + (menerima ? "Ya" : "Tidak") + ".");
        return menerima ? boxPenutupYa : boxPenutupTidak;
    }

    void Update()
    {
        if (sudahTampil) return;
        if (timeManager == null) return;

        // Hanya di level terakhir, dan hanya setelah jam penutup (20.00) tercapai
        bool jamTercapai = timeManager.LevelTerakhirAktif && timeManager.SudahJamPenutup;
        if (!jamTercapai)
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
                Debug.Log("[Level 5] Jam penutup sudah tercapai, penutup menunggu: " + alasan);
            }
            return;
        }

        Tampilkan();
    }

    // Mengembalikan alasan penutup harus menunggu, atau null kalau tidak ada aktivitas yang berjalan
    private string AlasanMasihSibuk()
    {
        if (Time.timeScale == 0f) return "game sedang berhenti (tutorial atau pop-up lain sedang tampil)";
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
    [ContextMenu("Tes Tampilkan Penutup (tanpa menunggu jam 20.00)")]
    private void TesTampilkan()
    {
        if (sudahTampil) return;
        Tampilkan();
    }

    private void Tampilkan()
    {
        if (panelPopup == null)
        {
            Debug.LogWarning("[Level 5] Slot Panel Popup masih kosong di Inspector! Pop-up tidak bisa tampil.");
            return;
        }

        sudahTampil = true;
        StartCoroutine(AlurTampil());
    }

    // Box penutup satu per satu (Space), lalu pop-up hasil
    private IEnumerator AlurTampil()
    {
        Debug.Log("[Level 5] Jam penutup tercapai. Box penutup ditampilkan.");

        if (hentikanWaktuSaatTampil) Time.timeScale = 0f;

        // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
        if (overlayHitam != null)
        {
            overlayHitam.SetActive(true);
            overlayHitam.transform.SetAsLastSibling();
        }

        // 1. Box penutup sesuai pilihan MC di Scene 3, Space (atau klik kiri) untuk lanjut
        GameObject[] daftarBox = PilihBoxPenutup();
        if (daftarBox != null)
        {
            foreach (GameObject box in daftarBox)
            {
                if (box == null) continue;

                box.SetActive(true);
                box.transform.SetAsLastSibling();

                if (!box.activeInHierarchy)
                    Debug.LogWarning("[Level 5] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

                // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
                yield return new WaitForSecondsRealtime(0.25f);

                while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                    yield return null;

                box.SetActive(false);
            }
        }

        // 1b. Popup achievement, hanya kalau MC menolak tawaran (Tidak)
        if (!menerimaTawaran && popupAchievement != null)
        {
            popupAchievement.SetActive(true);
            popupAchievement.transform.SetAsLastSibling();

            if (!popupAchievement.activeInHierarchy)
                Debug.LogWarning("[Level 5] Popup Achievement sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya Space terakhir dari box tidak langsung menutup popup
            yield return new WaitForSecondsRealtime(0.25f);

            float lamaTunggu = 0f;
            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
            {
                if (lamaPopupAchievementOtomatis > 0f)
                {
                    lamaTunggu += Time.unscaledDeltaTime;
                    if (lamaTunggu >= lamaPopupAchievementOtomatis) break;
                }

                yield return null;
            }

            popupAchievement.SetActive(false);

            // Jeda singkat supaya Space yang menutup popup tidak ikut terbaca oleh pop-up hasil
            yield return new WaitForSecondsRealtime(0.1f);
        }

        // 2. Pop-up hasil: poin dan integritas terbaru
        if (teksXP != null && ScoreManager.Instance != null)
            teksXP.text = ScoreManager.Instance.currentScore.ToString();

        // Integritas tampil sebagai angka persen, dan rank (S, A, B, C, F) tampil sebagai gambar badge
        if (IntegritasManager.Instance != null)
        {
            int persen = Mathf.RoundToInt(IntegritasManager.Instance.CurrentIntegritas);
            RankAkhir = HitungRank(persen);
            Debug.Log("[Level 5] Integritas akhir " + persen + "%, rank " + RankAkhir + ".");

            if (teksIntegritas != null) teksIntegritas.text = persen + "%";
            TampilkanObjekRank(RankAkhir);
        }
        else
        {
            RankAkhir = "-";
            if (teksIntegritas != null) teksIntegritas.text = "-";
            if (objekRankS != null) objekRankS.SetActive(false);
            if (objekRankA != null) objekRankA.SetActive(false);
            if (objekRankB != null) objekRankB.SetActive(false);
            if (objekRankC != null) objekRankC.SetActive(false);
            if (objekRankF != null) objekRankF.SetActive(false);
        }

        panelPopup.SetActive(true);
        panelPopup.transform.SetAsLastSibling();

        if (!panelPopup.activeInHierarchy)
            Debug.LogWarning("[Level 5] Panel '" + panelPopup.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil.");

        Debug.Log("[Level 5] Pop-up hasil level 5 ditampilkan.");
    }

    // ---------- Tombol Home dan konfirmasi keluar ----------
    // Menampilkan / menyembunyikan tiga box konfirmasi (box kalimat, box Ya, box Tidak)
    private void TampilkanKonfirmasi(bool tampil)
    {
        if (boxKonfirmasi != null) boxKonfirmasi.SetActive(tampil);
        if (boxYa != null) boxYa.SetActive(tampil);
        if (boxTidak != null) boxTidak.SetActive(tampil);
        if (tombolYa != null) tombolYa.gameObject.SetActive(tampil);
        if (tombolTidak != null) tombolTidak.gameObject.SetActive(tampil);

        if (tampil)
        {
            // Urutan penting: box tampil di atas overlay
            if (boxKonfirmasi != null) boxKonfirmasi.transform.SetAsLastSibling();
            if (boxYa != null) boxYa.transform.SetAsLastSibling();
            if (boxTidak != null) boxTidak.transform.SetAsLastSibling();
        }

        // Selama konfirmasi tampil, tombol Home di belakangnya tidak bisa diklik
        if (tombolHome != null) tombolHome.interactable = !tampil;
    }

    // Dipanggil tombol Home: pop-up hasil disembunyikan, muncul konfirmasi "ingin keluar?" (kalau box konfirmasi diisi), kalau tidak langsung ke home
    public void BukaKonfirmasiHome()
    {
        if (sudahKeluar) return;

        if (boxKonfirmasi == null)
        {
            KeHome();
            return;
        }

        // Pop-up hasil disembunyikan dulu, yang tampil hanya box konfirmasi (overlay hitam tetap ada)
        if (panelPopup != null) panelPopup.SetActive(false);
        TampilkanKonfirmasi(true);
    }

    // Dipanggil tombol Tidak: konfirmasi ditutup, pop-up hasil muncul lagi
    public void TutupKonfirmasiHome()
    {
        TampilkanKonfirmasi(false);

        if (panelPopup != null)
        {
            panelPopup.SetActive(true);
            panelPopup.transform.SetAsLastSibling();
        }
    }

    // Dipanggil tombol Ya: pop-up ditutup, game dilanjutkan (timeScale 1), lalu pindah ke scene menu utama
    public void KeHome()
    {
        if (sudahKeluar) return;   // cegah klik ganda
        sudahKeluar = true;

        TampilkanKonfirmasi(false);
        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        // Waktu dikembalikan supaya scene tujuan tidak ikut berhenti (pop-up menghentikan waktu)
        Time.timeScale = 1f;

        saatTombolHome.Invoke();

        if (string.IsNullOrEmpty(namaSceneMenuUtama))
        {
            Debug.LogWarning("[Level 5] Tombol Home ditekan, tapi Nama Scene Menu Utama masih kosong di Inspector. Scene tidak dipindah.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(namaSceneMenuUtama))
        {
            Debug.LogWarning("[Level 5] Scene '" + namaSceneMenuUtama + "' tidak ditemukan. Cek ejaannya, dan pastikan scene sudah ditambahkan di Build Profiles (Scene List).");
            return;
        }

        if (resetProgresSaatPindahScene)
        {
            ScoreManager.ResetProgres();
            IntegritasManager.ResetProgres();

            // Permainan tamat: progres tersimpan dihapus supaya Continue tidak kembali ke awal level 5
            //ProgresPermainan.Hapus();
        }

        Debug.Log("[Level 5] Tombol Home ditekan, pindah ke scene '" + namaSceneMenuUtama + "'.");
        SceneManager.LoadScene(namaSceneMenuUtama);
    }
}