using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Level 2 - Scene 6 (Melakukan pengabdian), Lokasi: Community Center
//   1. Scene sebelumnya selesai -> SiapkanScene() dipanggil (Mitra Pengabdian muncul)
//   2. MC mendekati kursi dan menekan Space untuk duduk
//   3. PERTAMA KALI: dua box dialog (Mitra Pengabdian, lalu Pemain), Space untuk lanjut
//   4. Ikon E muncul. Tekan E: bar hijau berputar -> poin +5 -> reward -> energi berkurang -> cooldown (bar bulat)
//   5. Setelah cooldown selesai, kegiatan bisa dikerjakan lagi (tanpa dialog)
//
// Pasang script ini di objek mana saja yang selalu aktif (mis. objek kosong "AdeganPengabdian").
// Kursinya memakai komponen KursiPerpusInteraction (Space untuk duduk, W/A/S/D untuk berdiri).
public class AdeganLevel2Scene6 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan scene sebelumnya
    public GameObject mitraObjek;               // Opsional: Mitra Pengabdian. Muncul saat Scene 6 siap. Kosong = sudah ada dari awal

    [Header("Kursi")]
    public KursiPerpusInteraction kursiPengabdian;   // Kursi di community center (MC duduk sendiri dengan Space)

    [Header("Dialog Pertama (Mitra Pengabdian lalu Pemain)")]
    public GameObject[] boxDialog;              // Dua box dialog, dibuat di Hierarchy
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;

    [Header("Kegiatan (Tekan E)")]
    public GameObject ikonE;                    // Ikon E (objek khusus Scene 6)
    public KeyCode tombolInteraksi = KeyCode.E;
    public GameObject canvasProgressBar;        // Objek induk bar hijau
    public Image circularProgressBar;           // Image bar (Image Type: Filled)
    public float durasiBar = 5f;                // Lama bar berputar (detik)

    [Header("Hasil Setiap Selesai")]
    public int jumlahPoin = 5;                  // Poin yang didapat
    public int indexReward = 7;                 // Nomor reward di RewardManager (7 = Pengabdian, elemen ke-8; 6 = Seminar). -1 = tanpa reward
    public float biayaEnergi = 10f;             // Energi yang berkurang
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Box Tutorial Setelah Bar Selesai")]
    public GameObject[] boxTutorial;            // Box tutorial (dibuat di Hierarchy), urut dari atas. Kosong = tanpa tutorial
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public bool hentikanWaktuSaatTutorial = true;
    public bool tutorialHanyaPertamaKali = true;   // true = tutorial hanya muncul di kegiatan pertama

    [Header("Cooldown (Bar Bulat)")]
    public float durasiCooldown = 45f;          // Jeda (detik) sebelum kegiatan bisa dikerjakan lagi
    public GameObject barCooldownBulat;         // Objek induk bar bulat cooldown
    public Image isiBarCooldown;                // Image bar (Image Type: Filled), terisi pelan-pelan
    public GameObject teksIstirahat;            // Opsional: teks "Tunggu sebentar" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;

    [Header("Efek Kursi Saat Cooldown (seperti layar laptop redup)")]
    public SpriteRenderer[] spriteKursiRedup;   // Gambar kursi yang diredupkan selama cooldown (kalau kursi berupa sprite sendiri)
    public GameObject bayanganKursi;            // Opsional: lapisan gelap semi-transparan di atas kursi (kalau kursi menyatu dengan peta)
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Warna kursi saat redup

    [Header("Setelah Kegiatan Pertama Selesai")]
    public UnityEvent saatKegiatanPertamaSelesai;   // Opsional: untuk menyambung ke scene berikutnya (Evaluasi)

    private bool siap = false;
    private bool dialogSelesai = false;
    private bool sedangBerjalan = false;
    private bool cooldownAktif = false;
    private bool kegiatanPertamaDilaporkan = false;
    private bool tutorialSudahTampil = false;
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;
    private float waktuSiapE = 0f;
    private Color[] warnaAsli;                  // Warna asli sprite kursi, untuk dikembalikan setelah cooldown

    public bool SedangSibuk { get { return sedangBerjalan; } }

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorial);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (ikonE != null) ikonE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (barCooldownBulat != null) barCooldownBulat.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        if (spriteKursiRedup != null)
        {
            warnaAsli = new Color[spriteKursiRedup.Length];
            for (int i = 0; i < spriteKursiRedup.Length; i++)
            {
                if (spriteKursiRedup[i] != null) warnaAsli[i] = spriteKursiRedup[i].color;
            }
        }
        if (bayanganKursi != null) bayanganKursi.SetActive(false);

        if (mitraObjek != null) mitraObjek.SetActive(siap);
    }

    // Dipanggil dari event scene sebelumnya (mis. "Saat Adegan Selesai" di Scene 5)
    public void SiapkanScene()
    {
        siap = true;
        if (mitraObjek != null) mitraObjek.SetActive(true);
        Debug.Log("[Adegan L2-S6] Scene 6 siap, menunggu MC duduk di kursi community center.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Siap (Scene 6 langsung siap)")]
    private void TesSiap()
    {
        SiapkanScene();
    }

    void Update()
    {
        // Teks "istirahat" hilang otomatis
        if (teksIstirahat != null && teksIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksIstirahat.SetActive(false);

        // Bar bulat cooldown terisi pelan-pelan
        if (cooldownAktif)
        {
            float sisa = Mathf.Max(0f, cooldownSelesaiPada - Time.time);
            if (isiBarCooldown != null && durasiCooldown > 0f)
                isiBarCooldown.fillAmount = 1f - (sisa / durasiCooldown);

            if (Time.time >= cooldownSelesaiPada) SelesaiCooldown();
        }

        if (!siap || sedangBerjalan)
        {
            if (ikonE != null) ikonE.SetActive(false);
            return;
        }

        bool duduk = (kursiPengabdian == null) || kursiPengabdian.sedangDuduk;

        // Dialog pertama: mulai begitu MC duduk
        if (!dialogSelesai)
        {
            if (duduk) StartCoroutine(AlurDialog());
            return;
        }

        bool bolehTekanE = Time.time - waktuSiapE >= 0.4f;

        // Selama cooldown, E tidak memulai kegiatan
        if (Time.time < cooldownSelesaiPada)
        {
            if (ikonE != null) ikonE.SetActive(false);

            if (duduk && bolehTekanE && Input.GetKeyDown(tombolInteraksi) && teksIstirahat != null)
            {
                teksIstirahat.SetActive(true);
                teksSembunyiPada = Time.time + lamaTeksIstirahat;
            }
            return;
        }

        if (ikonE != null) ikonE.SetActive(duduk);

        if (duduk && bolehTekanE && Input.GetKeyDown(tombolInteraksi))
            StartCoroutine(AlurKegiatan());
    }

    private void SembunyikanSemua(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    // Dua dialog, hanya sekali (saat MC pertama kali duduk)
    private IEnumerator AlurDialog()
    {
        sedangBerjalan = true;
        Debug.Log("[Adegan L2-S6] MC duduk di kursi community center, dialog dimulai.");

        if (kursiPengabdian != null) kursiPengabdian.TerkunciEksternal = true;

        yield return new WaitForSeconds(0.5f);

        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L2-S6] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog));

        if (kursiPengabdian != null) kursiPengabdian.TerkunciEksternal = false;

        dialogSelesai = true;
        waktuSiapE = Time.time;
        sedangBerjalan = false;
    }

    // Satu kali kegiatan: bar hijau, poin, reward, energi, cooldown
    private IEnumerator AlurKegiatan()
    {
        sedangBerjalan = true;
        if (ikonE != null) ikonE.SetActive(false);

        // MC tidak bisa berdiri selama kegiatan berjalan
        if (kursiPengabdian != null) kursiPengabdian.TerkunciEksternal = true;

        // 1. Bar hijau berputar
        if (canvasProgressBar != null) canvasProgressBar.SetActive(true);
        if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;

        float timer = 0f;
        while (timer < durasiBar)
        {
            timer += Time.deltaTime;
            if (circularProgressBar != null) circularProgressBar.fillAmount = timer / durasiBar;
            yield return null;
        }

        if (circularProgressBar != null) circularProgressBar.fillAmount = 1f;
        yield return new WaitForSeconds(0.3f);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);

        // 2. Poin
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoin);
            Debug.Log("[Adegan L2-S6] Pengabdian selesai! Skor bertambah +" + jumlahPoin);
        }
        else
        {
            Debug.LogWarning("[Adegan L2-S6] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 3. Reward muncul sendiri begitu bar selesai (hilang sendiri)
        bool tampilkanTutorial = boxTutorial != null && boxTutorial.Length > 0
                                 && (!tutorialHanyaPertamaKali || !tutorialSudahTampil);

        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));

            // Kalau tutorial menyusul, tunggu box reward hilang dulu supaya tidak bertumpuk
            if (tampilkanTutorial)
                yield return new WaitForSecondsRealtime(RewardManager.Instance.lamaTampil + 0.3f);
        }

        // 3b. Box tutorial (Space untuk lanjut)
        if (tampilkanTutorial)
        {
            tutorialSudahTampil = true;

            if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;
            if (overlayHitam != null) overlayHitam.SetActive(true);

            yield return StartCoroutine(TampilkanBoxBerurutan(boxTutorial));

            if (overlayHitam != null) overlayHitam.SetActive(false);
            if (hentikanWaktuSaatTutorial) Time.timeScale = 1f;
        }

        // 4. Energi berkurang, lalu cek apakah habis
        if (energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        // 5. Cooldown (bar bulat)
        MulaiCooldown();

        if (kursiPengabdian != null) kursiPengabdian.TerkunciEksternal = false;
        sedangBerjalan = false;

        // 6. Laporkan kegiatan pertama selesai (sekali saja)
        if (!kegiatanPertamaDilaporkan)
        {
            kegiatanPertamaDilaporkan = true;
            saatKegiatanPertamaSelesai.Invoke();
            Debug.Log("[Adegan L2-S6] Kegiatan pertama selesai.");
        }
    }

    private void MulaiCooldown()
    {
        cooldownSelesaiPada = Time.time + durasiCooldown;
        cooldownAktif = true;

        if (barCooldownBulat == null || isiBarCooldown == null)
            Debug.LogWarning("[Adegan L2-S6] Bar Cooldown Bulat atau Isi Bar Cooldown belum diisi di Inspector, jadi bar tidak tampil.");

        if (isiBarCooldown != null) isiBarCooldown.fillAmount = 0f;
        if (barCooldownBulat != null) barCooldownBulat.SetActive(true);

        // Kursi meredup seperti layar laptop saat cooldown
        RedupkanKursi(true);
    }

    private void SelesaiCooldown()
    {
        cooldownAktif = false;
        if (barCooldownBulat != null) barCooldownBulat.SetActive(false);

        // Kursi terang lagi, siap dipakai
        RedupkanKursi(false);
        Debug.Log("[Adegan L2-S6] Cooldown selesai, kegiatan siap dikerjakan lagi.");
    }

    private void RedupkanKursi(bool redup)
    {
        if (spriteKursiRedup != null)
        {
            for (int i = 0; i < spriteKursiRedup.Length; i++)
            {
                if (spriteKursiRedup[i] == null) continue;

                spriteKursiRedup[i].color = redup
                    ? warnaCooldown
                    : ((warnaAsli != null && i < warnaAsli.Length) ? warnaAsli[i] : Color.white);
            }
        }

        if (bayanganKursi != null) bayanganKursi.SetActive(redup);
    }

    // Box tampil satu per satu: Space (atau klik kiri) menutup box dan lanjut ke berikutnya
    private IEnumerator TampilkanBoxBerurutan(GameObject[] daftar)
    {
        foreach (GameObject box in daftar)
        {
            if (box == null) continue;

            box.SetActive(true);

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }
}