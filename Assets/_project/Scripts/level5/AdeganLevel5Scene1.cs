using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Level 5 - Scene 1 (Onboarding Guru Besar), Lokasi: Ruang Direktur
// Alurnya mirip Level 4 Scene 1, tapi tanpa panah, transisi layar hitam, dan box kalimat (itu hanya ada di Level 4):
//   1. Level 5 dimulai (event "Saat Masuk Level Berikutnya" di PopupLevel4Selesai) -> Scene 1 siap
//      MC dipindah ke Titik Mulai MC di ruang direktur (kalau diisi)
//   2. MC duduk di meja kerja ruang direktur (sendiri, atau otomatis kalau "Duduk Otomatis" dicentang, hanya untuk kursi InteractiveDesk)
//      Gambar duduk MC level 5 (objek dari Hierarchy atau sprite dari folder, kalau diisi) dipasang ke kursi sebelum MC duduk
//   3. Kaprodi muncul di Titik Awal, berjalan lewat Titik Masuk, lalu duduk di kursi tamu
//   4. Empat box dialog (Kaprodi, Pemain, Kaprodi, Pemain), Space untuk lanjut
//   5. Kaprodi berdiri dan berjalan keluar lewat Titik Keluar. Box tutorial (opsional) lalu tampil
//   6. MC dibebaskan, Scene 1 selesai (event "Saat Adegan Selesai")
//
// Kursi ruang direktur boleh memakai script sendiri (KursiRektorInteraction), InteractiveDesk, atau KursiPerpusInteraction. Isi salah satu saja.
// Script ini dipasang di objek apa saja (tidak perlu collider).
// Cara menyambung: di PopupLevel4Selesai, event "Saat Masuk Level Berikutnya",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Bagian yang tidak diisi di Inspector (mis. tutorial, poin, reward, gambar duduk) dilewati.
public class AdeganLevel5Scene1 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa melewati pop-up Level 4 dulu

    [Header("Kursi Ruang Direktur (isi SALAH SATU sesuai komponen di kursinya)")]
    public KursiRektorInteraction kursiRektor;          // Kalau kursinya memakai script sendiri (KursiRektorInteraction)
    public InteractiveDesk kursiRektorDesk;             // Kalau kursinya memakai komponen InteractiveDesk
    public KursiPerpusInteraction kursiRektorPerpus;    // Kalau kursinya memakai komponen KursiPerpusInteraction
    public bool dudukOtomatis = false;                  // false = MC berjalan ke kursi dan duduk sendiri. true = MC langsung didudukkan (hanya untuk kursi InteractiveDesk)
    public bool lepasEvaluasiLevel1 = true;             // true = duduk di kursi InteractiveDesk tidak lagi memicu evaluasi / bimbingan level 1

    [Header("Posisi Awal MC di Ruang Direktur (opsional)")]
    [Tooltip("Objek kosong di ruang direktur (dekat meja kerja). Saat Scene 1 siap, MC dipindah ke titik ini. Kosong = MC tidak dipindah (tetap di titik awal transisi).")]
    public Transform titikMulaiMC;

    [Header("Gambar Duduk MC Level 5 (opsional)")]
    [Tooltip("CARA TERMUDAH: buat objek di Hierarchy dengan SpriteRenderer berisi gambar MC duduk level 5, atur posisi dan ukurannya langsung di tab Scene. Script memasangnya sebagai objek duduk (Sit) kursi, jadi ukuran dan posisinya persis seperti yang kamu lihat. Objek ini otomatis disembunyikan saat Play dan hanya muncul saat MC duduk. Berlaku untuk kursi KursiRektorInteraction dan KursiPerpusInteraction. Untuk kursi InteractiveDesk, hanya gambarnya (sprite) yang dipakai.")]
    public GameObject objekDudukMCLevel5;
    [Tooltip("Alternatif kalau Objek Duduk di atas kosong: sprite dari folder Project. Dipasang ke Sprite Duduk Baru (KursiRektorInteraction), Sit Sprite (InteractiveDesk), atau SpriteRenderer objek Sit (KursiPerpusInteraction).")]
    public Sprite spriteDudukMCLevel5;

    [Header("Kaprodi (jalan di ruang direktur, sama seperti Level 4 Scene 4)")]
    public KaprodiJalan2Arah kaprodi;           // Objek induk Kaprodi (yang punya script KaprodiJalan2Arah). Disembunyikan di awal
    public Transform titikAwal;                 // Tempat Kaprodi muncul
    public Transform[] titikMasuk;              // Jalur ke kursi tamu, urut. Titik terakhir sebaiknya dekat kursi tamu
    public Transform kursiTamu;                 // Objek kursi tamu. Setelah sampai di titik terakhir, Kaprodi langsung duduk tepat di posisi kursi ini. Kosong = duduk di titik terakhir Titik Masuk
    public Vector2 offsetDudukDiKursi = Vector2.zero;   // Geser posisi duduk dari pusat kursi tamu (kalau Kaprodi terlihat terlalu tinggi / rendah di kursi)
    public Transform[] titikKeluar;             // Jalur keluar, urut. Titik terakhir = tempat Kaprodi menghilang
    public float jedaSebelumKaprodi = 0.8f;     // Jeda (detik) setelah MC duduk sebelum Kaprodi muncul
    public float jedaSetelahDudukKaprodi = 0.5f;   // Jeda (detik) setelah Kaprodi duduk sebelum dialog

    [Header("Dijeda Selama Adegan (opsional)")]
    [Tooltip("Script adegan Level 4 yang masih aktif dan bisa ikut membaca tombol E / Space di ruang direktur (mis. AdeganLevel4Scene4, AdeganLevel4Scene6). Dimatikan selama adegan ini, dinyalakan lagi di akhir.")]
    public Behaviour[] scriptDijeda;
    [Tooltip("Objek tampilan lama yang harus tersembunyi selama adegan ini (mis. ikon E laptop direktur, lingkaran / baterai cooldown).")]
    public GameObject[] objekDisembunyikan;

    [Header("Box Dialog (urut: Kaprodi, Pemain, Kaprodi, Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.3f;      // Jeda (detik) setelah Kaprodi duduk sebelum dialog muncul

    [Header("Box Tutorial (opsional, muncul setelah Kaprodi keluar)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;            // Kosong = tanpa tutorial
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Poin dan Reward (opsional)")]
    public int jumlahPoin = 0;                  // Poin yang didapat setelah adegan (0 = tanpa poin)
    public int indexReward = -1;                // Nomor reward di RewardManager. -1 = tanpa reward

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil setelah semuanya selesai (untuk menyambung ke adegan Level 5 berikutnya)

    private bool siap = false;
    private bool sudahJalan = false;
    private bool selesaiSemua = false;
    private bool peringatanKursiDitampilkan = false;

    private PlayerMovement pm;
    private readonly List<Behaviour> interaksiDimatikan = new List<Behaviour>();
    private readonly List<Behaviour> scriptDijedaAktif = new List<Behaviour>();

    // Penahan posisi: selama dialog, MC dikembalikan ke tempat duduknya kalau ada yang menggesernya
    private bool tahanPosisi = false;
    private Vector3 posisiTahan;
    private bool sudahDilaporkanPindah = false;
    private TransisiKembaliKeAwal transisi;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorial);
        if (kaprodi != null) kaprodi.gameObject.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        // Objek gambar duduk MC level 5 disembunyikan saat Play (baru muncul saat MC duduk, lewat kursi)
        if (objekDudukMCLevel5 != null) objekDudukMCLevel5.SetActive(false);

        transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();

        // Mode tes: MC dipindah ke ruang direktur dan gambar duduk level 5 langsung dipasang (di game biasa, lewat SiapkanScene)
        if (langsungSiap)
        {
            PindahkanMCKeTitikMulai();
            TerapkanSpriteMC();
        }

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel5Scene1[] semua = FindObjectsByType<AdeganLevel5Scene1>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L5-S1] Ada " + semua.Length + " objek yang memasang script AdeganLevel5Scene1. Pakai satu saja.");
    }

    // Dipanggil dari event "Saat Masuk Level Berikutnya" di PopupLevel4Selesai
    public void SiapkanScene()
    {
        siap = true;
        PindahkanMCKeTitikMulai();
        LepasEvaluasiLevel1();
        TerapkanSpriteMC();
        Debug.Log("[Adegan L5-S1] Scene 1 Level 5 siap, menunggu MC duduk di meja kerja ruang direktur.");
    }

    // Memindahkan MC ke Titik Mulai MC di ruang direktur (kalau diisi)
    private void PindahkanMCKeTitikMulai()
    {
        if (titikMulaiMC == null) return;

        PlayerMovement p = FindFirstObjectByType<PlayerMovement>();
        if (p == null)
        {
            Debug.LogWarning("[Adegan L5-S1] Pemain (PlayerMovement) tidak ditemukan, MC tidak bisa dipindah ke Titik Mulai MC.");
            return;
        }

        Vector3 pos = titikMulaiMC.position;
        pos.z = p.transform.position.z;

        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = new Vector2(pos.x, pos.y);
        }

        p.transform.position = pos;

        Debug.Log("[Adegan L5-S1] MC dipindah ke Titik Mulai MC di ruang direktur.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (langsung mulai)")]
    private void TesAdegan()
    {
        if (sudahJalan) return;
        siap = true;
        sudahJalan = true;
        PindahkanMCKeTitikMulai();
        TerapkanSpriteMC();
        StartCoroutine(Alur());
    }

    // Menunggu MC duduk di kursi ruang direktur (atau langsung mulai kalau Duduk Otomatis dicentang)
    void Update()
    {
        if (sudahJalan) return;

        // "Langsung Siap" dibaca di sini (bukan hanya di Start) supaya juga berlaku kalau dicentang saat Play
        if (!siap && !langsungSiap) return;

        if (!AdaKursi())
        {
            if (!peringatanKursiDitampilkan)
            {
                peringatanKursiDitampilkan = true;
                Debug.LogWarning("[Adegan L5-S1] Kursi belum diisi di Inspector (isi salah satu: Kursi Rektor, Kursi Rektor Desk, atau Kursi Rektor Perpus), adegan tidak bisa dimulai.");
            }
            return;
        }

        if (dudukOtomatis || MCSedangDuduk())
        {
            sudahJalan = true;
            Debug.Log("[Adegan L5-S1] Adegan dimulai.");
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
                Debug.LogWarning("[Adegan L5-S1] Ada script yang menggeser MC dari kursi (posisi " + pm.transform.position
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
    //   - KursiRektorInteraction: objek Sit diganti, dan Sprite Duduk Baru dikosongkan supaya sprite objek tidak ditimpa sprite lama
    //   - KursiPerpusInteraction: objek Sit diganti
    //   - InteractiveDesk: tidak punya objek Sit (sprite pemain sendiri yang diganti), jadi hanya gambar dari objek yang dipakai
    // Cara 2 (Sprite dari folder, kalau Objek Duduk kosong):
    //   - KursiRektorInteraction memakai field spriteDudukBaru saat MC duduk (juga dipasang langsung ke gambar Sit)
    //   - InteractiveDesk memakai field sitSprite saat SitDown()
    //   - KursiPerpusInteraction memakai objek "Sit" terpisah, jadi sprite dipasang ke SpriteRenderer objek itu
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
                Debug.Log("[Adegan L5-S1] Sprite duduk kursi (KursiRektorInteraction) diganti ke sprite level 5.");
            }

            // Sekalian dipasang langsung ke gambar Sit (juga kalau SpriteRenderer-nya ada di child)
            if (kursiRektor.spriteDuduk != null)
            {
                SpriteRenderer sr = kursiRektor.spriteDuduk.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null && sr.sprite != spriteDudukMCLevel5) sr.sprite = spriteDudukMCLevel5;
            }
        }

        if (kursiRektorDesk != null && kursiRektorDesk.sitSprite != spriteDudukMCLevel5)
        {
            kursiRektorDesk.sitSprite = spriteDudukMCLevel5;
            Debug.Log("[Adegan L5-S1] Sprite duduk kursi (InteractiveDesk) diganti ke sprite level 5.");
        }

        if (kursiRektorPerpus != null && kursiRektorPerpus.spriteDuduk != null)
        {
            SpriteRenderer sr = kursiRektorPerpus.spriteDuduk.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null)
            {
                Debug.LogWarning("[Adegan L5-S1] Objek Sprite Duduk pada kursi tidak punya SpriteRenderer (di objek itu maupun child-nya), sprite level 5 tidak dipasang.");
            }
            else if (sr.sprite != spriteDudukMCLevel5)
            {
                sr.sprite = spriteDudukMCLevel5;
                Debug.Log("[Adegan L5-S1] Sprite duduk kursi (KursiPerpusInteraction) diganti ke sprite level 5.");
            }
        }
    }

    // Objek Duduk dari Hierarchy dipasang sebagai objek Sit kursi
    private void TerapkanObjekDuduk()
    {
        SpriteRenderer srObjek = objekDudukMCLevel5.GetComponentInChildren<SpriteRenderer>(true);
        if (srObjek == null)
            Debug.LogWarning("[Adegan L5-S1] Objek Duduk MC Level 5 tidak punya SpriteRenderer (di objek itu maupun child-nya). Tambahkan SpriteRenderer berisi gambar MC duduk.");

        if (kursiRektor != null)
        {
            if (kursiRektor.spriteDuduk != objekDudukMCLevel5)
            {
                kursiRektor.spriteDuduk = objekDudukMCLevel5;
                Debug.Log("[Adegan L5-S1] Objek duduk kursi (KursiRektorInteraction) diganti ke objek duduk level 5.");
            }

            // Kosongkan supaya kursi tidak menimpa sprite objek dengan sprite lama saat MC duduk
            kursiRektor.spriteDudukBaru = null;
        }

        if (kursiRektorPerpus != null && kursiRektorPerpus.spriteDuduk != objekDudukMCLevel5)
        {
            kursiRektorPerpus.spriteDuduk = objekDudukMCLevel5;
            Debug.Log("[Adegan L5-S1] Objek duduk kursi (KursiPerpusInteraction) diganti ke objek duduk level 5.");
        }

        // InteractiveDesk tidak punya objek Sit: hanya gambar dari objek yang dipakai
        if (kursiRektorDesk != null && srObjek != null && srObjek.sprite != null && kursiRektorDesk.sitSprite != srObjek.sprite)
        {
            kursiRektorDesk.sitSprite = srObjek.sprite;
            Debug.Log("[Adegan L5-S1] Sprite duduk kursi (InteractiveDesk) diambil dari objek duduk level 5.");
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
                Debug.LogWarning("[Adegan L5-S1] Duduk Otomatis hanya bisa untuk kursi InteractiveDesk. MC harus duduk sendiri di kursi ini.");
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

        yield return new WaitForSeconds(jedaSebelumKaprodi);

        if (kaprodi == null)
        {
            Debug.LogWarning("[Adegan L5-S1] Kaprodi belum diisi di Inspector, bagian Kaprodi dilewati.");
        }
        else
        {
            yield return StartCoroutine(AlurKaprodi());
        }

        // MC dibebaskan dari kursi
        tahanPosisi = false;
        KunciKursi(false);
        NyalakanInteraksiLain();

        // Poin dan reward (opsional)
        if (jumlahPoin > 0 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoin);
            Debug.Log("[Adegan L5-S1] Skor bertambah +" + jumlahPoin);
        }

        if (indexReward >= 0 && RewardManager.Instance != null)
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));

        Selesai();
    }

    // Kaprodi masuk dan duduk, empat dialog, Kaprodi keluar, tutorial (opsional)
    private IEnumerator AlurKaprodi()
    {
        // 1. Kaprodi muncul dan berjalan ke kursi tamu
        if (titikAwal != null) kaprodi.transform.position = titikAwal.position;
        kaprodi.gameObject.SetActive(true);

        if (titikMasuk == null || titikMasuk.Length == 0)
            Debug.LogWarning("[Adegan L5-S1] Titik Masuk masih kosong di Inspector, Kaprodi langsung duduk di tempat.");
        else
            yield return StartCoroutine(kaprodi.JalanMelalui(titikMasuk));

        // Sampai di titik terakhir: langsung duduk, gambar duduk dipasang tepat di posisi kursi tamu
        if (kursiTamu != null)
        {
            kaprodi.DudukDi((Vector2)kursiTamu.position + offsetDudukDiKursi);
        }
        else
        {
            Debug.LogWarning("[Adegan L5-S1] Kursi Tamu belum diisi di Inspector, Kaprodi duduk di titik terakhir Titik Masuk.");
            kaprodi.Duduk();
        }

        yield return new WaitForSeconds(jedaSetelahDudukKaprodi);

        // 2. Empat box dialog
        yield return new WaitForSeconds(jedaSebelumDialog);

        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L5-S1] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

        // 3. Kaprodi berdiri dan berjalan keluar
        yield return StartCoroutine(KaprodiKeluar());

        // 4. Tutorial (opsional)
        yield return StartCoroutine(TampilkanTutorial());
    }

    // Kaprodi berdiri, berjalan ke titik keluar, lalu menghilang
    private IEnumerator KaprodiKeluar()
    {
        if (kaprodi == null) yield break;

        kaprodi.Berdiri();

        if (titikKeluar == null || titikKeluar.Length == 0)
            Debug.LogWarning("[Adegan L5-S1] Titik Keluar masih kosong di Inspector, Kaprodi langsung menghilang.");
        else
            yield return StartCoroutine(kaprodi.JalanMelalui(titikKeluar));

        kaprodi.gameObject.SetActive(false);
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
                Debug.LogWarning("[Adegan L5-S1] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    // Script adegan Level 4 yang bisa ikut membaca E / Space dimatikan selama adegan ini, dinyalakan lagi di akhir
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

    // Mematikan sementara script kursi dan laptop lain yang ikut membaca tombol Space (kursi ruang direktur sendiri tetap menyala)
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

        saatAdeganSelesai.Invoke();

        Debug.Log("[Adegan L5-S1] Scene 1 Level 5 selesai.");
    }
}