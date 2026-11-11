using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Level 4 - Scene 3 (Mengembangkan Kolaborasi), Lokasi: Research Center
//   1. Scene 2 Level 4 selesai -> Scene 3 siap, Peneliti sudah duduk menunggu di Research Center
//   2. MC masuk zona Research Center: MC duduk di kursi, dua box dialog (Peneliti lalu Pemain), Space untuk lanjut
//   3. Setelah dialog, MC dilepas. Saat MC duduk di kursi, ikon E muncul di laptop
//   4. Tekan E: dua box pilihan jujur (atas) / bohong (bawah, integritas berkurang), lalu box pilihan kolaborasi (4 pilihan)
//      Lama bar bergantung pada jujur / bohong: jujur 15 detik, bohong 10 detik
//   5. Bar hijau berputar, poin bertambah, reward, box tutorial (opsional, sekali saja)
//   6. Energi berkurang, cooldown baterai mulai (layar laptop redup). Setelah penuh, laptop bisa dipakai lagi (berulang)
//
// Pasang script ini di objek ZONA yang punya Collider2D dengan Is Trigger dicentang (di Research Center).
// Cara menyambung dari Scene 2 Level 4: di AdeganLevel4Scene2, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Objek laptop: pakai SpriteRenderer yang sudah ada (isi Sprite Laptop), ATAU biarkan kosong lalu isi
// "Sprite Gambar Laptop" + "Posisi Laptop", maka script membuat objek laptopnya sendiri saat Play.
public class AdeganLevel4Scene3 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 2 dulu
    public string tagPemain = "Player";

    [Header("Referensi Kursi dan Peneliti")]
    public InteractiveDesk kursiPeneliti;       // Kursi di Research Center (komponen InteractiveDesk, isi Sit Point)
    public Sprite spriteDudukLevel4;            // Opsional: sprite MC level 4 saat duduk di kursi ini
    public bool matikanBimbinganDiKursi = true; // true = kursi ini tidak memicu bimbingan level 1 lagi
    public bool dudukOtomatis = false;          // false = MC duduk sendiri, dialog muncul setelah duduk. true = MC langsung dipindah duduk
    public GameObject[] penelitiObjek;          // Peneliti yang sudah duduk menunggu. Nonaktif di awal, muncul saat Scene 3 siap

    [Header("Script dan Objek Level 2 (dimatikan sejak Scene 3 Level 4 siap)")]
    public AdeganLevel2Scene3 scriptLevel2Scene3;   // Opsional: Scene 3 Level 2 yang memakai zona dan kursi yang sama
    public GameObject[] penelitiLevel2;             // Opsional: peneliti Level 2 yang dihilangkan. JANGAN isi dengan objek yang sama dengan Peneliti Objek

    [Header("Box Dialog (dibuat di Hierarchy, urut: Peneliti lalu Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 1f;        // Jeda (detik) setelah MC duduk sebelum dialog pertama muncul

    [Header("Box Tutorial (di atas overlay hitam, opsional)")]
    public GameObject overlayHitam;
    public GameObject[] boxTutorialSetelahBar;  // Muncul setelah bar hijau dan reward
    public bool tutorialBarSekaliSaja = true;
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Laptop Research Center")]
    public SpriteRenderer spriteLaptop;         // Gambar laptop (diredupkan saat cooldown). Kosong = dibuat otomatis kalau Sprite Gambar Laptop diisi
    public Sprite spriteGambarLaptop;           // Dipakai untuk membuat objek laptop otomatis
    public Transform posisiLaptop;              // Objek kosong di meja tempat laptop dibuat / acuan jarak
    public string sortingLayerLaptop = "Default";
    public int urutanSortingLaptop = 5;
    public GameObject ikonE;                    // Ikon "E" di atas laptop
    public KeyCode tombolInteraksi = KeyCode.E;
    public bool harusDudukUntukLaptop = true;   // true = ikon E muncul saat MC duduk. false = muncul saat MC dekat laptop (Jarak Laptop)
    public float jarakLaptop = 1.5f;            // Dipakai kalau Harus Duduk dimatikan (lingkaran hijau di Scene saat objek dipilih)

    [Header("Pilihan Jujur / Bohong (muncul dulu sebelum pilihan kolaborasi)")]
    public GameObject boxJujur;
    public GameObject boxBohong;
    public Button tombolJujur;                  // Jangan isi On Click di Inspector, dipasang lewat script
    public Button tombolBohong;                 // Jangan isi On Click di Inspector, dipasang lewat script
    public float penguranganIntegritasBohong = 15f;

    [Header("Box Pilihan Kolaborasi (empat pilihan, urut dari atas ke bawah)")]
    public GameObject boxPilihan;
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
    public GameObject canvasProgressBar;
    public Image circularProgressBar;           // Image Type: Filled

    [Header("Setelah Bar Selesai")]
    public int indexReward = -1;                // Nomor reward di RewardManager. -1 = pakai Box Reward di bawah (kalau diisi)
    public GameObject boxReward;
    public float lamaBoxReward = 2f;
    public float biayaEnergi = 15f;
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Aktivitas Berulang")]
    public bool bisaDiulang = true;

    [Header("Cooldown Baterai")]
    public float durasiCooldown = 60f;
    public GameObject ikonBateraiLemah;
    public IndikatorBaterai indikatorBaterai;
    public TMP_Text teksCooldown;
    public string isiTeksCooldown = "Recharging...";
    public GameObject teksIstirahat;            // Opsional: "Laptop sedang istirahat" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Setelah Scene 3 Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah aktivitas laptop pertama selesai

    private bool siap = false;
    private bool dialogMulai = false;
    private bool dialogSelesai = false;
    private bool sedangBerjalan = false;
    private bool sudahPernahSelesai = false;
    private bool eventSudahDipanggil = false;
    private bool tutorialBarSudahTampil = false;
    private bool peringatanBelumSiap = false;
    private bool level2Dimatikan = false;
    private int pilihanJujur = 0;               // 0 = belum, 1 = jujur, 2 = bohong
    private int pilihan = 0;                    // 0 = belum, 1 sampai 4

    private PlayerMovement pm;
    private Transform pemainTf;

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

        BuatLaptopOtomatis();

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorialSetelahBar);
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
        if (tombolPilihan1 != null) tombolPilihan1.onClick.AddListener(Pilih1);
        if (tombolPilihan2 != null) tombolPilihan2.onClick.AddListener(Pilih2);
        if (tombolPilihan3 != null) tombolPilihan3.onClick.AddListener(Pilih3);
        if (tombolPilihan4 != null) tombolPilihan4.onClick.AddListener(Pilih4);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek (objek zona)
        AdeganLevel4Scene3[] semua = FindObjectsByType<AdeganLevel4Scene3>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L4-S3] Ada " + semua.Length + " objek yang memasang script AdeganLevel4Scene3. Pakai satu saja (objek zona).");

        // Peneliti baru dimunculkan saat Scene 3 siap
        if (siap)
        {
            MatikanLevel2();
            NonaktifkanBimbingan();
            AktifkanPeneliti(true);
        }
        else
        {
            AktifkanPeneliti(false);
        }
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

    // Lingkaran hijau di tab Scene (saat objek ini dipilih): jarak laptop
    void OnDrawGizmosSelected()
    {
        Transform acuan = AcuanLaptop();
        if (acuan == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(acuan.position, jarakLaptop);
    }

    private Transform AcuanLaptop()
    {
        if (posisiLaptop != null) return posisiLaptop;
        if (spriteLaptop != null) return spriteLaptop.transform;
        if (ikonE != null) return ikonE.transform;
        return null;
    }

    // Membuat objek laptop kalau belum ada (isi Sprite Gambar Laptop dan Posisi Laptop)
    private void BuatLaptopOtomatis()
    {
        if (spriteLaptop != null) return;
        if (spriteGambarLaptop == null || posisiLaptop == null) return;

        GameObject laptop = new GameObject("Laptop Research Center");
        laptop.transform.position = posisiLaptop.position;

        SpriteRenderer sr = laptop.AddComponent<SpriteRenderer>();
        sr.sprite = spriteGambarLaptop;
        sr.sortingLayerName = sortingLayerLaptop;
        sr.sortingOrder = urutanSortingLaptop;

        spriteLaptop = sr;
        Debug.Log("[Adegan L4-S3] Objek laptop Research Center dibuat otomatis.");
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel4Scene2
    public void SiapkanScene()
    {
        siap = true;
        MatikanLevel2();
        NonaktifkanBimbingan();
        AktifkanPeneliti(true);
        Debug.Log("[Adegan L4-S3] Scene 3 siap, Peneliti menunggu di Research Center.");
    }

    private void AktifkanPeneliti(bool aktif)
    {
        if (penelitiObjek == null) return;

        foreach (GameObject p in penelitiObjek)
        {
            if (p != null) p.SetActive(aktif);
        }
    }

    // Script dan peneliti Level 2 dimatikan supaya tidak bentrok dengan Level 4
    private void MatikanLevel2()
    {
        if (level2Dimatikan) return;
        level2Dimatikan = true;

        if (scriptLevel2Scene3 != null && scriptLevel2Scene3 != (object)this)
            scriptLevel2Scene3.enabled = false;

        if (penelitiLevel2 == null) return;

        foreach (GameObject p in penelitiLevel2)
        {
            if (p == null) continue;

            if (penelitiObjek != null && System.Array.IndexOf(penelitiObjek, p) >= 0)
            {
                Debug.LogWarning("[Adegan L4-S3] '" + p.name + "' ada di Peneliti Objek DAN Peneliti Level 2. Objek ini dilewati.");
                continue;
            }

            if (p.activeSelf) p.SetActive(false);
        }
    }

    private void NonaktifkanBimbingan()
    {
        if (!matikanBimbinganDiKursi || kursiPeneliti == null) return;

        if (kursiPeneliti.bimbinganScript != null)
        {
            kursiPeneliti.bimbinganScript = null;
            Debug.Log("[Adegan L4-S3] Bimbingan level 1 dilepas dari kursi ini.");
        }
    }

    void OnTriggerEnter2D(Collider2D other) { CobaMulai(other); }
    void OnTriggerStay2D(Collider2D other) { CobaMulai(other); }

    private void CobaMulai(Collider2D other)
    {
        if (dialogMulai || dialogSelesai || sedangBerjalan) return;
        if (!other.CompareTag(tagPemain)) return;

        // "Langsung Siap" dibaca di sini supaya bisa dicentang saat game sedang Play
        if (!siap && !langsungSiap)
        {
            if (!peringatanBelumSiap)
            {
                peringatanBelumSiap = true;
                Debug.Log("[Adegan L4-S3] MC masuk zona, tapi Scene 3 belum siap. Centang 'Langsung Siap' untuk tes, atau sambungkan event Scene 2 Level 4 ke SiapkanScene.");
            }
            return;
        }

        if (!siap)
        {
            siap = true;
            MatikanLevel2();
            NonaktifkanBimbingan();
            AktifkanPeneliti(true);
        }

        dialogMulai = true;
        Debug.Log("[Adegan L4-S3] MC masuk zona, adegan dimulai.");
        StartCoroutine(AlurDialog());
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (langsung mulai)")]
    private void TesAdegan()
    {
        if (dialogMulai || dialogSelesai) return;
        siap = true;
        dialogMulai = true;
        MatikanLevel2();
        NonaktifkanBimbingan();
        AktifkanPeneliti(true);
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
        if (!dialogSelesai) return;

        // Tahap laptop: ikon E muncul, tekan E untuk memulai
        if (sudahPernahSelesai && !bisaDiulang)
        {
            if (ikonE != null && ikonE.activeSelf) ikonE.SetActive(false);
            return;
        }

        bool boleh = LaptopBisaDipakai();

        bool duduk = kursiPeneliti != null && kursiPeneliti.SedangDuduk;
        if (duduk && !dudukSebelumnya) waktuMulaiDuduk = Time.time;
        dudukSebelumnya = duduk;

        bool lewatJeda = !harusDudukUntukLaptop || (Time.time - waktuMulaiDuduk >= jedaSetelahDuduk);
        bool bolehTekan = boleh && lewatJeda;

        // Selama cooldown, E tidak memulai aktivitas
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

        if (ikonE != null && ikonE.activeSelf != boleh) ikonE.SetActive(boleh);

        if (bolehTekan && Input.GetKeyDown(tombolInteraksi))
            StartCoroutine(AlurLaptop());
    }

    // Laptop bisa dipakai kalau MC duduk (atau dekat laptop, kalau Harus Duduk dimatikan)
    private bool LaptopBisaDipakai()
    {
        if (harusDudukUntukLaptop)
        {
            if (kursiPeneliti == null) return false;
            return kursiPeneliti.SedangDuduk;
        }

        Transform acuan = AcuanLaptop();
        if (acuan == null) return true;

        if (pemainTf == null || !pemainTf.gameObject.activeInHierarchy)
        {
            GameObject g = GameObject.FindGameObjectWithTag(tagPemain);
            pemainTf = (g != null) ? g.transform : null;
        }

        return pemainTf != null && Vector2.Distance(acuan.position, pemainTf.position) <= jarakLaptop;
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

    // ---------- Duduk dan dialog dengan Peneliti ----------
    private IEnumerator AlurDialog()
    {
        sedangBerjalan = true;

        // 1. MC duduk di kursi (otomatis, atau menunggu MC duduk sendiri)
        if (kursiPeneliti != null)
        {
            if (spriteDudukLevel4 != null) kursiPeneliti.sitSprite = spriteDudukLevel4;

            if (dudukOtomatis)
            {
                kursiPeneliti.DudukUntukAdegan();
            }
            else
            {
                Debug.Log("[Adegan L4-S3] Menunggu MC duduk sendiri di kursi Research Center.");
                while (!kursiPeneliti.SedangDuduk) yield return null;
            }

            // MC dikunci di kursi selama dialog
            kursiPeneliti.TerkunciEksternal = true;
        }
        else
        {
            Debug.LogWarning("[Adegan L4-S3] Kursi Peneliti belum diisi di Inspector, MC tidak didudukkan.");
        }

        // Mode duduk otomatis: pastikan MC sudah di titik duduk (maksimal 0,5 detik)
        if (dudukOtomatis && kursiPeneliti != null)
        {
            GameObject pemainObj = GameObject.FindGameObjectWithTag(tagPemain);
            float tunggu = 0f;

            while (tunggu < 0.5f)
            {
                bool sudahDuduk = kursiPeneliti.SedangDuduk;
                bool diTitikDuduk = kursiPeneliti.sitPoint == null || pemainObj == null ||
                    Vector2.Distance(kursiPeneliti.sitPoint.position, pemainObj.transform.position) < 0.1f;

                if (sudahDuduk && diTitikDuduk) break;

                tunggu += Time.deltaTime;
                yield return null;
            }
        }

        // Jeda singkat supaya MC terlihat duduk dulu
        yield return new WaitForSeconds(jedaSebelumDialog);

        Debug.Log("[Adegan L4-S3] MC sudah duduk, dialog dipanggil.");

        // 2. Dua box dialog (Peneliti lalu Pemain)
        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L4-S3] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

        // 3. MC dilepas, ikon E akan muncul di laptop saat MC duduk
        if (kursiPeneliti != null) kursiPeneliti.TerkunciEksternal = false;

        dialogSelesai = true;
        sedangBerjalan = false;

        // Cegah Space / klik terakhir dialog terbaca untuk laptop
        waktuMulaiDuduk = Time.time;

        Debug.Log("[Adegan L4-S3] Dialog selesai, ikon E muncul di laptop.");
    }

    // ---------- Aktivitas laptop ----------
    private IEnumerator AlurLaptop()
    {
        bool pertamaKali = !sudahPernahSelesai;
        sedangBerjalan = true;

        if (ikonE != null) ikonE.SetActive(false);

        // MC tidak bisa berdiri selama aktivitas berjalan
        if (kursiPeneliti != null) kursiPeneliti.TerkunciEksternal = true;

        // 0. Dua box pilihan: jujur (atas) atau bohong (bawah)
        pilihanJujur = 0;
        TampilkanPilihanJujur(true);

        while (pilihanJujur == 0) yield return null;

        TampilkanPilihanJujur(false);

        if (pilihanJujur == 2)
        {
            if (IntegritasManager.Instance != null)
            {
                IntegritasManager.Instance.ReduceIntegritas(penguranganIntegritasBohong);
                Debug.Log("[Adegan L4-S3] Memilih bohong, integritas berkurang " + penguranganIntegritasBohong + ".");
            }
            else
            {
                Debug.LogWarning("[Adegan L4-S3] IntegritasManager.Instance tidak ditemukan di Scene!");
            }
        }

        // 1. Box pilihan kolaborasi
        pilihan = 0;
        TampilkanPilihan(true);

        while (pilihan == 0) yield return null;

        TampilkanPilihan(false);

        int poin = poinPilihan1;
        float durasi = (pilihanJujur == 2) ? durasiBohong : durasiJujur;

        if (pilihan == 2) poin = poinPilihan2;
        else if (pilihan == 3) poin = poinPilihan3;
        else if (pilihan == 4) poin = poinPilihan4;

        Debug.Log("[Adegan L4-S3] Memilih pilihan " + pilihan + ": bar " + durasi + " detik, poin " + poin + ".");

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
            Debug.Log("[Adegan L4-S3] Kolaborasi berkembang! Skor bertambah +" + poin);
        }
        else
        {
            Debug.LogWarning("[Adegan L4-S3] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 4. Reward
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

        // 5. Tutorial (sekali saja kalau diatur begitu)
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

        // 7. Cooldown baterai mulai
        sudahPernahSelesai = true;
        if (bisaDiulang) MulaiCooldown();

        // 8. MC dibebaskan (boleh berdiri)
        if (kursiPeneliti != null) kursiPeneliti.TerkunciEksternal = false;
        sedangBerjalan = false;

        // Event untuk scene berikutnya hanya dipanggil sekali
        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log(pertamaKali ? "[Adegan L4-S3] Scene 3 Level 4 selesai." : "[Adegan L4-S3] Aktivitas ulang selesai.");
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

        Debug.Log("[Adegan L4-S3] Laptop Research Center siap dipakai lagi.");
    }

    // ---------- Pembantu ----------
    private IEnumerator TampilkanTutorial(GameObject[] daftar)
    {
        if (daftar == null || daftar.Length == 0) yield break;

        if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;

        // Urutan penting: overlay dulu, baru box
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

            if (keDepan) box.transform.SetAsLastSibling();

            if (!box.activeInHierarchy)
                Debug.LogWarning("[Adegan L4-S3] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }
}