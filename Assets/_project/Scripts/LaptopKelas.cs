using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

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

    private bool tutorialSudahTampil = false;
    private bool sedangTutorial = false;   // true selama box tutorial tampil (cek energi menunggu sampai box ditutup)

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

    private enum Status { Siap, Bekerja, Cooldown }
    private Status status = Status.Siap;

    private float progressTimer = 0f;

    // True setelah player pertama kali menyelesaikan tugas mengajar (dipakai Scene 3)
    public bool SudahMengajar { get; private set; } = false;
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
            SelesaiMengajar();
    }

    // ---------- COOLDOWN: layar redup, baterai lemah, hitung mundur ----------
    void UpdateCooldown()
    {
        float sisa = cooldownSelesaiPada - Time.time;

        if (teksCountdown != null)
        {
            int detik = Mathf.Max(0, Mathf.CeilToInt(sisa));
            teksCountdown.text = (detik / 60) + ":" + (detik % 60).ToString("00");
        }

        // Isi baterai bertahap (0 = kosong, 1 = penuh)
        if (indikatorBaterai != null)
            indikatorBaterai.SetProgress(Mathf.Clamp01(1f - (sisa / durasiCooldown)));

        // Player tekan E saat laptop masih istirahat
        if (PlayerBolehInteraksi() && BolehTekanE() && Input.GetKeyDown(tombolInteraksi))
        {
            if (teksIstirahat != null)
            {
                teksIstirahat.SetActive(true);
                teksSembunyiPada = Time.time + lamaTeksIstirahat;
            }
        }

        if (sisa <= 0f)
            SelesaiCooldown();
    }

    // ---------- Transisi ----------
    void MulaiMengajar()
    {
        Debug.Log("[LaptopKelas] Mulai mengajar.");

        status = Status.Bekerja;
        progressTimer = 0f;

        if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;
        if (promptPressE != null) promptPressE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(true);
    }

    void BatalkanKerja()
    {
        status = Status.Siap;
        progressTimer = 0f;

        if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
    }

    void SelesaiMengajar()
    {
        if (circularProgressBar != null) circularProgressBar.fillAmount = 1f;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoinReward);
            Debug.Log("[LaptopKelas] Tugas di kelas selesai! Skor bertambah +" + jumlahPoinReward);
        }
        else
        {
            Debug.LogWarning("[LaptopKelas] ScoreManager.Instance tidak ditemukan di Scene ini!");
        }

        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);

        SudahMengajar = true;

        // Energi berkurang setelah mengajar
        if (energyBar != null) energyBar.DecreaseEnergy(biayaEnergi);

        // Munculkan panah penunjuk arah ke ruang dosen
        // Box tutorial + overlay hitam (panah menyala setelah box ditutup)
        bool tampilkanTutorial = (overlayHitam != null || boxTutorial != null)
                                 && (!tutorialSudahTampil || !hanyaPertamaKali);

        if (tampilkanTutorial)
        {
            tutorialSudahTampil = true;
            StartCoroutine(TampilkanTutorial());
        }
        else if (panahKeRuangDosen != null)
        {
            panahKeRuangDosen.SetActive(true);
        }

        MulaiCooldown();

        // Cek energi habis (menunggu box tutorial ditutup kalau sedang tampil)
        StartCoroutine(CekEnergiHabisSetelahSelesai());
    }

    void MulaiCooldown()
    {
        status = Status.Cooldown;
        cooldownSelesaiPada = Time.time + durasiCooldown;

        // Layar redup + baterai lemah + angka hitung mundur
        if (promptPressE != null) promptPressE.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaCooldown;
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(true);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(true);
        if (indikatorBaterai != null) indikatorBaterai.SetProgress(0f);
    }

    // Overlay hitam + box tutorial. Ditutup dengan Space (atau klik). Setelah itu panah ke ruang dosen menyala.
    private IEnumerator TampilkanTutorial()
    {
        sedangTutorial = true;
        if (kursiKelas != null) kursiKelas.TerkunciEksternal = true;

        // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
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

        if (panahKeRuangDosen != null) panahKeRuangDosen.SetActive(true);
    }

    // Menunggu box tutorial ditutup, lalu cek apakah energi habis (kalau habis: transisi hitam)
    private IEnumerator CekEnergiHabisSetelahSelesai()
    {
        while (sedangTutorial) yield return null;

        yield return new WaitForSeconds(0.3f);

        if (energyBar != null) energyBar.CekEnergiHabis();
    }

    void SelesaiCooldown()
    {
        Debug.Log("[LaptopKelas] Laptop menyala lagi, siap dipakai.");

        status = Status.Siap;

        // Layar menyala lagi, ikon baterai hilang, ikon E akan muncul di UpdateSiap
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);
    }
}