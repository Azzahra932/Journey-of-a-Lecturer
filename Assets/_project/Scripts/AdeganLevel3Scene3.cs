using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Level 3 - Scene 3 (Membimbing Mahasiswa), Lokasi: Research Center
//   1. Scene 2 Level 3 selesai -> Scene 3 siap
//   2. MC duduk di kursinya di Research Center: mahasiswa tingkat akhir muncul di pintu, berjalan, lalu duduk di kursi di depan MC
//      (kalau Kursi MC tidak diisi, adegan mulai saat MC mendekati Titik Research, MC berdiri)
//   3. Setelah mahasiswa duduk, dua box dialog (Mahasiswa lalu Pemain), Space untuk lanjut
//   4. Begitu dialog selesai, bar hijau langsung muncul dan berputar
//   5. Poin bertambah, reward muncul (opsional)
//   6. Mahasiswa berjalan keluar, lalu box tutorial (di atas overlay hitam)
//   7. Energi berkurang, cooldown lingkaran mulai (lingkaran terisi bertahap, kursi MC meredup), MC dibebaskan
//   8. Opsional (Bisa Diulang): setelah cooldown penuh dan MC duduk lagi di kursi, mahasiswa datang lagi
//
// Script ini dipasang di objek apa saja (tidak perlu collider). Tidak bergantung pada NPCBimbingan / DialogBimbingan Level 1,
// tapi boleh memakai objek mahasiswa dan jalur yang sama dengan Level 1 Scene 3.
// Cara menyambung dari Scene 2: di AdeganLevel3Scene2, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Bagian yang tidak diisi di Inspector (mis. reward) dilewati.
public class AdeganLevel3Scene3 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 2 dulu

    [Header("Kursi MC di Research Center (adegan mulai saat MC duduk di sini)")]
    public InteractiveDesk kursiMC;             // Kursi tempat MC duduk (komponen InteractiveDesk). Kosong = pakai Titik Research di bawah
    public bool lepasBimbinganLevel1 = true;    // true = duduk di kursi ini tidak lagi memicu bimbingan Level 1
    public Sprite spriteDudukMCLevel3;          // Sprite MC level 3 saat duduk di kursi ini. Dipasang ke Sit Sprite kursi, supaya tidak memakai sprite bimbingan Level 1
    public float jedaSebelumMahasiswa = 0.8f;   // Jeda (detik) setelah MC duduk sebelum mahasiswa muncul

    [Header("Cadangan: Area Research Center (dipakai hanya kalau Kursi MC kosong)")]
    public Transform titikResearch;             // Objek kosong di dalam Research Center
    public float jarakMulai = 3f;               // Jarak MC ke titik supaya adegan mulai (lingkaran kuning terlihat di Scene saat objek ini dipilih)

    [Header("NPC Mahasiswa")]
    public GameObject mahasiswa;                // Objek mahasiswa (boleh sama dengan Level 1 Scene 3). Dinonaktifkan di awal
    public Transform[] jalur;                   // Titik jalan berurutan dari pintu sampai dekat MC
    public Transform titikDuduk;                // Dipakai kalau "Berhenti Di Dekat Player" tidak dicentang
    public bool berhentiDiDekatPlayer = true;   // true = mahasiswa berhenti di dekat MC. false = berjalan ke Titik Duduk
    public Vector2 offsetDariPlayer = new Vector2(-1f, 0f);   // Jarak berhenti dari MC (negatif X = di kiri MC)
    public float kecepatan = 2.5f;
    public Animator animator;                   // Opsional
    public string parameterJalan = "IsMoving";

    [Header("Tampilan Saat Duduk (Opsional, hanya kalau berjalan ke Titik Duduk)")]
    public Sprite spriteDuduk;                  // Kosong = sprite tidak diganti
    public int sortingOrderDuduk = 2;
    public float skalaSaatDuduk = 1f;
    public bool samakanUkuranDenganSpriteJalan = true;   // true = setelah mahasiswa tiba, gambar apa pun yang tampil (dari Sprite Duduk maupun animasi diam/duduk di Animator) disamakan tingginya dengan gambar jalan, jadi ukurannya tidak berubah

    [Header("Box Dialog (dibuat di Hierarchy, urut: Mahasiswa lalu Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.8f;      // Jeda (detik) setelah mahasiswa DUDUK sebelum dialog muncul

    [Header("Bar Hijau Berputar (muncul langsung setelah dialog selesai)")]
    public GameObject canvasProgressBar;        // Objek induk bar (boleh objek yang sama dengan scene lain)
    public Image circularProgressBar;           // Image bar (Image Type: Filled)
    public float durasiBar = 5f;                // Lama bar berputar (detik)

    [Header("Hasil Setelah Bar Selesai")]
    public int jumlahPoin = 15;                 // Poin yang didapat
    public int indexReward = -1;                // Nomor reward di RewardManager. -1 = pakai Box Reward di bawah (kalau diisi)
    public GameObject boxReward;                // Opsional: box reward buatan sendiri, dipakai kalau Index Reward = -1
    public float lamaBoxReward = 2f;
    public float biayaEnergi = 15f;             // Energi yang berkurang setelah selesai (0 = tidak mengurangi energi)
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Box Tutorial (muncul setelah mahasiswa pergi)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;            // Box tutorial, urut dari atas
    public bool hentikanWaktuSaatTutorial = true;
    public bool tutorialSekaliSaja = true;      // true = tutorial hanya muncul pada bimbingan pertama (berlaku kalau Bisa Diulang dicentang)

    [Header("Cooldown Lingkaran (mulai setelah adegan selesai)")]
    public float durasiCooldown = 60f;          // Lama cooldown (detik)
    public GameObject objekCooldown;            // Objek induk lingkaran cooldown, muncul saat cooldown (duplikat dari lingkaran cooldown scene lain)
    public Image gambarCooldown;                // Image lingkaran (Image Type: Filled, Radial 360). Terisi dari kosong sampai penuh selama cooldown
    public bool redupkanKursi = true;           // true = gambar kursi MC diredupkan selama cooldown
    public SpriteRenderer spriteKursiMC;        // Gambar yang diredupkan. Kosong = SpriteRenderer di objek Kursi MC
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Warna redup

    [Header("Bimbingan Berulang (opsional, butuh Kursi MC)")]
    public bool bisaDiulang = false;            // true = setelah cooldown penuh dan MC duduk lagi di kursi, mahasiswa datang lagi
    public bool dialogSekaliSaja = true;        // true = dialog hanya muncul pada bimbingan pertama, pengulangan langsung ke bar

    [Header("Pengaturan")]
    public bool kunciGerakPlayer = true;        // MC berhenti selama adegan
    public float batasWaktuAdegan = 120f;       // Pengaman: kalau mahasiswa macet, adegan dilanjutkan setelah sekian detik

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Opsional: untuk menyambung ke scene Level 3 berikutnya

    private bool siap = false;
    private bool sudahJalan = false;
    private PlayerMovement pm;
    private bool pemainDikunci = false;

    // Penahan ukuran mahasiswa: selama adegan, ukurannya dikembalikan kalau ada yang mengubahnya (mis. Animator saat duduk)
    private Transform trMahasiswa;
    private Vector3 skalaTetap = Vector3.one;
    private bool kunciUkuran = false;
    private bool ukuranSudahDilaporkan = false;
    private Vector3 skalaJalanTersimpan = Vector3.one;
    private SpriteRenderer srMahasiswa;
    private bool ukuranOtomatis = false;        // true setelah mahasiswa tiba: ukuran mengikuti tinggi gambar yang tampil
    private float tinggiJalan = 0f;             // tinggi gambar saat berjalan (dalam satuan dunia)
    private Sprite spriteTerakhirDilaporkan;

    // Cooldown dan pengulangan
    private bool cooldownAktif = false;
    private float cooldownSelesaiPada = 0f;
    private Color warnaAsliKursi = Color.white;
    private bool dialogSudahTampil = false;
    private bool tutorialSudahTampil = false;
    private bool eventSudahDipanggil = false;
    private bool perluBerdiriDulu = false;      // true setelah adegan selesai: MC harus berdiri dulu sebelum bimbingan berikutnya bisa mulai

    void Start()
    {
        siap = langsungSiap;

        if (mahasiswa != null) mahasiswa.SetActive(false);
        SembunyikanSemua(boxDialog);
        if (boxReward != null) boxReward.SetActive(false);
        SembunyikanSemua(boxTutorial);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (objekCooldown != null) objekCooldown.SetActive(false);

        // Gambar kursi MC yang diredupkan saat cooldown (warna aslinya dicatat, dikembalikan setelah cooldown)
        if (spriteKursiMC == null && kursiMC != null) spriteKursiMC = kursiMC.GetComponent<SpriteRenderer>();
        if (spriteKursiMC != null) warnaAsliKursi = spriteKursiMC.color;

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel3Scene3[] semua = FindObjectsByType<AdeganLevel3Scene3>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L3-S3] Ada " + semua.Length + " objek yang memasang script AdeganLevel3Scene3. Pakai satu saja.");
    }

    // Lingkaran kuning di tab Scene (saat objek ini dipilih): area adegan mulai
    void OnDrawGizmosSelected()
    {
        if (titikResearch == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(titikResearch.position, jarakMulai);
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel3Scene2
    public void SiapkanScene()
    {
        siap = true;
        TerapkanSpriteMC();
        Debug.Log("[Adegan L3-S3] Scene 3 siap, menunggu MC duduk di kursi Research Center.");
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

    void Update()
    {
        // Lingkaran cooldown terisi bertahap
        if (cooldownAktif) TickCooldown();

        if (sudahJalan) return;

        // "Langsung Siap" dibaca di sini (bukan hanya di Start) supaya juga berlaku kalau dicentang saat Play
        if (!siap && !langsungSiap) return;

        // Cara utama: adegan mulai saat MC duduk di kursinya
        if (kursiMC != null)
        {
            LepasBimbingan();

            if (!kursiMC.SedangDuduk)
            {
                TerapkanSpriteMC();
                perluBerdiriDulu = false;   // MC sudah berdiri, boleh duduk lagi untuk bimbingan berikutnya
            }

            if (kursiMC.SedangDuduk && !perluBerdiriDulu && !cooldownAktif)
            {
                sudahJalan = true;
                Debug.Log("[Adegan L3-S3] MC duduk di kursi Research Center, adegan dimulai.");
                StartCoroutine(Alur());
            }
            return;
        }

        // Cadangan: adegan mulai saat MC mendekati Titik Research
        if (titikResearch == null) return;

        if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
        if (pm == null) return;

        if (Vector2.Distance(titikResearch.position, pm.transform.position) <= jarakMulai)
        {
            sudahJalan = true;
            Debug.Log("[Adegan L3-S3] MC masuk Research Center, adegan dimulai.");
            StartCoroutine(Alur());
        }
    }

    // Pasang sprite duduk MC level 3 ke kursi (harus sebelum MC duduk, karena sprite dipakai saat tombol duduk ditekan)
    private void TerapkanSpriteMC()
    {
        if (kursiMC == null || spriteDudukMCLevel3 == null) return;
        if (kursiMC.sitSprite != spriteDudukMCLevel3) kursiMC.sitSprite = spriteDudukMCLevel3;
    }

    // LateUpdate jalan setelah Animator: kalau ukuran mahasiswa berubah (mis. saat duduk), langsung dikembalikan
    void LateUpdate()
    {
        if (!kunciUkuran || trMahasiswa == null) return;

        Vector3 target = skalaTetap;

        // Setelah tiba: ukuran dihitung dari tinggi gambar yang sedang tampil, supaya gambar duduk / diam dari Animator
        // yang lebih besar (Pixels Per Unit berbeda) tetap tampil sebesar gambar jalan
        if (ukuranOtomatis && srMahasiswa != null && srMahasiswa.sprite != null)
        {
            float t = srMahasiswa.sprite.bounds.size.y;
            if (t > 0.001f && tinggiJalan > 0.001f)
            {
                float faktor = tinggiJalan / t;
                if (spriteDuduk != null && srMahasiswa.sprite == spriteDuduk) faktor *= skalaSaatDuduk;
                target = skalaJalanTersimpan * faktor;

                if (srMahasiswa.sprite != spriteTerakhirDilaporkan)
                {
                    spriteTerakhirDilaporkan = srMahasiswa.sprite;
                    Debug.Log("[Adegan L3-S3] Gambar mahasiswa sekarang '" + srMahasiswa.sprite.name + "' (tinggi " + t.ToString("0.00")
                        + ", tinggi saat jalan " + tinggiJalan.ToString("0.00") + "), ukuran dikoreksi faktor " + faktor.ToString("0.00") + ".");
                }
            }
        }

        if (trMahasiswa.localScale != target)
        {
            if (!ukuranSudahDilaporkan && !ukuranOtomatis)
            {
                ukuranSudahDilaporkan = true;
                Debug.LogWarning("[Adegan L3-S3] Ukuran mahasiswa diubah oleh script / Animator lain (" + trMahasiswa.localScale
                    + "), dikembalikan ke " + target + ". Kalau mau tahu pelakunya, cek animasi duduk atau script lain di objek '" + trMahasiswa.name + "'.");
            }
            trMahasiswa.localScale = target;
        }
    }

    // Kursi ini dipakai untuk adegan level 3, jadi duduk di sini tidak boleh memicu bimbingan level 1 lagi
    private void LepasBimbingan()
    {
        if (!lepasBimbinganLevel1 || kursiMC == null) return;
        if (kursiMC.bimbinganScript != null) kursiMC.bimbinganScript = null;
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
        if (mahasiswa == null)
        {
            Debug.LogWarning("[Adegan L3-S3] Objek Mahasiswa belum diisi di Inspector, adegan dibatalkan.");
            yield break;
        }

        // 1. MC dikunci selama adegan: duduk di kursinya (tidak bisa berdiri), atau berhenti kalau tidak pakai kursi
        LepasBimbingan();
        if (kursiMC != null)
        {
            kursiMC.TerkunciEksternal = true;
            yield return new WaitForSeconds(jedaSebelumMahasiswa);
        }
        else if (kunciGerakPlayer)
        {
            KunciPemain(true);
        }

        // 2. Mahasiswa muncul di pintu dan berjalan menemui MC
        if (jalur != null && jalur.Length > 0 && jalur[0] != null)
            mahasiswa.transform.position = jalur[0].position;

        mahasiswa.SetActive(true);
        SetAnimasiJalan(true);

        SpriteRenderer sr = mahasiswa.GetComponentInChildren<SpriteRenderer>();
        float mulai = Time.time;

        // Ukuran saat berjalan dicatat dan ditahan selama adegan, supaya tidak membesar / mengecil saat duduk
        trMahasiswa = (sr != null) ? sr.transform : mahasiswa.transform;
        Vector3 skalaJalan = trMahasiswa.localScale;
        skalaJalanTersimpan = skalaJalan;
        skalaTetap = skalaJalan;
        srMahasiswa = sr;
        ukuranOtomatis = false;
        spriteTerakhirDilaporkan = null;
        ukuranSudahDilaporkan = false;
        kunciUkuran = true;

        if (jalur != null)
        {
            foreach (Transform titik in jalur)
            {
                if (titik == null) continue;
                yield return JalanKe(titik.position, sr, mulai);
            }
        }

        bool duduk = false;

        if (berhentiDiDekatPlayer)
        {
            if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
                yield return JalanKe(pm.transform.position + (Vector3)offsetDariPlayer, sr, mulai);
        }
        else if (titikDuduk != null)
        {
            yield return JalanKe(titikDuduk.position, sr, mulai);
            duduk = true;
        }

        // Catat tinggi gambar saat berjalan (sekarang masih gambar jalan), lalu ukuran dijaga mengikuti tinggi ini
        if (samakanUkuranDenganSpriteJalan && sr != null && sr.sprite != null)
        {
            tinggiJalan = sr.sprite.bounds.size.y;
            ukuranOtomatis = true;
        }

        SetAnimasiJalan(false);

        // Tampilan duduk (opsional)
        Sprite spriteAwal = null;
        Vector3 skalaAwal = Vector3.one;
        int sortingAwal = 0;
        bool spriteDiganti = false;

        if (duduk && sr != null)
        {
            if (spriteDuduk != null)
            {
                if (animator != null) animator.enabled = false;
                spriteAwal = sr.sprite;
                skalaAwal = sr.transform.localScale;
                spriteDiganti = true;
                sr.sprite = spriteDuduk;

                // Samakan tinggi gambar duduk dengan gambar jalan (gambar dari folder lain bisa punya Pixels Per Unit berbeda)
                float rasio = 1f;
                if (samakanUkuranDenganSpriteJalan && spriteAwal != null && spriteDuduk.bounds.size.y > 0.001f)
                    rasio = spriteAwal.bounds.size.y / spriteDuduk.bounds.size.y;

                sr.transform.localScale = sr.transform.localScale * rasio * skalaSaatDuduk;

                Debug.Log("[Adegan L3-S3] Ukuran duduk: tinggi sprite jalan=" + (spriteAwal != null ? spriteAwal.bounds.size.y.ToString("0.00") : "-")
                    + ", tinggi sprite duduk=" + spriteDuduk.bounds.size.y.ToString("0.00")
                    + ", rasio otomatis=" + rasio.ToString("0.00") + ", skala duduk=" + skalaSaatDuduk
                    + ", skala akhir=" + sr.transform.localScale);

                skalaTetap = sr.transform.localScale;
            }
            sortingAwal = sr.sortingOrder;
            sr.sortingOrder = sortingOrderDuduk;
        }

        // Tunggu satu frame supaya posisi dan sprite duduk sudah terpasang, lalu jeda singkat. Dialog baru muncul SETELAH ini
        yield return null;

        string info = "[Adegan L3-S3] Mahasiswa siap dialog: duduk=" + duduk + ", posisi=" + mahasiswa.transform.position;
        if (titikDuduk != null)
            info += ", jarak ke titik duduk=" + Vector2.Distance(mahasiswa.transform.position, titikDuduk.position).ToString("0.00");
        info += ", sprite=" + ((sr != null && sr.sprite != null) ? sr.sprite.name : "-");
        Debug.Log(info);

        yield return new WaitForSeconds(jedaSebelumDialog);

        // 3. Dua box dialog (pada pengulangan dilewati kalau Dialog Sekali Saja dicentang)
        if (dialogSekaliSaja && dialogSudahTampil)
        {
            // dialog sudah pernah tampil, langsung ke bar
        }
        else if (boxDialog == null || boxDialog.Length == 0)
        {
            Debug.LogWarning("[Adegan L3-S3] Box Dialog masih kosong di Inspector, dialog dilewati.");
        }
        else
        {
            dialogSudahTampil = true;
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog));
        }

        // 3b. Bar hijau langsung muncul dan berputar setelah dialog selesai
        if (canvasProgressBar != null) canvasProgressBar.SetActive(true);
        if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;

        float timerBar = 0f;
        while (timerBar < durasiBar)
        {
            timerBar += Time.deltaTime;
            if (circularProgressBar != null) circularProgressBar.fillAmount = timerBar / durasiBar;
            yield return null;
        }

        if (circularProgressBar != null) circularProgressBar.fillAmount = 1f;
        yield return new WaitForSeconds(0.3f);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);

        // 4. Poin
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoin);
            Debug.Log("[Adegan L3-S3] Membimbing mahasiswa selesai! Skor bertambah +" + jumlahPoin);
        }
        else
        {
            Debug.LogWarning("[Adegan L3-S3] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // 5. Reward (lewat RewardManager, atau Box Reward buatan sendiri)
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

        // 6. Mahasiswa berjalan keluar lewat jalur (urutan terbalik), lalu menghilang
        if (sr != null && spriteDiganti)
        {
            sr.sprite = spriteAwal;
            sr.transform.localScale = skalaAwal;
            sr.sortingOrder = sortingAwal;
        }

        ukuranOtomatis = false;
        skalaTetap = skalaJalan;
        if (animator != null) animator.enabled = true;
        SetAnimasiJalan(true);

        float mulaiKeluar = Time.time;
        if (jalur != null)
        {
            for (int i = jalur.Length - 1; i >= 0; i--)
            {
                if (jalur[i] == null) continue;
                yield return JalanKe(jalur[i].position, sr, mulaiKeluar);
            }
        }

        SetAnimasiJalan(false);
        kunciUkuran = false;
        mahasiswa.SetActive(false);

        // 7. Box tutorial setelah mahasiswa pergi (sekali saja kalau diatur begitu)
        if (!(tutorialSekaliSaja && tutorialSudahTampil))
        {
            tutorialSudahTampil = true;
            yield return StartCoroutine(TampilkanTutorial());
        }

        // 8. Energi berkurang, lalu cek apakah habis
        if (biayaEnergi > 0f && energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        // 9. Cooldown lingkaran mulai (lingkaran terisi bertahap, kursi MC meredup)
        MulaiCooldown();
        perluBerdiriDulu = true;

        // 10. MC dibebaskan
        if (kursiMC != null) kursiMC.TerkunciEksternal = false;
        else if (kunciGerakPlayer) KunciPemain(false);

        // Event untuk scene berikutnya hanya dipanggil sekali
        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log("[Adegan L3-S3] Scene 3 Level 3 selesai.");
    }

    private IEnumerator JalanKe(Vector3 tujuan, SpriteRenderer sr, float waktuMulai)
    {
        tujuan.z = mahasiswa.transform.position.z;

        while (Vector3.Distance(mahasiswa.transform.position, tujuan) > 0.02f)
        {
            // Pengaman: kalau terlalu lama, langsung dipindah ke tujuan
            if (Time.time - waktuMulai > batasWaktuAdegan)
            {
                Debug.LogWarning("[Adegan L3-S3] Batas waktu habis saat mahasiswa berjalan, posisinya langsung dipindah.");
                break;
            }

            Vector3 posisi = mahasiswa.transform.position;
            Vector3 arah = tujuan - posisi;

            // Hadap kiri/kanan sesuai arah jalan
            if (sr != null && Mathf.Abs(arah.x) > 0.01f)
                sr.flipX = arah.x < 0f;

            mahasiswa.transform.position = Vector3.MoveTowards(posisi, tujuan, kecepatan * Time.deltaTime);
            yield return null;
        }

        mahasiswa.transform.position = tujuan;
    }

    // ---------- Cooldown lingkaran ----------
    private void MulaiCooldown()
    {
        cooldownSelesaiPada = Time.time + durasiCooldown;
        cooldownAktif = true;

        if (gambarCooldown != null) gambarCooldown.fillAmount = 0f;
        if (objekCooldown != null) objekCooldown.SetActive(true);

        if (redupkanKursi && spriteKursiMC != null) spriteKursiMC.color = warnaCooldown;
    }

    private void TickCooldown()
    {
        float sisa = Mathf.Max(0f, cooldownSelesaiPada - Time.time);

        // Lingkaran terisi bertahap: 0 (kosong) -> 1 (penuh)
        if (gambarCooldown != null && durasiCooldown > 0f)
            gambarCooldown.fillAmount = 1f - (sisa / durasiCooldown);

        if (Time.time >= cooldownSelesaiPada) SelesaiCooldown();
    }

    private void SelesaiCooldown()
    {
        cooldownAktif = false;

        if (objekCooldown != null) objekCooldown.SetActive(false);
        if (redupkanKursi && spriteKursiMC != null) spriteKursiMC.color = warnaAsliKursi;

        // Bimbingan berikutnya bisa mulai lagi (butuh Kursi MC: MC harus berdiri lalu duduk lagi)
        if (bisaDiulang && kursiMC != null) sudahJalan = false;

        Debug.Log("[Adegan L3-S3] Cooldown selesai" + ((bisaDiulang && kursiMC != null) ? ", bimbingan berikutnya bisa dimulai." : "."));
    }

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
    private IEnumerator TampilkanBoxBerurutan(GameObject[] daftar, bool keDepan = false)
    {
        foreach (GameObject box in daftar)
        {
            if (box == null) continue;

            box.SetActive(true);

            // Box tutorial dipaksa tampil paling depan di Canvas-nya, di atas overlay hitam
            if (keDepan) box.transform.SetAsLastSibling();

            if (!box.activeInHierarchy)
                Debug.LogWarning("[Adegan L3-S3] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    private void SetAnimasiJalan(bool jalan)
    {
        if (animator == null || string.IsNullOrEmpty(parameterJalan)) return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == parameterJalan && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterJalan, jalan);
                return;
            }
        }
    }

    // Menahan atau membebaskan pergerakan MC
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