using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Level 2 - Scene 4 (Menghasilkan karya ilmiah), Lokasi: Perpustakaan
//   1. Scene 3 selesai -> Dosen Senior muncul di depan pintu perpustakaan
//   2. MC masuk zona pintu: MC berhenti, dua box dialog (Dosen Senior lalu Pemain), Space untuk lanjut
//   3. MC berjalan ke kursi perpustakaan dan duduk sendiri (Space di dekat kursi)
//   4. Ikon E muncul di laptop. Tekan E: muncul dua box pilihan (Jujur / Curang)
//   5. Setelah memilih: bar hijau berputar, poin bertambah, reward, box tutorial, energi berkurang
//   6. MC dibebaskan
//
// Pasang script ini di objek zona di pintu perpustakaan (Collider2D dengan Is Trigger dicentang).
// Cara menyambung dari Scene 3: di AdeganLevel2Scene3, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
public class AdeganLevel2Scene4 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 3 dulu
    public string tagPemain = "Player";

    [Header("Dosen Senior")]
    public GameObject dosenSeniorObjek;         // Dosen Senior di depan pintu. Nonaktif di awal, muncul saat Scene 4 siap
    public Transform titikDosenSenior;          // Titik di depan pintu perpustakaan. Dosen Senior dipindahkan ke sini saat muncul (aman walau objeknya juga dipakai level 1)
    public bool sembunyikanDosenSetelahSelesai = false;   // true = Dosen Senior hilang setelah adegan selesai

    [Header("Box Dialog di Pintu (urut: Dosen Senior lalu Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;

    [Header("Menuju Kursi Perpustakaan")]
    public KursiPerpusInteraction kursiPerpus;  // Kursi perpustakaan (MC duduk sendiri dengan Space)
    public GameObject panahKeKursi;             // Opsional: panah yang menunjuk ke kursi, muncul setelah dialog selesai
    public BukuInteraction bukuPerpus;          // Opsional: dimatikan sementara selama adegan supaya tombol E tidak bentrok
    public bool bukuTidakDipakaiLagi = true;    // true = setelah adegan selesai, aktivitas buku tidak menyala lagi (digantikan laptop)
    public LaptopPerpusInteraction laptopPerpus; // Aktivitas laptop berulang yang dibuka setelah adegan selesai

    [Header("Laptop (Ikon E)")]
    public GameObject ikonE;                    // Ikon "E" di atas laptop perpustakaan
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
    public int indexReward = 5;                 // Nomor reward di RewardManager (nomor mulai dari 0; 5 = elemen ke-6, harus sudah ada di daftar). -1 = tanpa reward
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;            // Box tutorial (dibuat di Hierarchy), urut dari atas
    public bool hentikanWaktuSaatTutorial = true;
    public float biayaEnergi = 15f;             // Energi yang berkurang setelah selesai
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Opsional: untuk menyambung ke scene berikutnya

    private bool siap = false;
    private bool sudahJalan = false;
    private int pilihan = 0;                    // 0 = belum memilih, 1 = jujur, 2 = curang

    private PlayerMovement pm;
    private bool pemainDikunci = false;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorial);
        if (ikonE != null) ikonE.SetActive(false);
        if (panahKeKursi != null) panahKeKursi.SetActive(false);
        if (boxPilihan1 != null) boxPilihan1.SetActive(false);
        if (boxPilihan2 != null) boxPilihan2.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);

        if (tombolJujur != null) tombolJujur.onClick.AddListener(PilihJujur);
        if (tombolCurang != null) tombolCurang.onClick.AddListener(PilihCurang);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Dosen Senior baru dimunculkan saat Scene 4 siap
        if (siap) MunculkanDosenSenior();
        else if (dosenSeniorObjek != null) dosenSeniorObjek.SetActive(false);
    }

    void OnDestroy()
    {
        if (tombolJujur != null) tombolJujur.onClick.RemoveListener(PilihJujur);
        if (tombolCurang != null) tombolCurang.onClick.RemoveListener(PilihCurang);
    }

    // Memindahkan Dosen Senior ke titik di depan pintu perpustakaan lalu memunculkannya
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

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel2Scene3
    public void SiapkanScene()
    {
        siap = true;
        MunculkanDosenSenior();
        Debug.Log("[Adegan L2-S4] Scene 4 siap, Dosen Senior menunggu di pintu perpustakaan.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[Adegan L2-S4] Ada yang masuk zona: '" + other.name + "' (tag " + other.tag + "). siap=" + siap + ", sudahJalan=" + sudahJalan);
        CobaMulai(other);
    }
    void OnTriggerStay2D(Collider2D other) { CobaMulai(other); }

    private void CobaMulai(Collider2D other)
    {
        if (!siap || sudahJalan) return;
        if (!other.CompareTag(tagPemain)) return;

        sudahJalan = true;
        StartCoroutine(Alur());
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (langsung mulai)")]
    private void TesAdegan()
    {
        if (sudahJalan) return;
        siap = true;
        sudahJalan = true;
        MunculkanDosenSenior();
        StartCoroutine(Alur());
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
        Debug.Log("[Adegan L2-S4] MC tiba di pintu perpustakaan, adegan dimulai.");

        // 0. MC berhenti di pintu. Aktivitas buku dimatikan sementara supaya tombol E tidak bentrok.
        KunciPemain(true);

        if (bukuPerpus != null)
        {
            if (bukuPerpus.petunjukTekanE != null) bukuPerpus.petunjukTekanE.SetActive(false);
            bukuPerpus.enabled = false;
        }

        yield return new WaitForSeconds(0.3f);

        // 1. Dua box dialog di pintu
        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L2-S4] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog));

        // 2. MC bebas berjalan ke kursi perpustakaan
        KunciPemain(false);
        if (panahKeKursi != null) panahKeKursi.SetActive(true);

        // 3. Tunggu MC duduk sendiri di kursi perpustakaan
        if (kursiPerpus == null)
        {
            Debug.LogWarning("[Adegan L2-S4] Kursi Perpus belum diisi di Inspector, adegan lanjut tanpa menunggu MC duduk.");
        }
        else
        {
            while (!kursiPerpus.sedangDuduk) yield return null;
            kursiPerpus.TerkunciEksternal = true;   // MC tidak bisa berdiri selama adegan
        }

        if (panahKeKursi != null) panahKeKursi.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        // 4. Ikon E di laptop, tunggu E ditekan
        if (ikonE != null) ikonE.SetActive(true);

        yield return new WaitForSeconds(0.3f);
        while (!Input.GetKeyDown(tombolInteraksi)) yield return null;

        if (ikonE != null) ikonE.SetActive(false);

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
                Debug.LogWarning("[Adegan L2-S4] IntegritasManager.Instance tidak ditemukan di Scene!");
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
            Debug.Log("[Adegan L2-S4] Karya ilmiah selesai! Skor bertambah +" + jumlahPoin);
        }
        else
        {
            Debug.LogWarning("[Adegan L2-S4] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 8. Reward muncul sendiri begitu bar selesai (hilang sendiri), lalu box tutorial menyusul
        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));

            // Tunggu box reward hilang dulu supaya tidak bertumpuk dengan box tutorial
            yield return new WaitForSecondsRealtime(RewardManager.Instance.lamaTampil + 0.3f);
        }

        // 9. Box tutorial
        if (boxTutorial != null && boxTutorial.Length > 0)
        {
            if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;
            if (overlayHitam != null) overlayHitam.SetActive(true);

            yield return StartCoroutine(TampilkanBoxBerurutan(boxTutorial));

            if (overlayHitam != null) overlayHitam.SetActive(false);
            if (hentikanWaktuSaatTutorial) Time.timeScale = 1f;
        }

        // 10. Energi berkurang, lalu cek apakah habis
        if (energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        Selesai();
    }

    // Box tampil satu per satu: Space (atau klik kiri) menutup box dan lanjut ke berikutnya
    private IEnumerator TampilkanBoxBerurutan(GameObject[] daftar)
    {
        foreach (GameObject box in daftar)
        {
            if (box == null) continue;

            box.SetActive(true);

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

            if (pm.animator != null)
            {
                pm.animator.SetBool("IsMoving", false);
                pm.animator.SetFloat("MoveX", 0f);
                pm.animator.SetFloat("MoveY", 0f);
            }

            Rigidbody2D rb = pm.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            pm.enabled = false;
            pemainDikunci = true;
        }
        else
        {
            if (pemainDikunci && pm != null) pm.enabled = true;
            pemainDikunci = false;
        }
    }

    private void Selesai()
    {
        // MC bebas berdiri lagi, aktivitas buku menyala lagi
        if (kursiPerpus != null) kursiPerpus.TerkunciEksternal = false;
        // Buku digantikan laptop: aktivitas buku tetap mati, aktivitas laptop berulang dibuka
        if (bukuPerpus != null && !bukuTidakDipakaiLagi) bukuPerpus.enabled = true;
        if (laptopPerpus != null) laptopPerpus.BukaAktivitasBerulang();

        if (sembunyikanDosenSetelahSelesai && dosenSeniorObjek != null)
            dosenSeniorObjek.SetActive(false);

        saatAdeganSelesai.Invoke();

        Debug.Log("[Adegan L2-S4] Scene 4 Menghasilkan karya ilmiah selesai.");
    }
}