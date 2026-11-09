using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Level 3 - Scene 5 (Kolaborasi Akademik), Lokasi: Ruang dosen
//   1. Scene 4 Level 3 selesai -> Scene 5 siap
//   2. MC ke ruang dosen dan duduk di kursinya (kursi dosen). Bisa duduk sendiri, atau otomatis kalau "Duduk Otomatis" dicentang
//   3. Rekan dosen muncul di pintu, berjalan lewat jalur, lalu duduk di kursi di sampingnya
//   4. Tiga box dialog (Rekan Dosen, Pemain, Rekan Dosen), Space untuk lanjut
//   5. Ikon E muncul di laptop (selama MC duduk). Tekan E: muncul dua box pilihan dulu, jujur (atas) atau bohong (bawah, integritas -15),
//      lalu box pilihan jurnal (klik salah satu)
//        Jurnal Sinta 1-2 : poin 10
//        Jurnal Sinta 3-4 : poin 8
//        Jurnal Sinta 5-6 : poin 6
//      Lama bar bergantung pada pilihan jujur / bohong (bukan pilihan Sinta): jujur 15 detik, bohong 10 detik
//   6. Bar hijau berputar, poin bertambah, reward, lalu box tutorial (di atas overlay hitam, sekali saja)
//   7. Energi berkurang, cooldown baterai mulai (layar laptop redup + baterai terisi)
//   8. Setelah cooldown penuh, MC bisa memakai laptop lagi (berulang): ikon E, pilihan jurnal, bar, poin, reward
//
// Rekan dosen TIDAK memakai Animator: gambar diam dan jalan diganti langsung oleh script dari daftar sprite di Inspector (RD1, RD2, RD3).
// Dua objek: yang BERJALAN dan yang sudah DUDUK di kursinya. Saat yang berjalan tiba, ia menghilang dan yang duduk muncul.
// (Kalau Rekan Dosen Duduk dikosongkan, gambar yang berjalan diganti jadi Sprite Duduk Rekan.)
//
// Script ini dipasang di objek apa saja (tidak perlu collider).
// Cara menyambung dari Scene 4: di AdeganLevel3Scene4, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Bagian yang tidak diisi di Inspector (mis. reward atau tutorial) dilewati.
public class AdeganLevel3Scene5 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 4 dulu

    [Header("Kursi MC (kursi dosen di ruang dosen)")]
    public InteractiveDesk kursiDosen;          // Kursi dosen (sama dengan yang dipakai Scene 1 Level 3)
    public bool dudukOtomatis = false;          // false = MC berjalan ke kursi dan duduk sendiri. true = MC langsung didudukkan saat Scene 5 mulai
    public bool lepasEvaluasiLevel1 = true;     // true = duduk di kursi dosen tidak lagi memicu evaluasi / bimbingan level 1
    public LaptopInteraction laptopDosenLama;   // Opsional: script LaptopInteraction di laptop dosen. Dimatikan begitu Scene 5 mulai dan tidak dinyalakan lagi

    [Header("Rekan Dosen (gambar digerakkan script, tanpa Animator)")]
    public GameObject rekanDosen;               // Objek rekan dosen yang BERJALAN (punya SpriteRenderer). Dinonaktifkan di awal
    public GameObject rekanDudukObjek;          // Objek rekan dosen yang sudah DUDUK di kursinya (diletakkan manual di kursi samping MC). Nonaktif di awal; muncul saat yang berjalan tiba, lalu yang berjalan menghilang
    public Transform[] jalur;                   // Titik jalan berurutan dari pintu sampai dekat kursi
    public Transform titikDuduk;                // Titik tujuan rekan dosen yang berjalan (dekat kursi samping kursi MC)
    public float kecepatan = 2.5f;
    public bool gambarMenghadapKanan = true;    // true = gambar jalan aslinya menghadap kanan (dibalik otomatis saat berjalan ke kiri). Kalau terbalik, hilangkan centang

    [Header("Gambar Rekan Dosen")]
    public Sprite spriteDiam;                   // Gambar berdiri diam (mis. RD1)
    public Sprite[] spriteJalan;                // Gambar berjalan berurutan, mis. RD2, RD1, RD3, RD1
    public float fpsJalan = 8f;                 // Kecepatan ganti gambar saat berjalan (gambar per detik)
    public Sprite spriteDudukRekan;             // Hanya dipakai kalau Rekan Dosen Duduk kosong: gambar duduk untuk objek yang berjalan (mis. RDduduk)
    public int sortingOrderDuduk = 2;           // Order in Layer saat duduk (hanya kalau memakai Sprite Duduk Rekan)
    public float skalaSaatDuduk = 1f;           // Pengali ukuran saat duduk (hanya kalau memakai Sprite Duduk Rekan)

    [Header("Box Dialog (urut: Rekan Dosen, Pemain, Rekan Dosen)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumRekan = 0.8f;       // Jeda (detik) setelah MC duduk sebelum rekan dosen muncul
    public float jedaSebelumDialog = 0.8f;      // Jeda (detik) setelah rekan dosen duduk sebelum dialog muncul
    public float batasWaktuAdegan = 120f;       // Pengaman: kalau rekan dosen macet, adegan dilanjutkan setelah sekian detik

    [Header("Laptop Dosen")]
    public GameObject ikonE;                    // Ikon "E" di atas laptop dosen, muncul selama MC duduk setelah dialog selesai
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Pilihan Jujur / Bohong (muncul dulu sebelum pilihan jurnal)")]
    public GameObject boxJujur;                 // Box pilihan atas: jujur
    public GameObject boxBohong;                // Box pilihan bawah: bohong
    public Button tombolJujur;                  // Button di box jujur (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolBohong;                 // Button di box bohong (jangan isi On Click di Inspector, dipasang lewat script)
    public float penguranganIntegritasBohong = 15f;   // Integritas berkurang kalau memilih bohong

    [Header("Box Pilihan Jurnal (pertanyaannya berbeda dari Scene 4, tombolnya sama)")]
    public GameObject boxPilihan;               // Panel pilihan (judul + tiga tombol)
    public Button tombolSinta12;                // Button Jurnal Sinta 1-2 (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolSinta34;                // Button Jurnal Sinta 3-4
    public Button tombolSinta56;                // Button Jurnal Sinta 5-6

    [Header("Poin per Pilihan Jurnal")]
    public int poinSinta12 = 10;
    public int poinSinta34 = 8;
    public int poinSinta56 = 6;

    [Header("Lama Bar Berdasarkan Pilihan Jujur / Bohong (berlaku untuk semua pilihan Sinta)")]
    public float durasiJujur = 15f;             // Lama bar berputar kalau memilih jujur (detik)
    public float durasiBohong = 10f;            // Lama bar berputar kalau memilih bohong (detik)

    [Header("Bar Hijau Berputar")]
    public GameObject canvasProgressBar;        // Objek induk bar (boleh objek yang sama dengan Scene 4)
    public Image circularProgressBar;           // Image bar (Image Type: Filled)

    [Header("Setelah Bar Selesai")]
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
    public bool bisaDiulang = true;             // true = setelah cooldown selesai, laptop dosen bisa dipakai lagi

    [Header("Cooldown Baterai (seperti Scene 4)")]
    public float durasiCooldown = 60f;          // Jeda (detik) sebelum laptop bisa dipakai lagi
    public GameObject ikonBateraiLemah;         // Objek induk baterai, muncul saat cooldown
    public IndikatorBaterai indikatorBaterai;   // Script pengisi segmen baterai
    public TMP_Text teksCooldown;               // Teks di dekat baterai (contoh "Recharging...")
    public string isiTeksCooldown = "Recharging...";
    public GameObject teksIstirahat;            // Opsional: teks "Laptop sedang istirahat" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public SpriteRenderer spriteLaptop;         // Gambar laptop dosen yang diredupkan selama cooldown
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Layar redup

    [Header("Event")]
    public UnityEvent saatDialogSelesai;        // Dipanggil sekali setelah tiga dialog selesai
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah aktivitas laptop pertama selesai (untuk menyambung ke scene berikutnya)

    private bool siap = false;
    private bool sudahJalan = false;            // true setelah adegan (rekan dosen + dialog) dimulai
    private bool sedangBerjalan = false;
    private bool dialogSelesai = false;
    private bool sudahPernahSelesai = false;
    private bool eventSudahDipanggil = false;
    private bool tutorialSudahTampil = false;
    private bool peringatanKursiDitampilkan = false;
    private int pilihanJujur = 0;               // 0 = belum memilih, 1 = jujur, 2 = bohong
    private int pilihan = 0;                    // 0 = belum memilih, 1 = Sinta 1-2, 2 = Sinta 3-4, 3 = Sinta 5-6

    private PlayerMovement pm;
    private readonly List<Behaviour> interaksiDimatikan = new List<Behaviour>();

    // Penahan posisi: selama dialog, MC dikembalikan ke tempat duduknya kalau ada yang menggesernya
    private bool tahanPosisi = false;
    private Vector3 posisiTahan;
    private bool sudahDilaporkanPindah = false;
    private TransisiKembaliKeAwal transisi;

    // Cegah tombol untuk DUDUK ikut terbaca sebagai E untuk laptop
    private bool dudukSebelumnya = false;
    private float waktuMulaiDuduk = 0f;
    private const float jedaSetelahDuduk = 0.3f;

    // Cooldown
    private bool cooldownAktif = false;
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;

    // Rekan dosen
    private SpriteRenderer srRekan;
    private Vector3 skalaRekanAwal = Vector3.one;
    private float waktuAnimasi = 0f;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorial);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxReward != null) boxReward.SetActive(false);
        if (ikonE != null) ikonE.SetActive(false);
        if (rekanDosen != null) rekanDosen.SetActive(false);
        if (rekanDudukObjek != null) rekanDudukObjek.SetActive(false);
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
        transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel3Scene5[] semua = FindObjectsByType<AdeganLevel3Scene5>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L3-S5] Ada " + semua.Length + " objek yang memasang script AdeganLevel3Scene5. Pakai satu saja.");
    }

    void OnDestroy()
    {
        if (tombolJujur != null) tombolJujur.onClick.RemoveListener(PilihJujur);
        if (tombolBohong != null) tombolBohong.onClick.RemoveListener(PilihBohong);

        if (tombolSinta12 != null) tombolSinta12.onClick.RemoveListener(PilihSinta12);
        if (tombolSinta34 != null) tombolSinta34.onClick.RemoveListener(PilihSinta34);
        if (tombolSinta56 != null) tombolSinta56.onClick.RemoveListener(PilihSinta56);
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel3Scene4
    public void SiapkanScene()
    {
        siap = true;
        LepasEvaluasiLevel1();
        Debug.Log("[Adegan L3-S5] Scene 5 siap, menunggu MC duduk di kursi dosen.");
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

        if (kursiDosen == null)
        {
            if (!peringatanKursiDitampilkan)
            {
                peringatanKursiDitampilkan = true;
                Debug.LogWarning("[Adegan L3-S5] Kursi Dosen belum diisi di Inspector, adegan tidak bisa dimulai.");
            }
            return;
        }

        bool duduk = kursiDosen.SedangDuduk;
        if (duduk && !dudukSebelumnya) waktuMulaiDuduk = Time.time;
        dudukSebelumnya = duduk;

        // Tahap 1: tunggu MC duduk di kursi dosen (atau langsung mulai kalau Duduk Otomatis dicentang)
        if (!sudahJalan)
        {
            if (dudukOtomatis || duduk)
            {
                sudahJalan = true;
                Debug.Log("[Adegan L3-S5] Adegan dimulai.");
                StartCoroutine(Alur());
            }
            return;
        }

        // Tahap 2: setelah dialog selesai, ikon E muncul di laptop selama MC duduk
        if (!dialogSelesai) return;

        if (sudahPernahSelesai && !bisaDiulang)
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
            StartCoroutine(AlurAktivitas());
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
                Debug.LogWarning("[Adegan L3-S5] Ada script yang menggeser MC dari kursi dosen (posisi " + pm.transform.position
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

    private void TampilkanPilihan(bool tampil)
    {
        if (boxPilihan != null) boxPilihan.SetActive(tampil);
        if (tombolSinta12 != null) tombolSinta12.gameObject.SetActive(tampil);
        if (tombolSinta34 != null) tombolSinta34.gameObject.SetActive(tampil);
        if (tombolSinta56 != null) tombolSinta56.gameObject.SetActive(tampil);
    }

    // Kursi dosen dipakai untuk adegan level 3, jadi duduk di sini tidak boleh memicu evaluasi / bimbingan level 1 lagi
    private void LepasEvaluasiLevel1()
    {
        if (!lepasEvaluasiLevel1 || kursiDosen == null) return;

        if (kursiDosen.evaluasiScript != null) kursiDosen.evaluasiScript = null;
        if (kursiDosen.bimbinganScript != null) kursiDosen.bimbinganScript = null;
    }

    // ---------- Adegan: MC duduk, rekan dosen datang, tiga dialog ----------
    private IEnumerator Alur()
    {
        sedangBerjalan = true;
        LepasEvaluasiLevel1();

        // 1. MC duduk di kursi dosen (otomatis kalau diatur begitu), lalu dikunci di kursi
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

        yield return new WaitForSeconds(jedaSebelumRekan);

        // 2. Rekan dosen masuk, berjalan, lalu duduk di samping MC
        if (rekanDosen == null)
        {
            Debug.LogWarning("[Adegan L3-S5] Rekan Dosen belum diisi di Inspector, bagian rekan dosen dilewati.");
        }
        else
        {
            yield return StartCoroutine(RekanMasukDanDuduk());
            yield return new WaitForSeconds(jedaSebelumDialog);
        }

        // 3. Tiga box dialog
        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L3-S5] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

        // 4. Dialog selesai: MC bebas berdiri, ikon E akan muncul di laptop selama MC duduk
        tahanPosisi = false;
        kursiDosen.TerkunciEksternal = false;
        NyalakanInteraksiLain();

        dialogSelesai = true;
        sedangBerjalan = false;
        saatDialogSelesai.Invoke();

        Debug.Log("[Adegan L3-S5] Dialog selesai, ikon E muncul di laptop saat MC duduk.");
    }

    // ---------- Aktivitas laptop dosen: pilihan, bar, poin, reward, tutorial, energi, cooldown ----------
    private IEnumerator AlurAktivitas()
    {
        bool pertamaKali = !sudahPernahSelesai;
        sedangBerjalan = true;

        if (ikonE != null) ikonE.SetActive(false);

        // MC tidak bisa berdiri selama aktivitas berjalan
        kursiDosen.TerkunciEksternal = true;

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

        Debug.Log("[Adegan L3-S5] Memilih jurnal " + namaPilihan + ": bar " + durasi + " detik, poin " + poin + ".");

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
            Debug.Log("[Adegan L3-S5] Kolaborasi akademik! Skor bertambah +" + poin);
        }
        else
        {
            Debug.LogWarning("[Adegan L3-S5] ScoreManager.Instance tidak ditemukan di Scene!");
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

        // 5. Box tutorial dengan overlay hitam (sekali saja kalau diatur begitu)
        if (!(tutorialSekaliSaja && tutorialSudahTampil))
        {
            tutorialSudahTampil = true;
            yield return StartCoroutine(TampilkanTutorial());
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
        kursiDosen.TerkunciEksternal = false;
        sedangBerjalan = false;

        // Event untuk scene berikutnya hanya dipanggil sekali
        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log(pertamaKali ? "[Adegan L3-S5] Scene 5 Level 3 selesai." : "[Adegan L3-S5] Aktivitas ulang selesai.");
    }

    // ---------- Cooldown baterai (sama seperti Scene 4) ----------
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

        Debug.Log("[Adegan L3-S5] Laptop dosen siap dipakai lagi.");
    }

    // ---------- Rekan dosen ----------
    private IEnumerator RekanMasukDanDuduk()
    {
        srRekan = rekanDosen.GetComponentInChildren<SpriteRenderer>();

        // Kalau objek rekan dosen masih punya Animator, dimatikan supaya tidak menimpa gambar dari script
        Animator sisaAnimator = rekanDosen.GetComponentInChildren<Animator>(true);
        if (sisaAnimator != null) sisaAnimator.enabled = false;

        if (srRekan != null)
        {
            skalaRekanAwal = srRekan.transform.localScale;
            if (spriteDiam != null) srRekan.sprite = spriteDiam;
            srRekan.flipX = false;
        }
        else
        {
            Debug.LogWarning("[Adegan L3-S5] Objek Rekan Dosen tidak punya SpriteRenderer, gambar tidak bisa diganti.");
        }

        // Muncul di titik pertama jalur (pintu)
        if (jalur != null && jalur.Length > 0 && jalur[0] != null)
            rekanDosen.transform.position = jalur[0].position;

        rekanDosen.SetActive(true);

        float mulai = Time.time;
        waktuAnimasi = 0f;

        // Jalan melewati semua titik, lalu ke titik duduk
        if (jalur != null)
        {
            foreach (Transform titik in jalur)
            {
                if (titik == null) continue;
                yield return JalanKe(titik.position, mulai);
            }
        }

        if (titikDuduk != null)
            yield return JalanKe(titikDuduk.position, mulai);
        else
            Debug.LogWarning("[Adegan L3-S5] Titik Duduk rekan dosen belum diisi, rekan dosen berhenti di titik terakhir jalur.");

        // Sampai: berdiri diam sebentar, lalu duduk
        if (srRekan != null && spriteDiam != null) srRekan.sprite = spriteDiam;
        yield return new WaitForSeconds(0.2f);

        if (rekanDudukObjek != null)
        {
            // Dua objek: yang berjalan menghilang, yang sudah duduk di kursi muncul
            rekanDosen.SetActive(false);
            rekanDudukObjek.SetActive(true);
        }
        else if (srRekan != null)
        {
            // Satu objek: gambar diganti gambar duduk
            if (spriteDudukRekan != null) srRekan.sprite = spriteDudukRekan;
            srRekan.flipX = false;
            srRekan.sortingOrder = sortingOrderDuduk;
            srRekan.transform.localScale = skalaRekanAwal * skalaSaatDuduk;
        }

        Debug.Log("[Adegan L3-S5] Rekan dosen duduk di kursi samping MC.");
    }

    private IEnumerator JalanKe(Vector3 tujuan, float waktuMulai)
    {
        tujuan.z = rekanDosen.transform.position.z;

        while (Vector3.Distance(rekanDosen.transform.position, tujuan) > 0.02f)
        {
            // Pengaman: kalau terlalu lama, langsung dipindah ke tujuan
            if (Time.time - waktuMulai > batasWaktuAdegan)
            {
                Debug.LogWarning("[Adegan L3-S5] Batas waktu habis saat rekan dosen berjalan, posisinya langsung dipindah.");
                break;
            }

            Vector3 posisi = rekanDosen.transform.position;
            Vector3 arah = tujuan - posisi;

            if (srRekan != null)
            {
                // Hadap kiri/kanan sesuai arah jalan
                if (Mathf.Abs(arah.x) > 0.01f)
                    srRekan.flipX = (arah.x < 0f) == gambarMenghadapKanan;

                // Ganti gambar jalan berurutan
                if (spriteJalan != null && spriteJalan.Length > 0)
                {
                    waktuAnimasi += Time.deltaTime * fpsJalan;
                    int indeks = Mathf.FloorToInt(waktuAnimasi) % spriteJalan.Length;
                    if (spriteJalan[indeks] != null) srRekan.sprite = spriteJalan[indeks];
                }
            }

            rekanDosen.transform.position = Vector3.MoveTowards(posisi, tujuan, kecepatan * Time.deltaTime);
            yield return null;
        }

        rekanDosen.transform.position = tujuan;
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
                Debug.LogWarning("[Adegan L3-S5] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    // Mematikan sementara script kursi dan laptop lain yang ikut membaca tombol Space / E (kursi dosen sendiri tetap menyala)
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

        // Script laptop dosen lama dimatikan dan tidak dinyalakan lagi (laptop dosen sekarang dipakai adegan ini)
        if (laptopDosenLama != null) laptopDosenLama.enabled = false;
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
}