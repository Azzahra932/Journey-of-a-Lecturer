using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Memunculkan pop-up "Status Baru" saat poin mencapai target (150), menampilkan XP dan Integritas.
// Tombol Lanjut (tetap di scene yang sama) akan:
//   1. mengganti tampilan MC (animator berjalan/diam, dan sprite MC saat duduk)
//   2. mengganti nama MC di top bar
//   3. mengganti icon MC di top bar
//   4. menaikkan target poin (150 -> 300), jadi top bar tampil 150/300
//   5. membuka ruangan yang terkunci (Ruang C dan D)
public class PopupLevelSelesai : MonoBehaviour
{
    [System.Serializable]
    public class GantiSpriteItem
    {
        public SpriteRenderer target;   // SpriteRenderer objek MC duduk (mis. Tpngajar di kelas, Sit di perpustakaan, MC duduk di ruang dosen)
        public Sprite spriteBaru;       // Sprite MC level 2 versi duduk untuk objek itu
    }

    [Header("Panel Pop-up")]
    public GameObject panelPopup;           // Panel "Status Baru"
    public GameObject overlayHitam;         // Panel hitam transparan di belakang pop-up (opsional)
    public TMP_Text teksXP;                 // Teks angka di sebelah ikon bintang
    public TMP_Text teksIntegritas;         // Teks angka di sebelah ikon perisai
    public Button tombolLanjut;             // Tombol "Lanjut" (jangan isi On Click di Inspector, dipasang lewat script)

    [Header("0. Box Pengantar Level 2 (muncul dulu, baru masuk ruang dosen)")]
    public GameObject[] boxPengantarLevel2;         // Box tampil berurutan setelah Lanjut diklik. Kosong = langsung masuk ruang dosen
    public KeyCode tombolLanjutBox = KeyCode.Space; // Menutup box dan lanjut ke box berikutnya
    public bool bolehKlikMouse = true;              // Klik kiri mouse juga bisa

    [Header("1. Perubahan Karakter MC (Berjalan / Diam)")]
    public SpriteRenderer playerSpriteRenderer;             // SpriteRenderer pemain (dipakai kalau pemain TIDAK pakai Animator)
    public Sprite spriteMCLevel2;                           // Sprite MC level 2
    public Animator[] animatorPemain;                       // Animator pemain (TPdiam, dst). Dipakai kalau MC digerakkan Animator
    public RuntimeAnimatorController animatorControllerLevel2;   // Animator Controller MC level 2

    [Header("1b. Sprite MC Saat Duduk (Kursi Dosen, Kelas, Perpustakaan)")]
    public GantiSpriteItem[] spriteDuduk;                   // Satu elemen per objek MC duduk

    [Header("1c. Sprite Duduk di Kursi Dosen (InteractiveDesk)")]
    public InteractiveDesk kursiDosen;                      // Objek yang memasang script Interactive Desk (kursi dosen)
    public Sprite spriteDudukDosenLevel2;                   // Sprite MC level 2 saat duduk di kursi dosen (tampak belakang)

    [Header("2 & 3. Nama & Icon MC di Top Bar")]
    public TMP_Text teksNamaMCTopBar;       // Teks nama MC di top bar
    public string namaBaruMC = "Asisten Ahli";
    public Image iconTopBar;                // Image icon MC di top bar
    public Sprite iconLevel2;               // Sprite icon level 2

    [Header("4. Target Poin Level 2 (150 / 300)")]
    public int targetSkorLevel2 = 300;      // 0 = target tidak diubah

    [Header("5. Membuka Ruangan C & D")]
    public RuangTerkunci[] ruangYangDibuka; // Isi dengan objek yang memasang script RuangTerkunci (Ruang C dan D)

    [Header("6. Mulai Level 2 di Ruang Dosen (Energi Penuh)")]
    public bool kembaliKeAwalSaatLanjut = true;   // MC dipindah ke titik awal (ruang dosen) lewat transisi layar hitam
    public TransisiKembaliKeAwal transisi;        // Kosong = dicari otomatis
    public EnergyBar energyBar;                   // Kosong = dicari otomatis
    public GameTimeManager timeManager;           // Kosong = dicari otomatis. Jam dikembalikan ke 08.00 saat level 2 dimulai
    public UnityEvent saatMasukLevel2;            // Opsional: dipanggil setelah MC berada di titik awal (untuk hal tambahan lain)

    [Header("7. Ganti Objek di Level 2 (mis. buku diganti laptop)")]
    public GameObject[] objekDisembunyikanLevel2;   // Objek yang HILANG di level 2 (mis. buku perpustakaan, ikon E buku, bar cooldown buku)
    public GameObject[] objekDimunculkanLevel2;     // Objek yang MUNCUL di level 2 (mis. laptop perpustakaan)

    [Header("Tutorial")]
    public TutorialManager tutorialManager;     // Kosong = dicari otomatis. Di Level 2, step tutorial top bar dilewati

    [Header("Waktu Muncul")]
    public float jedaSebelumPopup = 1.0f;       // Jeda (detik) setelah poin 150 tercapai
    public BukuInteraction buku;                // Opsional: pop-up menunggu aktivitas buku selesai
    public LaptopInteraction laptopDosen;       // Opsional: pop-up menunggu tugas RPS selesai
    public bool hentikanWaktuSaatTampil = true; // Game berhenti selama pop-up tampil

    private bool sudahTampil = false;
    private bool sudahLanjut = false;
    private float waktuTercapai = -1f;

    void Start()
    {
        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxPengantarLevel2 != null)
        {
            foreach (GameObject b in boxPengantarLevel2)
            {
                if (b != null) b.SetActive(false);
            }
        }
        if (tombolLanjut != null) tombolLanjut.onClick.AddListener(Lanjut);
        if (tutorialManager == null) tutorialManager = FindFirstObjectByType<TutorialManager>();
        if (transisi == null) transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();
        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();
        if (timeManager == null) timeManager = FindFirstObjectByType<GameTimeManager>();
    }

    void OnDestroy()
    {
        if (tombolLanjut != null) tombolLanjut.onClick.RemoveListener(Lanjut);
    }

    void Update()
    {
        if (sudahTampil || ScoreManager.Instance == null) return;

        bool tercapai = ScoreManager.Instance.currentScore >= ScoreManager.Instance.targetScore;
        if (!tercapai)
        {
            waktuTercapai = -1f;
            return;
        }

        if (waktuTercapai < 0f) waktuTercapai = Time.time;
        if (Time.time - waktuTercapai < jedaSebelumPopup) return;

        // Tunggu aktivitas yang sedang berjalan selesai (supaya integritas akhir sudah terhitung)
        if (buku != null && buku.SedangSibuk) return;
        if (laptopDosen != null && laptopDosen.SedangSibuk) return;

        Tampilkan();
    }

    private void Tampilkan()
    {
        sudahTampil = true;

        if (teksXP != null)
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
            Debug.LogWarning("[Level] Slot Panel Popup masih kosong di Inspector! Pop-up tidak bisa tampil.");
            return;
        }

        // Urutan penting: overlay dulu, baru panel (panel harus tampil di atas overlay)
        if (overlayHitam != null) overlayHitam.SetActive(true);
        panelPopup.SetActive(true);

        if (hentikanWaktuSaatTampil) Time.timeScale = 0f;

        Debug.Log("[Level] Target poin tercapai. Pop-up status baru ditampilkan.");
    }

    // Dipanggil tombol Lanjut: masuk ke level 2 tanpa pindah scene
    public void Lanjut()
    {
        if (sudahLanjut) return;   // cegah klik ganda
        sudahLanjut = true;

        if (panelPopup != null) panelPopup.SetActive(false);

        StartCoroutine(AlurLanjut());
    }

    private IEnumerator AlurLanjut()
    {
        // 1. Pop-up dan overlay ditutup, waktu berjalan lagi supaya transisi layar hitam bisa jalan
        Time.timeScale = 1f;
        if (overlayHitam != null) overlayHitam.SetActive(false);

        // 2. Terapkan perubahan level 2
        GantiKarakterMC();
        GantiSpriteMCDuduk();
        GantiNamaDanIconTopBar();
        NaikkanTargetSkor();
        BukaRuangan();
        GantiObjekLevel2();

        // Riwayat reward level 1 dikosongkan, level 2 mulai dari awal
        if (RewardManager.Instance != null) RewardManager.Instance.MulaiLevelBaru();

        // 3. Transisi layar hitam: MC kembali ke ruang dosen (titik awal).
        //    Box pengantar dan tutorial baru muncul SETELAH transisi selesai.
        if (kembaliKeAwalSaatLanjut && transisi != null && !transisi.SedangTransisi)
        {
            transisi.saatTransisiSelesai.AddListener(SetelahTransisiLevel2);
            transisi.MulaiTransisi();
        }
        else
        {
            if (kembaliKeAwalSaatLanjut && transisi == null)
                Debug.LogWarning("[Level] TransisiKembaliKeAwal tidak ditemukan, MC tidak dipindah ke titik awal.");

            SetelahTransisiLevel2();
        }

        Debug.Log("[Level] Masuk Level 2.");
        yield break;
    }

    // Dijalankan setelah transisi selesai dan MC berada di titik awal level 2
    private void SetelahTransisiLevel2()
    {
        if (transisi != null) transisi.saatTransisiSelesai.RemoveListener(SetelahTransisiLevel2);

        // Energi penuh
        if (energyBar != null) energyBar.IsiPenuh();

        // Jam kembali ke 08.00 pagi
        if (timeManager != null) timeManager.ResetKeJamPagi();

        StartCoroutine(AlurBoxLaluTutorial());
    }

    // Setelah transisi: box pengantar (tunggu Space), lalu tutorial menuju Scene 1
    private IEnumerator AlurBoxLaluTutorial()
    {
        if (boxPengantarLevel2 != null && boxPengantarLevel2.Length > 0)
        {
            // Game berhenti dan layar digelapkan selama box tampil
            Time.timeScale = 0f;
            if (overlayHitam != null) overlayHitam.SetActive(true);

            foreach (GameObject box in boxPengantarLevel2)
            {
                if (box == null) continue;

                box.SetActive(true);

                // Jeda singkat supaya tombol yang masih tertekan tidak langsung menutup box
                yield return new WaitForSecondsRealtime(0.25f);

                while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                    yield return null;

                box.SetActive(false);
            }

            if (overlayHitam != null) overlayHitam.SetActive(false);
            Time.timeScale = 1f;
        }

        // Hal tambahan yang dihubungkan lewat Inspector
        saatMasukLevel2.Invoke();

        // Tutorial setelah top bar tampil lagi, penjelasan top bar dilewati
        if (tutorialManager != null) tutorialManager.MasukLevel2();

        Debug.Log("[Level] MC di titik awal level 2, energi penuh, jam 08.00.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Lanjut (tanpa menunggu 150 poin)")]
    private void TesLanjut()
    {
        Lanjut();
    }

    // 1. Ganti tampilan MC berjalan / diam
    private void GantiKarakterMC()
    {
        // Kalau MC digerakkan Animator, sprite harus diganti lewat Animator Controller,
        // kalau tidak, Animator akan menimpa sprite setiap frame.
        if (animatorControllerLevel2 != null && animatorPemain != null)
        {
            foreach (Animator a in animatorPemain)
            {
                if (a != null) a.runtimeAnimatorController = animatorControllerLevel2;
            }
        }

        if (playerSpriteRenderer != null && spriteMCLevel2 != null)
            playerSpriteRenderer.sprite = spriteMCLevel2;
    }

    // 1b. Ganti sprite MC versi duduk di tiap kursi
    private void GantiSpriteMCDuduk()
    {
        // Kursi dosen: InteractiveDesk memakai "Sit Sprite" untuk menggantikan gambar pemain saat duduk
        if (kursiDosen != null && spriteDudukDosenLevel2 != null)
        {
            kursiDosen.sitSprite = spriteDudukDosenLevel2;
            Debug.Log("[Level] Sprite duduk kursi dosen diganti.");
        }

        if (spriteDuduk == null) return;

        foreach (GantiSpriteItem item in spriteDuduk)
        {
            if (item == null || item.target == null || item.spriteBaru == null) continue;

            item.target.sprite = item.spriteBaru;
            Debug.Log("[Level] Sprite duduk '" + item.target.name + "' diganti.");
        }
    }

    // 2 & 3. Nama dan icon MC di top bar
    private void GantiNamaDanIconTopBar()
    {
        if (teksNamaMCTopBar != null && !string.IsNullOrEmpty(namaBaruMC))
            teksNamaMCTopBar.text = namaBaruMC;

        if (iconTopBar != null && iconLevel2 != null)
            iconTopBar.sprite = iconLevel2;
    }

    // 4. Target poin jadi 300, skor tetap 150 -> top bar tampil 150/300
    private void NaikkanTargetSkor()
    {
        if (targetSkorLevel2 <= 0 || ScoreManager.Instance == null) return;

        ScoreManager.Instance.targetScore = targetSkorLevel2;

        if (ScoreManager.Instance.scoreSlider != null)
            ScoreManager.Instance.scoreSlider.maxValue = targetSkorLevel2;

        ScoreManager.Instance.AddScore(0);   // perbarui teks dan bar poin
    }

    // 7. Ganti objek: sebagian dihilangkan, sebagian dimunculkan
    private void GantiObjekLevel2()
    {
        if (objekDisembunyikanLevel2 != null)
        {
            foreach (GameObject o in objekDisembunyikanLevel2)
            {
                if (o != null) o.SetActive(false);
            }
        }

        if (objekDimunculkanLevel2 != null)
        {
            foreach (GameObject o in objekDimunculkanLevel2)
            {
                if (o != null) o.SetActive(true);
            }
        }
    }

    // 5. Buka ruangan yang terkunci
    private void BukaRuangan()
    {
        if (ruangYangDibuka == null) return;

        foreach (RuangTerkunci r in ruangYangDibuka)
        {
            if (r != null) r.Buka();
        }
    }
}