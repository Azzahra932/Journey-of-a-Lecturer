using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Level 2 - Scene 5 (Menyampaikan materi seminar), Lokasi: Conference Hall
//   1. Scene 4 selesai -> Scene 5 siap
//   2. MC berjalan mendekati podium. Setelah dekat, tekan Space: MC berdiri tepat di podium menghadap depan (seperti mengajar) dan dikunci di sana
//   3. Box dialog Pemain, Space untuk lanjut (hanya pertama kali)
//   4. Ikon E muncul di podium. Tekan E: bar hijau berputar (tanpa pilihan)
//   5. Poin bertambah, box tutorial (hanya pertama kali), reward, energi berkurang
//   6. Cooldown bar bulat mulai terisi. MC tetap di titik podium sampai pemain menekan arah kiri / kanan, baru MC bebas berjalan keluar
//   7. Setelah bar cooldown penuh, aktivitas podium bisa dikerjakan lagi (berulang): Space di podium -> E -> bar -> poin -> reward -> energi -> cooldown
//
// Script ini dipasang di objek apa saja (tidak perlu collider). Jarak dihitung dari "Titik Podium".
// Cara menyambung dari Scene 4: di AdeganLevel2Scene4, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Bagian yang tidak diisi di Inspector (mis. box tutorial atau bar cooldown) dilewati.
public class AdeganLevel2Scene5 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 4 dulu

    [Header("Podium (MC mendekat, lalu tekan tombol)")]
    public Transform titikPodium;               // Objek kosong di tempat MC berdiri saat menyampaikan materi (di belakang podium)
    public float jarakPodium = 1.2f;            // Jarak MC ke titik podium supaya Space bisa ditekan (lingkaran kuning terlihat di Scene saat objek ini dipilih)
    public KeyCode tombolMulai = KeyCode.Space; // Tombol untuk berdiri di podium

    [Header("Keluar dari Podium")]
    public bool tungguKiriKananSetelahSelesai = true;   // true = setelah semua selesai, MC tetap di titik podium sampai pemain menekan arah kiri / kanan
    public bool hanyaKiriKanan = true;                  // true = hanya A / D atau panah kiri / kanan yang melepas MC. false = semua arah (WASD atau panah)

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
    public bool dialogHanyaPertamaKali = true;  // true = dialog hanya muncul saat Scene 5 pertama kali. Pengulangan langsung ke ikon E

    [Header("Ikon E di Podium")]
    public GameObject ikonE;                    // Ikon "E" di podium
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Bar Hijau Berputar")]
    public GameObject canvasProgressBar;        // Objek induk bar
    public Image circularProgressBar;           // Image bar (Image Type: Filled)
    public float durasiBar = 10f;               // Lama bar berputar (detik)

    [Header("Hasil Setelah Bar Selesai")]
    public int jumlahPoin = 15;                 // Poin yang didapat tiap selesai
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;            // Box tutorial (dibuat di Hierarchy), urut dari atas
    public bool tutorialHanyaPertamaKali = true;   // true = box tutorial hanya muncul saat Scene 5 pertama kali
    public bool hentikanWaktuSaatTutorial = true;
    public int indexReward = 6;                 // Nomor reward di RewardManager (harus sudah ada di daftar). -1 = tanpa reward
    public float biayaEnergi = 15f;             // Energi yang berkurang tiap selesai
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Aktivitas Berulang")]
    public bool bisaDiulang = true;             // true = setelah bar cooldown penuh, podium bisa dipakai lagi

    [Header("Cooldown (Bar Bulat)")]
    public float durasiCooldown = 45f;          // Jeda (detik) sebelum podium bisa dipakai lagi
    public GameObject barCooldownBulat;         // Objek induk bar bulat cooldown (duplikat dari bar cooldown buku)
    public Image isiBarCooldown;                // Image isi bar (Image Type: Filled), terisi dari kosong sampai penuh
    public GameObject teksIstirahat;            // Opsional: teks "Podium sedang istirahat" saat Space ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;

    [Header("Efek Podium Saat Cooldown (seperti layar laptop redup)")]
    public SpriteRenderer[] spritePodiumRedup;  // Gambar podium yang diredupkan selama cooldown
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah Scene 5 pertama kali selesai (untuk menyambung ke scene berikutnya)

    private bool siap = false;
    private bool sedangBerjalan = false;
    private bool sudahPernahSelesai = false;
    private bool eventSudahDipanggil = false;
    private bool peringatanPodiumDitampilkan = false;

    private PlayerMovement pm;
    private bool pemainDikunci = false;
    private bool animatorDimatikan = false;
    private int urutanLayarAsli = 0;
    private bool urutanLayarDiubah = false;
    private readonly List<Behaviour> interaksiDimatikan = new List<Behaviour>();

    // Penahan posisi: selama adegan, MC selalu dikembalikan ke titik podium kalau ada yang menggesernya
    private bool tahanPosisi = false;
    private Vector3 posisiTahan;
    private bool sudahDilaporkanPindah = false;
    private bool transisiTerjadi = false;
    private TransisiKembaliKeAwal transisi;

    // Cooldown
    private bool cooldownAktif = false;
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorial);
        if (ikonE != null) ikonE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (barCooldownBulat != null) barCooldownBulat.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);
        AturWarnaPodium(warnaNormal);

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();
        transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();

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
        if (sedangBerjalan) return;
        siap = true;
        MulaiAdegan();
    }

    private void MulaiAdegan()
    {
        sedangBerjalan = true;
        StartCoroutine(Alur());
    }

    // Menunggu MC mendekati podium, lalu menekan tombol mulai
    void Update()
    {
        // Teks "istirahat" hilang otomatis
        if (teksIstirahat != null && teksIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksIstirahat.SetActive(false);

        // Bar cooldown terisi bertahap (tetap jalan walau MC sedang di podium)
        if (cooldownAktif) TickCooldown();

        if (sedangBerjalan) return;
        if (sudahPernahSelesai && !bisaDiulang) return;

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
            // Selama cooldown, Space tidak memulai aktivitas: tampilkan teks istirahat
            if (cooldownAktif)
            {
                if (teksIstirahat != null)
                {
                    teksIstirahat.SetActive(true);
                    teksSembunyiPada = Time.time + lamaTeksIstirahat;
                }
                return;
            }

            Debug.Log("[Adegan L2-S5] MC berdiri di podium, adegan dimulai.");
            MulaiAdegan();
        }
    }

    // LateUpdate jalan setelah semua Update: kalau script lain menggeser MC dari podium (mis. karena Space),
    // MC langsung dikembalikan ke titik podium sebelum sempat terlihat berpindah.
    void LateUpdate()
    {
        if (!tahanPosisi || pm == null) return;

        // Energi habis: MC dipindah oleh transisi, jadi MC tidak ditahan lagi
        if (SedangTransisi())
        {
            tahanPosisi = false;
            transisiTerjadi = true;
            return;
        }

        if (Vector2.Distance(pm.transform.position, posisiTahan) > 0.01f)
        {
            if (!sudahDilaporkanPindah)
            {
                sudahDilaporkanPindah = true;
                Debug.LogWarning("[Adegan L2-S5] Ada script yang menggeser MC dari podium (posisi " + pm.transform.position
                    + "). MC dikembalikan ke titik podium. Lihat baris Console sebelum ini untuk tahu script mana pelakunya.");
            }

            pm.transform.position = posisiTahan;

            Rigidbody2D rb = pm.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    private bool SedangTransisi()
    {
        return transisi != null && transisi.SedangTransisi;
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
        bool pertamaKali = !sudahPernahSelesai;

        // 1. MC berdiri tepat di podium, menghadap depan, dan dikunci di sana selama adegan
        KunciPemain(true);
        if (titikPodium != null) PindahkanPemain(titikPodium.position);
        TerapkanPoseDepan();

        // Selama adegan, interaksi kursi dan laptop lain dimatikan sementara supaya Space tidak ikut memicunya
        MatikanInteraksiLain();

        yield return new WaitForSeconds(jedaSebelumDialog);

        // 2. Box dialog Pemain (hanya pertama kali kalau "Dialog Hanya Pertama Kali" dicentang)
        if (!dialogHanyaPertamaKali || pertamaKali)
        {
            if (boxDialog == null || boxDialog.Length == 0)
                Debug.LogWarning("[Adegan L2-S5] Box Dialog masih kosong di Inspector, dialog dilewati.");
            else
            {
                Debug.Log("[Adegan L2-S5] Dialog dipanggil.");
                yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog));
            }
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

        // 6. Box tutorial (hanya pertama kali kalau "Tutorial Hanya Pertama Kali" dicentang)
        if ((!tutorialHanyaPertamaKali || pertamaKali) && boxTutorial != null && boxTutorial.Length > 0)
        {
            if (hentikanWaktuSaatTutorial) Time.timeScale = 0f;

            // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
            if (overlayHitam != null)
            {
                overlayHitam.SetActive(true);
                overlayHitam.transform.SetAsLastSibling();   // overlay di depan UI lain, lalu box tutorial dipaksa di depan overlay
            }

            yield return StartCoroutine(TampilkanBoxBerurutan(boxTutorial, true));

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

        // 9. Cooldown bar bulat mulai terisi (aktivitas berulang)
        sudahPernahSelesai = true;
        if (bisaDiulang) MulaiCooldown();

        // 10. MC tetap di titik podium sampai pemain menekan arah kiri / kanan
        if (tungguKiriKananSetelahSelesai)
            yield return StartCoroutine(TungguKeluarPodium());

        Selesai();
    }

    // MC tetap di titik podium. Baru setelah pemain menekan arah kiri / kanan, MC dibebaskan dan bisa berjalan keluar.
    private IEnumerator TungguKeluarPodium()
    {
        Debug.Log("[Adegan L2-S5] MC tetap di podium, menunggu pemain menekan arah kiri / kanan.");

        // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung melepas MC,
        // dan supaya transisi energi habis (kalau terjadi) sempat terdeteksi
        yield return new WaitForSecondsRealtime(0.3f);

        while (true)
        {
            // Energi habis: MC sudah dipindah oleh transisi, tidak perlu menunggu lagi
            if (transisiTerjadi || SedangTransisi()) yield break;

            if (TombolKeluarDitekan()) break;

            yield return null;
        }
    }

    private bool TombolKeluarDitekan()
    {
        bool kiriKanan = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ||
                         Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

        if (hanyaKiriKanan) return kiriKanan;

        return kiriKanan ||
               Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) ||
               Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);
    }

    // ---------- Cooldown bar bulat (sama seperti bar cooldown buku) ----------
    private void MulaiCooldown()
    {
        cooldownSelesaiPada = Time.time + durasiCooldown;
        cooldownAktif = true;

        if (isiBarCooldown != null) isiBarCooldown.fillAmount = 0f;
        if (barCooldownBulat != null) barCooldownBulat.SetActive(true);

        AturWarnaPodium(warnaCooldown);
    }

    private void TickCooldown()
    {
        float sisa = Mathf.Max(0f, cooldownSelesaiPada - Time.time);

        // Bar terisi dari kosong (0) sampai penuh (1)
        if (isiBarCooldown != null && durasiCooldown > 0f)
            isiBarCooldown.fillAmount = Mathf.Clamp01(1f - (sisa / durasiCooldown));

        if (Time.time >= cooldownSelesaiPada) SelesaiCooldown();
    }

    private void SelesaiCooldown()
    {
        cooldownAktif = false;

        if (barCooldownBulat != null) barCooldownBulat.SetActive(false);
        AturWarnaPodium(warnaNormal);

        Debug.Log("[Adegan L2-S5] Podium siap dipakai lagi.");
    }

    private void AturWarnaPodium(Color warna)
    {
        if (spritePodiumRedup == null) return;

        foreach (SpriteRenderer s in spritePodiumRedup)
        {
            if (s != null) s.color = warna;
        }
    }

    // Box tampil satu per satu: Space (atau klik kiri) menutup box dan lanjut ke berikutnya
    private IEnumerator TampilkanBoxBerurutan(GameObject[] daftar, bool keDepan = false)
    {
        foreach (GameObject box in daftar)
        {
            if (box == null) continue;

            box.SetActive(true);

            // Box tutorial dipaksa tampil paling depan di Canvas-nya, di atas overlay hitam
            if (keDepan)
            {
                box.transform.SetAsLastSibling();
                LaporBoxTutorial(box);

                if (overlayHitam != null && box.transform.parent != overlayHitam.transform.parent)
                    Debug.LogWarning("[Adegan L2-S5] Box tutorial '" + box.name + "' dan Overlay Hitam tidak berada di bawah induk yang sama, jadi urutan tampilnya mengikuti induk masing-masing. Pindahkan keduanya ke Canvas yang sama.");
            }

            if (!box.activeInHierarchy)
                Debug.LogWarning("[Adegan L2-S5] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    // Mematikan sementara script kursi dan laptop yang ikut membaca tombol Space (InteractiveDesk, kursi perpus, laptop perpus)
    private void MatikanInteraksiLain()
    {
        foreach (InteractiveDesk d in FindObjectsByType<InteractiveDesk>(FindObjectsSortMode.None))
            TambahDimatikan(d);

        foreach (KursiPerpusInteraction k in FindObjectsByType<KursiPerpusInteraction>(FindObjectsSortMode.None))
            TambahDimatikan(k);

        foreach (LaptopPerpusInteraction l in FindObjectsByType<LaptopPerpusInteraction>(FindObjectsSortMode.None))
            TambahDimatikan(l);
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

    // Laporan di Console: keadaan box tutorial saat tampil (untuk mencari tahu kenapa box tidak terlihat)
    private void LaporBoxTutorial(GameObject box)
    {
        RectTransform rt = box.GetComponent<RectTransform>();
        Canvas canvasBox = box.GetComponentInParent<Canvas>();
        Canvas canvasOverlay = (overlayHitam != null) ? overlayHitam.GetComponentInParent<Canvas>() : null;
        Image gambar = box.GetComponentInChildren<Image>(true);
        CanvasGroup grup = box.GetComponentInParent<CanvasGroup>();

        string info = "[Adegan L2-S5] Info box tutorial '" + box.name + "': ";
        info += "induk=" + ((box.transform.parent != null) ? box.transform.parent.name : "-");
        info += ", ukuran=" + ((rt != null) ? rt.rect.size.ToString() : "-");
        info += ", Canvas=" + ((canvasBox != null) ? canvasBox.name + " (sort " + canvasBox.sortingOrder + ", " + canvasBox.renderMode + ")" : "-");
        info += ", Canvas overlay=" + ((canvasOverlay != null) ? canvasOverlay.name + " (sort " + canvasOverlay.sortingOrder + ")" : "-");
        info += ", Image=" + ((gambar != null) ? ((gambar.sprite != null) ? gambar.sprite.name : "TANPA SPRITE") + " alpha " + gambar.color.a.ToString("0.00") : "tidak ada");
        info += ", CanvasGroup alpha=" + ((grup != null) ? grup.alpha.ToString("0.00") : "-");

        Debug.Log(info);
    }

    // Memindahkan MC ke titik tertentu (z tetap) dan mulai menahannya di sana
    private void PindahkanPemain(Vector3 tujuan)
    {
        if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
        if (pm == null) return;

        posisiTahan = new Vector3(tujuan.x, tujuan.y, pm.transform.position.z);
        pm.transform.position = posisiTahan;

        Rigidbody2D rb = pm.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        sudahDilaporkanPindah = false;
        transisiTerjadi = false;
        tahanPosisi = true;
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
        // MC tidak ditahan lagi, interaksi kursi dan laptop dinyalakan lagi
        tahanPosisi = false;
        NyalakanInteraksiLain();

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

        sedangBerjalan = false;

        // Event untuk scene berikutnya hanya dipanggil sekali
        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log("[Adegan L2-S5] Scene 5 Menyampaikan materi seminar selesai.");
    }
}