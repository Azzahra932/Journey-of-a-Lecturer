using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Level 2 - Scene 3 (Mengikuti penelitian), Lokasi: Research Center
//   1. MC masuk zona Research Center (setelah Scene 2 selesai): MC langsung duduk di kursi, Peneliti sudah duduk di sana
//   2. Dua box dialog (Peneliti, lalu Pemain), Space untuk lanjut
//   3. Box tutorial pertama (setelah percakapan dengan Peneliti)
//   4. MC dilepas. Saat MC mendekati mesin penelitian (mesin biru), ikon E muncul. Tekan E: muncul dua box pilihan (Jujur / Curang)
//   5. Setelah memilih: bar hijau berputar, poin bertambah, box tutorial kedua, reward, energi berkurang
//   6. MC dibebaskan
//
// Pasang script ini di objek zona yang punya Collider2D dengan Is Trigger dicentang (di Research Center).
// Interaksi mesin TIDAK butuh collider: cukup objek kosong "Titik Mesin" di mesin, jaraknya dihitung dari titik itu.
// Cara menyambung ke Scene 2: di LaptopKelas, event "Saat Mengajar Pertama Level 2 Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
public class AdeganLevel2Scene3 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 2 dulu
    public string tagPemain = "Player";

    [Header("Referensi")]
    public InteractiveDesk kursiPeneliti;       // Kursi di Research Center (komponen InteractiveDesk, isi Sit Point)
    public Sprite spriteDudukLevel2;            // Opsional: sprite MC level 2 saat duduk di kursi ini. Dipasang saat adegan mulai, jadi level 1 (bimbingan) tidak terpengaruh
    public bool matikanBimbinganDiKursi = true; // true = di level 2 kursi ini tidak lagi memicu bimbingan mahasiswa (Scene 3 level 1)
    public bool dudukOtomatis = false;          // false = MC duduk sendiri (jalan ke kursi, tekan tombol duduk), dialog baru muncul setelah MC duduk. true = MC langsung dipindah duduk saat masuk zona
    public GameObject penelitiObjek;            // Opsional: Peneliti yang baru dimunculkan saat Scene 3 siap. Kosong = Peneliti sudah ada dari awal

    [Header("Box Dialog (dibuat di Hierarchy, urut: Peneliti lalu Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 1f;        // Jeda (detik) setelah MC duduk sebelum dialog pertama muncul

    [Header("Box Tutorial 1 (muncul setelah percakapan dengan Peneliti)")]
    public GameObject[] boxTutorialSetelahDialog;

    [Header("Mesin Penelitian (ikon E muncul saat MC mendekat)")]
    public Transform titikMesin;                // Objek kosong di mesin biru. Kosong = pakai posisi Ikon E
    public float jarakInteraksi = 1.2f;         // Jarak MC ke titik mesin supaya ikon E muncul (lingkaran hijau terlihat di Scene saat objek ini dipilih)
    public GameObject ikonE;                    // Ikon "E" di mesin biru
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Pilihan Jujur / Curang")]
    public GameObject boxPilihan1;              // Box pilihan jujur
    public GameObject boxPilihan2;              // Box pilihan curang
    public Button tombolJujur;                  // Button di box jujur (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolCurang;                 // Button di box curang (jangan isi On Click di Inspector, dipasang lewat script)
    public float durasiJujur = 10f;             // Lama bar berputar kalau jujur (detik)
    public float durasiCurang = 5f;             // Lama bar berputar kalau curang (detik)
    public float penguranganIntegritasCurang = 10f;   // Integritas berkurang kalau curang

    [Header("Bar Hijau Berputar")]
    public GameObject canvasProgressBar;        // Objek induk bar
    public Image circularProgressBar;           // Image bar (Image Type: Filled)

    [Header("Hasil Setelah Bar Selesai")]
    public int jumlahPoin = 15;                 // Poin yang didapat
    public GameObject[] boxTutorialSetelahBar;  // Box tutorial 2 (muncul setelah bar hijau selesai)
    public int indexReward = 2;                 // Nomor reward di RewardManager (harus sudah ada). -1 = tanpa reward
    public float biayaEnergi = 15f;             // Energi yang berkurang setelah selesai
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Pengaturan Box Tutorial")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Opsional: untuk menyambung ke scene berikutnya

    private bool siap = false;
    private bool sudahJalan = false;
    private bool peringatanBelumSiap = false;   // supaya log "belum siap" hanya muncul sekali
    private int pilihan = 0;                    // 0 = belum memilih, 1 = jujur, 2 = curang

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorialSetelahDialog);
        SembunyikanSemua(boxTutorialSetelahBar);
        if (ikonE != null) ikonE.SetActive(false);
        if (boxPilihan1 != null) boxPilihan1.SetActive(false);
        if (boxPilihan2 != null) boxPilihan2.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);

        if (tombolJujur != null) tombolJujur.onClick.AddListener(PilihJujur);
        if (tombolCurang != null) tombolCurang.onClick.AddListener(PilihCurang);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek (objek zona yang punya Collider2D)
        AdeganLevel2Scene3[] semua = FindObjectsByType<AdeganLevel2Scene3>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L2-S3] Ada " + semua.Length + " objek yang memasang script AdeganLevel2Scene3. Pakai satu saja (objek zona), dan sambungkan event LaptopKelas ke objek itu.");

        // Kalau Peneliti baru dimunculkan saat Scene 3 siap, sembunyikan dulu
        if (penelitiObjek != null && !siap) penelitiObjek.SetActive(false);
    }

    void OnDestroy()
    {
        if (tombolJujur != null) tombolJujur.onClick.RemoveListener(PilihJujur);
        if (tombolCurang != null) tombolCurang.onClick.RemoveListener(PilihCurang);
    }

    // Lingkaran hijau di tab Scene (saat objek ini dipilih): area MC bisa menekan E di mesin
    void OnDrawGizmosSelected()
    {
        Transform acuan = AcuanMesin();
        if (acuan == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(acuan.position, jarakInteraksi);
    }

    private Transform AcuanMesin()
    {
        if (titikMesin != null) return titikMesin;
        if (ikonE != null) return ikonE.transform;
        return null;
    }

    // Dipanggil dari event "Saat Mengajar Pertama Level 2 Selesai" di LaptopKelas
    public void SiapkanScene()
    {
        siap = true;
        NonaktifkanBimbingan();
        if (penelitiObjek != null) penelitiObjek.SetActive(true);
        Debug.Log("[Adegan L2-S3] Scene 3 siap, menunggu MC masuk Research Center.");
    }

    void OnTriggerEnter2D(Collider2D other) { CobaMulai(other); }
    void OnTriggerStay2D(Collider2D other) { CobaMulai(other); }

    private void CobaMulai(Collider2D other)
    {
        if (sudahJalan) return;
        if (!other.CompareTag(tagPemain)) return;

        // "Langsung Siap" dibaca di sini (bukan hanya di Start) supaya bisa dicentang saat game sedang Play
        if (!siap && !langsungSiap)
        {
            if (!peringatanBelumSiap)
            {
                peringatanBelumSiap = true;
                Debug.Log("[Adegan L2-S3] MC masuk zona, tapi Scene 3 belum siap. Centang 'Langsung Siap' untuk tes, atau selesaikan Scene 2 (event LaptopKelas harus tersambung ke SiapkanScene).");
            }
            return;
        }

        sudahJalan = true;
        Debug.Log("[Adegan L2-S3] MC masuk zona, adegan dimulai.");
        StartCoroutine(Alur());
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

    // Laporan di Console: pose MC saat dialog akan dimulai (untuk mencari tahu kenapa MC terlihat berdiri)
    private void LaporPoseDuduk()
    {
        GameObject p = GameObject.FindGameObjectWithTag(tagPemain);
        if (p == null)
        {
            Debug.LogWarning("[Adegan L2-S3] Objek MC (tag '" + tagPemain + "') tidak ditemukan.");
            return;
        }

        SpriteRenderer sr = p.GetComponent<SpriteRenderer>();
        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        Animator an = p.GetComponent<Animator>();

        string namaSprite = (sr != null && sr.sprite != null) ? sr.sprite.name : "-";
        string namaSitKursi = (kursiPeneliti != null && kursiPeneliti.sitSprite != null) ? kursiPeneliti.sitSprite.name : "-";

        Debug.Log("[Adegan L2-S3] Pose MC saat dialog: objek=" + p.name
            + ", sprite=" + namaSprite
            + ", Sit Sprite kursi=" + namaSitKursi
            + ", Animator aktif=" + (an != null && an.enabled)
            + ", Simulated=" + (rb != null && rb.simulated));
    }

    // Di level 2 kursi ini dipakai untuk ngobrol dengan Peneliti, jadi duduk lewat tombol tidak boleh memicu bimbingan level 1 lagi
    private void NonaktifkanBimbingan()
    {
        if (!matikanBimbinganDiKursi || kursiPeneliti == null) return;

        if (kursiPeneliti.bimbinganScript != null)
        {
            kursiPeneliti.bimbinganScript = null;
            Debug.Log("[Adegan L2-S3] Bimbingan level 1 dilepas dari kursi ini.");
        }
    }

    public void PilihJujur()
    {
        if (pilihan == 0) pilihan = 1;
    }

    public void PilihCurang()
    {
        if (pilihan == 0) pilihan = 2;
    }

    private void SembunyikanSemua(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    private IEnumerator Alur()
    {
        NonaktifkanBimbingan();

        // Peneliti dimunculkan (kalau tadinya disembunyikan)
        if (penelitiObjek != null) penelitiObjek.SetActive(true);

        // 1. MC duduk di kursi. Otomatis (langsung dipindah), atau menunggu MC duduk sendiri di kursi
        if (kursiPeneliti != null)
        {
            if (spriteDudukLevel2 != null) kursiPeneliti.sitSprite = spriteDudukLevel2;

            if (dudukOtomatis)
            {
                kursiPeneliti.DudukUntukAdegan();
            }
            else
            {
                Debug.Log("[Adegan L2-S3] Menunggu MC duduk sendiri di kursi Research Center.");

                // Dialog baru dipanggil setelah MC benar-benar duduk
                while (!kursiPeneliti.SedangDuduk) yield return null;
            }

            // MC dikunci di kursi selama dialog dan tutorial pertama
            kursiPeneliti.TerkunciEksternal = true;
        }
        else
        {
            Debug.LogWarning("[Adegan L2-S3] Kursi Peneliti belum diisi di Inspector, MC tidak didudukkan.");
        }

        // Mode Duduk Otomatis: pastikan MC sudah berada di titik duduk (maksimal 0,5 detik).
        // Mode duduk sendiri tidak perlu menunggu, karena dialog baru dipanggil setelah MC duduk.
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

        // Jeda singkat supaya MC terlihat duduk dulu, baru dialog dipanggil
        yield return new WaitForSeconds(jedaSebelumDialog);

        LaporPoseDuduk();
        Debug.Log("[Adegan L2-S3] MC sudah duduk, dialog dipanggil.");

        // 2. Dua box dialog
        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L2-S3] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog));

        // 3. Box tutorial pertama
        if (boxTutorialSetelahDialog == null || boxTutorialSetelahDialog.Length == 0)
            Debug.LogWarning("[Adegan L2-S3] Box Tutorial Setelah Dialog masih kosong di Inspector, dilewati.");
        else
            yield return StartCoroutine(TampilkanTutorial(boxTutorialSetelahDialog));

        // 4. MC dilepas. Ikon E muncul saat MC mendekati mesin, tunggu E ditekan
        if (kursiPeneliti != null) kursiPeneliti.TerkunciEksternal = false;

        yield return StartCoroutine(TungguInteraksiMesin());

        // MC dikunci selama pilihan, bar, dan tutorial kedua
        KunciPemain(true);
        if (kursiPeneliti != null) kursiPeneliti.TerkunciEksternal = true;

        // 5. Dua box pilihan (jujur / curang)
        pilihan = 0;
        if (boxPilihan1 != null) boxPilihan1.SetActive(true);
        if (boxPilihan2 != null) boxPilihan2.SetActive(true);

        while (pilihan == 0) yield return null;

        if (boxPilihan1 != null) boxPilihan1.SetActive(false);
        if (boxPilihan2 != null) boxPilihan2.SetActive(false);

        float durasi = durasiJujur;

        if (pilihan == 2)
        {
            durasi = durasiCurang;

            if (IntegritasManager.Instance != null)
                IntegritasManager.Instance.ReduceIntegritas(penguranganIntegritasCurang);
            else
                Debug.LogWarning("[Adegan L2-S3] IntegritasManager.Instance tidak ditemukan di Scene!");
        }

        // 6. Bar hijau berputar
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

        // 7. Poin
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoin);
            Debug.Log("[Adegan L2-S3] Penelitian selesai! Skor bertambah +" + jumlahPoin);
        }
        else
        {
            Debug.LogWarning("[Adegan L2-S3] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 8. Box tutorial kedua (setelah bar hijau selesai)
        if (boxTutorialSetelahBar == null || boxTutorialSetelahBar.Length == 0)
            Debug.LogWarning("[Adegan L2-S3] Box Tutorial Setelah Bar masih kosong di Inspector, dilewati.");
        else
            yield return StartCoroutine(TampilkanTutorial(boxTutorialSetelahBar));

        // 9. Reward (muncul sendiri, lalu hilang sendiri)
        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));
        }

        // 10. Energi berkurang, lalu cek apakah habis
        if (energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        Selesai();
    }

    // Ikon E hanya muncul saat MC berada dalam "Jarak Interaksi" dari mesin. Tekan E di jarak itu untuk lanjut.
    private IEnumerator TungguInteraksiMesin()
    {
        Transform acuan = AcuanMesin();
        if (acuan == null)
            Debug.LogWarning("[Adegan L2-S3] Titik Mesin dan Ikon E kosong, E bisa ditekan di mana saja.");

        Transform pemain = null;

        while (true)
        {
            if (pemain == null || !pemain.gameObject.activeInHierarchy)
            {
                GameObject g = GameObject.FindGameObjectWithTag(tagPemain);
                pemain = (g != null) ? g.transform : null;
            }

            bool dekat = (acuan == null) || (pemain != null && Vector2.Distance(acuan.position, pemain.position) <= jarakInteraksi);

            if (ikonE != null && ikonE.activeSelf != dekat) ikonE.SetActive(dekat);

            if (dekat && Input.GetKeyDown(tombolInteraksi)) break;

            yield return null;
        }

        if (ikonE != null) ikonE.SetActive(false);
    }

    // Box tutorial: game berhenti (opsional) dan layar digelapkan selama box tampil
    private IEnumerator TampilkanTutorial(GameObject[] daftar)
    {
        if (daftar == null || daftar.Length == 0) yield break;

        if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;

        // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
        if (overlayHitam != null) overlayHitam.SetActive(true);

        yield return StartCoroutine(TampilkanBoxBerurutan(daftar));

        if (overlayHitam != null) overlayHitam.SetActive(false);

        if (hentikanWaktuSaatTutorial) Time.timeScale = 1f;
    }

    // Box tampil satu per satu: Space (atau klik kiri) menutup box dan lanjut ke berikutnya
    private IEnumerator TampilkanBoxBerurutan(GameObject[] daftar)
    {
        foreach (GameObject box in daftar)
        {
            if (box == null) continue;

            box.SetActive(true);

            if (!box.activeInHierarchy)
                Debug.LogWarning("[Adegan L2-S3] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    // Menahan pergerakan MC (sama seperti di LaptopInteraction)
    private void KunciPemain(bool kunci)
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm == null) return;

        if (kunci && pm.animator != null && pm.animator.enabled)
        {
            pm.animator.SetBool("IsMoving", false);
            pm.animator.SetFloat("MoveX", 0f);
            pm.animator.SetFloat("MoveY", 0f);
        }

        pm.enabled = !kunci;
    }

    private void Selesai()
    {
        // MC bebas bergerak dan berdiri lagi
        KunciPemain(false);
        if (kursiPeneliti != null) kursiPeneliti.TerkunciEksternal = false;

        saatAdeganSelesai.Invoke();

        Debug.Log("[Adegan L2-S3] Scene 3 Mengikuti penelitian selesai.");
    }
}