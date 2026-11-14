using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Level 2 - Scene 1 (Onboarding), Lokasi: Ruang Dosen, Slot waktu: Pagi
//   1. MC muncul berdiri di ruang dosen (seperti level 1), lalu berjalan ke kursi dan menekan E untuk duduk
//   2. Begitu MC duduk, MC dikunci di kursi dan Kaprodi masuk menghampiri (jalur waypoint yang sama dengan level 1)
//   3. Setelah Kaprodi duduk, box dialog (dibuat di Hierarchy) tampil satu per satu, Space untuk lanjut
//   4. Kaprodi berjalan keluar (MC tetap terkunci di kursi)
//   5. Setelah Kaprodi keluar, box tutorial tampil satu per satu, Space untuk lanjut
//   6. MC dibebaskan
// Tidak ada panah petunjuk di level 2, dan tidak ada ikon E / RPS di laptop dosen selama Scene 1.
// Selama Scene 1 berjalan, semua ikon E laptop (laptop dosen, laptop kelas, dan yang diisi di "Ikon E Disembunyikan") juga disembunyikan.
//
// Cara menyambung: di ManagerLevel1Selesai (PopupLevelSelesai), event "Saat Masuk Level 2",
// tambahkan objek ini dan pilih fungsi MulaiAdegan.
//
// Batalkan(): dipanggil AdeganLevel3Scene1 saat Level 3 dimulai. Kalau Scene 1 Level 2 belum selesai (mis. MC tidak pernah duduk
// karena level dilewati saat tes), adegan ini dihentikan dan tidak ikut jalan di Level 3.
public class AdeganLevel2Scene1 : MonoBehaviour
{
    [Header("Referensi")]
    public InteractiveDesk kursiDosen;          // Objek yang memasang InteractiveDesk (kursi dosen)
    public NPCCutscene kaprodi;                 // Objek Kaprodi (kapdiam)
    public LaptopInteraction laptopDosen;       // Kosong = dicari otomatis. Ikon E dan RPS di laptop dosen dimatikan selama Scene 1

    [Header("Ikon E Disembunyikan Selama Scene 1")]
    public bool sembunyikanIkonELaptop = true;  // true = ikon E laptop dosen dan laptop kelas ikut disembunyikan selama Scene 1 berjalan
    public LaptopKelas laptopKelas;             // Kosong = dicari otomatis. Ikon E laptop kelas disembunyikan selama Scene 1
    [Tooltip("Ikon E lain yang tidak boleh muncul selama Scene 1 level 2 (mis. ikon E laptop perpustakaan atau laboratorium). Dimatikan terus selama Scene 1 berjalan, dan dibiarkan bebas setelah Scene 1 selesai.")]
    public GameObject[] ikonEDisembunyikan;

    [Header("Box Dialog Level 2 - Scene 1 (dibuat di Hierarchy, urut dari atas)")]
    public GameObject[] boxDialog;              // Box 1 = Kaprodi, Box 2 = Pemain, dst. Otomatis disembunyikan saat Play
    public KeyCode tombolLanjutBox = KeyCode.Space;   // Menutup box dan lanjut ke box berikutnya
    public bool bolehKlikMouse = true;                // Klik kiri mouse juga bisa

    [Header("Box Tutorial Setelah Kaprodi Keluar (dibuat di Hierarchy, urut dari atas)")]
    public GameObject[] boxTutorial;            // Box tutorial 1, 2, dst. Otomatis disembunyikan saat Play
    public GameObject overlayHitam;             // Opsional: panel hitam transparan satu layar di belakang box tutorial
    public bool kunciSelamaTutorial = true;     // true = MC tetap terkunci di kursi sampai tutorial ditutup. false = MC dilepas begitu box tutorial pertama muncul
    public bool hentikanWaktuSaatTutorial = true;   // Game (jam, dll) berhenti selama box tutorial tampil

    [Header("Panah Dimatikan Selama Level 2")]
    [Tooltip("Panah kursi dosen (Arrow Indicator dan Panah Dimatikan Saat Duduk di InteractiveDesk) otomatis ikut dimatikan. Isi di sini untuk panah lain, mis. panah ke kelas atau ke ruang dosen.")]
    public GameObject[] panahLainDimatikan;

    [Header("Pengaturan")]
    public float jedaSebelumKaprodi = 0.8f;     // Jeda (detik) setelah MC duduk sebelum Kaprodi masuk
    public float batasWaktuAdegan = 120f;       // Pengaman: kalau Kaprodi macet, MC dibebaskan setelah sekian detik

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Opsional: untuk menyambung ke Scene 2 level 2, dst

    private bool sudahJalan = false;
    private bool kaprodiTiba = false;

    // Untuk Batalkan(): status adegan dan hal-hal yang harus dikembalikan kalau adegan dihentikan di tengah jalan
    private bool sudahSelesai = false;
    private bool dibatalkan = false;
    private bool dialogDimatikan = false;
    private DialogueManager dialogAsliTersimpan;
    private bool tutorialAktif = false;

    void Start()
    {
        // Box boleh dibiarkan aktif saat kamu mendesain di editor, otomatis disembunyikan saat game dimulai
        SembunyikanSemuaBox(boxDialog);
        SembunyikanSemuaBox(boxTutorial);

        if (laptopDosen == null) laptopDosen = FindFirstObjectByType<LaptopInteraction>();
        if (laptopKelas == null) laptopKelas = FindFirstObjectByType<LaptopKelas>();
    }

    // LateUpdate jalan setelah semua Update: panah yang dinyalakan script lain langsung dimatikan lagi
    // sebelum sempat tampil di layar. Aktif sejak level 2 dimulai.
    void LateUpdate()
    {
        if (!sudahJalan) return;

        if (kursiDosen != null)
        {
            MatikanPanah(kursiDosen.arrowIndicator);
            MatikanPanahDaftar(kursiDosen.panahDimatikanSaatDuduk);
        }

        MatikanPanahDaftar(panahLainDimatikan);

        // Ikon E disembunyikan selama Scene 1 berjalan (setelah Scene 1 selesai atau dibatalkan, dibiarkan bebas)
        if (!sudahSelesai && !dibatalkan)
        {
            if (sembunyikanIkonELaptop)
            {
                if (laptopDosen != null) MatikanPanah(laptopDosen.petunjukTekanE);
                if (laptopKelas != null) MatikanPanah(laptopKelas.promptPressE);
            }

            MatikanPanahDaftar(ikonEDisembunyikan);
        }
    }

    private void MatikanPanah(GameObject p)
    {
        if (p != null && p.activeSelf) p.SetActive(false);
    }

    private void MatikanPanahDaftar(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject p in daftar)
        {
            MatikanPanah(p);
        }
    }

    // Dipanggil dari event "Saat Masuk Level 2" di PopupLevelSelesai.
    // Mulai menunggu: adegan baru berjalan setelah MC duduk di kursi dosen.
    public void MulaiAdegan()
    {
        if (sudahJalan || dibatalkan) return;
        sudahJalan = true;

        // Scene 1 level 2 hanya percakapan: ikon E dan RPS di laptop dosen dimatikan sampai adegan selesai
        if (laptopDosen != null) laptopDosen.isDialogDone = false;

        StartCoroutine(Alur());
    }

    // Dipanggil AdeganLevel3Scene1 saat Level 3 dimulai: kalau adegan ini belum selesai, dihentikan dan semuanya dikembalikan,
    // supaya box dialog Level 2 tidak ikut muncul menumpuk dengan dialog Level 3.
    public void Batalkan()
    {
        if (dibatalkan) return;
        dibatalkan = true;

        // Adegan sudah selesai dengan benar, tidak ada yang perlu dihentikan
        if (sudahSelesai) return;

        StopAllCoroutines();

        SembunyikanSemuaBox(boxDialog);
        SembunyikanSemuaBox(boxTutorial);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        if (tutorialAktif)
        {
            tutorialAktif = false;
            if (hentikanWaktuSaatTutorial) Time.timeScale = 1f;
        }

        if (kaprodi != null)
        {
            kaprodi.SaatTibaDanDuduk -= SaatKaprodiTiba;

            // Dialog bawaan Kaprodi dikembalikan (tadinya dimatikan sementara oleh adegan ini)
            if (dialogDimatikan)
            {
                kaprodi.dialogueManager = dialogAsliTersimpan;
                dialogDimatikan = false;
            }
        }

        if (kursiDosen != null) kursiDosen.TerkunciEksternal = false;
        if (laptopDosen != null) laptopDosen.isDialogDone = true;

        Debug.Log("[Adegan L2-S1] Scene 1 Level 2 belum selesai saat Level 3 dimulai, adegan dihentikan supaya tidak ikut jalan di Level 3.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (mulai menunggu MC duduk)")]
    private void TesAdegan()
    {
        MulaiAdegan();
    }

    private void SaatKaprodiTiba()
    {
        kaprodiTiba = true;
    }

    private void SembunyikanSemuaBox(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    private IEnumerator Alur()
    {
        // 1. Tunggu MC duduk sendiri di kursi dosen (jalan ke kursi, tekan E), lalu kunci di kursi
        if (kursiDosen != null)
        {
            while (!kursiDosen.SedangDuduk) yield return null;
            kursiDosen.TerkunciEksternal = true;
        }
        else
        {
            Debug.LogWarning("[Adegan L2-S1] Kursi Dosen belum diisi di Inspector, Kaprodi langsung masuk tanpa menunggu MC duduk.");
        }

        yield return new WaitForSeconds(jedaSebelumKaprodi);

        if (kaprodi == null)
        {
            Debug.LogWarning("[Adegan L2-S1] Kaprodi belum diisi di Inspector, adegan dilewati.");
            Selesai();
            yield break;
        }

        // 2. Kaprodi masuk. Dialog bawaan level 1 dimatikan sementara,
        //    dialog level 2 memakai box buatan sendiri (dikembalikan lagi setelah adegan)
        DialogueManager dialogAsli = kaprodi.dialogueManager;
        dialogAsliTersimpan = dialogAsli;
        dialogDimatikan = true;
        kaprodi.dialogueManager = null;

        kaprodiTiba = false;
        kaprodi.SaatTibaDanDuduk += SaatKaprodiTiba;
        kaprodi.MulaiJalanMasuk();

        float mulai = Time.time;
        while (!kaprodiTiba && Time.time - mulai < batasWaktuAdegan)
            yield return null;

        if (kaprodiTiba)
        {
            // 3. Kaprodi sudah duduk: tampilkan box dialog satu per satu
            yield return StartCoroutine(TampilkanBoxDialog());

            // 4. Kaprodi berjalan keluar (MC tetap terkunci di kursi)
            kaprodi.MulaiJalanKeluar();

            mulai = Time.time;
            while (kaprodi.gameObject.activeSelf && Time.time - mulai < batasWaktuAdegan)
                yield return null;
        }
        else
        {
            Debug.LogWarning("[Adegan L2-S1] Kaprodi tidak sampai ke tujuan sebelum batas waktu, MC dibebaskan.");
        }

        kaprodi.SaatTibaDanDuduk -= SaatKaprodiTiba;
        kaprodi.dialogueManager = dialogAsli;
        dialogDimatikan = false;

        // 5. Setelah Kaprodi keluar: box tutorial
        if (kaprodiTiba)
        {
            yield return StartCoroutine(TampilkanBoxTutorial());
        }

        Selesai();
    }

    private IEnumerator TampilkanBoxDialog()
    {
        if (boxDialog == null || boxDialog.Length == 0)
        {
            Debug.LogWarning("[Adegan L2-S1] Box Dialog masih kosong di Inspector, dialog dilewati.");
            yield break;
        }

        foreach (GameObject box in boxDialog)
        {
            if (box == null) continue;

            box.SetActive(true);

            // Jeda singkat supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSeconds(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    private IEnumerator TampilkanBoxTutorial()
    {
        if (boxTutorial == null || boxTutorial.Length == 0)
        {
            Debug.LogWarning("[Adegan L2-S1] Box Tutorial masih kosong di Inspector, tutorial dilewati.");
            yield break;
        }

        tutorialAktif = true;
        if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;

        // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
        if (overlayHitam != null) overlayHitam.SetActive(true);

        bool pertama = true;

        foreach (GameObject box in boxTutorial)
        {
            if (box == null) continue;

            box.SetActive(true);

            // Kalau "Kunci Selama Tutorial" dimatikan, MC dilepas begitu box tutorial pertama muncul
            if (pertama)
            {
                pertama = false;
                if (!kunciSelamaTutorial && kursiDosen != null) kursiDosen.TerkunciEksternal = false;
            }

            // Pakai waktu nyata karena game bisa sedang berhenti (timeScale 0)
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }

        if (overlayHitam != null) overlayHitam.SetActive(false);

        if (hentikanWaktuSaatTutorial) Time.timeScale = 1f;
        tutorialAktif = false;
    }

    private void Selesai()
    {
        sudahSelesai = true;

        // MC bebas berdiri lagi
        if (kursiDosen != null) kursiDosen.TerkunciEksternal = false;

        // Laptop dosen boleh dipakai lagi (ikon E dan RPS aktif kembali)
        if (laptopDosen != null) laptopDosen.isDialogDone = true;

        saatAdeganSelesai.Invoke();

        Debug.Log("[Adegan L2-S1] Scene 1 Onboarding selesai.");
    }
}