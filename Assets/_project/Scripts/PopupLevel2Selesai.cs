using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Pop-up "Level 2 selesai" (popuplv2): muncul saat poin mencapai target level 2 (300), menampilkan XP dan Integritas.
// Pop-up punya dua tombol: Home (ikon rumah, muncul konfirmasi keluar lalu pindah ke menu utama) dan Lanjut.
// Tombol Lanjut (tetap di scene yang sama, seperti level 1 ke level 2) akan:
//   1. mengganti tampilan MC (animator berjalan/diam, dan sprite MC saat duduk)
//   2. mengganti nama MC di top bar (Lektor)
//   3. mengganti icon MC di top bar
//   4. menaikkan target poin (300 -> target level 3)
//   5. membuka ruangan yang terkunci (Ruang A)
//   6. transisi layar hitam: MC kembali ke ruang dosen, energi penuh, jam 08.00
//   7. box pengantar level 3 (opsional), lalu event "Saat Masuk Level 3"
// Kalau poin tercapai saat sebuah aktivitas masih berjalan (laptop, podium, tutorial), pop-up menunggu sampai aktivitas itu selesai.
//
// Pasang script ini di satu objek kosong (mis. "ManagerLevel2Selesai").
public class PopupLevel2Selesai : MonoBehaviour
{
    [System.Serializable]
    public class GantiSpriteItem
    {
        public SpriteRenderer target;   // SpriteRenderer objek MC duduk (mis. Tpngajar di kelas, Sit di perpustakaan)
        public Sprite spriteBaru;       // Sprite MC level 3 versi duduk untuk objek itu
    }

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

    [Header("Tombol Home (ikon rumah di pop-up)")]
    public Button tombolHome;                   // Tombol "Home" (jangan isi On Click di Inspector, dipasang lewat script). Kosong = tombol home tidak dipakai

    [Header("Konfirmasi Keluar (muncul saat tombol Home ditekan)")]
    public GameObject boxKonfirmasi;            // Box kalimat: "Apakah Anda ingin keluar dari permainan?" Kosong = tombol Home langsung pindah scene tanpa konfirmasi
    public GameObject boxYa;                    // Box tombol "Ya" (di bawah box kalimat)
    public GameObject boxTidak;                 // Box tombol "Tidak" (di bawah box kalimat)
    public Button tombolYa;                     // Button di box Ya: ke halaman home (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolTidak;                  // Button di box Tidak: kembali ke pop-up (jangan isi On Click di Inspector, dipasang lewat script)
    [Tooltip("Nama scene menu utama persis seperti di Build Profiles / Scene List (huruf besar kecil berpengaruh). Kosong = tidak pindah scene, hanya event Saat Tombol Home yang jalan.")]
    public string namaSceneMenuUtama = "";
    public UnityEvent saatTombolHome;           // Dipanggil begitu tombol Ya ditekan (mis. menyimpan data), sebelum pindah scene

    [Header("Menunggu Aktivitas Selesai (kosong = dicari otomatis)")]
    public LaptopKelas laptopKelas;
    public LaptopInteraction laptopDosen;
    public LaptopPerpusInteraction laptopPerpus;

    [Header("0. Box Pengantar Level 3 (opsional, muncul setelah MC kembali ke ruang dosen)")]
    public GameObject[] boxPengantarLevel3;         // Box tampil berurutan setelah transisi. Kosong = langsung lanjut
    public KeyCode tombolLanjutBox = KeyCode.Space; // Menutup box dan lanjut ke box berikutnya
    public bool bolehKlikMouse = true;              // Klik kiri mouse juga bisa

    [Header("1. Perubahan Karakter MC Level 3 (Berjalan / Diam)")]
    public SpriteRenderer playerSpriteRenderer;             // SpriteRenderer pemain (dipakai kalau pemain TIDAK pakai Animator)
    public Sprite spriteMCLevel3;                           // Sprite MC level 3
    public Animator[] animatorPemain;                       // Animator pemain (TPdiam, dst)
    public RuntimeAnimatorController animatorControllerLevel3;   // Animator (Override) Controller MC level 3

    [Header("1b. Sprite MC Saat Duduk (Kelas, Perpustakaan, dst)")]
    public GantiSpriteItem[] spriteDuduk;                   // Satu elemen per objek MC duduk

    [Header("1c. Sprite Duduk di Kursi Dosen (InteractiveDesk)")]
    public InteractiveDesk kursiDosen;                      // Objek yang memasang script Interactive Desk (kursi dosen)
    public Sprite spriteDudukDosenLevel3;                   // Sprite MC level 3 saat duduk di kursi dosen

    [Header("2 & 3. Nama & Icon MC di Top Bar")]
    public TMP_Text teksNamaMCTopBar;           // Teks nama MC di top bar
    public string namaBaruMC = "Lektor";
    public Image iconTopBar;                    // Image icon MC di top bar
    public Sprite iconLevel3;                   // Sprite icon level 3

    [Header("4. Target Poin Level 3")]
    public int targetSkorLevel3 = 450;          // Target poin level 3 (isi sesuai desain). 0 = target tidak diubah

    [Header("5. Membuka Ruangan (Ruang A)")]
    public RuangTerkunci[] ruangYangDibuka;     // Isi dengan objek yang memasang script RuangTerkunci untuk Ruang A

    [Header("6. Mulai Level 3 di Ruang Dosen (Energi Penuh)")]
    public bool kembaliKeAwalSaatLanjut = true;   // MC dipindah ke titik awal (ruang dosen) lewat transisi layar hitam
    public TransisiKembaliKeAwal transisi;        // Kosong = dicari otomatis
    public EnergyBar energyBar;                   // Kosong = dicari otomatis
    public GameTimeManager timeManager;           // Kosong = dicari otomatis. Jam dikembalikan ke 08.00 saat level 3 dimulai

    [Header("Saat Tombol Lanjut / Masuk Level 3")]
    public UnityEvent saatLanjut;               // Dipanggil begitu tombol Lanjut ditekan
    public UnityEvent saatMasukLevel3;          // Dipanggil setelah MC di titik awal dan box pengantar selesai (untuk menyambung adegan Level 3)

    private bool sudahTampil = false;
    private bool sudahLanjut = false;
    private float waktuTercapai = -1f;
    private bool sudahDilaporkanMenunggu = false;
    private PlayerMovement pm;

    void Start()
    {
        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxPengantarLevel3 != null)
        {
            foreach (GameObject b in boxPengantarLevel3)
            {
                if (b != null) b.SetActive(false);
            }
        }

        TampilkanKonfirmasi(false);

        if (tombolLanjut != null) tombolLanjut.onClick.AddListener(Lanjut);
        if (tombolHome != null) tombolHome.onClick.AddListener(BukaKonfirmasiHome);
        if (tombolYa != null) tombolYa.onClick.AddListener(KeHome);
        if (tombolTidak != null) tombolTidak.onClick.AddListener(TutupKonfirmasiHome);

        if (laptopKelas == null) laptopKelas = FindFirstObjectByType<LaptopKelas>();
        if (laptopDosen == null) laptopDosen = FindFirstObjectByType<LaptopInteraction>();
        if (laptopPerpus == null) laptopPerpus = FindFirstObjectByType<LaptopPerpusInteraction>(FindObjectsInactive.Include);
        if (transisi == null) transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();
        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();
        if (timeManager == null) timeManager = FindFirstObjectByType<GameTimeManager>();
    }

    void OnDestroy()
    {
        if (tombolLanjut != null) tombolLanjut.onClick.RemoveListener(Lanjut);
        if (tombolHome != null) tombolHome.onClick.RemoveListener(BukaKonfirmasiHome);
        if (tombolYa != null) tombolYa.onClick.RemoveListener(KeHome);
        if (tombolTidak != null) tombolTidak.onClick.RemoveListener(TutupKonfirmasiHome);
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

    [ContextMenu("Tes Lanjut ke Level 3 (tanpa pop-up)")]
    private void TesLanjut()
    {
        sudahTampil = true;
        Lanjut();
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

        // Selama konfirmasi tampil, tombol Home dan Lanjut tidak bisa diklik
        if (tombolHome != null) tombolHome.interactable = !tampil;
        if (tombolLanjut != null) tombolLanjut.interactable = !tampil;
    }

    // Dipanggil tombol Home: pop-up disembunyikan, muncul konfirmasi "ingin keluar?" (kalau box konfirmasi diisi), kalau tidak langsung ke home
    public void BukaKonfirmasiHome()
    {
        if (sudahLanjut) return;

        if (boxKonfirmasi == null)
        {
            KeHome();
            return;
        }

        // Pop-up disembunyikan dulu, yang tampil hanya box konfirmasi (overlay hitam tetap ada)
        if (panelPopup != null) panelPopup.SetActive(false);
        TampilkanKonfirmasi(true);
    }

    // Dipanggil tombol Tidak: konfirmasi ditutup, pop-up muncul lagi
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
        if (sudahLanjut) return;   // cegah klik ganda, dan tidak bentrok dengan tombol Lanjut
        sudahLanjut = true;

        TampilkanKonfirmasi(false);
        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        // Waktu dikembalikan supaya scene tujuan tidak ikut berhenti (pop-up menghentikan waktu)
        Time.timeScale = 1f;

        saatTombolHome.Invoke();

        if (string.IsNullOrEmpty(namaSceneMenuUtama))
        {
            Debug.LogWarning("[Level 2] Tombol Home ditekan, tapi Nama Scene Menu Utama masih kosong di Inspector. Scene tidak dipindah.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(namaSceneMenuUtama))
        {
            Debug.LogWarning("[Level 2] Scene '" + namaSceneMenuUtama + "' tidak ditemukan. Cek ejaannya, dan pastikan scene sudah ditambahkan di Build Profiles (Scene List).");
            return;
        }

        Debug.Log("[Level 2] Tombol Home ditekan, pindah ke scene '" + namaSceneMenuUtama + "'.");
        SceneManager.LoadScene(namaSceneMenuUtama);
    }

    // ---------- Tombol Lanjut ----------

    // Dipanggil tombol Lanjut: masuk ke level 3 tanpa pindah scene
    public void Lanjut()
    {
        if (sudahLanjut) return;   // cegah klik ganda
        sudahLanjut = true;

        if (panelPopup != null) panelPopup.SetActive(false);

        saatLanjut.Invoke();

        StartCoroutine(AlurLanjut());
    }

    private IEnumerator AlurLanjut()
    {
        // 1. Pop-up dan overlay ditutup, waktu berjalan lagi supaya transisi layar hitam bisa jalan
        Time.timeScale = 1f;
        if (overlayHitam != null) overlayHitam.SetActive(false);

        // 2. Terapkan perubahan level 3
        GantiKarakterMC();
        GantiSpriteMCDuduk();
        GantiNamaDanIconTopBar();
        NaikkanTargetSkor();
        BukaRuangan();

        // Riwayat reward dikosongkan, level 3 mulai dari awal
        if (RewardManager.Instance != null) RewardManager.Instance.MulaiLevelBaru();

        // 3. Transisi layar hitam: MC kembali ke ruang dosen (titik awal)
        if (kembaliKeAwalSaatLanjut && transisi != null && !transisi.SedangTransisi)
        {
            transisi.saatTransisiSelesai.AddListener(SetelahTransisiLevel3);
            transisi.MulaiTransisi();
        }
        else
        {
            if (kembaliKeAwalSaatLanjut && transisi == null)
                Debug.LogWarning("[Level 3] TransisiKembaliKeAwal tidak ditemukan, MC tidak dipindah ke titik awal.");

            SetelahTransisiLevel3();
        }

        Debug.Log("[Level 3] Masuk Level 3.");
        yield break;
    }

    // Dijalankan setelah transisi selesai dan MC berada di titik awal level 3
    private void SetelahTransisiLevel3()
    {
        if (transisi != null) transisi.saatTransisiSelesai.RemoveListener(SetelahTransisiLevel3);

        // Energi penuh
        if (energyBar != null) energyBar.IsiPenuh();

        // Jam kembali ke 08.00 pagi
        if (timeManager != null) timeManager.ResetKeJamPagi();

        StartCoroutine(AlurBoxPengantar());
    }

    // Setelah transisi: box pengantar (tunggu Space), lalu event masuk level 3
    private IEnumerator AlurBoxPengantar()
    {
        if (boxPengantarLevel3 != null && boxPengantarLevel3.Length > 0)
        {
            // Game berhenti dan layar digelapkan selama box tampil
            Time.timeScale = 0f;
            if (overlayHitam != null)
            {
                overlayHitam.SetActive(true);
                overlayHitam.transform.SetAsLastSibling();
            }

            foreach (GameObject box in boxPengantarLevel3)
            {
                if (box == null) continue;

                box.SetActive(true);
                box.transform.SetAsLastSibling();

                // Jeda singkat supaya tombol yang masih tertekan tidak langsung menutup box
                yield return new WaitForSecondsRealtime(0.25f);

                while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                    yield return null;

                box.SetActive(false);
            }

            if (overlayHitam != null) overlayHitam.SetActive(false);
            Time.timeScale = 1f;
        }

        // Hal tambahan yang dihubungkan lewat Inspector (mis. adegan pertama Level 3)
        saatMasukLevel3.Invoke();

        Debug.Log("[Level 3] MC di titik awal level 3, energi penuh, jam 08.00.");
    }

    // 1. Ganti tampilan MC berjalan / diam
    private void GantiKarakterMC()
    {
        // Kalau MC digerakkan Animator, sprite harus diganti lewat Animator Controller,
        // kalau tidak, Animator akan menimpa sprite setiap frame.
        if (animatorControllerLevel3 != null && animatorPemain != null)
        {
            foreach (Animator a in animatorPemain)
            {
                if (a != null) a.runtimeAnimatorController = animatorControllerLevel3;
            }
        }

        if (playerSpriteRenderer != null && spriteMCLevel3 != null)
            playerSpriteRenderer.sprite = spriteMCLevel3;
    }

    // 1b & 1c. Ganti sprite MC versi duduk di tiap kursi
    private void GantiSpriteMCDuduk()
    {
        // Kursi dosen: InteractiveDesk memakai "Sit Sprite" untuk menggantikan gambar pemain saat duduk
        if (kursiDosen != null && spriteDudukDosenLevel3 != null)
        {
            kursiDosen.sitSprite = spriteDudukDosenLevel3;
            Debug.Log("[Level 3] Sprite duduk kursi dosen diganti.");
        }

        if (spriteDuduk == null) return;

        foreach (GantiSpriteItem item in spriteDuduk)
        {
            if (item == null || item.target == null || item.spriteBaru == null) continue;

            item.target.sprite = item.spriteBaru;
            Debug.Log("[Level 3] Sprite duduk '" + item.target.name + "' diganti.");
        }
    }

    // 2 & 3. Nama dan icon MC di top bar
    private void GantiNamaDanIconTopBar()
    {
        if (teksNamaMCTopBar != null && !string.IsNullOrEmpty(namaBaruMC))
            teksNamaMCTopBar.text = namaBaruMC;

        if (iconTopBar != null && iconLevel3 != null)
            iconTopBar.sprite = iconLevel3;
    }

    // 4. Target poin naik, skor tetap -> top bar tampil mis. 300/450
    private void NaikkanTargetSkor()
    {
        if (targetSkorLevel3 <= 0 || ScoreManager.Instance == null) return;

        ScoreManager.Instance.targetScore = targetSkorLevel3;

        if (ScoreManager.Instance.scoreSlider != null)
            ScoreManager.Instance.scoreSlider.maxValue = targetSkorLevel3;

        ScoreManager.Instance.AddScore(0);   // perbarui teks dan bar poin
    }

    // 5. Buka ruangan yang terkunci (Ruang A)
    private void BukaRuangan()
    {
        if (ruangYangDibuka == null) return;

        foreach (RuangTerkunci r in ruangYangDibuka)
        {
            if (r != null) r.Buka();
        }
    }
}