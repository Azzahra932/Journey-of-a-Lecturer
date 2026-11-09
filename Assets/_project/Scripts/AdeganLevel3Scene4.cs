using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Level 3 - Scene 4 (Publikasi Penelitian), Lokasi: Perpustakaan
//   1. Scene 3 Level 3 selesai -> Scene 4 siap
//   2. MC duduk di kursi perpustakaan: box dialog Pemain muncul (Space untuk lanjut)
//   3. Ikon E muncul di laptop perpustakaan. Tekan E: muncul dua box pilihan dulu, jujur (atas) atau bohong (bawah, integritas -15),
//      lalu box pilihan jurnal (klik salah satu)
//        Jurnal Sinta 1-2 : poin 25
//        Jurnal Sinta 3-4 : poin 20
//        Jurnal Sinta 5-6 : poin 15
//      Lama bar bergantung pada pilihan jujur / bohong (bukan pilihan Sinta): jujur 15 detik, bohong 10 detik
//   4. Bar hijau berputar, poin bertambah
//   5. Box dialog setelah selesai (opsional), reward, lalu box tutorial (di atas overlay hitam)
//   6. Energi berkurang, cooldown baterai mulai (layar laptop redup + baterai terisi)
//   7. Setelah cooldown penuh, MC bisa duduk lagi dan memakai laptop berulang (langsung ikon E, pilihan jurnal, bar, poin, reward).
//      Dialog awal, dialog selesai, dan tutorial hanya muncul pada aktivitas pertama (bisa diubah di Inspector).
//
// Script ini dipasang di objek apa saja (tidak perlu collider).
// Cara menyambung dari Scene 3: di AdeganLevel3Scene3, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Kursi dan laptop yang dipakai adalah kursi dan laptop perpustakaan yang sudah ada. Script laptop perpustakaan lama
// (LaptopPerpusInteraction) dimatikan sejak Level 3 dimulai (fungsi MulaiLevel3) dan tidak dinyalakan lagi, jadi laptop itu tidak bisa dipakai
// sebelum Scene 4, dan tombol E tidak terbaca dua kali.
// Bagian yang tidak diisi di Inspector (mis. reward atau tutorial) dilewati.
public class AdeganLevel3Scene4 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 3 dulu

    [Header("Kursi dan Laptop Perpustakaan")]
    public KursiPerpusInteraction kursiPerpus;  // Kursi perpustakaan (komponen KursiPerpusInteraction). Adegan mulai saat MC duduk di sini
    public LaptopPerpusInteraction laptopPerpusLama;   // Script laptop perpustakaan lama (dimatikan begitu adegan ini mulai). Boleh kosong
    public BukuInteraction bukuLama;            // Script buku Level 1 di perpustakaan. Dimatikan sejak Level 3 supaya cooldown buku tidak tampil lagi
    public GameObject[] objekLamaDisembunyikan; // Tampilan lama yang harus mati di Level 3: lingkaran/bar cooldown buku, baterai atau ikon E laptop lama, dll. Dijaga tetap mati meski script lama menyalakannya lagi
    public Sprite spriteDudukMCLevel3;          // Opsional: sprite MC level 3 saat duduk di kursi ini (dipasang ke gambar duduk kursi, sebelum MC duduk)
    public GameObject ikonE;                    // Ikon "E" di atas laptop perpustakaan
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Box Dialog Awal (dibuat di Hierarchy, Pemain)")]
    public GameObject[] boxDialogAwal;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.8f;      // Jeda (detik) setelah MC duduk sebelum dialog muncul

    [Header("Pilihan Jujur / Bohong (muncul dulu sebelum pilihan jurnal)")]
    public GameObject boxJujur;                 // Box pilihan atas: jujur
    public GameObject boxBohong;                // Box pilihan bawah: bohong
    public Button tombolJujur;                  // Button di box jujur (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolBohong;                 // Button di box bohong (jangan isi On Click di Inspector, dipasang lewat script)
    public float penguranganIntegritasBohong = 15f;   // Integritas berkurang kalau memilih bohong

    [Header("Box Pilihan Jurnal")]
    public GameObject boxPilihan;               // Panel pilihan (judul + tiga tombol)
    public Button tombolSinta12;                // Button Jurnal Sinta 1-2 (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolSinta34;                // Button Jurnal Sinta 3-4
    public Button tombolSinta56;                // Button Jurnal Sinta 5-6

    [Header("Poin per Pilihan Jurnal")]
    public int poinSinta12 = 25;
    public int poinSinta34 = 20;
    public int poinSinta56 = 15;

    [Header("Lama Bar Berdasarkan Pilihan Jujur / Bohong (berlaku untuk semua pilihan Sinta)")]
    public float durasiJujur = 15f;             // Lama bar berputar kalau memilih jujur (detik)
    public float durasiBohong = 10f;            // Lama bar berputar kalau memilih bohong (detik)

    [Header("Bar Hijau Berputar")]
    public GameObject canvasProgressBar;        // Objek induk bar
    public Image circularProgressBar;           // Image bar (Image Type: Filled)

    [Header("Setelah Bar Selesai")]
    public GameObject[] boxDialogSelesai;       // Opsional: dialog Pemain "Artikel ilmiah telah selesai disusun dan siap diajukan."
    public bool dialogSelesaiSekaliSaja = true; // true = dialog ini hanya muncul pada aktivitas pertama
    public int indexReward = -1;                // Nomor reward di RewardManager (muncul tiap selesai). -1 = pakai Box Reward di bawah (kalau diisi)
    public GameObject boxReward;                // Opsional: box reward buatan sendiri, dipakai kalau Index Reward = -1
    public float lamaBoxReward = 2f;
    public float biayaEnergi = 15f;             // Energi yang berkurang tiap selesai (0 = tidak mengurangi energi)
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Box Tutorial (muncul setelah reward, di atas overlay hitam)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;            // Box tutorial, urut dari atas
    public bool hentikanWaktuSaatTutorial = true;
    public bool tutorialSekaliSaja = true;      // true = tutorial hanya muncul pada aktivitas pertama

    [Header("Aktivitas Berulang")]
    public bool bisaDiulang = true;             // true = setelah cooldown selesai, laptop bisa dipakai lagi

    [Header("Cooldown Baterai (seperti laptop lab)")]
    public float durasiCooldown = 60f;          // Jeda (detik) sebelum laptop bisa dipakai lagi
    public GameObject ikonBateraiLemah;         // Objek induk baterai, muncul saat cooldown (duplikat dari baterai laptop lab / kelas)
    public IndikatorBaterai indikatorBaterai;   // Script pengisi segmen baterai
    public TMP_Text teksCooldown;               // Teks di dekat baterai (contoh "Recharging...")
    public string isiTeksCooldown = "Recharging...";
    public GameObject teksIstirahat;            // Opsional: teks "Laptop sedang istirahat" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public SpriteRenderer spriteLaptop;         // Gambar laptop perpustakaan yang diredupkan selama cooldown
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Layar redup

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah aktivitas pertama selesai (untuk menyambung ke scene Level 3 berikutnya)

    private bool siap = false;
    private bool sudahJalan = false;            // true setelah adegan pertama (dialog awal) dimulai
    private bool sedangBerjalan = false;
    private bool sudahPernahSelesai = false;
    private bool eventSudahDipanggil = false;
    private bool tutorialSudahTampil = false;
    private bool dialogSelesaiSudahTampil = false;
    private bool peringatanKursiDitampilkan = false;
    private int pilihanJujur = 0;               // 0 = belum memilih, 1 = jujur, 2 = bohong
    private int pilihan = 0;                    // 0 = belum memilih, 1 = Sinta 1-2, 2 = Sinta 3-4, 3 = Sinta 5-6
    private bool jagaObjekLama = false;         // true sejak Level 3 dimulai: objek tampilan lama dijaga tetap mati

    // Cegah tombol E atau Space untuk DUDUK ikut terbaca sebagai E untuk laptop
    private bool dudukSebelumnya = false;
    private float waktuMulaiDuduk = 0f;
    private const float jedaSetelahDuduk = 0.3f;

    // Cooldown
    private bool cooldownAktif = false;
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialogAwal);
        SembunyikanSemua(boxDialogSelesai);
        SembunyikanSemua(boxTutorial);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxReward != null) boxReward.SetActive(false);
        if (ikonE != null) ikonE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCooldown != null) teksCooldown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;
        TampilkanPilihan(false);

        TampilkanPilihanJujur(false);
        if (tombolJujur != null) tombolJujur.onClick.AddListener(PilihJujur);
        if (tombolBohong != null) tombolBohong.onClick.AddListener(PilihBohong);

        if (tombolSinta12 != null) tombolSinta12.onClick.AddListener(PilihSinta12);
        if (tombolSinta34 != null) tombolSinta34.onClick.AddListener(PilihSinta34);
        if (tombolSinta56 != null) tombolSinta56.onClick.AddListener(PilihSinta56);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Mode tes: laptop lama langsung dimatikan (di game biasa, dimatikan lewat MulaiLevel3 saat Level 3 dimulai)
        if (langsungSiap) MatikanLaptopLama();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel3Scene4[] semua = FindObjectsByType<AdeganLevel3Scene4>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L3-S4] Ada " + semua.Length + " objek yang memasang script AdeganLevel3Scene4. Pakai satu saja.");
    }

    void OnDestroy()
    {
        if (tombolJujur != null) tombolJujur.onClick.RemoveListener(PilihJujur);
        if (tombolBohong != null) tombolBohong.onClick.RemoveListener(PilihBohong);

        if (tombolSinta12 != null) tombolSinta12.onClick.RemoveListener(PilihSinta12);
        if (tombolSinta34 != null) tombolSinta34.onClick.RemoveListener(PilihSinta34);
        if (tombolSinta56 != null) tombolSinta56.onClick.RemoveListener(PilihSinta56);
    }

    // Dipanggil saat Level 3 dimulai, dari event "Saat Masuk Level 3" di ManagerLevel2Selesai (tambahkan bersama AdeganLevel3Scene1).
    // Laptop perpustakaan lama dimatikan, jadi tidak bisa dipakai sebelum Scene 4 dimulai.
    public void MulaiLevel3()
    {
        MatikanLaptopLama();
        Debug.Log("[Adegan L3-S4] Level 3 dimulai, laptop perpustakaan lama dimatikan sampai Scene 4.");
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel3Scene3
    public void SiapkanScene()
    {
        siap = true;
        MatikanLaptopLama();   // pengaman kalau MulaiLevel3 lupa disambungkan
        TerapkanSpriteMC();
        Debug.Log("[Adegan L3-S4] Scene 4 siap, menunggu MC duduk di kursi perpustakaan.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (langsung mulai, tanpa menunggu duduk)")]
    private void TesAdegan()
    {
        if (sudahJalan) return;
        siap = true;
        sudahJalan = true;
        StartCoroutine(Alur());
    }

    public void PilihJujur() { if (pilihanJujur == 0) pilihanJujur = 1; }
    public void PilihBohong() { if (pilihanJujur == 0) pilihanJujur = 2; }

    private void TampilkanPilihanJujur(bool tampil)
    {
        if (boxJujur != null) boxJujur.SetActive(tampil);
        if (boxBohong != null) boxBohong.SetActive(tampil);
        if (tombolJujur != null) tombolJujur.gameObject.SetActive(tampil);
        if (tombolBohong != null) tombolBohong.gameObject.SetActive(tampil);
    }

    public void PilihSinta12() { if (pilihan == 0) pilihan = 1; }
    public void PilihSinta34() { if (pilihan == 0) pilihan = 2; }
    public void PilihSinta56() { if (pilihan == 0) pilihan = 3; }

    void Update()
    {
        // Teks "istirahat" hilang otomatis
        if (teksIstirahat != null && teksIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksIstirahat.SetActive(false);

        // Baterai cooldown terisi bertahap
        if (cooldownAktif) TickCooldown();

        if (sedangBerjalan) return;

        // "Langsung Siap" dibaca di sini (bukan hanya di Start) supaya juga berlaku kalau dicentang saat Play
        if (!siap && !langsungSiap) return;

        if (kursiPerpus == null)
        {
            if (!peringatanKursiDitampilkan)
            {
                peringatanKursiDitampilkan = true;
                Debug.LogWarning("[Adegan L3-S4] Kursi Perpus belum diisi di Inspector, adegan tidak bisa dimulai.");
            }
            return;
        }

        bool duduk = kursiPerpus.sedangDuduk;
        if (duduk && !dudukSebelumnya) waktuMulaiDuduk = Time.time;
        dudukSebelumnya = duduk;

        // Tahap 1: adegan pertama dimulai saat MC duduk di kursi perpustakaan
        if (!sudahJalan)
        {
            if (!duduk)
            {
                TerapkanSpriteMC();   // sprite duduk dipasang sebelum MC duduk
                return;
            }

            sudahJalan = true;
            Debug.Log("[Adegan L3-S4] MC duduk di kursi perpustakaan, adegan dimulai.");
            StartCoroutine(Alur());
            return;
        }

        // Tahap 2: aktivitas berulang setelah aktivitas pertama selesai
        if (!sudahPernahSelesai) return;

        if (!bisaDiulang)
        {
            if (ikonE != null && ikonE.activeSelf) ikonE.SetActive(false);
            return;
        }

        bool bolehTekan = duduk && (Time.time - waktuMulaiDuduk >= jedaSetelahDuduk);

        // Selama cooldown, E tidak memulai aktivitas: ikon E mati, E menampilkan teks istirahat
        if (cooldownAktif)
        {
            if (ikonE != null && ikonE.activeSelf) ikonE.SetActive(false);

            if (bolehTekan && Input.GetKeyDown(tombolInteraksi) && teksIstirahat != null)
            {
                teksIstirahat.SetActive(true);
                teksSembunyiPada = Time.time + lamaTeksIstirahat;
            }
            return;
        }

        if (ikonE != null && ikonE.activeSelf != duduk) ikonE.SetActive(duduk);

        if (bolehTekan && Input.GetKeyDown(tombolInteraksi))
            StartCoroutine(AlurAktivitas(false));
    }

    // Pasang sprite MC level 3 ke gambar duduk kursi (harus sebelum MC duduk)
    private void TerapkanSpriteMC()
    {
        if (kursiPerpus == null || spriteDudukMCLevel3 == null || kursiPerpus.spriteDuduk == null) return;

        SpriteRenderer sr = kursiPerpus.spriteDuduk.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != spriteDudukMCLevel3) sr.sprite = spriteDudukMCLevel3;
    }

    private void SembunyikanSemua(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    private void TampilkanPilihan(bool tampil)
    {
        if (boxPilihan != null) boxPilihan.SetActive(tampil);
        if (tombolSinta12 != null) tombolSinta12.gameObject.SetActive(tampil);
        if (tombolSinta34 != null) tombolSinta34.gameObject.SetActive(tampil);
        if (tombolSinta56 != null) tombolSinta56.gameObject.SetActive(tampil);
    }

    // Script laptop perpustakaan lama dimatikan, dan tampilan sisanya (ikon E, baterai, layar redup) dibersihkan
    private void MatikanLaptopLama()
    {
        MatikanPerlengkapanLama();

        if (laptopPerpusLama == null) return;

        laptopPerpusLama.enabled = false;

        if (laptopPerpusLama.petunjukTekanE != null) laptopPerpusLama.petunjukTekanE.SetActive(false);
        if (laptopPerpusLama.ikonBateraiLemah != null) laptopPerpusLama.ikonBateraiLemah.SetActive(false);
        if (laptopPerpusLama.teksCooldown != null) laptopPerpusLama.teksCooldown.gameObject.SetActive(false);
        if (laptopPerpusLama.teksIstirahat != null) laptopPerpusLama.teksIstirahat.SetActive(false);
        if (laptopPerpusLama.spriteLaptop != null) laptopPerpusLama.spriteLaptop.color = laptopPerpusLama.warnaNormal;
    }

    // Buku Level 1 dan tampilan lamanya (cooldown buku, baterai laptop lama, dll) dimatikan dan dijaga tetap mati di Level 3
    private void MatikanPerlengkapanLama()
    {
        jagaObjekLama = true;

        if (bukuLama != null) bukuLama.enabled = false;

        SembunyikanObjekLama();
    }

    private void SembunyikanObjekLama()
    {
        if (objekLamaDisembunyikan == null) return;

        foreach (GameObject o in objekLamaDisembunyikan)
        {
            if (o == null || DipakaiSendiri(o)) continue;
            if (o.activeSelf) o.SetActive(false);
        }
    }

    // Objek yang dipakai Scene 4 sendiri (ikon E, bar, baterai, teks) tidak boleh ikut dimatikan walau salah dimasukkan ke daftar
    private bool DipakaiSendiri(GameObject o)
    {
        GameObject[] milikSendiri =
        {
            ikonE, canvasProgressBar, ikonBateraiLemah, teksIstirahat, boxPilihan, boxJujur, boxBohong,
            (teksCooldown != null) ? teksCooldown.gameObject : null
        };

        foreach (GameObject g in milikSendiri)
        {
            if (g != null && (g == o || g.transform.IsChildOf(o.transform))) return true;
        }

        return false;
    }

    // LateUpdate jalan setelah semua Update: kalau script lama menyalakan lagi tampilannya, langsung dimatikan sebelum tampil di layar
    void LateUpdate()
    {
        if (jagaObjekLama) SembunyikanObjekLama();
    }

    // ---------- Adegan pertama: dialog awal, ikon E, lalu aktivitas pertama ----------
    private IEnumerator Alur()
    {
        sedangBerjalan = true;

        // MC dikunci di kursi selama adegan, script laptop perpustakaan lama dimatikan
        if (kursiPerpus != null) kursiPerpus.TerkunciEksternal = true;
        MatikanLaptopLama();

        yield return new WaitForSeconds(jedaSebelumDialog);

        // 1. Dialog awal (Pemain)
        if (boxDialogAwal == null || boxDialogAwal.Length == 0)
            Debug.LogWarning("[Adegan L3-S4] Box Dialog Awal masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialogAwal, false));

        // 2. Ikon E muncul di laptop, tunggu E ditekan
        if (ikonE != null) ikonE.SetActive(true);

        // Jeda singkat supaya tombol yang masih tertekan tidak langsung terbaca
        yield return new WaitForSeconds(0.3f);

        while (!Input.GetKeyDown(tombolInteraksi))
            yield return null;

        if (ikonE != null) ikonE.SetActive(false);

        // 3. Aktivitas pertama
        yield return StartCoroutine(AlurAktivitas(true));
    }

    // ---------- Aktivitas laptop: pilihan, bar, poin, dialog, reward, tutorial, energi, cooldown ----------
    private IEnumerator AlurAktivitas(bool pertamaKali)
    {
        sedangBerjalan = true;

        if (ikonE != null) ikonE.SetActive(false);

        // MC tidak bisa berdiri selama aktivitas berjalan
        if (kursiPerpus != null) kursiPerpus.TerkunciEksternal = true;

        // 0. Dua box pilihan dulu: jujur (atas) atau bohong (bawah). Bohong mengurangi integritas
        pilihanJujur = 0;
        TampilkanPilihanJujur(true);

        while (pilihanJujur == 0) yield return null;

        TampilkanPilihanJujur(false);

        if (pilihanJujur == 2)
        {
            if (IntegritasManager.Instance != null)
            {
                IntegritasManager.Instance.ReduceIntegritas(penguranganIntegritasBohong);
                Debug.Log("[" + GetType().Name + "] Memilih bohong, integritas berkurang " + penguranganIntegritasBohong + ".");
            }
            else
            {
                Debug.LogWarning("[" + GetType().Name + "] IntegritasManager.Instance tidak ditemukan di Scene!");
            }
        }

        // 1. Box pilihan jurnal
        pilihan = 0;
        TampilkanPilihan(true);

        while (pilihan == 0) yield return null;

        TampilkanPilihan(false);

        int poin = poinSinta12;
        float durasi = (pilihanJujur == 2) ? durasiBohong : durasiJujur;   // jujur 15 detik, bohong 10 detik, apa pun pilihan Sinta-nya
        string namaPilihan = "Sinta 1-2";

        if (pilihan == 2)
        {
            poin = poinSinta34;
            namaPilihan = "Sinta 3-4";
        }
        else if (pilihan == 3)
        {
            poin = poinSinta56;
            namaPilihan = "Sinta 5-6";
        }

        Debug.Log("[Adegan L3-S4] Memilih jurnal " + namaPilihan + ": bar " + durasi + " detik, poin " + poin + ".");

        // 2. Bar hijau berputar
        if (canvasProgressBar != null) canvasProgressBar.SetActive(true);
        if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;

        float timer = 0f;
        while (timer < durasi)
        {
            timer += Time.deltaTime;
            if (circularProgressBar != null) circularProgressBar.fillAmount = timer / durasi;
            yield return null;
        }

        if (circularProgressBar != null) circularProgressBar.fillAmount = 1f;
        yield return new WaitForSeconds(0.3f);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);

        // 3. Poin
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(poin);
            Debug.Log("[Adegan L3-S4] Artikel selesai disusun! Skor bertambah +" + poin);
        }
        else
        {
            Debug.LogWarning("[Adegan L3-S4] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 4. Dialog setelah selesai (opsional)
        if (boxDialogSelesai != null && boxDialogSelesai.Length > 0 && !(dialogSelesaiSekaliSaja && dialogSelesaiSudahTampil))
        {
            dialogSelesaiSudahTampil = true;
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialogSelesai, false));
        }

        // 5. Reward (lewat RewardManager, atau Box Reward buatan sendiri)
        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));
        }
        else if (boxReward != null)
        {
            boxReward.SetActive(true);
            yield return new WaitForSecondsRealtime(lamaBoxReward);
            boxReward.SetActive(false);
        }

        // 6. Box tutorial dengan overlay hitam (sekali saja kalau diatur begitu)
        if (!(tutorialSekaliSaja && tutorialSudahTampil))
        {
            tutorialSudahTampil = true;
            yield return StartCoroutine(TampilkanTutorial());
        }

        // 7. Energi berkurang, lalu cek apakah habis
        if (biayaEnergi > 0f && energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        // 8. Cooldown baterai mulai (aktivitas berulang)
        sudahPernahSelesai = true;
        if (bisaDiulang) MulaiCooldown();

        // 9. MC dibebaskan (boleh berdiri)
        if (kursiPerpus != null) kursiPerpus.TerkunciEksternal = false;
        sedangBerjalan = false;

        // Event untuk scene berikutnya hanya dipanggil sekali
        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log(pertamaKali ? "[Adegan L3-S4] Scene 4 Level 3 selesai." : "[Adegan L3-S4] Aktivitas ulang selesai.");
    }

    // ---------- Cooldown baterai (sama seperti laptop lab) ----------
    private void MulaiCooldown()
    {
        cooldownSelesaiPada = Time.time + durasiCooldown;
        cooldownAktif = true;

        if (indikatorBaterai != null) indikatorBaterai.SetProgress(0f);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(true);

        if (teksCooldown != null)
        {
            teksCooldown.text = isiTeksCooldown;
            teksCooldown.gameObject.SetActive(true);
        }

        if (spriteLaptop != null) spriteLaptop.color = warnaCooldown;
    }

    private void TickCooldown()
    {
        float sisa = Mathf.Max(0f, cooldownSelesaiPada - Time.time);

        // Segmen baterai terisi bertahap: 0 (kosong) -> 1 (penuh)
        if (indikatorBaterai != null && durasiCooldown > 0f)
            indikatorBaterai.SetProgress(1f - (sisa / durasiCooldown));

        if (Time.time >= cooldownSelesaiPada) SelesaiCooldown();
    }

    private void SelesaiCooldown()
    {
        cooldownAktif = false;

        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCooldown != null) teksCooldown.gameObject.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;

        Debug.Log("[Adegan L3-S4] Laptop perpustakaan siap dipakai lagi.");
    }

    // ---------- Pembantu ----------
    // Box tutorial: game berhenti (opsional) dan layar digelapkan selama box tampil
    private IEnumerator TampilkanTutorial()
    {
        if (boxTutorial == null || boxTutorial.Length == 0) yield break;

        if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;

        // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
        if (overlayHitam != null)
        {
            overlayHitam.SetActive(true);
            overlayHitam.transform.SetAsLastSibling();
        }

        yield return StartCoroutine(TampilkanBoxBerurutan(boxTutorial, true));

        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (hentikanWaktuSaatTutorial) Time.timeScale = 1f;
    }

    // Box tampil satu per satu: Space (atau klik kiri) menutup box dan lanjut ke berikutnya
    private IEnumerator TampilkanBoxBerurutan(GameObject[] daftar, bool keDepan)
    {
        foreach (GameObject box in daftar)
        {
            if (box == null) continue;

            box.SetActive(true);

            // Box tutorial dipaksa tampil paling depan di Canvas-nya, di atas overlay hitam
            if (keDepan) box.transform.SetAsLastSibling();

            if (!box.activeInHierarchy)
                Debug.LogWarning("[Adegan L3-S4] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }
}