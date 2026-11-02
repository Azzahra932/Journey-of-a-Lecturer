using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class LaptopKelas : MonoBehaviour
{
    [Header("UI Progress Bar Kerja (Kelas)")]
    public GameObject canvasProgressBar;   // barbulet (bar kerja 3 detik) - milik KELAS
    public Image circularProgressBar;      // fill (Image Type: Filled)

    [Header("Ikon Status Laptop")]
    public GameObject promptPressE;        // ikon "E" -> muncul saat laptop SIAP
    public GameObject ikonBateraiLemah;    // objek induk baterai -> muncul saat COOLDOWN
    public IndikatorBaterai indikatorBaterai;  // script pengisi segmen baterai (pasang di objek baterai)
    public TMP_Text teksCountdown;         // angka hitung mundur (contoh 0:45), opsional
    public GameObject teksIstirahat;       // teks "Laptop sedang istirahat" saat E ditekan, opsional
    public float lamaTeksIstirahat = 1.5f;

    [Header("Petunjuk Arah Setelah Dapat Poin")]
    public GameObject panahKeRuangDosen;   // panah penunjuk jalan ke ruang dosen, muncul setelah dapat poin

    [Header("Box Tutorial + Overlay Hitam (Muncul Setelah Bar Bulat Selesai)")]
    public GameObject overlayHitam;          // Panel hitam transparan satu layar (di belakang box). Kosong = tanpa overlay
    public GameObject boxTutorial;           // Box tutorial buatanmu (gambar + teks sudah jadi). Kosong = tanpa box
    public ChairInteraction kursiKelas;      // Kursi kelas: dikunci supaya player tidak bisa berdiri selama box tampil
    public KeyCode tombolTutupBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public bool hanyaPertamaKali = true;     // true = tutorial hanya muncul saat mengajar pertama kali

    [Header("Box Tutorial Level 2")]
    public GameObject boxTutorialLevel2;     // Box tutorial khusus level 2 (dibuat di Hierarchy). Dipakai setelah MasukLevel2() dipanggil
    public UnityEvent saatMengajarPertamaLevel2Selesai;   // Dipanggil sekali: setelah mengajar pertama di level 2 selesai (tutorial, reward, energi). Dipakai untuk membuka Scene 3

    [Header("Sound Effect Mengetik (selama bar bulat berputar)")]
    public AudioClip suaraKetik;                // File mp3 suara mengetik. Kosong = tanpa suara
    [Range(0f, 1f)] public float volumeSuaraKetik = 1f;
    public bool suaraDiulang = true;            // true = suara diulang terus selama bar berputar
    public AudioSource audioSource;             // Kosong = dibuat otomatis di objek ini

    [Header("Reward (Setelah Mengajar Selesai)")]
    public int indexReward = 1;              // Nomor reward di RewardManager (1 = Ngajar). -1 = tanpa reward

    private bool tutorialSudahTampil = false;
    private bool sedangTutorial = false;   // true selama box tutorial tampil
    private bool level2Aktif = false;      // True setelah MasukLevel2() dipanggil
    private bool mengajarLevel2Dilaporkan = false;   // True setelah event mengajar pertama level 2 dipanggil

    [Header("Efek Layar Laptop")]
    public SpriteRenderer spriteLaptop;    // kosong = ambil dari objek ini
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);  // layar redup

    [Header("Pengaturan Game")]
    public float durasiKerja = 3f;         // detik bar kerja terisi
    public float durasiCooldown = 60f;     // detik laptop istirahat
    public int jumlahPoinReward = 10;
    public float biayaEnergi = 20f;        // energi yang berkurang tiap selesai mengajar
    public EnergyBar energyBar;            // kosong = dicari otomatis
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Syarat Player")]
    [Tooltip("Drag objek Tpngajar (player duduk di kelas). Hanya aktif saat player duduk.")]
    public Transform playerTransform;
    public bool wajibDuduk = true;

    [Header("Deteksi Jarak (Opsional)")]
    public bool cekJarak = false;
    public Transform interactionPoint;
    public float interactionDistance = 3f;

    private enum Status { Siap, Bekerja, Menyelesaikan, Cooldown }
    private Status status = Status.Siap;

    private float progressTimer = 0f;

    // True setelah player pertama kali menyelesaikan tugas mengajar (dipakai Scene 3)
    public bool SudahMengajar { get; private set; } = false;

    // True selama mengajar berjalan sampai semua langkah akhirnya (tutorial, reward) selesai
    public bool SedangSibuk { get { return status == Status.Bekerja || status == Status.Menyelesaikan; } }

    [Header("Level Berikutnya")]
    public bool mulaiModeBerulang = false; // Centang di level 2: langsung tanpa panah petunjuk dan tanpa box tutorial

    private bool modeBerulang = false;     // True setelah Scene 5: tanpa panah petunjuk dan tanpa box tutorial
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;

    // Cegah tombol E untuk DUDUK ikut terbaca sebagai E untuk laptop
    private bool dudukSebelumnya = false;
    private float waktuMulaiDuduk = 0f;
    private const float jedaSetelahDuduk = 0.3f;

    void Start()
    {
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (promptPressE != null) promptPressE.SetActive(false);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);
        if (panahKeRuangDosen != null) panahKeRuangDosen.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxTutorial != null) boxTutorial.SetActive(false);
        if (boxTutorialLevel2 != null) boxTutorialLevel2.SetActive(false);

        if (interactionPoint == null) interactionPoint = transform;
        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();
        if (spriteLaptop == null) spriteLaptop = GetComponent<SpriteRenderer>();
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;

        // Cari Tpngajar otomatis (termasuk yang nonaktif)
        if (playerTransform == null)
        {
            Transform[] semua = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform t in semua)
            {
                if (t.name == "Tpngajar") { playerTransform = t; break; }
            }
        }

        if (playerTransform == null)
            Debug.LogWarning("[LaptopKelas] Player Transform (Tpngajar) tidak ditemukan! Drag manual di Inspector.");

        // Level 2 (scene terpisah): langsung dalam mode berulang
        if (mulaiModeBerulang) BukaAktivitasBerulang();
    }

    // Dipanggil setelah Scene 5 selesai (lewat UnityEvent "Saat Dialog Selesai", opsional)
    // Setelah itu mengajar tidak lagi memunculkan panah petunjuk dan box tutorial
    public void BukaAktivitasBerulang()
    {
        modeBerulang = true;
        if (panahKeRuangDosen != null) panahKeRuangDosen.SetActive(false);
        Debug.Log("[LaptopKelas] Mode berulang aktif (tanpa panah dan tutorial).");
    }

    // Dipanggil saat level 2 dimulai (lewat event "Saat Masuk Level 2" di PopupLevelSelesai):
    // tutorial mengajar muncul lagi dengan box level 2, tanpa panah petunjuk
    public void MasukLevel2()
    {
        level2Aktif = true;
        tutorialSudahTampil = false;

        // Box level 1 diganti box level 2 (kalau box level 2 kosong, tidak ada tutorial di level 2)
        if (boxTutorial != null) boxTutorial.SetActive(false);
        boxTutorial = boxTutorialLevel2;

        if (panahKeRuangDosen != null) panahKeRuangDosen.SetActive(false);

        Debug.Log("[LaptopKelas] Level 2: tutorial memakai box level 2, tanpa panah.");
    }

    // ---------- Sound effect mengetik ----------
    void MulaiSuaraKetik()
    {
        if (suaraKetik == null) return;

        // Buat AudioSource khusus di objek ini (tidak mengganggu AudioSource lain yang mungkin sudah ada)
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;   // suara 2D: terdengar sama di mana pun posisi kamera
        }

        audioSource.clip = suaraKetik;
        audioSource.loop = suaraDiulang;
        audioSource.volume = volumeSuaraKetik;
        audioSource.Play();
    }

    void HentikanSuaraKetik()
    {
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == suaraKetik)
            audioSource.Stop();
    }

    void OnDisable()
    {
        HentikanSuaraKetik();
    }

    bool PlayerBolehInteraksi()
    {
        if (playerTransform == null) return false;
        if (wajibDuduk && !playerTransform.gameObject.activeInHierarchy) return false;

        if (cekJarak)
        {
            float jarak = Vector2.Distance(interactionPoint.position, playerTransform.position);
            if (jarak > interactionDistance) return false;
        }
        return true;
    }

    // Tombol E boleh dipakai untuk laptop hanya setelah jeda singkat sejak player duduk
    bool BolehTekanE()
    {
        return Time.time - waktuMulaiDuduk >= jedaSetelahDuduk;
    }

    void Update()
    {
        // Catat saat player baru saja duduk (Tpngajar baru aktif)
        bool dudukSekarang = playerTransform != null && playerTransform.gameObject.activeInHierarchy;
        if (dudukSekarang && !dudukSebelumnya) waktuMulaiDuduk = Time.time;
        dudukSebelumnya = dudukSekarang;

        // Teks "istirahat" hilang otomatis
        if (teksIstirahat != null && teksIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksIstirahat.SetActive(false);

        switch (status)
        {
            case Status.Siap: UpdateSiap(); break;
            case Status.Bekerja: UpdateBekerja(); break;
            case Status.Cooldown: UpdateCooldown(); break;
        }
    }

    // ---------- SIAP: layar terang, ikon E ----------
    void UpdateSiap()
    {
        bool boleh = PlayerBolehInteraksi();
        if (promptPressE != null) promptPressE.SetActive(boleh);

        if (boleh && BolehTekanE() && Input.GetKeyDown(tombolInteraksi))
            MulaiMengajar();
    }

    void MulaiMengajar()
    {
        status = Status.Bekerja;
        progressTimer = 0f;

        if (promptPressE != null) promptPressE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(true);
        MulaiSuaraKetik();
        if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;
    }

    // ---------- BEKERJA: bar bulat ----------
    void UpdateBekerja()
    {
        // Kalau player berdiri di tengah proses, batalkan (tanpa cooldown, tanpa poin)
        if (!PlayerBolehInteraksi())
        {
            BatalkanKerja();
            return;
        }

        progressTimer += Time.deltaTime;

        if (circularProgressBar != null)
            circularProgressBar.fillAmount = Mathf.Clamp01(progressTimer / durasiKerja);

        if (progressTimer >= durasiKerja)
        {
            HentikanSuaraKetik();
            status = Status.Menyelesaikan;
            StartCoroutine(SelesaiKerja());
        }
    }

    void BatalkanKerja()
    {
        HentikanSuaraKetik();
        status = Status.Siap;
        progressTimer = 0f;

        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;
    }

    // ---------- SELESAI: poin, panah, tutorial, reward, energi, lalu cooldown ----------
    IEnumerator SelesaiKerja()
    {
        if (circularProgressBar != null) circularProgressBar.fillAmount = 1f;
        yield return new WaitForSeconds(0.3f);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);

        // Poin
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoinReward);
            Debug.Log("[LaptopKelas] Mengajar selesai! Skor bertambah +" + jumlahPoinReward);
        }
        else
        {
            Debug.LogWarning("[LaptopKelas] ScoreManager.Instance tidak ditemukan di Scene ini!");
        }

        SudahMengajar = true;

        // Panah ke ruang dosen (hanya level 1 sebelum Scene 5, tidak pernah di level 2)
        if (panahKeRuangDosen != null && !modeBerulang && !level2Aktif) panahKeRuangDosen.SetActive(true);

        // Box tutorial: level 1 hanya sebelum Scene 5, level 2 muncul lagi (box level 2).
        // Hanya pertama kali kalau "Hanya Pertama Kali" dicentang.
        bool tampilkanTutorial = boxTutorial != null && (!modeBerulang || level2Aktif) && (!hanyaPertamaKali || !tutorialSudahTampil);
        if (tampilkanTutorial)
        {
            yield return StartCoroutine(TampilkanTutorial());
        }

        // Reward muncul sendiri, lalu hilang sendiri
        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));
        }

        // Energi berkurang, lalu cek apakah habis (menunggu tutorial dan reward selesai)
        if (energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        // Level 2: mengajar pertama selesai -> beri tahu adegan berikutnya (Scene 3 Research Center)
        if (level2Aktif && !mengajarLevel2Dilaporkan)
        {
            mengajarLevel2Dilaporkan = true;
            saatMengajarPertamaLevel2Selesai.Invoke();
        }

        MulaiCooldown();
    }

    IEnumerator TampilkanTutorial()
    {
        tutorialSudahTampil = true;
        sedangTutorial = true;

        // Kunci kursi supaya player tidak bisa berdiri selama box tampil
        if (kursiKelas != null) kursiKelas.TerkunciEksternal = true;

        // Urutan penting: overlay dulu, baru box
        if (overlayHitam != null) overlayHitam.SetActive(true);
        if (boxTutorial != null) boxTutorial.SetActive(true);

        // Jeda singkat supaya tombol yang sedang ditekan tidak langsung menutup box
        yield return new WaitForSeconds(0.3f);

        while (!(Input.GetKeyDown(tombolTutupBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
            yield return null;

        if (boxTutorial != null) boxTutorial.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        if (kursiKelas != null) kursiKelas.TerkunciEksternal = false;
        sedangTutorial = false;
    }

    // ---------- COOLDOWN: layar redup, baterai terisi ----------
    void MulaiCooldown()
    {
        status = Status.Cooldown;
        cooldownSelesaiPada = Time.time + durasiCooldown;

        if (promptPressE != null) promptPressE.SetActive(false);

        if (indikatorBaterai != null) indikatorBaterai.SetProgress(0f);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(true);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(true);
        if (spriteLaptop != null) spriteLaptop.color = warnaCooldown;
    }

    void UpdateCooldown()
    {
        float sisa = Mathf.Max(0f, cooldownSelesaiPada - Time.time);

        if (indikatorBaterai != null && durasiCooldown > 0f)
            indikatorBaterai.SetProgress(1f - (sisa / durasiCooldown));

        if (teksCountdown != null)
        {
            int menit = Mathf.FloorToInt(sisa / 60f);
            int detik = Mathf.FloorToInt(sisa % 60f);
            teksCountdown.text = $"{menit}:{detik:00}";
        }

        // E saat cooldown: tampilkan teks "laptop sedang istirahat"
        if (teksIstirahat != null && PlayerBolehInteraksi() && BolehTekanE() && Input.GetKeyDown(tombolInteraksi))
        {
            teksIstirahat.SetActive(true);
            teksSembunyiPada = Time.time + lamaTeksIstirahat;
        }

        if (Time.time >= cooldownSelesaiPada)
            SelesaiCooldown();
    }

    void SelesaiCooldown()
    {
        status = Status.Siap;

        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;

        Debug.Log("[LaptopKelas] Laptop menyala lagi, siap dipakai.");
    }
}