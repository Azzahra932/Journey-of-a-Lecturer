using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Level 5 - Scene 4 (Pengabdian kepada Masyarakat), Lokasi: Community Center
//   1. Scene 3 Level 5 selesai -> SiapkanScene() dipanggil. Peneliti pengabdian muncul, sudah duduk menunggu
//   2. MC mendekati kursi di sampingnya dan duduk sendiri (Space)
//   3. PERTAMA KALI: dua box dialog (Peneliti, lalu Pemain), lalu SATU dialog Peneliti lagi yang bergantung pada pilihan di Scene 3:
//        - MC tadi menerima tawaran Dewan Profesor -> dialog "Terima"
//        - MC tadi menolak tawaran Dewan Profesor  -> dialog "Tolak"
//   4. Ikon E muncul. Tekan E: bar hijau berputar 15 detik -> poin +5 -> reward -> box tutorial (opsional) -> energi berkurang -> cooldown (bar bulat)
//   5. Setelah cooldown selesai, kegiatan bisa dikerjakan lagi (tanpa dialog)
//
// Alurnya mirip Level 2 Scene 6. Pasang script ini di objek mana saja yang selalu aktif (mis. objek kosong "AdeganLevel5Scene4").
// Cara menyambung dari Scene 3 Level 5: di event "Saat Adegan Selesai" Scene 3, tambahkan objek ini dan pilih fungsi SiapkanScene.
// Pilihan Scene 3 dibaca dari script AdeganLevel5Scene3 (isi slot "Script Scene 3").
// Kursi boleh memakai KursiPerpusInteraction, InteractiveDesk, atau KursiRektorInteraction. Isi salah satu saja.
public class AdeganLevel5Scene4 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 3 dulu (centang SEBELUM Play)
    public GameObject penelitiObjek;            // Peneliti pengabdian yang sudah duduk menunggu. Nonaktif di awal, muncul saat Scene 4 siap
    public Transform titikPeneliti;             // Opsional: peneliti dipindahkan ke titik ini saat muncul (aman walau objeknya dipakai level lain)

    [Header("Script Level 2 Scene 6 (dimatikan sejak Scene 4 Level 5 siap)")]
    public AdeganLevel2Scene6 scriptLevel2Scene6;   // Opsional: Scene 6 Level 2 yang memakai kursi dan community center yang sama

    [Header("Pilihan di Scene 3 (menentukan dialog ketiga)")]
    public AdeganLevel5Scene3 scriptScene3;     // Script Scene 3 Level 5 (tempat MC memilih Ya / Tidak)
    public bool tesAnggapMenerimaTawaran = false;   // Hanya dipakai saat tes tanpa Scene 3: true = dianggap menerima, false = dianggap menolak

    [Header("Kursi MC (isi SALAH SATU sesuai komponen di kursinya)")]
    public KursiPerpusInteraction kursiPerpus;          // Kursi memakai KursiPerpusInteraction (Space untuk duduk)
    public InteractiveDesk kursiDesk;                   // Kursi memakai InteractiveDesk
    public KursiRektorInteraction kursiRektor;          // Kursi memakai KursiRektorInteraction
    public bool lepasEvaluasiLevel1 = true;             // true = duduk di kursi InteractiveDesk tidak lagi memicu evaluasi / bimbingan level 1

    [Header("Gambar Duduk MC Level 5 (opsional)")]
    [Tooltip("CARA TERMUDAH: buat objek di Hierarchy dengan SpriteRenderer berisi gambar MC duduk level 5, atur posisi dan ukurannya di tab Scene. Script memasangnya sebagai objek duduk (Sit) kursi. Disembunyikan saat Play, baru muncul saat MC duduk. Untuk kursi InteractiveDesk, hanya gambarnya yang dipakai.")]
    public GameObject objekDudukMCLevel5;
    [Tooltip("Alternatif kalau Objek Duduk di atas kosong: sprite dari folder Project.")]
    public Sprite spriteDudukMCLevel5;

    [Header("Dialog Pertama (Peneliti lalu Pemain)")]
    public GameObject[] boxDialog;              // Dua box dialog, dibuat di Hierarchy (urut: Peneliti, Pemain)
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.5f;      // Jeda (detik) setelah MC duduk sebelum dialog muncul

    [Header("Dialog Ketiga Peneliti (bergantung pada pilihan Scene 3)")]
    public GameObject[] boxDialogTerima;        // Dialog kalau MC menerima tawaran Dewan Profesor ("Saya mendengar Anda menerima tawaran...")
    public GameObject[] boxDialogTolak;         // Dialog kalau MC menolak tawaran Dewan Profesor ("Saya menghargai keputusan Anda...")

    [Header("Kegiatan (Tekan E)")]
    public GameObject ikonE;                    // Ikon E (objek khusus Scene 4 Level 5)
    public KeyCode tombolInteraksi = KeyCode.E;
    public GameObject canvasProgressBar;        // Objek induk bar hijau
    public Image circularProgressBar;           // Image bar (Image Type: Filled)
    public float durasiBar = 15f;               // Lama bar berputar (detik)

    [Header("Hasil Setiap Selesai")]
    public int jumlahPoin = 5;                  // Poin yang didapat
    public int indexReward = -1;                // Nomor reward di RewardManager. -1 = tanpa reward
    public float biayaEnergi = 10f;             // Energi yang berkurang (0 = tidak mengurangi energi)
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Box Tutorial Setelah Bar Selesai (opsional)")]
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
    public UnityEvent saatKegiatanPertamaSelesai;   // Opsional: untuk menyambung ke scene berikutnya

    private bool siap = false;
    private bool dialogSelesai = false;
    private bool sedangBerjalan = false;
    private bool cooldownAktif = false;
    private bool kegiatanPertamaDilaporkan = false;
    private bool tutorialSudahTampil = false;
    private bool persiapanSelesai = false;
    private bool peringatanKursiDitampilkan = false;
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;
    private float waktuSiapE = 0f;
    private Color[] warnaAsli;                  // Warna asli sprite kursi, untuk dikembalikan setelah cooldown

    public bool SedangSibuk { get { return sedangBerjalan; } }

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxDialogTerima);
        SembunyikanSemua(boxDialogTolak);
        SembunyikanSemua(boxTutorial);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (ikonE != null) ikonE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (barCooldownBulat != null) barCooldownBulat.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);

        // Objek gambar duduk MC level 5 disembunyikan saat Play (baru muncul saat MC duduk, lewat kursi)
        if (objekDudukMCLevel5 != null) objekDudukMCLevel5.SetActive(false);

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

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel5Scene4[] semua = FindObjectsByType<AdeganLevel5Scene4>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L5-S4] Ada " + semua.Length + " objek yang memasang script AdeganLevel5Scene4. Pakai satu saja.");

        // Peneliti baru dimunculkan saat Scene 4 siap
        if (siap) PersiapanScene();
        else if (penelitiObjek != null) penelitiObjek.SetActive(false);
    }

    // Dipanggil dari event "Saat Adegan Selesai" di Scene 3 Level 5
    public void SiapkanScene()
    {
        siap = true;
        PersiapanScene();
        Debug.Log("[Adegan L5-S4] Scene 4 siap, Peneliti menunggu di community center.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Siap (Scene 4 langsung siap)")]
    private void TesSiap()
    {
        SiapkanScene();
    }

    // Persiapan: script Level 2 dimatikan, gambar duduk dipasang, Peneliti dimunculkan
    private void PersiapanScene()
    {
        if (persiapanSelesai) return;
        persiapanSelesai = true;

        MatikanLevel2Scene6();
        LepasEvaluasiLevel1();
        TerapkanSpriteMC();

        if (penelitiObjek != null)
        {
            if (titikPeneliti != null)
            {
                Vector3 posisi = titikPeneliti.position;
                posisi.z = penelitiObjek.transform.position.z;
                penelitiObjek.transform.position = posisi;
            }

            penelitiObjek.SetActive(true);
        }
    }

    // Script Level 2 Scene 6 dimatikan dan tampilan sisanya dibersihkan, supaya tombol E tidak terbaca dua kali
    private void MatikanLevel2Scene6()
    {
        AdeganLevel2Scene6 s = scriptLevel2Scene6;
        if (s == null) return;

        s.enabled = false;

        if (s.ikonE != null) s.ikonE.SetActive(false);
        if (s.barCooldownBulat != null) s.barCooldownBulat.SetActive(false);
        if (s.teksIstirahat != null) s.teksIstirahat.SetActive(false);
        if (s.bayanganKursi != null) s.bayanganKursi.SetActive(false);
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

        // "Langsung Siap" dibaca di sini supaya juga berlaku kalau dicentang saat Play
        if (!siap && langsungSiap)
        {
            siap = true;
        }

        if (!siap || sedangBerjalan)
        {
            if (ikonE != null) ikonE.SetActive(false);
            return;
        }

        if (!persiapanSelesai) PersiapanScene();

        if (!AdaKursi() && !peringatanKursiDitampilkan)
        {
            peringatanKursiDitampilkan = true;
            Debug.LogWarning("[Adegan L5-S4] Kursi belum diisi di Inspector (isi salah satu: Kursi Perpus, Kursi Desk, atau Kursi Rektor), dialog dan ikon E langsung dianggap MC sudah duduk.");
        }

        bool duduk = !AdaKursi() || MCSedangDuduk();

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

    // ---------- Pembantu kursi (mendukung KursiPerpusInteraction, InteractiveDesk, dan KursiRektorInteraction) ----------
    private bool AdaKursi()
    {
        return kursiPerpus != null || kursiDesk != null || kursiRektor != null;
    }

    private bool MCSedangDuduk()
    {
        bool d = false;
        if (kursiPerpus != null) d = kursiPerpus.sedangDuduk;
        if (kursiDesk != null) d = d || kursiDesk.SedangDuduk;
        if (kursiRektor != null) d = d || kursiRektor.SedangDuduk;
        return d;
    }

    // MC tidak bisa berdiri selama dikunci
    private void KunciKursi(bool kunci)
    {
        if (kursiPerpus != null) kursiPerpus.TerkunciEksternal = kunci;
        if (kursiDesk != null) kursiDesk.TerkunciEksternal = kunci;
        if (kursiRektor != null) kursiRektor.TerkunciEksternal = kunci;
    }

    // Kursi InteractiveDesk dipakai untuk adegan level 5, jadi duduk di sini tidak boleh memicu evaluasi / bimbingan level 1 lagi
    private void LepasEvaluasiLevel1()
    {
        if (!lepasEvaluasiLevel1 || kursiDesk == null) return;

        if (kursiDesk.evaluasiScript != null) kursiDesk.evaluasiScript = null;
        if (kursiDesk.bimbinganScript != null) kursiDesk.bimbinganScript = null;
    }

    // Pasang gambar duduk MC level 5 ke kursi (harus sebelum MC duduk).
    // Cara 1 (Objek Duduk dari Hierarchy): objek itu dipasang sebagai objek "Sit" kursi, jadi ukuran dan posisinya persis seperti di Scene.
    // Cara 2 (Sprite dari folder, kalau Objek Duduk kosong): dipasang ke field sprite kursi.
    private void TerapkanSpriteMC()
    {
        if (objekDudukMCLevel5 != null)
        {
            TerapkanObjekDuduk();
            return;
        }

        if (spriteDudukMCLevel5 == null) return;

        if (kursiRektor != null)
        {
            if (kursiRektor.spriteDudukBaru != spriteDudukMCLevel5)
            {
                kursiRektor.spriteDudukBaru = spriteDudukMCLevel5;
                Debug.Log("[Adegan L5-S4] Sprite duduk kursi (KursiRektorInteraction) diganti ke sprite level 5.");
            }

            if (kursiRektor.spriteDuduk != null)
            {
                SpriteRenderer sr = kursiRektor.spriteDuduk.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null && sr.sprite != spriteDudukMCLevel5) sr.sprite = spriteDudukMCLevel5;
            }
        }

        if (kursiDesk != null && kursiDesk.sitSprite != spriteDudukMCLevel5)
        {
            kursiDesk.sitSprite = spriteDudukMCLevel5;
            Debug.Log("[Adegan L5-S4] Sprite duduk kursi (InteractiveDesk) diganti ke sprite level 5.");
        }

        if (kursiPerpus != null && kursiPerpus.spriteDuduk != null)
        {
            SpriteRenderer sr = kursiPerpus.spriteDuduk.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null)
            {
                Debug.LogWarning("[Adegan L5-S4] Objek Sprite Duduk pada kursi tidak punya SpriteRenderer (di objek itu maupun child-nya), sprite level 5 tidak dipasang.");
            }
            else if (sr.sprite != spriteDudukMCLevel5)
            {
                sr.sprite = spriteDudukMCLevel5;
                Debug.Log("[Adegan L5-S4] Sprite duduk kursi (KursiPerpusInteraction) diganti ke sprite level 5.");
            }
        }
    }

    // Objek Duduk dari Hierarchy dipasang sebagai objek Sit kursi
    private void TerapkanObjekDuduk()
    {
        SpriteRenderer srObjek = objekDudukMCLevel5.GetComponentInChildren<SpriteRenderer>(true);
        if (srObjek == null)
            Debug.LogWarning("[Adegan L5-S4] Objek Duduk MC Level 5 tidak punya SpriteRenderer (di objek itu maupun child-nya). Tambahkan SpriteRenderer berisi gambar MC duduk.");

        if (kursiRektor != null)
        {
            if (kursiRektor.spriteDuduk != objekDudukMCLevel5)
            {
                kursiRektor.spriteDuduk = objekDudukMCLevel5;
                Debug.Log("[Adegan L5-S4] Objek duduk kursi (KursiRektorInteraction) diganti ke objek duduk level 5.");
            }

            // Kosongkan supaya kursi tidak menimpa sprite objek dengan sprite lama saat MC duduk
            kursiRektor.spriteDudukBaru = null;
        }

        if (kursiPerpus != null && kursiPerpus.spriteDuduk != objekDudukMCLevel5)
        {
            kursiPerpus.spriteDuduk = objekDudukMCLevel5;
            Debug.Log("[Adegan L5-S4] Objek duduk kursi (KursiPerpusInteraction) diganti ke objek duduk level 5.");
        }

        // InteractiveDesk tidak punya objek Sit: hanya gambar dari objek yang dipakai
        if (kursiDesk != null && srObjek != null && srObjek.sprite != null && kursiDesk.sitSprite != srObjek.sprite)
        {
            kursiDesk.sitSprite = srObjek.sprite;
            Debug.Log("[Adegan L5-S4] Sprite duduk kursi (InteractiveDesk) diambil dari objek duduk level 5.");
        }
    }

    // True kalau MC tadi (di Scene 3) menerima tawaran Dewan Profesor
    private bool MenerimaTawaranScene3()
    {
        if (scriptScene3 != null && scriptScene3.SudahMemilih)
            return scriptScene3.MenerimaTawaran;

        Debug.LogWarning("[Adegan L5-S4] Pilihan Scene 3 belum ada (Script Scene 3 kosong atau MC belum memilih), dipakai nilai tes: "
            + (tesAnggapMenerimaTawaran ? "menerima" : "menolak") + ".");
        return tesAnggapMenerimaTawaran;
    }

    // ---------- Dialog, hanya sekali (saat MC pertama kali duduk) ----------
    private IEnumerator AlurDialog()
    {
        sedangBerjalan = true;
        Debug.Log("[Adegan L5-S4] MC duduk di kursi community center, dialog dimulai.");

        KunciKursi(true);

        yield return new WaitForSeconds(jedaSebelumDialog);

        // 1. Dua box dialog (Peneliti lalu Pemain)
        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L5-S4] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

        // 2. Dialog ketiga Peneliti: bergantung pada pilihan MC di Scene 3
        bool terima = MenerimaTawaranScene3();
        GameObject[] dialogKetiga = terima ? boxDialogTerima : boxDialogTolak;

        Debug.Log("[Adegan L5-S4] Dialog ketiga: MC di Scene 3 " + (terima ? "menerima" : "menolak") + " tawaran Dewan Profesor.");

        if (dialogKetiga == null || dialogKetiga.Length == 0)
            Debug.LogWarning("[Adegan L5-S4] Box Dialog " + (terima ? "Terima" : "Tolak") + " masih kosong di Inspector, dialog ketiga dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(dialogKetiga, false));

        KunciKursi(false);

        dialogSelesai = true;
        waktuSiapE = Time.time;
        sedangBerjalan = false;
    }

    // ---------- Satu kali kegiatan: bar hijau, poin, reward, tutorial, energi, cooldown ----------
    private IEnumerator AlurKegiatan()
    {
        sedangBerjalan = true;
        if (ikonE != null) ikonE.SetActive(false);

        // MC tidak bisa berdiri selama kegiatan berjalan
        KunciKursi(true);

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
            Debug.Log("[Adegan L5-S4] Pengabdian selesai! Skor bertambah +" + jumlahPoin);
        }
        else
        {
            Debug.LogWarning("[Adegan L5-S4] ScoreManager.Instance tidak ditemukan di Scene!");
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

        // 4. Energi berkurang, lalu cek apakah habis
        if (biayaEnergi > 0f && energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        // 5. Cooldown (bar bulat)
        MulaiCooldown();

        KunciKursi(false);
        sedangBerjalan = false;

        // 6. Laporkan kegiatan pertama selesai (sekali saja)
        if (!kegiatanPertamaDilaporkan)
        {
            kegiatanPertamaDilaporkan = true;
            saatKegiatanPertamaSelesai.Invoke();
            Debug.Log("[Adegan L5-S4] Kegiatan pertama selesai.");
        }
    }

    // ---------- Cooldown ----------
    private void MulaiCooldown()
    {
        cooldownSelesaiPada = Time.time + durasiCooldown;
        cooldownAktif = true;

        if (barCooldownBulat == null || isiBarCooldown == null)
            Debug.LogWarning("[Adegan L5-S4] Bar Cooldown Bulat atau Isi Bar Cooldown belum diisi di Inspector, jadi bar tidak tampil.");

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
        Debug.Log("[Adegan L5-S4] Cooldown selesai, kegiatan siap dikerjakan lagi.");
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
    private IEnumerator TampilkanBoxBerurutan(GameObject[] daftar, bool keDepan)
    {
        foreach (GameObject box in daftar)
        {
            if (box == null) continue;

            box.SetActive(true);

            // Box tutorial dipaksa tampil paling depan di Canvas-nya, di atas overlay hitam
            if (keDepan) box.transform.SetAsLastSibling();

            if (!box.activeInHierarchy)
                Debug.LogWarning("[Adegan L5-S4] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }
}