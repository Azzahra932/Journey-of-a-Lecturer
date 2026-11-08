using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Level 3 - Scene 4 (Publikasi Penelitian), Lokasi: Perpustakaan
//   1. Scene 3 Level 3 selesai -> Scene 4 siap
//   2. MC duduk di kursi perpustakaan: box dialog Pemain muncul (Space untuk lanjut)
//   3. Ikon E muncul di laptop perpustakaan. Tekan E: muncul box pilihan jurnal (klik salah satu)
//        Jurnal Sinta 1-2 : bar berputar 15 detik, poin 25
//        Jurnal Sinta 3-4 : bar berputar 10 detik, poin 20
//        Jurnal Sinta 5-6 : bar berputar  5 detik, poin 15
//   4. Bar hijau berputar, poin bertambah
//   5. Box dialog setelah selesai (opsional), reward via RewardManager, lalu box tutorial (di atas overlay hitam)
//   6. Energi berkurang, cooldown baterai mulai (layar laptop redup + baterai terisi)
//   7. Setelah cooldown penuh, MC bisa duduk lagi dan memakai laptop berulang (langsung ikon E, pilihan jurnal, bar, poin, reward).
//      Dialog awal, dialog selesai, dan tutorial hanya muncul pada aktivitas pertama (bisa diubah di Inspector).
public class AdeganLevel3Scene4 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;             // Centang untuk tes tanpa menyelesaikan Scene 3 dulu

    [Header("Kursi dan Laptop Perpustakaan")]
    public KursiPerpusInteraction kursiPerpus;  // Kursi perpustakaan (komponen KursiPerpusInteraction). Adegan mulai saat MC duduk di sini
    public LaptopPerpusInteraction laptopPerpusLama;    // Script laptop perpustakaan lama (dimatikan begitu adegan ini mulai). Boleh kosong
    public Sprite spriteDudukMCLevel3;          // Opsional: sprite MC level 3 saat duduk di kursi ini (dipasang ke gambar duduk kursi, sebelum MC duduk)
    public GameObject ikonE;                      // Ikon "E" di atas laptop perpustakaan
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Box Dialog Awal (dibuat di Hierarchy, Pemain)")]
    public GameObject[] boxDialogAwal;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.8f;      // Jeda (detik) setelah MC duduk sebelum dialog muncul

    [Header("Box Pilihan Jurnal")]
    public GameObject boxPilihan;               // Panel pilihan (judul + tiga tombol)
    public Button tombolSinta12;                // Button Jurnal Sinta 1-2 (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolSinta34;                // Button Jurnal Sinta 3-4
    public Button tombolSinta56;                // Button Jurnal Sinta 5-6

    [Header("Poin dan Lama Bar per Pilihan")]
    public int poinSinta12 = 25;
    public float durasiSinta12 = 15f;
    public int poinSinta34 = 20;
    public float durasiSinta34 = 10f;
    public int poinSinta56 = 15;
    public float durasiSinta56 = 5f;

    [Header("Bar Hijau Berputar")]
    public GameObject canvasProgressBar;        // Objek induk bar
    public Image circularProgressBar;           // Image bar (Image Type: Filled)

    [Header("Setelah Bar Selesai")]
    public GameObject[] boxDialogSelesai;       // Opsional: dialog Pemain "Artikel ilmiah telah selesai disusun dan siap diajukan."
    public bool dialogSelesaiSekaliSaja = true; // true = dialog ini hanya muncul pada aktivitas pertama
    public int indexReward = 0;                 // Nomor indeks reward di RewardManager (muncul tiap selesai)
    public float biayaEnergi = 15f;               // Energi yang berkurang tiap selesai (0 = tidak mengurangi energi)
    public EnergyBar energyBar;                   // Kosong = dicari otomatis

    [Header("Box Tutorial (muncul setelah reward, di atas overlay hitam)")]
    public GameObject overlayHitam;               // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;              // Box tutorial, urut dari atas
    public bool hentikanWaktuSaatTutorial = true;
    public bool tutorialSekaliSaja = true;      // true = tutorial hanya muncul pada aktivitas pertama

    [Header("Aktivitas Berulang")]
    public bool bisaDiulang = true;              // true = setelah cooldown selesai, laptop bisa dipakai lagi

    [Header("Cooldown Baterai (seperti laptop lab)")]
    public float durasiCooldown = 60f;          // Jeda (detik) sebelum laptop bisa dipakai lagi
    public GameObject ikonBateraiLemah;           // Objek induk baterai, muncul saat cooldown (duplikat dari baterai laptop lab / kelas)
    public IndikatorBaterai indikatorBaterai;   // Script pengisi segmen baterai
    public TMP_Text teksCooldown;                 // Teks di dekat baterai (contoh "Recharging...")
    public string isiTeksCooldown = "Recharging...";
    public GameObject teksIstirahat;              // Opsional: teks "Laptop sedang istirahat" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public SpriteRenderer spriteLaptop;           // Gambar laptop perpustakaan yang diredupkan selama cooldown
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Layar redup

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah aktivitas pertama selesai (untuk menyambung ke scene Level 3 berikutnya)

    private bool siap = false;
    private bool sudahJalan = false;              // true setelah adegan pertama (dialog awal) dimulai
    private bool sedangBerjalan = false;
    private bool sudahPernahSelesai = false;
    private bool eventSudahDipanggil = false;
    private bool tutorialSudahTampil = false;
    private bool dialogSelesaiSudahTampil = false;
    private bool peringatanKursiDitampilkan = false;
    private int pilihan = 0;                      // 0 = belum memilih, 1 = Sinta 1-2, 2 = Sinta 3-4, 3 = Sinta 5-6

    // Cegah tombol E atau Space untuk DUDUK ikut terbaca sebagai E untuk laptop
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

        SembunyikanSemua(boxDialogAwal);
        SembunyikanSemua(boxDialogSelesai);
        SembunyikanSemua(boxTutorial);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (ikonE != null) ikonE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCooldown != null) teksCooldown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;
        TampilkanPilihan(false);

        if (tombolSinta12 != null) tombolSinta12.onClick.AddListener(PilihSinta12);
        if (tombolSinta34 != null) tombolSinta34.onClick.AddListener(PilihSinta34);
        if (tombolSinta56 != null) tombolSinta56.onClick.AddListener(PilihSinta56);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        if (langsungSiap) MatikanLaptopLama();

        AdeganLevel3Scene4[] semua = FindObjectsByType<AdeganLevel3Scene4>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L3-S4] Ada " + semua.Length + " objek yang memasang script AdeganLevel3Scene4. Pakai satu saja.");
    }

    void OnDestroy()
    {
        if (tombolSinta12 != null) tombolSinta12.onClick.RemoveListener(PilihSinta12);
        if (tombolSinta34 != null) tombolSinta34.onClick.RemoveListener(PilihSinta34);
        if (tombolSinta56 != null) tombolSinta56.onClick.RemoveListener(PilihSinta56);
    }

    public void MulaiLevel3()
    {
        MatikanLaptopLama();
        Debug.Log("[Adegan L3-S4] Level 3 dimulai, laptop perpustakaan lama dimatikan sampai Scene 4.");
    }

    public void SiapkanScene()
    {
        siap = true;
        MatikanLaptopLama();
        TerapkanSpriteMC();
        Debug.Log("[Adegan L3-S4] Scene 4 siap, menunggu MC duduk di kursi perpustakaan.");
    }

    [ContextMenu("Tes Adegan (langsung mulai, tanpa menunggu duduk)")]
    private void TesAdegan()
    {
        if (sudahJalan) return;
        siap = true;
        sudahJalan = true;
        StartCoroutine(Alur());
    }

    public void PilihSinta12() { if (pilihan == 0) pilihan = 1; }
    public void PilihSinta34() { if (pilihan == 0) pilihan = 2; }
    public void PilihSinta56() { if (pilihan == 0) pilihan = 3; }

    void Update()
    {
        if (teksIstirahat != null && teksIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksIstirahat.SetActive(false);

        if (cooldownAktif) TickCooldown();

        if (sedangBerjalan) return;

        if (!siap && !langsungSiap) return;

        if (kursiPerpus == null)
        {
            if (!peringatanKursiDitampilkan)
            {
                peringatanKursiDitampilkan = true;
                Debug.LogWarning("[Adegan L3-S4] Kursi Perpus belum diisi di Inspector, adegan tidak bisa dimulai.");
            }
            return;
        }

        bool duduk = kursiPerpus.sedangDuduk;
        if (duduk && !dudukSebelumnya) waktuMulaiDuduk = Time.time;
        dudukSebelumnya = duduk;

        if (!sudahJalan)
        {
            if (!duduk)
            {
                TerapkanSpriteMC();
                return;
            }

            sudahJalan = true;
            Debug.Log("[Adegan L3-S4] MC duduk di kursi perpustakaan, adegan dimulai.");
            StartCoroutine(Alur());
            return;
        }

        if (!sudahPernahSelesai) return;

        if (!bisaDiulang)
        {
            if (ikonE != null && ikonE.activeSelf) ikonE.SetActive(false);
            return;
        }

        bool bolehTekan = duduk && (Time.time - waktuMulaiDuduk >= jedaSetelahDuduk);

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
            StartCoroutine(AlurAktivitas(false));
    }

    private void TerapkanSpriteMC()
    {
        if (kursiPerpus == null || spriteDudukMCLevel3 == null || kursiPerpus.spriteDuduk == null) return;

        SpriteRenderer sr = kursiPerpus.spriteDuduk.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != spriteDudukMCLevel3) sr.sprite = spriteDudukMCLevel3;
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

    private void MatikanLaptopLama()
    {
        if (laptopPerpusLama == null) return;

        laptopPerpusLama.enabled = false;

        if (laptopPerpusLama.petunjukTekanE != null) laptopPerpusLama.petunjukTekanE.SetActive(false);
        if (laptopPerpusLama.ikonBateraiLemah != null) laptopPerpusLama.ikonBateraiLemah.SetActive(false);
        if (laptopPerpusLama.teksCooldown != null) laptopPerpusLama.teksCooldown.gameObject.SetActive(false);
        if (laptopPerpusLama.teksIstirahat != null) laptopPerpusLama.teksIstirahat.SetActive(false);
        if (laptopPerpusLama.spriteLaptop != null) laptopPerpusLama.spriteLaptop.color = laptopPerpusLama.warnaNormal;
    }

    private IEnumerator Alur()
    {
        sedangBerjalan = true;

        if (kursiPerpus != null) kursiPerpus.TerkunciEksternal = true;
        MatikanLaptopLama();

        yield return new WaitForSeconds(jedaSebelumDialog);

        if (boxDialogAwal == null || boxDialogAwal.Length == 0)
            Debug.LogWarning("[Adegan L3-S4] Box Dialog Awal masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialogAwal, false));

        if (ikonE != null) ikonE.SetActive(true);

        yield return new WaitForSeconds(0.3f);

        while (!Input.GetKeyDown(tombolInteraksi))
            yield return null;

        if (ikonE != null) ikonE.SetActive(false);

        yield return StartCoroutine(AlurAktivitas(true));
    }

    private IEnumerator AlurAktivitas(bool pertamaKali)
    {
        sedangBerjalan = true;

        if (ikonE != null) ikonE.SetActive(false);
        if (kursiPerpus != null) kursiPerpus.TerkunciEksternal = true;

        pilihan = 0;
        TampilkanPilihan(true);

        while (pilihan == 0) yield return null;

        TampilkanPilihan(false);

        int poin = poinSinta12;
        float durasi = durasiSinta12;
        string namaPilihan = "Sinta 1-2";

        if (pilihan == 2)
        {
            poin = poinSinta34;
            durasi = durasiSinta34;
            namaPilihan = "Sinta 3-4";
        }
        else if (pilihan == 3)
        {
            poin = poinSinta56;
            durasi = durasiSinta56;
            namaPilihan = "Sinta 5-6";
        }

        Debug.Log("[Adegan L3-S4] Memilih jurnal " + namaPilihan + ": bar " + durasi + " detik, poin " + poin + ".");

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

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(poin);
            Debug.Log("[Adegan L3-S4] Artikel selesai disusun! Skor bertambah +" + poin);
        }
        else
        {
            Debug.LogWarning("[Adegan L3-S4] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        if (boxDialogSelesai != null && boxDialogSelesai.Length > 0 && !(dialogSelesaiSekaliSaja && dialogSelesaiSudahTampil))
        {
            dialogSelesaiSudahTampil = true;
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialogSelesai, false));
        }

        // Reward dipanggil langsung via RewardManager berdasarkan indexReward
        if (RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));
        }
        else
        {
            Debug.LogWarning("[Adegan L3-S4] RewardManager.Instance tidak ditemukan di Scene!");
        }

        if (!(tutorialSekaliSaja && tutorialSudahTampil))
        {
            tutorialSudahTampil = true;
            yield return StartCoroutine(TampilkanTutorial());
        }

        if (biayaEnergi > 0f && energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        sudahPernahSelesai = true;
        if (bisaDiulang) MulaiCooldown();

        if (kursiPerpus != null) kursiPerpus.TerkunciEksternal = false;
        sedangBerjalan = false;

        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log(pertamaKali ? "[Adegan L3-S4] Scene 4 Level 3 selesai." : "[Adegan L3-S4] Aktivitas ulang selesai.");
    }

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

        Debug.Log("[Adegan L3-S4] Laptop perpustakaan siap dipakai lagi.");
    }

    private IEnumerator TampilkanTutorial()
    {
        if (boxTutorial == null || boxTutorial.Length == 0) yield break;

        if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;

        if (overlayHitam != null)
        {
            overlayHitam.SetActive(true);
            overlayHitam.transform.SetAsLastSibling();
        }

        yield return StartCoroutine(TampilkanBoxBerurutan(boxTutorial, true));

        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (hentikanWaktuSaatTutorial) Time.timeScale = 1f;
    }

    private IEnumerator TampilkanBoxBerurutan(GameObject[] daftar, bool keDepan)
    {
        foreach (GameObject box in daftar)
        {
            if (box == null) continue;

            box.SetActive(true);

            if (keDepan) box.transform.SetAsLastSibling();

            if (!box.activeInHierarchy)
                Debug.LogWarning("[Adegan L3-S4] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }
}