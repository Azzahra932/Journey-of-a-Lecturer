using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Level 4 - Scene 2 (Memimpin Penelitian dan Membentuk Kelompok Riset), Lokasi: Laboratorium
//   1. Scene 1 Level 4 selesai -> Scene 2 siap, para peneliti / rekan dosen berdiri di depan pintu laboratorium
//   2. MC mendekati pintu: MC berhenti, dua box dialog (Peneliti lalu Pemain), Space untuk lanjut. Setelah dialog, MC langsung bebas (tanpa box tutorial)
//   3. MC berjalan ke meja laboratorium dan duduk di kursinya. Saat MC duduk, para peneliti hilang dan ikon E muncul di laptop
//   4. Tekan E: muncul dua box pilihan dulu, jujur (atas) atau bohong (bawah, integritas -15), lalu box pilihan jurnal (klik salah satu)
//        Jurnal internasional, empat pilihan (dari atas ke bawah): poin berbeda tiap pilihan (diatur di Inspector)
//      Lama bar bergantung pada pilihan jujur / bohong (bukan pilihan Sinta): jujur 15 detik, bohong 10 detik
//   5. Bar hijau berputar, poin bertambah, reward, box tutorial (opsional, sekali saja)
//   6. Energi berkurang, cooldown baterai mulai (layar laptop redup + baterai terisi). Setelah penuh, laptop bisa dipakai lagi (berulang)
//
// Script ini dipasang di objek apa saja (tidak perlu collider). Alurnya sama dengan Scene 2 Level 3, dan pilihan di laptop sama dengan Scene 4 dan 5 Level 3,
// hanya saja pilihan jurnalnya jurnal internasional dengan empat pilihan.
// Kursi dan laptop laboratorium yang dipakai sama dengan Scene 2 Level 3. Script AdeganLevel3Scene2 dimatikan sejak Level 4 dimulai
// (fungsi MulaiLevel4), supaya tombol E di laptop tidak terbaca dua kali.
// Cara menyambung dari Scene 1 Level 4: di AdeganLevel4Scene1, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Bagian yang tidak diisi di Inspector (mis. reward atau tutorial) dilewati.
public class AdeganLevel4Scene2 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 1 dulu

    [Header("Peneliti / Rekan Dosen di Pintu Laboratorium")]
    public GameObject[] penelitiObjek;          // Para peneliti yang berdiri di depan pintu. Nonaktif di awal, muncul saat Scene 2 siap
    public Transform titikPintu;                // Objek kosong di depan pintu laboratorium (dialog mulai saat MC mendekat)
    public float jarakPintu = 1.5f;             // Jarak MC ke titik pintu supaya dialog mulai (lingkaran kuning terlihat di Scene saat objek ini dipilih)
    public bool penelitiHilangSaatDuduk = true; // true = para peneliti hilang saat MC duduk di kursi laboratorium

    [Header("Script Laboratorium Level 3 (dimatikan sejak Level 4)")]
    public AdeganLevel3Scene2 scriptLabLevel3;  // Script Scene 2 Level 3 yang memakai laptop dan kursi laboratorium yang sama
    [Tooltip("Peneliti / NPC dari Scene 2 Level 3 yang masih ada atau masih mengikuti MC. Dihilangkan sejak Level 4 dimulai. JANGAN isi dengan objek yang sama dengan Peneliti Objek di atas.")]
    public GameObject[] penelitiLevel3;

    [Header("Box Dialog (dibuat di Hierarchy, urut: Peneliti lalu Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.3f;      // Jeda (detik) setelah MC berhenti di pintu sebelum dialog muncul

    [Header("Box Tutorial (di atas overlay hitam, opsional)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorialSetelahBar;      // Tutorial: muncul setelah bar hijau dan reward
    public bool tutorialBarSekaliSaja = true;       // true = tutorial hanya muncul pada aktivitas laptop yang pertama
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Kursi dan Laptop Laboratorium")]
    public KursiPerpusInteraction kursiLab;     // Kursi di laboratorium (komponen KursiPerpusInteraction). Otomatis dimatikan sampai dialog di pintu selesai
    public Sprite spriteDudukMCLevel4;          // Opsional: sprite MC level 4 saat duduk di kursi ini (dipasang ke gambar duduk kursi, sebelum MC duduk)
    public GameObject ikonE;                    // Ikon "E" di atas laptop laboratorium
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Pilihan Jujur / Bohong (muncul dulu sebelum pilihan jurnal)")]
    public GameObject boxJujur;                 // Box pilihan atas: jujur
    public GameObject boxBohong;                // Box pilihan bawah: bohong
    public Button tombolJujur;                  // Button di box jujur (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolBohong;                 // Button di box bohong (jangan isi On Click di Inspector, dipasang lewat script)
    public float penguranganIntegritasBohong = 15f;   // Integritas berkurang kalau memilih bohong

    [Header("Box Pilihan Jurnal Internasional (empat pilihan, urut dari atas ke bawah)")]
    public GameObject boxPilihan;               // Panel pilihan (judul + empat tombol)
    public Button tombolJurnal1;                // Button pilihan 1 (paling atas). Jangan isi On Click di Inspector, dipasang lewat script
    public Button tombolJurnal2;                // Button pilihan 2
    public Button tombolJurnal3;                // Button pilihan 3
    public Button tombolJurnal4;                // Button pilihan 4 (paling bawah)

    [Header("Poin per Pilihan Jurnal")]
    public int poinJurnal1 = 30;
    public int poinJurnal2 = 25;
    public int poinJurnal3 = 20;
    public int poinJurnal4 = 15;

    [Header("Lama Bar Berdasarkan Pilihan Jujur / Bohong (berlaku untuk semua pilihan Sinta)")]
    public float durasiJujur = 15f;             // Lama bar berputar kalau memilih jujur (detik)
    public float durasiBohong = 10f;            // Lama bar berputar kalau memilih bohong (detik)

    [Header("Bar Hijau Berputar")]
    public GameObject canvasProgressBar;        // Objek induk bar (boleh objek yang sama dengan scene lain)
    public Image circularProgressBar;           // Image bar (Image Type: Filled)

    [Header("Setelah Bar Selesai")]
    public int indexReward = -1;                // Nomor reward di RewardManager (muncul tiap selesai). -1 = pakai Box Reward di bawah (kalau diisi)
    public GameObject boxReward;                // Opsional: box reward buatan sendiri, dipakai kalau Index Reward = -1
    public float lamaBoxReward = 2f;
    public float biayaEnergi = 15f;             // Energi yang berkurang tiap selesai (0 = tidak mengurangi energi)
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Aktivitas Berulang")]
    public bool bisaDiulang = true;             // true = setelah cooldown selesai, laptop laboratorium bisa dipakai lagi

    [Header("Cooldown Baterai (seperti laptop lab Level 3)")]
    public float durasiCooldown = 60f;          // Jeda (detik) sebelum laptop bisa dipakai lagi
    public GameObject ikonBateraiLemah;         // Objek induk baterai, muncul saat cooldown
    public IndikatorBaterai indikatorBaterai;   // Script pengisi segmen baterai
    public TMP_Text teksCooldown;               // Teks di dekat baterai (contoh "Recharging...")
    public string isiTeksCooldown = "Recharging...";
    public GameObject teksIstirahat;            // Opsional: teks "Laptop sedang istirahat" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public SpriteRenderer spriteLaptop;         // Gambar laptop laboratorium yang diredupkan selama cooldown
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Layar redup

    [Header("Setelah Scene 2 Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah aktivitas laptop pertama selesai (untuk menyambung ke scene berikutnya)

    private bool siap = false;
    private bool dialogSelesai = false;
    private bool sedangBerjalan = false;
    private bool sudahPernahSelesai = false;
    private bool eventSudahDipanggil = false;
    private bool tutorialBarSudahTampil = false;
    private bool penelitiSudahHilang = false;
    private bool labLamaDimatikan = false;
    private bool peringatanKursiDitampilkan = false;
    private bool peringatanPenelitiKosong = false;
    private int pilihanJujur = 0;               // 0 = belum memilih, 1 = jujur, 2 = bohong
    private int pilihan = 0;                    // 0 = belum memilih, 1 sampai 4 = pilihan jurnal dari atas ke bawah

    private PlayerMovement pm;
    private bool pemainDikunci = false;

    // Cegah tombol untuk DUDUK ikut terbaca sebagai E untuk laptop
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

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorialSetelahBar);
        SembunyikanSemua(penelitiObjek);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxReward != null) boxReward.SetActive(false);
        if (ikonE != null) ikonE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCooldown != null) teksCooldown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;
        TampilkanPilihanJujur(false);
        TampilkanPilihan(false);

        if (tombolJujur != null) tombolJujur.onClick.AddListener(PilihJujur);
        if (tombolBohong != null) tombolBohong.onClick.AddListener(PilihBohong);
        if (tombolJurnal1 != null) tombolJurnal1.onClick.AddListener(PilihJurnal1);
        if (tombolJurnal2 != null) tombolJurnal2.onClick.AddListener(PilihJurnal2);
        if (tombolJurnal3 != null) tombolJurnal3.onClick.AddListener(PilihJurnal3);
        if (tombolJurnal4 != null) tombolJurnal4.onClick.AddListener(PilihJurnal4);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Mode tes: laboratorium Level 3 langsung dimatikan (di game biasa, dimatikan lewat MulaiLevel4 saat Level 4 dimulai)
        if (langsungSiap)
        {
            MatikanLabLevel3();
            SembunyikanPenelitiLevel3();
        }

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel4Scene2[] semua = FindObjectsByType<AdeganLevel4Scene2>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L4-S2] Ada " + semua.Length + " objek yang memasang script AdeganLevel4Scene2. Pakai satu saja.");
    }

    void OnDestroy()
    {
        if (tombolJujur != null) tombolJujur.onClick.RemoveListener(PilihJujur);
        if (tombolBohong != null) tombolBohong.onClick.RemoveListener(PilihBohong);
        if (tombolJurnal1 != null) tombolJurnal1.onClick.RemoveListener(PilihJurnal1);
        if (tombolJurnal2 != null) tombolJurnal2.onClick.RemoveListener(PilihJurnal2);
        if (tombolJurnal3 != null) tombolJurnal3.onClick.RemoveListener(PilihJurnal3);
        if (tombolJurnal4 != null) tombolJurnal4.onClick.RemoveListener(PilihJurnal4);
    }

    // Lingkaran kuning di tab Scene (saat objek ini dipilih): area dialog di pintu
    void OnDrawGizmosSelected()
    {
        if (titikPintu == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(titikPintu.position, jarakPintu);
    }

    // Dipanggil saat Level 4 dimulai, dari event "Saat Masuk Level Berikutnya" di PopupLevel3Selesai.
    // Script laboratorium Level 3 dimatikan, dan kursi laboratorium dikunci sampai dialog di pintu selesai.
    public void MulaiLevel4()
    {
        MatikanLabLevel3();
        SembunyikanPenelitiLevel3();
        if (kursiLab != null && !dialogSelesai) kursiLab.enabled = false;
        Debug.Log("[Adegan L4-S2] Level 4 dimulai, laboratorium Level 3 dimatikan sampai Scene 2 Level 4.");
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel4Scene1
    public void SiapkanScene()
    {
        siap = true;
        MatikanLabLevel3();
        SembunyikanPenelitiLevel3();
        if (kursiLab != null && !dialogSelesai) kursiLab.enabled = false;
        TerapkanSpriteMC();

        foreach (GameObject p in penelitiObjek)
        {
            if (p != null) p.SetActive(true);
        }

        Debug.Log("[Adegan L4-S2] Scene 2 siap, para peneliti menunggu di pintu laboratorium.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Dialog di Pintu (langsung mulai)")]
    private void TesDialog()
    {
        if (sedangBerjalan || dialogSelesai) return;
        siap = true;
        MatikanLabLevel3();
        SembunyikanPenelitiLevel3();
        foreach (GameObject p in penelitiObjek)
        {
            if (p != null) p.SetActive(true);
        }
        StartCoroutine(AlurDialog());
    }

    public void PilihJujur() { if (pilihanJujur == 0) pilihanJujur = 1; }
    public void PilihBohong() { if (pilihanJujur == 0) pilihanJujur = 2; }
    public void PilihJurnal1() { if (pilihan == 0) pilihan = 1; }
    public void PilihJurnal2() { if (pilihan == 0) pilihan = 2; }
    public void PilihJurnal3() { if (pilihan == 0) pilihan = 3; }
    public void PilihJurnal4() { if (pilihan == 0) pilihan = 4; }

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

        // Tahap 1: dialog dengan peneliti saat MC mendekati pintu
        if (!dialogSelesai)
        {
            if (titikPintu == null) return;

            if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
            if (pm == null) return;

            if (Vector2.Distance(titikPintu.position, pm.transform.position) <= jarakPintu)
            {
                Debug.Log("[Adegan L4-S2] MC tiba di pintu laboratorium, dialog dimulai.");
                StartCoroutine(AlurDialog());
            }
            return;
        }

        // Tahap 2: MC duduk di kursi laboratorium, ikon E muncul di laptop
        if (sudahPernahSelesai && !bisaDiulang)
        {
            if (ikonE != null && ikonE.activeSelf) ikonE.SetActive(false);
            return;
        }

        if (kursiLab == null)
        {
            if (!peringatanKursiDitampilkan)
            {
                peringatanKursiDitampilkan = true;
                Debug.LogWarning("[Adegan L4-S2] Kursi Lab belum diisi di Inspector, laptop laboratorium tidak bisa dipakai.");
            }
            return;
        }

        bool duduk = kursiLab.sedangDuduk;
        if (duduk && !dudukSebelumnya) waktuMulaiDuduk = Time.time;
        dudukSebelumnya = duduk;

        // Para peneliti hilang saat MC duduk di kursi laboratorium (dijaga tiap frame selama duduk,
        // supaya kalau ada script lain yang menyalakan mereka lagi, langsung dimatikan lagi)
        if (duduk && penelitiHilangSaatDuduk)
        {
            if ((penelitiObjek == null || penelitiObjek.Length == 0) && !peringatanPenelitiKosong)
            {
                peringatanPenelitiKosong = true;
                Debug.LogWarning("[Adegan L4-S2] Peneliti Objek kosong di Inspector, tidak ada yang bisa disembunyikan.");
            }

            if (penelitiObjek != null)
            {
                foreach (GameObject p in penelitiObjek)
                {
                    if (p != null && p.activeSelf) p.SetActive(false);
                }
            }

            if (!penelitiSudahHilang)
            {
                penelitiSudahHilang = true;
                Debug.Log("[Adegan L4-S2] MC duduk di kursi laboratorium, para peneliti menghilang.");
            }
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
            StartCoroutine(AlurLaptop());
    }

    // Pasang sprite MC level 4 ke gambar duduk kursi (harus sebelum MC duduk)
    private void TerapkanSpriteMC()
    {
        if (kursiLab == null || spriteDudukMCLevel4 == null || kursiLab.spriteDuduk == null) return;

        SpriteRenderer sr = kursiLab.spriteDuduk.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != spriteDudukMCLevel4) sr.sprite = spriteDudukMCLevel4;
    }

    // Script laboratorium Level 3 dimatikan, dan tampilan sisanya (ikon E, baterai, layar redup) dibersihkan
    private void MatikanLabLevel3()
    {
        if (labLamaDimatikan || scriptLabLevel3 == null) return;
        labLamaDimatikan = true;

        scriptLabLevel3.enabled = false;

        if (scriptLabLevel3.ikonE != null) scriptLabLevel3.ikonE.SetActive(false);
        if (scriptLabLevel3.ikonBateraiLemah != null) scriptLabLevel3.ikonBateraiLemah.SetActive(false);
        if (scriptLabLevel3.teksCooldown != null) scriptLabLevel3.teksCooldown.gameObject.SetActive(false);
        if (scriptLabLevel3.teksIstirahat != null) scriptLabLevel3.teksIstirahat.SetActive(false);
        if (scriptLabLevel3.spriteLaptop != null) scriptLabLevel3.spriteLaptop.color = scriptLabLevel3.warnaNormal;
    }

    // Peneliti Scene 2 Level 3 dihilangkan (objeknya dinonaktifkan, jadi script ikut-mengikuti MC juga berhenti)
    private void SembunyikanPenelitiLevel3()
    {
        if (penelitiLevel3 == null) return;

        foreach (GameObject p in penelitiLevel3)
        {
            if (p == null) continue;

            // Pengaman: jangan matikan objek yang juga dipakai sebagai peneliti Level 4
            if (penelitiObjek != null && System.Array.IndexOf(penelitiObjek, p) >= 0)
            {
                Debug.LogWarning("[Adegan L4-S2] '" + p.name + "' ada di Peneliti Objek DAN Peneliti Level 3. Objek ini dilewati, keluarkan dari Peneliti Level 3.");
                continue;
            }

            if (p.activeSelf)
            {
                p.SetActive(false);
                Debug.Log("[Adegan L4-S2] Peneliti Level 3 '" + p.name + "' dihilangkan.");
            }
        }
    }

    private void SembunyikanSemua(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    private void TampilkanPilihanJujur(bool tampil)
    {
        if (boxJujur != null) boxJujur.SetActive(tampil);
        if (boxBohong != null) boxBohong.SetActive(tampil);
        if (tombolJujur != null) tombolJujur.gameObject.SetActive(tampil);
        if (tombolBohong != null) tombolBohong.gameObject.SetActive(tampil);
    }

    private void TampilkanPilihan(bool tampil)
    {
        if (boxPilihan != null) boxPilihan.SetActive(tampil);
        if (tombolJurnal1 != null) tombolJurnal1.gameObject.SetActive(tampil);
        if (tombolJurnal2 != null) tombolJurnal2.gameObject.SetActive(tampil);
        if (tombolJurnal3 != null) tombolJurnal3.gameObject.SetActive(tampil);
        if (tombolJurnal4 != null) tombolJurnal4.gameObject.SetActive(tampil);
    }

    // ---------- Dialog di pintu ----------
    private IEnumerator AlurDialog()
    {
        sedangBerjalan = true;

        // MC berhenti di pintu selama dialog
        KunciPemain(true);

        yield return new WaitForSeconds(jedaSebelumDialog);

        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L4-S2] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

        // MC bebas berjalan ke kursi laboratorium, kursinya sekarang bisa dipakai
        KunciPemain(false);
        if (kursiLab != null) kursiLab.enabled = true;

        dialogSelesai = true;
        sedangBerjalan = false;

        Debug.Log("[Adegan L4-S2] Dialog selesai, MC bebas menuju kursi laboratorium.");
    }

    // ---------- Aktivitas laptop laboratorium ----------
    private IEnumerator AlurLaptop()
    {
        bool pertamaKali = !sudahPernahSelesai;
        sedangBerjalan = true;

        if (ikonE != null) ikonE.SetActive(false);

        // MC tidak bisa berdiri selama aktivitas berjalan
        kursiLab.TerkunciEksternal = true;

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
                Debug.Log("[Adegan L4-S2] Memilih bohong, integritas berkurang " + penguranganIntegritasBohong + ".");
            }
            else
            {
                Debug.LogWarning("[Adegan L4-S2] IntegritasManager.Instance tidak ditemukan di Scene!");
            }
        }

        // 1. Box pilihan jurnal
        pilihan = 0;
        TampilkanPilihan(true);

        while (pilihan == 0) yield return null;

        TampilkanPilihan(false);

        int poin = poinJurnal1;
        float durasi = (pilihanJujur == 2) ? durasiBohong : durasiJujur;   // jujur 15 detik, bohong 10 detik, apa pun pilihan jurnalnya
        string namaPilihan = "jurnal 1";

        if (pilihan == 2)
        {
            poin = poinJurnal2;
            namaPilihan = "jurnal 2";
        }
        else if (pilihan == 3)
        {
            poin = poinJurnal3;
            namaPilihan = "jurnal 3";
        }
        else if (pilihan == 4)
        {
            poin = poinJurnal4;
            namaPilihan = "jurnal 4";
        }

        Debug.Log("[Adegan L4-S2] Memilih jurnal " + namaPilihan + ": bar " + durasi + " detik, poin " + poin + ".");

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
            Debug.Log("[Adegan L4-S2] Memimpin penelitian! Skor bertambah +" + poin);
        }
        else
        {
            Debug.LogWarning("[Adegan L4-S2] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 4. Reward (lewat RewardManager, atau Box Reward buatan sendiri)
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

        // 5. Tutorial: setelah bar hijau dan reward (sekali saja kalau diatur begitu)
        if (!(tutorialBarSekaliSaja && tutorialBarSudahTampil))
        {
            tutorialBarSudahTampil = true;
            yield return StartCoroutine(TampilkanTutorial(boxTutorialSetelahBar));
        }

        // 6. Energi berkurang, lalu cek apakah habis
        if (biayaEnergi > 0f && energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        // 7. Cooldown baterai mulai (aktivitas berulang)
        sudahPernahSelesai = true;
        if (bisaDiulang) MulaiCooldown();

        // 8. MC dibebaskan (boleh berdiri)
        kursiLab.TerkunciEksternal = false;
        sedangBerjalan = false;

        // Event untuk scene berikutnya hanya dipanggil sekali
        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log(pertamaKali ? "[Adegan L4-S2] Scene 2 Level 4 selesai." : "[Adegan L4-S2] Aktivitas ulang selesai.");
    }

    // ---------- Cooldown baterai ----------
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

        Debug.Log("[Adegan L4-S2] Laptop laboratorium siap dipakai lagi.");
    }

    // ---------- Pembantu ----------
    // Box tutorial: game berhenti (opsional) dan layar digelapkan selama box tampil
    private IEnumerator TampilkanTutorial(GameObject[] daftar)
    {
        if (daftar == null || daftar.Length == 0) yield break;

        if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;

        // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
        if (overlayHitam != null)
        {
            overlayHitam.SetActive(true);
            overlayHitam.transform.SetAsLastSibling();
        }

        yield return StartCoroutine(TampilkanBoxBerurutan(daftar, true));

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
                Debug.LogWarning("[Adegan L4-S2] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    // Menahan atau membebaskan pergerakan MC (dipakai saat dialog di pintu)
    private void KunciPemain(bool kunci)
    {
        if (kunci)
        {
            pm = FindFirstObjectByType<PlayerMovement>();
            if (pm == null || !pm.enabled) return;

            Rigidbody2D rb = pm.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            if (pm.animator != null && pm.animator.enabled)
            {
                pm.animator.SetBool("IsMoving", false);
                pm.animator.SetFloat("MoveX", 0f);
                pm.animator.SetFloat("MoveY", 0f);
            }

            pm.enabled = false;
            pemainDikunci = true;
        }
        else
        {
            if (pemainDikunci && pm != null) pm.enabled = true;
            pemainDikunci = false;
        }
    }
}