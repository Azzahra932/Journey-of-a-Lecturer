using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Level 3 - Scene 1 (Onboarding & Orientasi), Lokasi: Ruang dosen, Slot waktu: Pagi
//   1. Level 3 dimulai (event "Saat Masuk Level 3" di ManagerLevel2Selesai) -> Scene 1 siap
//   2. MC duduk di meja kerja (kursi dosen). Bisa duduk sendiri dengan tombol duduk, atau otomatis kalau "Duduk Otomatis" dicentang
//   3. Kaprodi masuk menghampiri (jalur waypoint yang sama dengan Level 2 Scene 1) dan duduk
//   4. Empat box dialog (Kaprodi, Pemain, Kaprodi, Pemain), Space untuk lanjut
//   5. Kaprodi berjalan keluar
//   6. Setelah Kaprodi pergi, box tutorial tampil satu per satu, Space untuk lanjut
//   7. MC dibebaskan
//
// Script ini dipasang di objek apa saja (tidak perlu collider).
// Cara menyambung: di ManagerLevel2Selesai (PopupLevel2Selesai), event "Saat Masuk Level 3",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
public class AdeganLevel3Scene1 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa melewati pop-up Level 2 dulu

    [Header("Referensi")]
    public InteractiveDesk kursiDosen;          // Kursi dosen di ruang dosen (meja kerja MC)
    public NPCCutscene kaprodi;                 // Kaprodi (kapdiam), jalur waypoint sama seperti Level 2 Scene 1
    public bool dudukOtomatis = false;          // false = MC berjalan ke kursi dan duduk sendiri. true = MC langsung didudukkan saat Scene 1 mulai
    public bool lepasEvaluasiLevel1 = true;     // true = duduk di kursi dosen tidak lagi memicu evaluasi / bimbingan level 1
    public AdeganLevel2Scene1 adeganLevel2Scene1;   // Opsional: Scene 1 Level 2. Kalau belum selesai saat Level 3 mulai, dihentikan supaya dialog Level 2 tidak ikut muncul. Kosong = dicari otomatis

    [Header("Box Dialog (urut: Kaprodi, Pemain, Kaprodi, Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumKaprodi = 0.8f;     // Jeda (detik) setelah MC duduk sebelum Kaprodi masuk
    public float batasWaktuAdegan = 120f;       // Pengaman: kalau Kaprodi macet, MC dibebaskan setelah sekian detik

    [Header("Box Tutorial (muncul setelah Kaprodi pergi)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;            // Box tutorial, urut dari atas
    public bool hentikanWaktuSaatTutorial = true;
    public bool kaprodiKeluarSebelumTutorial = true;   // true = tutorial muncul setelah Kaprodi pergi. false = tutorial langsung setelah dialog, Kaprodi keluar sesudahnya

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Opsional: untuk menyambung ke adegan Level 3 berikutnya

    private bool siap = false;
    private bool sudahJalan = false;
    private bool kaprodiTiba = false;
    private bool peringatanKursiDitampilkan = false;

    private PlayerMovement pm;
    private readonly List<Behaviour> interaksiDimatikan = new List<Behaviour>();

    // Penahan posisi: selama adegan, MC dikembalikan ke tempat duduknya kalau ada yang menggesernya
    private bool tahanPosisi = false;
    private Vector3 posisiTahan;
    private bool sudahDilaporkanPindah = false;
    private TransisiKembaliKeAwal transisi;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorial);

        transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel3Scene1[] semua = FindObjectsByType<AdeganLevel3Scene1>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L3-S1] Ada " + semua.Length + " objek yang memasang script AdeganLevel3Scene1. Pakai satu saja, dan sambungkan event Level 3 ke objek itu.");
    }

    // Dipanggil dari event "Saat Masuk Level 3" di ManagerLevel2Selesai
    public void SiapkanScene()
    {
        siap = true;
        BatalkanLevel2();
        LepasEvaluasiLevel1();
        Debug.Log("[Adegan L3-S1] Scene 1 Level 3 siap, menunggu MC duduk di kursi dosen.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (langsung mulai)")]
    private void TesAdegan()
    {
        if (sudahJalan) return;
        siap = true;
        sudahJalan = true;
        StartCoroutine(Alur());
    }

    // Menunggu MC duduk di kursi dosen (atau langsung mulai kalau Duduk Otomatis dicentang)
    void Update()
    {
        if (sudahJalan) return;

        // "Langsung Siap" dibaca di sini (bukan hanya di Start) supaya juga berlaku kalau dicentang saat Play
        if (!siap && !langsungSiap) return;

        if (kursiDosen == null)
        {
            if (!peringatanKursiDitampilkan)
            {
                peringatanKursiDitampilkan = true;
                Debug.LogWarning("[Adegan L3-S1] Kursi Dosen belum diisi di Inspector, adegan tidak bisa dimulai.");
            }
            return;
        }

        if (dudukOtomatis || kursiDosen.SedangDuduk)
        {
            sudahJalan = true;
            Debug.Log("[Adegan L3-S1] Adegan dimulai.");
            StartCoroutine(Alur());
        }
    }

    // LateUpdate jalan setelah semua Update: kalau script lain menggeser MC dari kursi (mis. karena Space),
    // MC langsung dikembalikan ke tempat duduknya.
    void LateUpdate()
    {
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
                Debug.LogWarning("[Adegan L3-S1] Ada script yang menggeser MC dari kursi dosen (posisi " + pm.transform.position
                    + "). MC dikembalikan. Lihat baris Console sebelum ini untuk tahu script mana pelakunya.");
            }

            pm.transform.position = posisiTahan;
        }
    }

    private void SaatKaprodiTiba()
    {
        kaprodiTiba = true;
    }

    private void SembunyikanSemua(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    // Scene 1 Level 2 dihentikan kalau belum selesai, supaya box dialog dan Kaprodi-nya tidak ikut jalan bersama Scene 1 Level 3
    private void BatalkanLevel2()
    {
        AdeganLevel2Scene1 a = adeganLevel2Scene1;
        if (a == null) a = FindFirstObjectByType<AdeganLevel2Scene1>();
        if (a != null) a.Batalkan();
    }

    // Kursi dosen dipakai untuk adegan level 3, jadi duduk di sini tidak boleh memicu evaluasi / bimbingan level 1 lagi
    private void LepasEvaluasiLevel1()
    {
        if (!lepasEvaluasiLevel1 || kursiDosen == null) return;

        if (kursiDosen.evaluasiScript != null) kursiDosen.evaluasiScript = null;
        if (kursiDosen.bimbinganScript != null) kursiDosen.bimbinganScript = null;
    }

    private IEnumerator Alur()
    {
        BatalkanLevel2();
        LepasEvaluasiLevel1();

        // 1. MC duduk di meja kerja (otomatis kalau diatur begitu), lalu dikunci di kursi
        if (dudukOtomatis && !kursiDosen.SedangDuduk)
            kursiDosen.DudukUntukAdegan();

        kursiDosen.TerkunciEksternal = true;
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
            Debug.LogWarning("[Adegan L3-S1] Kaprodi belum diisi di Inspector, bagian Kaprodi dilewati.");
            yield return StartCoroutine(TampilkanTutorial());
            Selesai();
            yield break;
        }

        // 2. Kaprodi masuk. Dialog bawaan level 1 dimatikan sementara (dikembalikan lagi setelah adegan).
        DialogueManager dialogAsli = kaprodi.dialogueManager;
        kaprodi.dialogueManager = null;

        // Kunci Kaprodi: perintah keluar dari script adegan lain (Level 1/2) diabaikan selama adegan ini
        kaprodi.KunciAdegan(true);

        kaprodiTiba = false;
        kaprodi.SaatTibaDanDuduk += SaatKaprodiTiba;
        kaprodi.MulaiJalanMasuk();

        float mulai = Time.time;
        while (!kaprodiTiba && Time.time - mulai < batasWaktuAdegan)
            yield return null;

        if (kaprodiTiba)
        {
            // 3. Empat box dialog
            if (boxDialog == null || boxDialog.Length == 0)
                Debug.LogWarning("[Adegan L3-S1] Box Dialog masih kosong di Inspector, dialog dilewati.");
            else
            {
                Debug.Log("[Adegan L3-S1] Dialog dipanggil.");
                yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));
            }

            // 4. Kaprodi pergi, lalu tutorial (atau tutorial dulu kalau diatur begitu)
            if (kaprodiKeluarSebelumTutorial)
            {
                yield return StartCoroutine(KaprodiKeluar());
                yield return StartCoroutine(TampilkanTutorial());
            }
            else
            {
                yield return StartCoroutine(TampilkanTutorial());
                yield return StartCoroutine(KaprodiKeluar());
            }
        }
        else
        {
            Debug.LogWarning("[Adegan L3-S1] Kaprodi tidak sampai ke tujuan sebelum batas waktu, adegan dilanjutkan tanpa dialog.");
            yield return StartCoroutine(TampilkanTutorial());
        }

        // Lepas kunci dan kembalikan pengaturan asli Kaprodi
        kaprodi.SaatTibaDanDuduk -= SaatKaprodiTiba;
        kaprodi.KunciAdegan(false);
        kaprodi.dialogueManager = dialogAsli;

        Selesai();
    }

    // Kaprodi berjalan keluar, tunggu sampai objeknya dinonaktifkan oleh NPCCutscene
    private IEnumerator KaprodiKeluar()
    {
        // Pakai method khusus adegan (bukan MulaiJalanKeluar) supaya tidak terblokir kunci
        kaprodi.MulaiJalanKeluarAdegan();

        float mulai = Time.time;
        while (kaprodi.gameObject.activeSelf && Time.time - mulai < batasWaktuAdegan)
            yield return null;

        if (kaprodi.gameObject.activeSelf)
            Debug.LogWarning("[Adegan L3-S1] Batas waktu habis sebelum Kaprodi selesai keluar, adegan dilanjutkan.");
    }

    // Box tutorial: game berhenti (opsional) dan layar digelapkan selama box tampil
    private IEnumerator TampilkanTutorial()
    {
        if (boxTutorial == null || boxTutorial.Length == 0)
        {
            Debug.LogWarning("[Adegan L3-S1] Box Tutorial masih kosong di Inspector, tutorial dilewati.");
            yield break;
        }

        if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;

        // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
        if (overlayHitam != null)
        {
            overlayHitam.SetActive(true);
            overlayHitam.transform.SetAsLastSibling();   // overlay di depan UI lain, lalu box tutorial dipaksa di depan overlay
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
                Debug.LogWarning("[Adegan L3-S1] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    // Mematikan sementara script kursi dan laptop lain yang ikut membaca tombol Space (kursi dosen sendiri tetap menyala)
    private void MatikanInteraksiLain()
    {
        foreach (InteractiveDesk d in FindObjectsByType<InteractiveDesk>(FindObjectsSortMode.None))
        {
            if (d != kursiDosen) TambahDimatikan(d);
        }

        foreach (KursiPerpusInteraction k in FindObjectsByType<KursiPerpusInteraction>(FindObjectsSortMode.None))
            TambahDimatikan(k);

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
        // MC tidak ditahan lagi, bebas berdiri
        tahanPosisi = false;
        if (kursiDosen != null) kursiDosen.TerkunciEksternal = false;

        // Pastikan Kaprodi tidak tertinggal dalam keadaan terkunci
        if (kaprodi != null) kaprodi.KunciAdegan(false);

        // Interaksi kursi dan laptop dinyalakan lagi
        NyalakanInteraksiLain();

        saatAdeganSelesai.Invoke();

        Debug.Log("[Adegan L3-S1] Scene 1 Level 3 selesai.");
    }
}