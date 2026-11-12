using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Level 4 - Scene 4 (Mengembangkan Strategi Akademik), Lokasi: Ruang Rektor
//   1. Scene 3 Level 4 selesai -> Scene 4 siap
//   2. MC duduk di kursi rektor. MC dikunci di kursi selama intro
//   3. Kaprodi muncul, berjalan menuju kursi tamu, lalu duduk
//   4. Dua box dialog (Kaprodi lalu Pemain), Space untuk lanjut
//   5. Ikon E muncul saat MC duduk. Tekan E: bar hijau berputar
//   6. Begitu bar selesai, SEKALIGUS: Kaprodi berdiri dan berjalan keluar, poin bertambah (10), reward muncul,
//      energi berkurang (opsional), dan cooldown baterai mulai (layar laptop redup + baterai terisi)
//   7. Setelah Kaprodi keluar dan reward selesai: box tutorial (sekali saja)
//   8. Setelah cooldown penuh, aktivitas bisa dipakai lagi (berulang)
//
// Kursi rektor boleh memakai script sendiri (KursiRektorInteraction), InteractiveDesk, atau KursiPerpusInteraction. Isi salah satu saja.
// Script ini dipasang di objek apa saja (tidak perlu collider).
// Cara menyambung dari Scene 3 Level 4: di AdeganLevel4Scene3, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Untuk tes tanpa melewati Scene 3: centang "Langsung Siap".
// Bagian yang tidak diisi di Inspector (mis. reward atau tutorial) dilewati.
public class AdeganLevel4Scene4 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 3 dulu

    [Header("Kursi Rektor (isi SALAH SATU sesuai komponen di kursinya)")]
    public KursiRektorInteraction kursiRektor;          // Kalau kursinya memakai script sendiri (KursiRektorInteraction), tidak lewat InteractiveDesk
    public InteractiveDesk kursiRektorDesk;             // Kalau kursinya memakai komponen InteractiveDesk
    public KursiPerpusInteraction kursiRektorPerpus;    // Kalau kursinya memakai komponen KursiPerpusInteraction
    public float jedaSetelahDuduk = 0.5f;               // Jeda (detik) setelah MC duduk sebelum Kaprodi muncul

    [Header("Kaprodi")]
    public KaprodiJalan2Arah kaprodi;           // Objek induk Kaprodi (yang punya script KaprodiJalan2Arah). Disembunyikan di awal
    public Transform titikAwal;                 // Tempat Kaprodi muncul
    public Transform[] titikMasuk;              // Jalur ke kursi tamu, urut. Titik terakhir sebaiknya dekat kursi tamu
    public Transform kursiTamu;                 // Objek kursi tamu (mis. kursikaprek). Setelah sampai di titik terakhir, Kaprodi langsung duduk dan gambar duduknya dipasang tepat di posisi kursi ini. Kosong = duduk di titik terakhir Titik Masuk
    public Vector2 offsetDudukDiKursi = Vector2.zero;   // Geser posisi duduk dari pusat kursi tamu (kalau Kaprodi terlihat terlalu tinggi / rendah di kursi)
    public Transform[] titikKeluar;             // Jalur keluar, urut. Titik terakhir = tempat Kaprodi menghilang
    public float jedaSetelahDudukKaprodi = 0.5f;
    public bool kaprodiKeluarSetelahDialog = false;  // false = Kaprodi tetap duduk sampai bar hijau selesai, lalu keluar. true = keluar langsung setelah dialog

    [Header("Box Dialog (dibuat di Hierarchy, urut: Kaprodi lalu Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.3f;

    [Header("Ikon E")]
    public GameObject ikonE;                    // Ikon "E" yang muncul saat MC duduk (setelah dialog)
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Bar Hijau Berputar")]
    public GameObject canvasProgressBar;        // Objek induk bar (boleh objek yang sama dengan scene lain)
    public Image circularProgressBar;           // Image bar (Image Type: Filled)
    public float durasiBar = 5f;                // Lama bar berputar (detik)

    [Header("Hasil Setelah Bar Selesai")]
    public int jumlahPoin = 10;                 // Poin yang didapat tiap selesai
    public int indexReward = -1;                // Nomor reward di RewardManager. -1 = pakai Box Reward di bawah (kalau diisi)
    public GameObject boxReward;                // Opsional: box reward buatan sendiri, dipakai kalau Index Reward = -1
    public float lamaBoxReward = 2f;
    public float biayaEnergi = 0f;              // Energi yang berkurang tiap selesai (0 = tidak mengurangi energi)
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Box Tutorial (muncul setelah Kaprodi keluar, opsional)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorialSetelahKeluar;   // Box tutorial, Space untuk lanjut
    public bool tutorialSekaliSaja = true;      // true = tutorial hanya muncul pada aktivitas yang pertama
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Aktivitas Berulang")]
    public bool bisaDiulang = true;             // true = setelah cooldown selesai, aktivitas bisa dipakai lagi

    [Header("Cooldown Baterai")]
    public float durasiCooldown = 60f;          // Jeda (detik) sebelum aktivitas bisa dipakai lagi
    public GameObject ikonBateraiLemah;         // Objek induk baterai, muncul saat cooldown
    public IndikatorBaterai indikatorBaterai;   // Script pengisi segmen baterai
    public TMP_Text teksCooldown;               // Teks di dekat baterai (contoh "Recharging...")
    public string isiTeksCooldown = "Recharging...";
    public GameObject teksIstirahat;            // Opsional: teks "Laptop sedang istirahat" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public SpriteRenderer spriteLaptop;         // Opsional: gambar laptop yang diredupkan selama cooldown
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Layar redup

    [Header("Setelah Scene 4 Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah aktivitas pertama selesai

    private bool siap = false;
    private bool sudahJalan = false;            // intro (Kaprodi + dialog) sudah dimulai
    private bool dialogSelesai = false;
    private bool sedangBerjalan = false;
    private bool sudahPernahSelesai = false;
    private bool eventSudahDipanggil = false;
    private bool kaprodiSudahKeluar = false;
    private bool tutorialSudahTampil = false;
    private bool peringatanKursi = false;
    private bool peringatanBelumSiap = false;   // supaya log "belum siap" hanya muncul sekali
    private bool dudukSebelumnya = false;
    private float waktuMulaiDuduk = 0f;
    private const float jedaSetelahDudukE = 0.3f;

    // Cooldown
    private bool cooldownAktif = false;
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorialSetelahKeluar);
        if (kaprodi != null) kaprodi.gameObject.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (ikonE != null) ikonE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (boxReward != null) boxReward.SetActive(false);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCooldown != null) teksCooldown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel4Scene4[] semua = FindObjectsByType<AdeganLevel4Scene4>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L4-S4] Ada " + semua.Length + " objek yang memasang script AdeganLevel4Scene4. Pakai satu saja.");
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel4Scene3
    public void SiapkanScene()
    {
        siap = true;
        Debug.Log("[Adegan L4-S4] Scene 4 siap, menunggu MC duduk di kursi rektor.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (langsung mulai)")]
    private void TesAdegan()
    {
        if (sudahJalan) return;
        siap = true;
        sudahJalan = true;
        StartCoroutine(AlurIntro());
    }

    void Update()
    {
        // Teks "istirahat" hilang otomatis
        if (teksIstirahat != null && teksIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksIstirahat.SetActive(false);

        // Baterai cooldown terisi bertahap
        if (cooldownAktif) TickCooldown();

        if (sedangBerjalan) return;

        // "Langsung Siap" dibaca di sini supaya bisa dicentang saat game sedang Play
        if (!siap && !langsungSiap)
        {
            // Beri tahu di Console kalau MC sudah duduk tapi Scene 4 belum siap (supaya tidak bingung kenapa tidak terjadi apa-apa)
            if (!peringatanBelumSiap && SedangDudukDiKursiRektor())
            {
                peringatanBelumSiap = true;
                Debug.Log("[Adegan L4-S4] MC duduk di kursi rektor, tapi Scene 4 belum siap. Centang 'Langsung Siap' untuk tes, atau pastikan event 'Saat Adegan Selesai' di AdeganLevel4Scene3 tersambung ke SiapkanScene.");
            }
            return;
        }

        if (kursiRektor == null && kursiRektorDesk == null && kursiRektorPerpus == null)
        {
            if (!peringatanKursi)
            {
                peringatanKursi = true;
                Debug.LogWarning("[Adegan L4-S4] Kursi Rektor belum diisi di Inspector (isi salah satu), adegan tidak bisa mulai.");
            }
            return;
        }

        bool duduk = SedangDudukDiKursiRektor();
        if (duduk && !dudukSebelumnya) waktuMulaiDuduk = Time.time;
        dudukSebelumnya = duduk;

        // Tahap 1: MC duduk di kursi rektor, intro dimulai
        if (!sudahJalan)
        {
            if (duduk && Time.time - waktuMulaiDuduk >= jedaSetelahDuduk)
            {
                sudahJalan = true;
                Debug.Log("[Adegan L4-S4] MC duduk di kursi rektor, adegan dimulai.");
                StartCoroutine(AlurIntro());
            }
            return;
        }

        if (!dialogSelesai) return;

        // Tahap 2: aktivitas dengan ikon E (berulang)
        if (sudahPernahSelesai && !bisaDiulang)
        {
            if (ikonE != null && ikonE.activeSelf) ikonE.SetActive(false);
            return;
        }

        bool bolehTekan = duduk && (Time.time - waktuMulaiDuduk >= jedaSetelahDudukE);

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

    private bool SedangDudukDiKursiRektor()
    {
        bool d = false;
        if (kursiRektor != null) d = kursiRektor.SedangDuduk;
        if (kursiRektorDesk != null) d = d || kursiRektorDesk.SedangDuduk;
        if (kursiRektorPerpus != null) d = d || kursiRektorPerpus.sedangDuduk;
        return d;
    }

    private void KunciKursi(bool kunci)
    {
        if (kursiRektor != null) kursiRektor.TerkunciEksternal = kunci;
        if (kursiRektorDesk != null) kursiRektorDesk.TerkunciEksternal = kunci;
        if (kursiRektorPerpus != null) kursiRektorPerpus.TerkunciEksternal = kunci;
    }

    private void SembunyikanSemua(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    // ---------- Intro: Kaprodi datang, dialog ----------
    private IEnumerator AlurIntro()
    {
        sedangBerjalan = true;

        // MC dikunci di kursi selama intro
        KunciKursi(true);

        // 1. Kaprodi datang dan duduk
        if (kaprodi == null)
        {
            Debug.LogWarning("[Adegan L4-S4] Kaprodi belum diisi di Inspector, adegan Kaprodi dilewati.");
        }
        else
        {
            if (titikAwal != null) kaprodi.transform.position = titikAwal.position;
            kaprodi.gameObject.SetActive(true);

            if (titikMasuk == null || titikMasuk.Length == 0)
                Debug.LogWarning("[Adegan L4-S4] Titik Masuk masih kosong di Inspector, Kaprodi langsung duduk di tempat.");
            else
                yield return StartCoroutine(kaprodi.JalanMelalui(titikMasuk));

            // Sampai di titik terakhir: langsung duduk, gambar duduk dipasang tepat di posisi kursi tamu
            if (kursiTamu != null)
            {
                kaprodi.DudukDi((Vector2)kursiTamu.position + offsetDudukDiKursi);
            }
            else
            {
                Debug.LogWarning("[Adegan L4-S4] Kursi Tamu belum diisi di Inspector, Kaprodi duduk di titik terakhir Titik Masuk.");
                kaprodi.Duduk();
            }

            yield return new WaitForSeconds(jedaSetelahDudukKaprodi);
        }

        // 2. Dua box dialog
        yield return new WaitForSeconds(jedaSebelumDialog);

        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L4-S4] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

        // 3. Kaprodi keluar sekarang (kalau diatur keluar setelah dialog), lalu MC dilepas
        if (kaprodiKeluarSetelahDialog)
        {
            yield return StartCoroutine(KaprodiKeluar());
            KunciKursi(false);
        }
        // Kalau Kaprodi masih duduk, MC tetap terkunci sampai aktivitas pertama selesai

        dialogSelesai = true;
        sedangBerjalan = false;
        waktuMulaiDuduk = Time.time;

        Debug.Log("[Adegan L4-S4] Dialog selesai, ikon E muncul.");
    }

    // ---------- Aktivitas: E, bar hijau, lalu Kaprodi keluar + poin + reward + energi + cooldown bersamaan, tutorial ----------
    private IEnumerator AlurAktivitas()
    {
        bool pertamaKali = !sudahPernahSelesai;
        sedangBerjalan = true;

        if (ikonE != null) ikonE.SetActive(false);

        // MC tidak bisa berdiri selama aktivitas berjalan
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

        // 1b. Bar selesai: Kaprodi berdiri dan jalan keluar (berjalan bersamaan dengan poin dan reward di bawah)
        Coroutine kaprodiKeluarJalan = null;
        if (!kaprodiSudahKeluar)
            kaprodiKeluarJalan = StartCoroutine(KaprodiKeluar());

        // 1c. Energi berkurang dan cooldown baterai mulai begitu bar selesai
        if (biayaEnergi > 0f && energyBar != null)
            energyBar.DecreaseEnergy(biayaEnergi);

        sudahPernahSelesai = true;
        if (bisaDiulang) MulaiCooldown();

        // 2. Poin
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoin);
            Debug.Log("[Adegan L4-S4] Strategi akademik selesai! Skor bertambah +" + jumlahPoin);
        }
        else
        {
            Debug.LogWarning("[Adegan L4-S4] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 3. Reward (lewat RewardManager, atau Box Reward buatan sendiri)
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

        // 3b. Tunggu Kaprodi selesai keluar (kalau belum) sebelum tutorial
        if (kaprodiKeluarJalan != null)
            yield return kaprodiKeluarJalan;

        // 4. Box tutorial: muncul setelah Kaprodi keluar (sekali saja kalau diatur begitu)
        if (!(tutorialSekaliSaja && tutorialSudahTampil))
        {
            tutorialSudahTampil = true;
            yield return StartCoroutine(TampilkanTutorial(boxTutorialSetelahKeluar));
        }

        // 5. Cek energi habis di akhir (supaya transisi energi habis tidak memotong Kaprodi, reward, dan tutorial)
        if (biayaEnergi > 0f && energyBar != null)
            energyBar.CekEnergiHabis();

        // 6. MC dibebaskan (boleh berdiri)
        KunciKursi(false);
        sedangBerjalan = false;
        waktuMulaiDuduk = Time.time;

        // Event untuk scene berikutnya hanya dipanggil sekali
        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log(pertamaKali ? "[Adegan L4-S4] Scene 4 selesai." : "[Adegan L4-S4] Aktivitas ulang selesai.");
    }

    // Kaprodi berdiri, berjalan ke titik keluar, lalu menghilang
    private IEnumerator KaprodiKeluar()
    {
        kaprodiSudahKeluar = true;
        if (kaprodi == null) yield break;

        kaprodi.Berdiri();

        if (titikKeluar == null || titikKeluar.Length == 0)
            Debug.LogWarning("[Adegan L4-S4] Titik Keluar masih kosong di Inspector, Kaprodi langsung menghilang.");
        else
            yield return StartCoroutine(kaprodi.JalanMelalui(titikKeluar));

        kaprodi.gameObject.SetActive(false);
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

        Debug.Log("[Adegan L4-S4] Aktivitas siap dipakai lagi.");
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
                Debug.LogWarning("[Adegan L4-S4] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }
}