using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Level 4 - Scene 5 (Publikasi Berkualitas dan Kontribusi Institusi), Lokasi: Perpustakaan
//   1. Scene 4 Level 4 selesai -> Scene 5 siap, tokoh (Kaprodi / peneliti) muncul di dekat pintu perpustakaan
//   2. MC mendekati pintu: MC berhenti, dua box dialog (tokoh lalu Pemain), Space untuk lanjut
//   3. Box tutorial pertama (setelah dialog)
//   4. MC bebas, berjalan ke kursi perpustakaan dan duduk sendiri (Space di dekat kursi)
//   5. Saat MC duduk, ikon E muncul di laptop. Tekan E: dua box pilihan jujur (atas) / bohong (bawah, integritas berkurang),
//      lalu box pilihan (4 pilihan). Lama bar bergantung pada jujur / bohong
//   6. Bar hijau berputar, poin bertambah, reward, energi berkurang, ikon baterai muncul (cooldown), box tutorial kedua (sekali saja)
//   7. Setelah baterai penuh, laptop bisa dipakai lagi (berulang)
//
// Script ini dipasang di objek apa saja (tidak perlu collider): dialog mulai saat MC mendekati Titik Pintu.
// Cara menyambung dari Scene 4 Level 4: di AdeganLevel4Scene4, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Bagian yang tidak diisi di Inspector (mis. reward atau tutorial) dilewati.
public class AdeganLevel4Scene5 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 4 dulu (centang SEBELUM Play)

    [Header("Tokoh di Dekat Pintu Perpustakaan (Kaprodi / peneliti)")]
    public GameObject tokohObjek;               // Tokoh yang menunggu di dekat pintu. Nonaktif di awal, muncul saat Scene 5 siap
    public Transform titikTokoh;                // Opsional: tokoh dipindahkan ke titik ini saat muncul (aman walau objeknya dipakai level lain)
    public Transform titikPintu;                // Objek kosong di dekat pintu (dialog mulai saat MC mendekat)
    public float jarakPintu = 1.5f;             // Jarak MC ke titik pintu supaya dialog mulai (lingkaran kuning terlihat di Scene saat objek ini dipilih)
    public bool sembunyikanTokohSetelahDialog = false;   // true = tokoh hilang setelah dialog dan tutorial pertama selesai

    [Header("Aktivitas Lama Perpustakaan (dimatikan sejak Scene 5 siap)")]
    public AdeganLevel2Scene4 scriptLevel2Scene4;        // Opsional: Scene 4 Level 2 yang memakai kursi dan laptop yang sama
    public BukuInteraction bukuPerpus;                   // Opsional: aktivitas buku, dimatikan supaya tombol E tidak bentrok
    public LaptopPerpusInteraction laptopPerpus;         // Opsional: aktivitas laptop Level 2, dimatikan (digantikan laptop Scene 5 ini)
    public GameObject[] objekTambahanDimatikan;          // Objek lain yang ikut dimatikan sejak Scene 5 siap (mis. ikon baterai buku, teks cooldown buku, ikon E buku).
                                                         // JANGAN isi dengan objek yang juga dipakai laptop Scene 5 (mis. baterai yang sama)

    [Header("Box Dialog (dibuat di Hierarchy, urut: tokoh lalu Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.3f;      // Jeda (detik) setelah MC berhenti di pintu sebelum dialog muncul

    [Header("Box Tutorial (di atas overlay hitam, opsional)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorialSetelahDialog;   // Tutorial pertama: muncul setelah dialog di pintu
    public GameObject[] boxTutorialSetelahBar;      // Tutorial kedua: muncul setelah bar, reward, energi, dan ikon baterai
    public bool tutorialBarSekaliSaja = true;       // true = tutorial kedua hanya muncul pada aktivitas laptop yang pertama
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Kursi dan Laptop Perpustakaan")]
    public KursiPerpusInteraction kursiPerpus;  // Kursi perpustakaan (MC duduk sendiri dengan Space). Otomatis dimatikan sampai dialog di pintu selesai
    public Sprite spriteDudukMCLevel4;          // Opsional: sprite MC level 4 saat duduk di kursi ini (dipasang ke gambar duduk kursi, sebelum MC duduk)
    public GameObject panahKeKursi;             // Opsional: panah yang menunjuk ke kursi, muncul setelah dialog dan tutorial selesai
    public GameObject ikonE;                    // Ikon "E" di atas laptop perpustakaan
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Pilihan Jujur / Bohong (muncul dulu sebelum pilihan)")]
    public GameObject boxJujur;                 // Box pilihan atas: jujur
    public GameObject boxBohong;                // Box pilihan bawah: bohong
    public Button tombolJujur;                  // Jangan isi On Click di Inspector, dipasang lewat script
    public Button tombolBohong;                 // Jangan isi On Click di Inspector, dipasang lewat script
    public float penguranganIntegritasBohong = 15f;

    [Header("Box Pilihan Scene 5 (empat pilihan, urut dari atas ke bawah)")]
    public GameObject boxPilihan;               // Panel pilihan (judul + empat tombol)
    public Button tombolPilihan1;               // Jangan isi On Click di Inspector, dipasang lewat script
    public Button tombolPilihan2;
    public Button tombolPilihan3;
    public Button tombolPilihan4;

    [Header("Poin per Pilihan")]
    public int poinPilihan1 = 30;
    public int poinPilihan2 = 25;
    public int poinPilihan3 = 20;
    public int poinPilihan4 = 15;

    [Header("Lama Bar Berdasarkan Pilihan Jujur / Bohong")]
    public float durasiJujur = 15f;
    public float durasiBohong = 10f;

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
    public bool bisaDiulang = true;             // true = setelah cooldown selesai, laptop perpustakaan bisa dipakai lagi

    [Header("Cooldown Baterai")]
    public float durasiCooldown = 60f;          // Jeda (detik) sebelum laptop bisa dipakai lagi
    public GameObject ikonBateraiLemah;         // Objek induk baterai, muncul saat cooldown
    public IndikatorBaterai indikatorBaterai;   // Script pengisi segmen baterai
    public TMP_Text teksCooldown;               // Teks di dekat baterai (contoh "Recharging...")
    public string isiTeksCooldown = "Recharging...";
    public GameObject teksIstirahat;            // Opsional: teks "Laptop sedang istirahat" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public SpriteRenderer spriteLaptop;         // Gambar laptop perpustakaan yang diredupkan selama cooldown
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Layar redup

    [Header("Setelah Scene 5 Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah aktivitas laptop pertama selesai (untuk menyambung ke scene berikutnya)

    private bool siap = false;
    private bool dialogSelesai = false;
    private bool sedangBerjalan = false;
    private bool sudahPernahSelesai = false;
    private bool eventSudahDipanggil = false;
    private bool tutorialBarSudahTampil = false;
    private bool aktivitasLamaDimatikan = false;
    private bool peringatanKursiDitampilkan = false;
    private int pilihanJujur = 0;               // 0 = belum memilih, 1 = jujur, 2 = bohong
    private int pilihan = 0;                    // 0 = belum memilih, 1 sampai 4 = pilihan dari atas ke bawah

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
        SembunyikanSemua(boxTutorialSetelahDialog);
        SembunyikanSemua(boxTutorialSetelahBar);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxReward != null) boxReward.SetActive(false);
        if (panahKeKursi != null) panahKeKursi.SetActive(false);
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
        if (tombolPilihan1 != null) tombolPilihan1.onClick.AddListener(Pilih1);
        if (tombolPilihan2 != null) tombolPilihan2.onClick.AddListener(Pilih2);
        if (tombolPilihan3 != null) tombolPilihan3.onClick.AddListener(Pilih3);
        if (tombolPilihan4 != null) tombolPilihan4.onClick.AddListener(Pilih4);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel4Scene5[] semua = FindObjectsByType<AdeganLevel4Scene5>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L4-S5] Ada " + semua.Length + " objek yang memasang script AdeganLevel4Scene5. Pakai satu saja.");

        // Tokoh baru dimunculkan saat Scene 5 siap
        if (siap) MulaiScene();
        else if (tokohObjek != null) tokohObjek.SetActive(false);
    }

    void OnDestroy()
    {
        if (tombolJujur != null) tombolJujur.onClick.RemoveListener(PilihJujur);
        if (tombolBohong != null) tombolBohong.onClick.RemoveListener(PilihBohong);
        if (tombolPilihan1 != null) tombolPilihan1.onClick.RemoveListener(Pilih1);
        if (tombolPilihan2 != null) tombolPilihan2.onClick.RemoveListener(Pilih2);
        if (tombolPilihan3 != null) tombolPilihan3.onClick.RemoveListener(Pilih3);
        if (tombolPilihan4 != null) tombolPilihan4.onClick.RemoveListener(Pilih4);
    }

    // Lingkaran kuning di tab Scene (saat objek ini dipilih): area dialog di pintu
    void OnDrawGizmosSelected()
    {
        if (titikPintu == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(titikPintu.position, jarakPintu);
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel4Scene4
    public void SiapkanScene()
    {
        siap = true;
        MulaiScene();
        Debug.Log("[Adegan L4-S5] Scene 5 siap, tokoh menunggu di dekat pintu perpustakaan.");
    }

    // Persiapan: aktivitas lama dimatikan, kursi dikunci sampai dialog selesai, tokoh dimunculkan
    private void MulaiScene()
    {
        MatikanAktivitasLama();
        TerapkanSpriteMC();
        if (kursiPerpus != null && !dialogSelesai) kursiPerpus.enabled = false;

        if (tokohObjek != null)
        {
            if (titikTokoh != null)
            {
                Vector3 posisi = titikTokoh.position;
                posisi.z = tokohObjek.transform.position.z;
                tokohObjek.transform.position = posisi;
            }

            tokohObjek.SetActive(true);
        }
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Dialog di Pintu (langsung mulai)")]
    private void TesDialog()
    {
        if (sedangBerjalan || dialogSelesai) return;
        siap = true;
        MulaiScene();
        StartCoroutine(AlurDialog());
    }

    public void PilihJujur() { if (pilihanJujur == 0) pilihanJujur = 1; }
    public void PilihBohong() { if (pilihanJujur == 0) pilihanJujur = 2; }
    public void Pilih1() { if (pilihan == 0) pilihan = 1; }
    public void Pilih2() { if (pilihan == 0) pilihan = 2; }
    public void Pilih3() { if (pilihan == 0) pilihan = 3; }
    public void Pilih4() { if (pilihan == 0) pilihan = 4; }

    void Update()
    {
        // Teks "istirahat" hilang otomatis
        if (teksIstirahat != null && teksIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksIstirahat.SetActive(false);

        // Baterai cooldown terisi bertahap
        if (cooldownAktif) TickCooldown();

        if (sedangBerjalan) return;

        // "Langsung Siap" dibaca di sini supaya juga berlaku kalau dicentang saat Play
        if (!siap && !langsungSiap) return;

        if (!aktivitasLamaDimatikan) MulaiScene();

        // Tahap 1: dialog dengan tokoh saat MC mendekati pintu
        if (!dialogSelesai)
        {
            if (titikPintu == null) return;

            if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
            if (pm == null) return;

            if (Vector2.Distance(titikPintu.position, pm.transform.position) <= jarakPintu)
            {
                Debug.Log("[Adegan L4-S5] MC tiba di pintu perpustakaan, dialog dimulai.");
                StartCoroutine(AlurDialog());
            }
            return;
        }

        // Tahap 2: MC duduk di kursi perpustakaan, ikon E muncul di laptop
        if (sudahPernahSelesai && !bisaDiulang)
        {
            if (ikonE != null && ikonE.activeSelf) ikonE.SetActive(false);
            return;
        }

        if (kursiPerpus == null)
        {
            if (!peringatanKursiDitampilkan)
            {
                peringatanKursiDitampilkan = true;
                Debug.LogWarning("[Adegan L4-S5] Kursi Perpus belum diisi di Inspector, laptop perpustakaan tidak bisa dipakai.");
            }
            return;
        }

        bool duduk = kursiPerpus.sedangDuduk;
        if (duduk && !dudukSebelumnya) waktuMulaiDuduk = Time.time;
        dudukSebelumnya = duduk;

        // Panah ke kursi hilang begitu MC duduk
        if (duduk && panahKeKursi != null && panahKeKursi.activeSelf) panahKeKursi.SetActive(false);

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

    // Aktivitas lama perpustakaan dimatikan supaya tombol E dan kursi tidak bentrok
    private void MatikanAktivitasLama()
    {
        if (aktivitasLamaDimatikan) return;
        aktivitasLamaDimatikan = true;

        if (scriptLevel2Scene4 != null) scriptLevel2Scene4.enabled = false;

        if (bukuPerpus != null)
        {
            if (bukuPerpus.petunjukTekanE != null) bukuPerpus.petunjukTekanE.SetActive(false);
            bukuPerpus.enabled = false;
        }

        if (laptopPerpus != null) laptopPerpus.enabled = false;

        // Objek tambahan (mis. baterai buku, teks cooldown buku, ikon E buku) ikut dimatikan
        if (objekTambahanDimatikan != null)
        {
            foreach (GameObject o in objekTambahanDimatikan)
            {
                if (o != null) o.SetActive(false);
            }
        }
    }

    // Pasang sprite MC level 4 ke gambar duduk kursi (harus sebelum MC duduk)
    private void TerapkanSpriteMC()
    {
        if (kursiPerpus == null || spriteDudukMCLevel4 == null || kursiPerpus.spriteDuduk == null) return;

        SpriteRenderer sr = kursiPerpus.spriteDuduk.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != spriteDudukMCLevel4) sr.sprite = spriteDudukMCLevel4;
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
        if (tombolPilihan1 != null) tombolPilihan1.gameObject.SetActive(tampil);
        if (tombolPilihan2 != null) tombolPilihan2.gameObject.SetActive(tampil);
        if (tombolPilihan3 != null) tombolPilihan3.gameObject.SetActive(tampil);
        if (tombolPilihan4 != null) tombolPilihan4.gameObject.SetActive(tampil);
    }

    // ---------- Dialog di pintu ----------
    private IEnumerator AlurDialog()
    {
        sedangBerjalan = true;

        // MC berhenti di pintu selama dialog dan tutorial pertama
        KunciPemain(true);

        yield return new WaitForSeconds(jedaSebelumDialog);

        // 1. Dua box dialog (tokoh lalu Pemain)
        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L4-S5] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

        // 2. Box tutorial pertama
        if (boxTutorialSetelahDialog != null && boxTutorialSetelahDialog.Length > 0)
            yield return StartCoroutine(TampilkanTutorial(boxTutorialSetelahDialog));

        // 3. MC bebas berjalan ke kursi perpustakaan, kursinya sekarang bisa dipakai
        KunciPemain(false);
        if (kursiPerpus != null) kursiPerpus.enabled = true;
        if (panahKeKursi != null) panahKeKursi.SetActive(true);

        if (sembunyikanTokohSetelahDialog && tokohObjek != null) tokohObjek.SetActive(false);

        dialogSelesai = true;
        sedangBerjalan = false;

        Debug.Log("[Adegan L4-S5] Dialog selesai, MC bebas menuju kursi perpustakaan.");
    }

    // ---------- Aktivitas laptop perpustakaan ----------
    private IEnumerator AlurLaptop()
    {
        bool pertamaKali = !sudahPernahSelesai;
        sedangBerjalan = true;

        if (ikonE != null) ikonE.SetActive(false);

        // MC tidak bisa berdiri selama aktivitas berjalan
        kursiPerpus.TerkunciEksternal = true;

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
                Debug.Log("[Adegan L4-S5] Memilih bohong, integritas berkurang " + penguranganIntegritasBohong + ".");
            }
            else
            {
                Debug.LogWarning("[Adegan L4-S5] IntegritasManager.Instance tidak ditemukan di Scene!");
            }
        }

        // 1. Box pilihan (empat pilihan)
        pilihan = 0;
        TampilkanPilihan(true);

        while (pilihan == 0) yield return null;

        TampilkanPilihan(false);

        int poin = poinPilihan1;
        float durasi = (pilihanJujur == 2) ? durasiBohong : durasiJujur;   // jujur 15 detik, bohong 10 detik, apa pun pilihannya

        if (pilihan == 2) poin = poinPilihan2;
        else if (pilihan == 3) poin = poinPilihan3;
        else if (pilihan == 4) poin = poinPilihan4;

        Debug.Log("[Adegan L4-S5] Memilih pilihan " + pilihan + ": bar " + durasi + " detik, poin " + poin + ".");

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
            Debug.Log("[Adegan L4-S5] Publikasi dan kontribusi selesai! Skor bertambah +" + poin);
        }
        else
        {
            Debug.LogWarning("[Adegan L4-S5] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 4. Reward (lewat RewardManager, atau Box Reward buatan sendiri)
        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));

            // Tunggu box reward hilang dulu supaya tidak bertumpuk dengan box tutorial
            yield return new WaitForSecondsRealtime(RewardManager.Instance.lamaTampil + 0.3f);
        }
        else if (boxReward != null)
        {
            boxReward.SetActive(true);
            yield return new WaitForSecondsRealtime(lamaBoxReward);
            boxReward.SetActive(false);
        }

        // 5. Energi berkurang, lalu cek apakah habis
        if (biayaEnergi > 0f && energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        // 6. Ikon baterai muncul: cooldown mulai (aktivitas berulang)
        sudahPernahSelesai = true;
        if (bisaDiulang) MulaiCooldown();

        // 7. Box tutorial kedua (sekali saja kalau diatur begitu)
        if (!(tutorialBarSekaliSaja && tutorialBarSudahTampil))
        {
            tutorialBarSudahTampil = true;
            yield return StartCoroutine(TampilkanTutorial(boxTutorialSetelahBar));
        }

        // 8. MC dibebaskan (boleh berdiri)
        kursiPerpus.TerkunciEksternal = false;
        sedangBerjalan = false;
        waktuMulaiDuduk = Time.time;

        // Event untuk scene berikutnya hanya dipanggil sekali
        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log(pertamaKali ? "[Adegan L4-S5] Scene 5 Level 4 selesai." : "[Adegan L4-S5] Aktivitas ulang selesai.");
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

        Debug.Log("[Adegan L4-S5] Laptop perpustakaan siap dipakai lagi.");
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
                Debug.LogWarning("[Adegan L4-S5] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

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