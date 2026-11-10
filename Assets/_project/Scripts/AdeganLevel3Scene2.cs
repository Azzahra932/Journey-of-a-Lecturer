using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

// Level 3 - Scene 2 (Mengembangkan penelitian), Lokasi: Laboratorium (Ruang A)
//   1. Scene 1 Level 3 selesai -> Scene 2 siap, Peneliti berdiri di depan pintu laboratorium
//   2. MC mendekati pintu: MC berhenti, dua box dialog (Peneliti lalu Pemain), Space untuk lanjut
//   3. Setelah dialog: box tutorial pertama (di atas overlay hitam)
//   4. MC berjalan ke meja laboratorium dan duduk (Peneliti hilang). Saat MC duduk, ikon E muncul di laptop
//   5. Tekan E: bar hijau berputar, lalu poin bertambah (15), box reward, lalu box tutorial kedua (sekali saja)
//   6. Cooldown baterai mulai (layar laptop redup + baterai terisi). Setelah penuh, laptop bisa dipakai lagi (berulang)
//
// Script ini dipasang di objek apa saja (tidak perlu collider). Dialog dimulai saat MC mendekati "Titik Pintu".
// Kursi laboratorium memakai komponen KursiPerpusInteraction (duplikat dari kursi perpustakaan), dan baru bisa dipakai setelah dialog selesai.
// Cara menyambung dari Scene 1: di AdeganLevel3Scene1, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Bagian yang tidak diisi di Inspector (mis. reward atau baterai) dilewati.
public class AdeganLevel3Scene2 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 1 dulu

    [Header("Peneliti di Pintu Laboratorium")]
    public GameObject penelitiObjek;            // Peneliti yang berdiri di depan pintu. Nonaktif di awal, muncul saat Scene 2 siap
    public Transform titikPintu;                // Objek kosong di depan pintu laboratorium (dialog mulai saat MC mendekat)
    public float jarakPintu = 1.5f;             // Jarak MC ke titik pintu supaya dialog mulai (lingkaran kuning terlihat di Scene saat objek ini dipilih)
    public bool penelitiHilangSaatDuduk = true; // true = Peneliti hilang saat MC duduk di kursi laboratorium

    [Header("Box Dialog (dibuat di Hierarchy, urut: Peneliti lalu Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.3f;      // Jeda (detik) setelah MC berhenti di pintu sebelum dialog muncul

    [Header("Box Tutorial (di atas overlay hitam)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    [FormerlySerializedAs("boxTutorial")]
    public GameObject[] boxTutorialSetelahDialog;   // Tutorial 1: muncul setelah dialog dengan Peneliti (isi 1 box)
    public GameObject[] boxTutorialSetelahBar;      // Tutorial 2: muncul setelah bar hijau berputar selesai (isi 1 box)
    public bool tutorialBarSekaliSaja = true;       // true = tutorial 2 hanya muncul pada aktivitas laptop yang pertama
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Kursi dan Laptop Laboratorium (MC duduk, seperti di kelas)")]
    public KursiPerpusInteraction kursiLab;     // Komponen kursi di kursi laboratorium (duplikat dari kursi perpustakaan). Otomatis dimatikan sampai dialog selesai
    public GameObject ikonE;                    // Ikon "E" di atas laptop laboratorium, muncul saat MC duduk dan laptop siap
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Gambar MC Duduk (dibuat otomatis kalau field Sprite Duduk di komponen kursi masih kosong)")]
    public Sprite spriteDudukLab;               // Sprite MC saat duduk (mis. gambar MC level 3 menghadap depan / bawah)
    public Transform titikDudukLab;             // Posisi gambar duduk. Kosong = posisi kursi laboratorium
    public int urutanLayarDuduk = 3;            // Order in Layer gambar duduk (di atas kursi, dan di bawah meja kalau meja harus menutupi tubuh MC)

    [Header("Bar Hijau Berputar")]
    public GameObject canvasProgressBar;        // Objek induk bar
    public Image circularProgressBar;           // Image bar (Image Type: Filled)
    public float durasiBar = 3f;                // Lama bar berputar (detik), sama seperti di kelas

    [Header("Hasil Setelah Bar Selesai")]
    public int jumlahPoin = 15;                 // Poin yang didapat tiap selesai
    public int indexReward = -1;                // Nomor reward di RewardManager (harus sudah ada di daftar). -1 = pakai Box Reward di bawah (kalau diisi)
    public GameObject boxReward;                // Opsional: box reward buatan sendiri, dipakai kalau Index Reward = -1
    public float lamaBoxReward = 2f;            // Lama Box Reward tampil (detik)
    public float biayaEnergi = 15f;             // Energi yang berkurang tiap selesai (0 = tidak mengurangi energi)
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Aktivitas Berulang")]
    public bool bisaDiulang = true;             // true = setelah cooldown selesai, laptop laboratorium bisa dipakai lagi

    [Header("Cooldown Baterai (seperti laptop kelas)")]
    public float durasiCooldown = 60f;          // Jeda (detik) sebelum laptop bisa dipakai lagi
    public GameObject ikonBateraiLemah;         // Objek induk baterai, muncul saat cooldown (duplikat dari baterai laptop kelas)
    public IndikatorBaterai indikatorBaterai;   // Script pengisi segmen baterai
    public TMP_Text teksCooldown;               // Teks di dekat baterai (contoh "Recharging...")
    public string isiTeksCooldown = "Recharging...";
    public GameObject teksIstirahat;            // Opsional: teks "Laptop sedang istirahat" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public SpriteRenderer spriteLaptop;         // Gambar laptop laboratorium yang diredupkan selama cooldown
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Layar redup

    [Header("Setelah Scene 2 Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah laptop laboratorium pertama kali selesai dikerjakan (untuk menyambung ke scene berikutnya)

    private bool siap = false;
    private bool dialogSelesai = false;
    private bool sedangBerjalan = false;

    // True selama adegan / aktivitas berjalan (dipakai PopupLevel3Selesai supaya pop-up menunggu sampai selesai)
    public bool SedangSibuk { get { return sedangBerjalan; } }
    private bool sudahPernahSelesai = false;
    private bool eventSudahDipanggil = false;
    private bool peringatanKursiDitampilkan = false;
    private bool tutorialBarSudahTampil = false;
    private bool penelitiSudahHilang = false;

    private PlayerMovement pm;
    private bool pemainDikunci = false;

    // Cegah tombol E untuk DUDUK ikut terbaca sebagai E untuk laptop
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
        if (ikonE != null) ikonE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCooldown != null) teksCooldown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;

        // Kursi laboratorium baru bisa dipakai setelah dialog di pintu selesai
        if (kursiLab != null) kursiLab.enabled = false;

        // Kalau belum ada objek gambar MC duduk, buat otomatis dari "Sprite Duduk Lab"
        BuatGambarDuduk();

        // Peneliti baru dimunculkan saat Scene 2 siap
        if (penelitiObjek != null) penelitiObjek.SetActive(siap);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel3Scene2[] semua = FindObjectsByType<AdeganLevel3Scene2>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L3-S2] Ada " + semua.Length + " objek yang memasang script AdeganLevel3Scene2. Pakai satu saja, dan sambungkan event Scene 1 ke objek itu.");
    }

    // Lingkaran kuning di tab Scene (saat objek ini dipilih): area dialog di pintu
    void OnDrawGizmosSelected()
    {
        if (titikPintu == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(titikPintu.position, jarakPintu);
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel3Scene1
    public void SiapkanScene()
    {
        siap = true;
        if (penelitiObjek != null) penelitiObjek.SetActive(true);
        Debug.Log("[Adegan L3-S2] Scene 2 siap, Peneliti menunggu di pintu laboratorium.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Dialog di Pintu (langsung mulai)")]
    private void TesDialog()
    {
        if (sedangBerjalan || dialogSelesai) return;
        siap = true;
        if (penelitiObjek != null) penelitiObjek.SetActive(true);
        StartCoroutine(AlurDialog());
    }

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

        // Tahap 1: dialog dengan Peneliti saat MC mendekati pintu
        if (!dialogSelesai)
        {
            if (titikPintu == null) return;

            if (penelitiObjek != null && !penelitiObjek.activeSelf) penelitiObjek.SetActive(true);

            if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
            if (pm == null) return;

            if (Vector2.Distance(titikPintu.position, pm.transform.position) <= jarakPintu)
            {
                Debug.Log("[Adegan L3-S2] MC tiba di pintu laboratorium, dialog dimulai.");
                StartCoroutine(AlurDialog());
            }
            return;
        }

        // Tahap 2: MC duduk di kursi laboratorium, ikon E muncul di laptop
        if (sudahPernahSelesai && !bisaDiulang)
        {
            if (ikonE != null && ikonE.activeSelf) ikonE.SetActive(false);
            return;
        }

        if (kursiLab == null)
        {
            if (!peringatanKursiDitampilkan)
            {
                peringatanKursiDitampilkan = true;
                Debug.LogWarning("[Adegan L3-S2] Kursi Lab belum diisi di Inspector, laptop laboratorium tidak bisa dipakai.");
            }
            return;
        }

        bool duduk = kursiLab.sedangDuduk;
        if (duduk && !dudukSebelumnya)
        {
            waktuMulaiDuduk = Time.time;
            StartCoroutine(LaporGambarDuduk());
        }
        dudukSebelumnya = duduk;

        // Peneliti hilang saat MC duduk di kursi laboratorium
        if (duduk && penelitiHilangSaatDuduk && !penelitiSudahHilang)
        {
            penelitiSudahHilang = true;
            if (penelitiObjek != null) penelitiObjek.SetActive(false);
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
            StartCoroutine(AlurLaptop());
    }

    private void SembunyikanSemua(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    // ---------- Dialog di pintu ----------
    private IEnumerator AlurDialog()
    {
        sedangBerjalan = true;

        // MC berhenti di pintu selama dialog
        KunciPemain(true);

        yield return new WaitForSeconds(jedaSebelumDialog);

        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L3-S2] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog));

        // Tutorial 1: setelah dialog dengan Peneliti
        yield return StartCoroutine(TampilkanTutorial(boxTutorialSetelahDialog));

        // MC bebas berjalan ke kursi laboratorium, kursinya sekarang bisa dipakai
        KunciPemain(false);
        if (kursiLab != null) kursiLab.enabled = true;

        dialogSelesai = true;
        sedangBerjalan = false;

        Debug.Log("[Adegan L3-S2] Dialog selesai, MC bebas menuju kursi laboratorium.");
    }

    // ---------- Aktivitas laptop laboratorium ----------
    private IEnumerator AlurLaptop()
    {
        sedangBerjalan = true;

        if (ikonE != null) ikonE.SetActive(false);

        // MC tidak bisa berdiri selama aktivitas berjalan
        kursiLab.TerkunciEksternal = true;

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

        // 2. Poin
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoin);
            Debug.Log("[Adegan L3-S2] Penelitian dikembangkan! Skor bertambah +" + jumlahPoin);
        }
        else
        {
            Debug.LogWarning("[Adegan L3-S2] ScoreManager.Instance tidak ditemukan di Scene!");
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

        // 4. Tutorial 2: setelah bar hijau selesai (sekali saja kalau diatur begitu)
        if (!(tutorialBarSekaliSaja && tutorialBarSudahTampil))
        {
            tutorialBarSudahTampil = true;
            yield return StartCoroutine(TampilkanTutorial(boxTutorialSetelahBar));
        }

        // 5. Energi berkurang, lalu cek apakah habis
        if (biayaEnergi > 0f && energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        // 6. Cooldown baterai mulai (aktivitas berulang)
        sudahPernahSelesai = true;
        if (bisaDiulang) MulaiCooldown();

        kursiLab.TerkunciEksternal = false;
        sedangBerjalan = false;

        // Event untuk scene berikutnya hanya dipanggil sekali
        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log("[Adegan L3-S2] Scene 2 Level 3 selesai.");
    }

    // ---------- Cooldown baterai (sama seperti laptop kelas) ----------
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

        Debug.Log("[Adegan L3-S2] Laptop laboratorium siap dipakai lagi.");
    }

    // Box tutorial: game berhenti (opsional) dan layar digelapkan selama box tampil
    private IEnumerator TampilkanTutorial(GameObject[] daftar)
    {
        if (daftar == null || daftar.Length == 0) yield break;

        if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;

        // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
        if (overlayHitam != null)
        {
            overlayHitam.SetActive(true);
            overlayHitam.transform.SetAsLastSibling();   // overlay di depan UI lain, lalu box tutorial dipaksa di depan overlay
        }

        yield return StartCoroutine(TampilkanBoxBerurutan(daftar, true));

        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (hentikanWaktuSaatTutorial) Time.timeScale = 1f;
    }

    // Membuat objek gambar MC duduk secara otomatis (ukuran dan Sorting Layer disamakan dengan MC),
    // lalu memasangnya ke field Sprite Duduk di komponen kursi laboratorium
    private void BuatGambarDuduk()
    {
        if (kursiLab == null || kursiLab.spriteDuduk != null || spriteDudukLab == null) return;

        GameObject g = new GameObject("MCDudukLab (otomatis)");

        Transform acuan = (titikDudukLab != null) ? titikDudukLab : kursiLab.transform;
        g.transform.position = acuan.position;

        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = spriteDudukLab;
        sr.sortingOrder = urutanLayarDuduk;

        // Ukuran dan Sorting Layer disamakan dengan MC supaya gambar tidak terlalu besar / kecil dan tampil di layer yang sama
        PlayerMovement mc = FindFirstObjectByType<PlayerMovement>();
        if (mc != null)
        {
            Vector3 skala = mc.transform.lossyScale;
            g.transform.localScale = new Vector3(skala.x, skala.y, 1f);

            SpriteRenderer srMC = mc.GetComponent<SpriteRenderer>();
            if (srMC != null) sr.sortingLayerID = srMC.sortingLayerID;
        }

        g.SetActive(false);
        kursiLab.spriteDuduk = g;

        Debug.Log("[Adegan L3-S2] Gambar MC duduk dibuat otomatis di kursi laboratorium dari sprite '" + spriteDudukLab.name + "'.");
    }

    // Laporan di Console saat MC duduk: keadaan gambar MC duduk (untuk mencari tahu kenapa MC tidak terlihat)
    private IEnumerator LaporGambarDuduk()
    {
        // Tunggu satu frame supaya komponen kursi sudah selesai mengaktifkan gambar duduk
        yield return null;

        GameObject g = kursiLab.spriteDuduk;
        if (g == null)
        {
            Debug.LogWarning("[Adegan L3-S2] Field Sprite Duduk di komponen kursi laboratorium masih kosong, jadi MC tidak punya gambar duduk. Isi dengan objek gambar MC duduk di kursi laboratorium.");
            yield break;
        }

        SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
        float jarak = Vector2.Distance(g.transform.position, kursiLab.transform.position);

        string info = "[Adegan L3-S2] Gambar MC duduk '" + g.name + "': aktif=" + g.activeInHierarchy;
        info += ", sprite=" + ((sr != null && sr.sprite != null) ? sr.sprite.name : "TANPA SPRITE");
        info += ", Order in Layer=" + ((sr != null) ? sr.sortingOrder.ToString() : "-");
        info += ", Sorting Layer=" + ((sr != null) ? sr.sortingLayerName : "-");
        info += ", alpha=" + ((sr != null) ? sr.color.a.ToString("0.00") : "-");
        info += ", jarak ke kursi=" + jarak.ToString("0.0");
        Debug.Log(info);

        if (jarak > 3f)
            Debug.LogWarning("[Adegan L3-S2] Gambar MC duduk berada jauh dari kursi laboratorium. Mungkin yang terpasang adalah gambar duduk milik kursi lain (mis. perpustakaan). Duplikat objeknya, lalu letakkan di kursi laboratorium.");
    }

    // ---------- Pembantu ----------
    // Box tampil satu per satu: Space (atau klik kiri) menutup box dan lanjut ke berikutnya
    private IEnumerator TampilkanBoxBerurutan(GameObject[] daftar, bool keDepan = false)
    {
        foreach (GameObject box in daftar)
        {
            if (box == null) continue;

            box.SetActive(true);

            // Box tutorial dipaksa tampil paling depan di Canvas-nya, di atas overlay hitam
            if (keDepan) box.transform.SetAsLastSibling();

            if (!box.activeInHierarchy)
                Debug.LogWarning("[Adegan L3-S2] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

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