using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Level 2 - Scene 5 (Menyampaikan materi seminar), Lokasi: Conference Hall
//   1. Scene 4 selesai -> Scene 5 siap
//   2. MC berjalan mendekati podium. Setelah dekat, tekan Space: MC berdiri tepat di podium menghadap depan (seperti mengajar) dan dikunci di sana
//   3. Box dialog Pemain, Space untuk lanjut
//   4. Ikon E muncul di podium. Tekan E: bar hijau berputar (tanpa pilihan)
//   5. Poin bertambah, box tutorial, reward, energi berkurang
//   6. MC dibebaskan
//
// Script ini dipasang di objek apa saja (tidak perlu collider). Jarak dihitung dari "Titik Podium".
// Cara menyambung dari Scene 4: di AdeganLevel2Scene4, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Bagian yang tidak diisi di Inspector (mis. box tutorial) dilewati.
public class AdeganLevel2Scene5 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 4 dulu

    [Header("Podium (MC mendekat, lalu tekan tombol)")]
    public Transform titikPodium;               // Objek kosong di tempat MC berdiri saat menyampaikan materi (di belakang podium)
    public float jarakPodium = 1.2f;            // Jarak MC ke titik podium supaya Space bisa ditekan (lingkaran kuning terlihat di Scene saat objek ini dipilih)
    public KeyCode tombolMulai = KeyCode.Space; // Tombol untuk berdiri di podium

    [Header("Pose Menghadap Depan (seperti mengajar)")]
    public bool terapkanArahHadap = true;       // true = MC menghadap arah di bawah saat berdiri di podium
    public Vector2 arahHadap = new Vector2(0f, -1f);   // (0,-1) = menghadap bawah / depan (ke arah penonton)
    public Sprite spriteMenghadapDepan;         // Opsional tapi paling pasti: sprite MC menghadap depan (mis. gambar "bawah" MC level 2). Kalau diisi, dipakai langsung menggantikan animasi selama adegan
    public bool ubahUrutanLayar = false;        // true = Order in Layer MC diganti selama di podium (supaya MC tampil di belakang podium)
    public int urutanLayarDiPodium = 3;         // Isi satu angka di bawah Order in Layer gambar podium. Dikembalikan seperti semula setelah adegan

    [Header("Box Dialog (dibuat di Hierarchy, urut dari atas)")]
    public GameObject[] boxDialog;              // Satu box: Pemain
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.5f;      // Jeda (detik) setelah MC berdiri di podium sebelum dialog muncul

    [Header("Ikon E di Podium")]
    public GameObject ikonE;                    // Ikon "E" di podium
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Bar Hijau Berputar")]
    public GameObject canvasProgressBar;        // Objek induk bar
    public Image circularProgressBar;           // Image bar (Image Type: Filled)
    public float durasiBar = 10f;               // Lama bar berputar (detik)

    [Header("Hasil Setelah Bar Selesai")]
    public int jumlahPoin = 15;                 // Poin yang didapat
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;            // Box tutorial (dibuat di Hierarchy), urut dari atas
    public bool hentikanWaktuSaatTutorial = true;
    public int indexReward = 6;                 // Nomor reward di RewardManager (harus sudah ada di daftar). -1 = tanpa reward
    public float biayaEnergi = 15f;             // Energi yang berkurang setelah selesai
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Opsional: untuk menyambung ke scene berikutnya

    private bool siap = false;
    private bool sudahJalan = false;
    private bool peringatanPodiumDitampilkan = false;

    private PlayerMovement pm;
    private bool pemainDikunci = false;
    private bool animatorDimatikan = false;
    private int urutanLayarAsli = 0;
    private bool urutanLayarDiubah = false;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorial);
        if (ikonE != null) ikonE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel2Scene5[] semua = FindObjectsByType<AdeganLevel2Scene5>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L2-S5] Ada " + semua.Length + " objek yang memasang script AdeganLevel2Scene5. Pakai satu saja, dan sambungkan event Scene 4 ke objek itu.");
    }

    // Lingkaran kuning di tab Scene (saat objek ini dipilih): area MC bisa menekan Space di podium
    void OnDrawGizmosSelected()
    {
        if (titikPodium == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(titikPodium.position, jarakPodium);
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel2Scene4
    public void SiapkanScene()
    {
        siap = true;
        Debug.Log("[Adegan L2-S5] Scene 5 siap, menunggu MC mendekati podium.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (langsung mulai di podium)")]
    private void TesAdegan()
    {
        if (sudahJalan) return;
        siap = true;
        sudahJalan = true;
        StartCoroutine(Alur());
    }

    // Menunggu MC mendekati podium, lalu menekan tombol mulai
    void Update()
    {
        if (sudahJalan) return;

        // "Langsung Siap" dibaca di sini (bukan hanya di Start) supaya juga berlaku kalau dicentang saat Play
        if (!siap && !langsungSiap) return;

        if (titikPodium == null)
        {
            if (!peringatanPodiumDitampilkan)
            {
                peringatanPodiumDitampilkan = true;
                Debug.LogWarning("[Adegan L2-S5] Titik Podium belum diisi di Inspector, adegan tidak bisa dimulai.");
            }
            return;
        }

        if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
        if (pm == null) return;

        bool dekat = Vector2.Distance(titikPodium.position, pm.transform.position) <= jarakPodium;

        if (dekat && Input.GetKeyDown(tombolMulai))
        {
            sudahJalan = true;
            Debug.Log("[Adegan L2-S5] MC berdiri di podium, adegan dimulai.");
            StartCoroutine(Alur());
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

    private IEnumerator Alur()
    {
        // 1. MC berdiri tepat di podium, menghadap depan, dan dikunci di sana selama adegan
        KunciPemain(true);
        if (titikPodium != null) PindahkanPemain(titikPodium.position);
        TerapkanPoseDepan();

        yield return new WaitForSeconds(jedaSebelumDialog);

        // 2. Box dialog Pemain
        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L2-S5] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
        {
            Debug.Log("[Adegan L2-S5] Dialog dipanggil.");
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog));
        }

        // 3. Ikon E di podium, tunggu E ditekan
        if (ikonE != null)
        {
            ikonE.SetActive(true);

            if (!ikonE.activeInHierarchy)
                Debug.LogWarning("[Adegan L2-S5] Ikon E '" + ikonE.name + "' sudah diaktifkan tapi induknya nonaktif, jadi tidak tampil.");

            yield return new WaitForSeconds(0.3f);
            while (!Input.GetKeyDown(tombolInteraksi)) yield return null;

            ikonE.SetActive(false);
        }

        // 4. Bar hijau berputar
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

        // 5. Poin
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoin);
            Debug.Log("[Adegan L2-S5] Seminar selesai! Skor bertambah +" + jumlahPoin);
        }
        else
        {
            Debug.LogWarning("[Adegan L2-S5] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 6. Box tutorial
        if (boxTutorial != null && boxTutorial.Length > 0)
        {
            if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;

            // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
            if (overlayHitam != null) overlayHitam.SetActive(true);

            yield return StartCoroutine(TampilkanBoxBerurutan(boxTutorial));

            if (overlayHitam != null) overlayHitam.SetActive(false);
            if (hentikanWaktuSaatTutorial) Time.timeScale = 1f;
        }

        // 7. Reward (muncul sendiri, lalu hilang sendiri)
        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));
        }

        // 8. Energi berkurang, lalu cek apakah habis
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

            if (!box.activeInHierarchy)
                Debug.LogWarning("[Adegan L2-S5] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    // Memindahkan MC ke titik tertentu (z tetap)
    private void PindahkanPemain(Vector3 tujuan)
    {
        if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
        if (pm == null) return;

        pm.transform.position = new Vector3(tujuan.x, tujuan.y, pm.transform.position.z);

        Rigidbody2D rb = pm.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // MC menghadap depan: lewat sprite langsung (kalau diisi), atau lewat parameter Animator
    private void TerapkanPoseDepan()
    {
        if (pm == null) return;

        if (spriteMenghadapDepan == null)
            Debug.LogWarning("[Adegan L2-S5] Sprite Menghadap Depan belum diisi. Arah hadap hanya lewat Animator, jadi MC bisa tetap menghadap arah terakhir (terlihat membelakangi penonton). Isi dengan gambar MC menghadap bawah.");

        // Urutan gambar MC (supaya MC bisa tampil di belakang podium)
        if (ubahUrutanLayar)
        {
            SpriteRenderer srUrutan = pm.GetComponent<SpriteRenderer>();
            if (srUrutan != null)
            {
                urutanLayarAsli = srUrutan.sortingOrder;
                srUrutan.sortingOrder = urutanLayarDiPodium;
                urutanLayarDiubah = true;
            }
        }

        if (spriteMenghadapDepan != null)
        {
            SpriteRenderer sr = pm.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (pm.animator != null && pm.animator.enabled)
                {
                    pm.animator.enabled = false;   // supaya sprite tidak ditimpa animasi
                    animatorDimatikan = true;
                }

                sr.sprite = spriteMenghadapDepan;
                sr.flipX = false;
            }
        }
    }

    // Menahan atau membebaskan pergerakan MC
    private void KunciPemain(bool kunci)
    {
        if (kunci)
        {
            pm = FindFirstObjectByType<PlayerMovement>();
            if (pm == null) return;

            Rigidbody2D rb = pm.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            if (pm.animator != null && pm.animator.enabled)
            {
                pm.animator.SetBool("IsMoving", false);
                pm.animator.SetFloat("MoveX", terapkanArahHadap ? arahHadap.x : 0f);
                pm.animator.SetFloat("MoveY", terapkanArahHadap ? arahHadap.y : 0f);
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

    private void Selesai()
    {
        // Urutan gambar MC dikembalikan seperti semula
        if (urutanLayarDiubah && pm != null)
        {
            SpriteRenderer srUrutan = pm.GetComponent<SpriteRenderer>();
            if (srUrutan != null) srUrutan.sortingOrder = urutanLayarAsli;
        }
        urutanLayarDiubah = false;

        // Animasi MC dinyalakan lagi (kalau tadi dimatikan untuk pose menghadap depan), lalu MC dibebaskan
        if (animatorDimatikan && pm != null && pm.animator != null)
            pm.animator.enabled = true;
        animatorDimatikan = false;

        KunciPemain(false);

        saatAdeganSelesai.Invoke();

        Debug.Log("[Adegan L2-S5] Scene 5 Menyampaikan materi seminar selesai.");
    }
}