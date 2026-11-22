using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Level 5 - Scene 3 (Tawaran Dewan Profesor), Lokasi: Perpustakaan
//   1. Scene 2 Level 5 selesai -> Scene 3 siap. Dosen Senior muncul, sudah duduk membaca referensi ilmiah di kursinya
//   2. MC berjalan ke kursinya dan duduk sendiri (atau otomatis kalau "Duduk Otomatis" dicentang, hanya untuk kursi InteractiveDesk)
//   3. Empat box dialog (Dosen Senior, Pemain, Dosen Senior, Pemain), Space untuk lanjut
//   4. Muncul box kalimat tawaran, dan di bawahnya tombol Ya / Tidak
//        Ya    -> box dialog Dosen Senior untuk "Ya", dan integritas berkurang
//        Tidak -> box dialog Dosen Senior untuk "Tidak" (integritas tidak berkurang)
//   5. Box tutorial (opsional), poin dan reward (opsional)
//   6. MC dibebaskan, Scene 3 selesai (event "Saat Adegan Selesai")
//
// Kursi MC boleh memakai script sendiri (KursiRektorInteraction), InteractiveDesk, atau KursiPerpusInteraction. Isi salah satu saja.
// Script ini dipasang di objek apa saja (tidak perlu collider).
// Cara menyambung dari Scene 2 Level 5: di AdeganLevel5Scene2, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Bagian yang tidak diisi di Inspector (mis. tutorial, poin, reward) dilewati.
public class AdeganLevel5Scene3 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 2 dulu (centang SEBELUM Play)

    [Header("Kursi MC (isi SALAH SATU sesuai komponen di kursinya)")]
    public KursiRektorInteraction kursiRektor;          // Kalau kursinya memakai script sendiri (KursiRektorInteraction)
    public InteractiveDesk kursiRektorDesk;             // Kalau kursinya memakai komponen InteractiveDesk
    public KursiPerpusInteraction kursiRektorPerpus;    // Kalau kursinya memakai komponen KursiPerpusInteraction
    public bool dudukOtomatis = false;                  // false = MC berjalan ke kursi dan duduk sendiri. true = MC langsung didudukkan (hanya untuk kursi InteractiveDesk)
    public bool lepasEvaluasiLevel1 = true;             // true = duduk di kursi InteractiveDesk tidak lagi memicu evaluasi / bimbingan level 1

    [Header("Gambar Duduk MC Level 5 (opsional)")]
    [Tooltip("CARA TERMUDAH: buat objek di Hierarchy dengan SpriteRenderer berisi gambar MC duduk level 5, atur posisi dan ukurannya di tab Scene. Script memasangnya sebagai objek duduk (Sit) kursi. Disembunyikan saat Play, baru muncul saat MC duduk. Untuk kursi InteractiveDesk, hanya gambarnya yang dipakai.")]
    public GameObject objekDudukMCLevel5;
    [Tooltip("Alternatif kalau Objek Duduk di atas kosong: sprite dari folder Project.")]
    public Sprite spriteDudukMCLevel5;

    [Header("Dosen Senior (sudah duduk di kursinya, hanya muncul setelah Scene 2)")]
    public GameObject dosenSeniorObjek;         // Dosen Senior yang duduk membaca. Nonaktif di awal, muncul saat Scene 3 siap
    public Transform titikDosenSenior;          // Opsional: Dosen Senior dipindahkan ke titik ini saat muncul (aman walau objeknya dipakai level lain)
    public bool sembunyikanDosenSetelahSelesai = false;   // true = Dosen Senior hilang setelah adegan selesai

    [Header("Script dan Objek Lama (dimatikan sejak Scene 3 Level 5 siap)")]
    public AdeganLevel4Scene5 scriptLevel4Scene5;   // Opsional: Scene 5 Level 4 (laptop perpustakaan) yang memakai kursi yang sama. Dimatikan dan tampilannya dibersihkan
    [Tooltip("Script adegan lain yang masih aktif dan bisa ikut membaca tombol E / Space di perpustakaan. Dimatikan selama adegan ini, dinyalakan lagi di akhir.")]
    public Behaviour[] scriptDijeda;
    [Tooltip("Objek tampilan lama yang harus tersembunyi selama adegan ini (mis. ikon E laptop, baterai / lingkaran cooldown).")]
    public GameObject[] objekDisembunyikan;

    [Header("Empat Box Dialog (urut: Dosen Senior, Pemain, Dosen Senior, Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.5f;      // Jeda (detik) setelah MC duduk sebelum dialog pertama muncul

    [Header("Tawaran Ya / Tidak")]
    public GameObject boxPenawaran;             // Box kalimat tawaran (teks tawaran). Tombol Ya dan Tidak boleh berada di dalam box ini atau terpisah
    public Button tombolYa;                     // Jangan isi On Click di Inspector, dipasang lewat script
    public Button tombolTidak;                  // Jangan isi On Click di Inspector, dipasang lewat script

    [Header("Hasil Pilihan")]
    public GameObject[] boxDialogYa;            // Dialog Dosen Senior kalau MC memilih Ya (satu box, boleh lebih)
    public GameObject[] boxDialogTidak;         // Dialog Dosen Senior kalau MC memilih Tidak ("Keputusan yang baik...")
    public float penguranganIntegritasYa = 15f; // Integritas berkurang kalau memilih Ya

    [Header("Box Tutorial (opsional, setelah dialog hasil pilihan)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;            // Kosong = tanpa tutorial
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Poin dan Reward (opsional)")]
    public int jumlahPoin = 0;                  // Poin yang didapat setelah adegan (0 = tanpa poin)
    public int indexReward = -1;                // Nomor reward di RewardManager. -1 = tanpa reward

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil setelah semuanya selesai (untuk menyambung ke adegan berikutnya)

    private bool siap = false;
    private bool sudahJalan = false;
    private bool selesaiSemua = false;
    private bool peringatanKursiDitampilkan = false;
    private bool scriptLamaDimatikan = false;
    private int pilihan = 0;                    // 0 = belum memilih, 1 = Ya, 2 = Tidak

    private PlayerMovement pm;
    private readonly List<Behaviour> interaksiDimatikan = new List<Behaviour>();
    private readonly List<Behaviour> scriptDijedaAktif = new List<Behaviour>();

    // Penahan posisi: selama adegan, MC dikembalikan ke tempat duduknya kalau ada yang menggesernya
    private bool tahanPosisi = false;
    private Vector3 posisiTahan;
    private bool sudahDilaporkanPindah = false;
    private TransisiKembaliKeAwal transisi;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxDialogYa);
        SembunyikanSemua(boxDialogTidak);
        SembunyikanSemua(boxTutorial);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        TampilkanTawaran(false);

        // Objek gambar duduk MC level 5 disembunyikan saat Play (baru muncul saat MC duduk, lewat kursi)
        if (objekDudukMCLevel5 != null) objekDudukMCLevel5.SetActive(false);

        if (tombolYa != null) tombolYa.onClick.AddListener(PilihYa);
        if (tombolTidak != null) tombolTidak.onClick.AddListener(PilihTidak);

        transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel5Scene3[] semua = FindObjectsByType<AdeganLevel5Scene3>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L5-S3] Ada " + semua.Length + " objek yang memasang script AdeganLevel5Scene3. Pakai satu saja.");

        // Dosen Senior baru dimunculkan saat Scene 3 siap
        if (siap) MulaiScene();
        else if (dosenSeniorObjek != null) dosenSeniorObjek.SetActive(false);
    }

    void OnDestroy()
    {
        if (tombolYa != null) tombolYa.onClick.RemoveListener(PilihYa);
        if (tombolTidak != null) tombolTidak.onClick.RemoveListener(PilihTidak);
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel5Scene2
    public void SiapkanScene()
    {
        siap = true;
        MulaiScene();
        Debug.Log("[Adegan L5-S3] Scene 3 siap, Dosen Senior menunggu di perpustakaan.");
    }

    // Persiapan: script lama dimatikan, gambar duduk dipasang, Dosen Senior dimunculkan
    private void MulaiScene()
    {
        MatikanScriptLama();
        LepasEvaluasiLevel1();
        TerapkanSpriteMC();
        MunculkanDosenSenior();
    }

    // Memindahkan Dosen Senior ke titik duduknya (kalau diisi) lalu memunculkannya
    private void MunculkanDosenSenior()
    {
        if (dosenSeniorObjek == null) return;

        if (titikDosenSenior != null)
        {
            Vector3 posisi = titikDosenSenior.position;
            posisi.z = dosenSeniorObjek.transform.position.z;
            dosenSeniorObjek.transform.position = posisi;
        }

        dosenSeniorObjek.SetActive(true);
    }

    // Script Level 4 Scene 5 (laptop perpustakaan) dimatikan dan tampilannya dibersihkan
    private void MatikanScriptLama()
    {
        if (scriptLamaDimatikan) return;
        scriptLamaDimatikan = true;

        AdeganLevel4Scene5 s = scriptLevel4Scene5;
        if (s == null) return;

        s.enabled = false;

        if (s.ikonE != null) s.ikonE.SetActive(false);
        if (s.ikonBateraiLemah != null) s.ikonBateraiLemah.SetActive(false);
        if (s.teksCooldown != null) s.teksCooldown.gameObject.SetActive(false);
        if (s.teksIstirahat != null) s.teksIstirahat.SetActive(false);
        if (s.spriteLaptop != null) s.spriteLaptop.color = s.warnaNormal;
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (langsung mulai)")]
    private void TesAdegan()
    {
        if (sudahJalan) return;
        siap = true;
        sudahJalan = true;
        MulaiScene();
        StartCoroutine(Alur());
    }

    public void PilihYa() { if (pilihan == 0) pilihan = 1; }
    public void PilihTidak() { if (pilihan == 0) pilihan = 2; }

    // Dibaca script adegan berikutnya (mis. Scene 6) untuk tahu pilihan MC di tawaran Dewan Profesor
    public bool SudahMemilih { get { return pilihan != 0; } }
    public bool MenerimaTawaran { get { return pilihan == 1; } }

    // Continue: pilihan MC di tawaran dipulihkan dari data simpanan (1 = Ya / menerima, 2 = Tidak / menolak).
    // Dipanggil PemulihProgres, supaya dialog Scene 4 dan penutup Level 5 tetap sesuai pilihan sebelumnya.
    public void PulihkanPilihan(int nilai)
    {
        if (nilai == 1 || nilai == 2) pilihan = nilai;
    }

    // Menunggu MC duduk di kursinya (atau langsung mulai kalau Duduk Otomatis dicentang)
    void Update()
    {
        if (sudahJalan) return;

        // "Langsung Siap" dibaca di sini (bukan hanya di Start) supaya juga berlaku kalau dicentang saat Play
        if (!siap && !langsungSiap) return;

        if (!scriptLamaDimatikan) MulaiScene();

        if (!AdaKursi())
        {
            if (!peringatanKursiDitampilkan)
            {
                peringatanKursiDitampilkan = true;
                Debug.LogWarning("[Adegan L5-S3] Kursi belum diisi di Inspector (isi salah satu: Kursi Rektor, Kursi Rektor Desk, atau Kursi Rektor Perpus), adegan tidak bisa dimulai.");
            }
            return;
        }

        if (dudukOtomatis || MCSedangDuduk())
        {
            sudahJalan = true;
            Debug.Log("[Adegan L5-S3] MC duduk di kursinya, adegan dimulai.");
            StartCoroutine(Alur());
        }
    }

    // LateUpdate jalan setelah semua Update: kalau script lain menggeser MC dari kursi (mis. karena Space),
    // MC langsung dikembalikan ke tempat duduknya. Objek tampilan lama juga dijaga tetap tersembunyi.
    void LateUpdate()
    {
        if (objekDisembunyikan != null && (sudahJalan && !selesaiSemua))
        {
            foreach (GameObject o in objekDisembunyikan)
            {
                if (o != null && o.activeSelf) o.SetActive(false);
            }
        }

        if (!tahanPosisi || pm == null) return;

        // Energi habis: MC dipindah oleh transisi, jadi MC tidak ditahan lagi
        if (transisi != null && transisi.SedangTransisi)
        {
            tahanPosisi = false;
            return;
        }

        if (Vector2.Distance(pm.transform.position, posisiTahan) > 0.01f)
        {
            if (!sudahDilaporkanPindah)
            {
                sudahDilaporkanPindah = true;
                Debug.LogWarning("[Adegan L5-S3] Ada script yang menggeser MC dari kursi (posisi " + pm.transform.position
                    + "). MC dikembalikan.");
            }

            pm.transform.position = posisiTahan;
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

    private void TampilkanTawaran(bool tampil)
    {
        if (boxPenawaran != null) boxPenawaran.SetActive(tampil);
        if (tombolYa != null) tombolYa.gameObject.SetActive(tampil);
        if (tombolTidak != null) tombolTidak.gameObject.SetActive(tampil);
    }

    // ---------- Pembantu kursi (mendukung KursiRektorInteraction, InteractiveDesk, dan KursiPerpusInteraction) ----------
    private bool AdaKursi()
    {
        return kursiRektor != null || kursiRektorDesk != null || kursiRektorPerpus != null;
    }

    private bool MCSedangDuduk()
    {
        bool d = false;
        if (kursiRektor != null) d = kursiRektor.SedangDuduk;
        if (kursiRektorDesk != null) d = d || kursiRektorDesk.SedangDuduk;
        if (kursiRektorPerpus != null) d = d || kursiRektorPerpus.sedangDuduk;
        return d;
    }

    // MC tidak bisa berdiri selama dikunci
    private void KunciKursi(bool kunci)
    {
        if (kursiRektor != null) kursiRektor.TerkunciEksternal = kunci;
        if (kursiRektorDesk != null) kursiRektorDesk.TerkunciEksternal = kunci;
        if (kursiRektorPerpus != null) kursiRektorPerpus.TerkunciEksternal = kunci;
    }

    // Kursi InteractiveDesk dipakai untuk adegan level 5, jadi duduk di sini tidak boleh memicu evaluasi / bimbingan level 1 lagi
    private void LepasEvaluasiLevel1()
    {
        if (!lepasEvaluasiLevel1 || kursiRektorDesk == null) return;

        if (kursiRektorDesk.evaluasiScript != null) kursiRektorDesk.evaluasiScript = null;
        if (kursiRektorDesk.bimbinganScript != null) kursiRektorDesk.bimbinganScript = null;
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
            // Field ini yang dibaca kursi setiap kali MC duduk
            if (kursiRektor.spriteDudukBaru != spriteDudukMCLevel5)
            {
                kursiRektor.spriteDudukBaru = spriteDudukMCLevel5;
                Debug.Log("[Adegan L5-S3] Sprite duduk kursi (KursiRektorInteraction) diganti ke sprite level 5.");
            }

            if (kursiRektor.spriteDuduk != null)
            {
                SpriteRenderer sr = kursiRektor.spriteDuduk.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null && sr.sprite != spriteDudukMCLevel5) sr.sprite = spriteDudukMCLevel5;
            }
        }

        if (kursiRektorDesk != null && kursiRektorDesk.sitSprite != spriteDudukMCLevel5)
        {
            kursiRektorDesk.sitSprite = spriteDudukMCLevel5;
            Debug.Log("[Adegan L5-S3] Sprite duduk kursi (InteractiveDesk) diganti ke sprite level 5.");
        }

        if (kursiRektorPerpus != null && kursiRektorPerpus.spriteDuduk != null)
        {
            SpriteRenderer sr = kursiRektorPerpus.spriteDuduk.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null)
            {
                Debug.LogWarning("[Adegan L5-S3] Objek Sprite Duduk pada kursi tidak punya SpriteRenderer (di objek itu maupun child-nya), sprite level 5 tidak dipasang.");
            }
            else if (sr.sprite != spriteDudukMCLevel5)
            {
                sr.sprite = spriteDudukMCLevel5;
                Debug.Log("[Adegan L5-S3] Sprite duduk kursi (KursiPerpusInteraction) diganti ke sprite level 5.");
            }
        }
    }

    // Objek Duduk dari Hierarchy dipasang sebagai objek Sit kursi
    private void TerapkanObjekDuduk()
    {
        SpriteRenderer srObjek = objekDudukMCLevel5.GetComponentInChildren<SpriteRenderer>(true);
        if (srObjek == null)
            Debug.LogWarning("[Adegan L5-S3] Objek Duduk MC Level 5 tidak punya SpriteRenderer (di objek itu maupun child-nya). Tambahkan SpriteRenderer berisi gambar MC duduk.");

        if (kursiRektor != null)
        {
            if (kursiRektor.spriteDuduk != objekDudukMCLevel5)
            {
                kursiRektor.spriteDuduk = objekDudukMCLevel5;
                Debug.Log("[Adegan L5-S3] Objek duduk kursi (KursiRektorInteraction) diganti ke objek duduk level 5.");
            }

            // Kosongkan supaya kursi tidak menimpa sprite objek dengan sprite lama saat MC duduk
            kursiRektor.spriteDudukBaru = null;
        }

        if (kursiRektorPerpus != null && kursiRektorPerpus.spriteDuduk != objekDudukMCLevel5)
        {
            kursiRektorPerpus.spriteDuduk = objekDudukMCLevel5;
            Debug.Log("[Adegan L5-S3] Objek duduk kursi (KursiPerpusInteraction) diganti ke objek duduk level 5.");
        }

        // InteractiveDesk tidak punya objek Sit: hanya gambar dari objek yang dipakai
        if (kursiRektorDesk != null && srObjek != null && srObjek.sprite != null && kursiRektorDesk.sitSprite != srObjek.sprite)
        {
            kursiRektorDesk.sitSprite = srObjek.sprite;
            Debug.Log("[Adegan L5-S3] Sprite duduk kursi (InteractiveDesk) diambil dari objek duduk level 5.");
        }
    }

    // ---------- Alur lengkap ----------
    private IEnumerator Alur()
    {
        LepasEvaluasiLevel1();
        TerapkanSpriteMC();
        JedaScriptLain(true);

        // 1. MC duduk di kursi (otomatis kalau diatur begitu), lalu dikunci di kursi
        if (dudukOtomatis && !MCSedangDuduk())
        {
            if (kursiRektorDesk != null)
                kursiRektorDesk.DudukUntukAdegan();
            else
                Debug.LogWarning("[Adegan L5-S3] Duduk Otomatis hanya bisa untuk kursi InteractiveDesk. MC harus duduk sendiri di kursi ini.");
        }

        KunciKursi(true);
        MatikanInteraksiLain();

        pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            // Tunggu satu frame supaya posisi duduk sudah terpasang sebelum posisinya ditahan
            yield return null;

            posisiTahan = pm.transform.position;
            sudahDilaporkanPindah = false;
            tahanPosisi = true;
        }

        yield return new WaitForSeconds(jedaSebelumDialog);

        // 2. Empat box dialog (Dosen Senior, Pemain, Dosen Senior, Pemain)
        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L5-S3] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

        // 3. Tawaran: box kalimat dan box tombol Ya / Tidak
        pilihan = 0;
        TampilkanTawaran(true);

        // Jeda singkat supaya Space terakhir dari dialog tidak ikut terbaca
        yield return new WaitForSecondsRealtime(0.25f);

        while (pilihan == 0) yield return null;

        TampilkanTawaran(false);

        // 4. Hasil pilihan: satu dialog Dosen Senior yang berbeda untuk Ya dan Tidak
        if (pilihan == 1)
        {
            Debug.Log("[Adegan L5-S3] Memilih Ya, integritas berkurang " + penguranganIntegritasYa + ".");

            if (IntegritasManager.Instance != null)
                IntegritasManager.Instance.ReduceIntegritas(penguranganIntegritasYa);
            else
                Debug.LogWarning("[Adegan L5-S3] IntegritasManager.Instance tidak ditemukan di Scene!");

            if (boxDialogYa == null || boxDialogYa.Length == 0)
                Debug.LogWarning("[Adegan L5-S3] Box Dialog Ya masih kosong di Inspector, dilewati.");
            else
                yield return StartCoroutine(TampilkanBoxBerurutan(boxDialogYa, false));
        }
        else
        {
            Debug.Log("[Adegan L5-S3] Memilih Tidak, integritas tidak berubah.");

            if (boxDialogTidak == null || boxDialogTidak.Length == 0)
                Debug.LogWarning("[Adegan L5-S3] Box Dialog Tidak masih kosong di Inspector, dilewati.");
            else
                yield return StartCoroutine(TampilkanBoxBerurutan(boxDialogTidak, false));
        }

        // 5. Tutorial (opsional)
        yield return StartCoroutine(TampilkanTutorial());

        // MC dibebaskan dari kursi
        tahanPosisi = false;
        KunciKursi(false);
        NyalakanInteraksiLain();

        // 6. Poin dan reward (opsional)
        if (jumlahPoin > 0 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoin);
            Debug.Log("[Adegan L5-S3] Skor bertambah +" + jumlahPoin);
        }

        if (indexReward >= 0 && RewardManager.Instance != null)
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));

        Selesai();
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
                Debug.LogWarning("[Adegan L5-S3] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    // Script adegan lain yang bisa ikut membaca E / Space dimatikan selama adegan ini, dinyalakan lagi di akhir
    private void JedaScriptLain(bool jeda)
    {
        if (jeda)
        {
            if (scriptDijeda == null) return;

            foreach (Behaviour b in scriptDijeda)
            {
                if (b != null && b.enabled)
                {
                    b.enabled = false;
                    scriptDijedaAktif.Add(b);
                }
            }
        }
        else
        {
            foreach (Behaviour b in scriptDijedaAktif)
            {
                if (b != null) b.enabled = true;
            }

            scriptDijedaAktif.Clear();
        }
    }

    // Mematikan sementara script kursi dan laptop lain yang ikut membaca tombol Space (kursi MC sendiri tetap menyala)
    private void MatikanInteraksiLain()
    {
        foreach (InteractiveDesk d in FindObjectsByType<InteractiveDesk>(FindObjectsSortMode.None))
        {
            if (d != kursiRektorDesk) TambahDimatikan(d);
        }

        foreach (KursiPerpusInteraction k in FindObjectsByType<KursiPerpusInteraction>(FindObjectsSortMode.None))
        {
            if (k != kursiRektorPerpus) TambahDimatikan(k);
        }

        foreach (LaptopPerpusInteraction l in FindObjectsByType<LaptopPerpusInteraction>(FindObjectsSortMode.None))
            TambahDimatikan(l);
    }

    private void TambahDimatikan(Behaviour b)
    {
        if (b != null && b.enabled)
        {
            b.enabled = false;
            interaksiDimatikan.Add(b);
        }
    }

    private void NyalakanInteraksiLain()
    {
        foreach (Behaviour b in interaksiDimatikan)
        {
            if (b != null) b.enabled = true;
        }

        interaksiDimatikan.Clear();
    }

    private void Selesai()
    {
        // MC tidak ditahan lagi, bebas bergerak dan berdiri
        tahanPosisi = false;
        if (pm != null && !pm.enabled) pm.enabled = true;
        KunciKursi(false);

        // Interaksi kursi dan laptop dinyalakan lagi, objek tampilan lama tidak dijaga lagi
        NyalakanInteraksiLain();
        selesaiSemua = true;
        JedaScriptLain(false);

        if (sembunyikanDosenSetelahSelesai && dosenSeniorObjek != null)
            dosenSeniorObjek.SetActive(false);

        saatAdeganSelesai.Invoke();

        Debug.Log("[Adegan L5-S3] Scene 3 Level 5 selesai.");
    }
}