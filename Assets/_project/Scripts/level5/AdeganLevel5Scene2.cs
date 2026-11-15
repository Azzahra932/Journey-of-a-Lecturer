using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Level 5 - Scene 2 (Memimpin Pengembangan Ilmu Pengetahuan), Lokasi: Research Center
// Alurnya sama persis dengan Level 4 Scene 3, termasuk box pilihannya (jujur / bohong, lalu empat pilihan):
//   1. Scene 1 Level 5 selesai -> Scene 2 siap, Peneliti sudah duduk menunggu di Research Center
//   2. MC masuk zona Research Center: MC duduk di kursi, dua box dialog (Peneliti lalu Pemain), Space untuk lanjut
//   3. Setelah dialog, MC dilepas. Saat MC duduk di kursi, ikon E muncul di laptop
//   4. Tekan E: dua box pilihan jujur (atas) / bohong (bawah, integritas berkurang), lalu box pilihan (4 pilihan)
//      Lama bar bergantung pada jujur / bohong: jujur 15 detik, bohong 10 detik
//   5. Bar hijau berputar, poin bertambah (sesuai pilihan), reward, box tutorial (opsional, sekali saja)
//   6. Energi berkurang, cooldown baterai mulai (layar laptop redup). Setelah penuh, laptop bisa dipakai lagi (berulang)
//
// Pasang script ini di objek ZONA BARU (objek sendiri, bukan objek zona Level 4) yang punya Collider2D dengan Is Trigger dicentang (di Research Center).
// Cara menyambung dari Scene 1 Level 5: di AdeganLevel5Scene1, event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Kursi, laptop, ikon E, bar, dan baterai yang sama dengan Level 4 Scene 3 boleh dibiarkan kosong di sini:
// kalau "Script Level 4 Scene 3" diisi, referensi yang kosong otomatis diambil dari sana.
//
// [DefaultExecutionOrder(-50)] supaya tombol E dibaca script ini lebih dulu daripada InteractiveDesk,
// jadi menekan E untuk laptop tidak membuat MC berdiri dari kursi.
[DefaultExecutionOrder(-50)]
public class AdeganLevel5Scene2 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan Scene 1 dulu (centang SEBELUM Play)
    public string tagPemain = "Player";

    [Header("Referensi Kursi dan Peneliti")]
    public InteractiveDesk kursiPeneliti;       // Kursi di Research Center. Kosong = diambil dari Script Level 4 Scene 3
    public SpriteRenderer sumberSpriteDuduk;    // Opsional (cara mudah): objek di Hierarchy dengan SpriteRenderer berisi gambar MC duduk level 5. Gambarnya dipakai sebagai Sit Sprite kursi
    public Sprite spriteDudukLevel5;            // Alternatif: sprite dari folder Project, dipakai kalau Sumber Sprite Duduk kosong
    public bool matikanBimbinganDiKursi = true; // true = kursi ini tidak memicu bimbingan level 1 lagi
    public bool dudukOtomatis = false;          // false = MC duduk sendiri, dialog muncul setelah duduk. true = MC langsung dipindah duduk
    public GameObject[] penelitiObjek;          // Peneliti yang sudah duduk menunggu (objek khusus Level 5). Nonaktif di awal, muncul saat Scene 2 siap

    [Header("Script dan Objek Level 4 (dimatikan sejak Scene 2 Level 5 siap)")]
    public AdeganLevel4Scene3 scriptLevel4Scene3;   // Scene 3 Level 4 yang memakai zona, kursi, dan laptop yang sama
    public GameObject[] penelitiLevel4;             // Opsional: peneliti Level 4 yang dihilangkan. JANGAN isi dengan objek yang sama dengan Peneliti Objek

    [Header("Box Dialog (dibuat di Hierarchy, urut: Peneliti lalu Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 1f;        // Jeda (detik) setelah MC duduk sebelum dialog pertama muncul

    [Header("Box Tutorial (di atas overlay hitam, opsional)")]
    public GameObject overlayHitam;             // Kosong = diambil dari Level 4 Scene 3
    public GameObject[] boxTutorialSetelahBar;  // Muncul setelah bar hijau dan reward
    public bool tutorialBarSekaliSaja = true;
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Laptop Research Center (kosong = diambil dari Level 4 Scene 3)")]
    public SpriteRenderer spriteLaptop;         // Gambar laptop (diredupkan saat cooldown)
    public Transform posisiLaptop;              // Acuan jarak kalau Harus Duduk dimatikan
    public GameObject ikonE;                    // Ikon "E" di atas laptop
    public KeyCode tombolInteraksi = KeyCode.E;
    public bool harusDudukUntukLaptop = true;   // true = ikon E muncul saat MC duduk. false = muncul saat MC dekat laptop (Jarak Laptop)
    public float jarakLaptop = 1.5f;

    [Header("Pilihan Jujur / Bohong (muncul dulu sebelum pilihan)")]
    public GameObject boxJujur;                 // Box pilihan atas: jujur
    public GameObject boxBohong;                // Box pilihan bawah: bohong
    public Button tombolJujur;                  // Jangan isi On Click di Inspector, dipasang lewat script
    public Button tombolBohong;                 // Jangan isi On Click di Inspector, dipasang lewat script
    public float penguranganIntegritasBohong = 15f;

    [Header("Box Pilihan Scene 2 (empat pilihan, urut dari atas ke bawah)")]
    public GameObject boxPilihan;               // Panel pilihan (judul + empat tombol)
    public Button tombolPilihan1;               // Jangan isi On Click di Inspector, dipasang lewat script
    public Button tombolPilihan2;
    public Button tombolPilihan3;
    public Button tombolPilihan4;

    [Header("Poin per Pilihan")]
    public int poinPilihan1 = 15;
    public int poinPilihan2 = 15;
    public int poinPilihan3 = 15;
    public int poinPilihan4 = 15;

    [Header("Lama Bar Berdasarkan Pilihan Jujur / Bohong")]
    public float durasiJujur = 15f;
    public float durasiBohong = 10f;

    [Header("Bar Hijau Berputar (kosong = diambil dari Level 4 Scene 3)")]
    public GameObject canvasProgressBar;
    public Image circularProgressBar;           // Image Type: Filled

    [Header("Setelah Bar Selesai")]
    public int indexReward = -1;                // Nomor reward di RewardManager. -1 = pakai Box Reward di bawah (kalau diisi)
    public GameObject boxReward;                // Kosong = diambil dari Level 4 Scene 3
    public float lamaBoxReward = 2f;
    public float biayaEnergi = 15f;
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Aktivitas Berulang")]
    public bool bisaDiulang = true;

    [Header("Cooldown Baterai (kosong = diambil dari Level 4 Scene 3)")]
    public float durasiCooldown = 60f;
    public GameObject ikonBateraiLemah;
    public IndikatorBaterai indikatorBaterai;
    public TMP_Text teksCooldown;
    public string isiTeksCooldown = "Recharging...";
    public GameObject teksIstirahat;            // Opsional: "Laptop sedang istirahat" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Setelah Scene 2 Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah aktivitas laptop pertama selesai

    private bool siap = false;
    private bool dialogMulai = false;
    private bool dialogSelesai = false;
    private bool sedangBerjalan = false;
    private bool sudahPernahSelesai = false;
    private bool eventSudahDipanggil = false;
    private bool tutorialBarSudahTampil = false;
    private bool peringatanBelumSiap = false;
    private bool level4Dimatikan = false;
    private int pilihanJujur = 0;               // 0 = belum, 1 = jujur, 2 = bohong
    private int pilihan = 0;                    // 0 = belum, 1 sampai 4

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

        SalinReferensiDariLevel4();

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorialSetelahBar);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxReward != null) boxReward.SetActive(false);
        if (ikonE != null) ikonE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCooldown != null) teksCooldown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);
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
        AdeganLevel5Scene2[] semua = FindObjectsByType<AdeganLevel5Scene2>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L5-S2] Ada " + semua.Length + " objek yang memasang script AdeganLevel5Scene2. Pakai satu saja (objek zona).");

        // Peneliti baru dimunculkan saat Scene 2 siap
        if (siap)
        {
            MatikanLevel4();
            NonaktifkanBimbingan();
            TerapkanSpriteDuduk();
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

    // Referensi yang kosong diambil dari Level 4 Scene 3 (kursi, laptop, ikon E, bar, baterai yang sama).
    // Dipanggil berulang-ulang (aman), karena laptop Level 4 bisa baru dibuat setelah Start.
    private void SalinReferensiDariLevel4()
    {
        AdeganLevel4Scene3 s = scriptLevel4Scene3;
        if (s == null) return;

        if (kursiPeneliti == null) kursiPeneliti = s.kursiPeneliti;
        if (spriteLaptop == null) spriteLaptop = s.spriteLaptop;
        if (posisiLaptop == null) posisiLaptop = s.posisiLaptop;
        if (ikonE == null) ikonE = s.ikonE;
        if (overlayHitam == null) overlayHitam = s.overlayHitam;
        if (canvasProgressBar == null) canvasProgressBar = s.canvasProgressBar;
        if (circularProgressBar == null) circularProgressBar = s.circularProgressBar;
        if (boxReward == null) boxReward = s.boxReward;
        if (ikonBateraiLemah == null) ikonBateraiLemah = s.ikonBateraiLemah;
        if (indikatorBaterai == null) indikatorBaterai = s.indikatorBaterai;
        if (teksCooldown == null) teksCooldown = s.teksCooldown;
        if (teksIstirahat == null) teksIstirahat = s.teksIstirahat;
        if (energyBar == null) energyBar = s.energyBar;
    }

    // Dipanggil dari event "Saat Adegan Selesai" di AdeganLevel5Scene1
    public void SiapkanScene()
    {
        siap = true;
        MatikanLevel4();
        NonaktifkanBimbingan();
        TerapkanSpriteDuduk();
        AktifkanPeneliti(true);
        Debug.Log("[Adegan L5-S2] Scene 2 siap, Peneliti menunggu di Research Center.");
    }

    private void AktifkanPeneliti(bool aktif)
    {
        if (penelitiObjek == null) return;

        foreach (GameObject p in penelitiObjek)
        {
            if (p != null) p.SetActive(aktif);
        }
    }

    // Script Level 4 dimatikan (supaya tombol E tidak terbaca dua kali), tampilan sisanya dibersihkan, peneliti Level 4 dihilangkan
    private void MatikanLevel4()
    {
        SalinReferensiDariLevel4();

        if (level4Dimatikan) return;
        level4Dimatikan = true;

        AdeganLevel4Scene3 s = scriptLevel4Scene3;
        if (s != null)
        {
            s.enabled = false;

            if (s.ikonE != null) s.ikonE.SetActive(false);
            if (s.ikonBateraiLemah != null) s.ikonBateraiLemah.SetActive(false);
            if (s.teksCooldown != null) s.teksCooldown.gameObject.SetActive(false);
            if (s.teksIstirahat != null) s.teksIstirahat.SetActive(false);
            if (s.spriteLaptop != null) s.spriteLaptop.color = s.warnaNormal;
        }

        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;

        if (penelitiLevel4 == null) return;

        foreach (GameObject p in penelitiLevel4)
        {
            if (p == null) continue;

            if (penelitiObjek != null && System.Array.IndexOf(penelitiObjek, p) >= 0)
            {
                Debug.LogWarning("[Adegan L5-S2] '" + p.name + "' ada di Peneliti Objek DAN Peneliti Level 4. Objek ini dilewati.");
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
            Debug.Log("[Adegan L5-S2] Bimbingan level 1 dilepas dari kursi ini.");
        }
    }

    // Pasang gambar duduk MC level 5 ke kursi (harus sebelum MC duduk)
    private void TerapkanSpriteDuduk()
    {
        if (kursiPeneliti == null) return;

        Sprite sprite = spriteDudukLevel5;
        if (sumberSpriteDuduk != null && sumberSpriteDuduk.sprite != null) sprite = sumberSpriteDuduk.sprite;

        if (sprite != null && kursiPeneliti.sitSprite != sprite)
        {
            kursiPeneliti.sitSprite = sprite;
            Debug.Log("[Adegan L5-S2] Sprite duduk kursi diganti ke sprite level 5.");
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
                Debug.Log("[Adegan L5-S2] MC masuk zona, tapi Scene 2 belum siap. Centang 'Langsung Siap' untuk tes, atau sambungkan event Scene 1 Level 5 ke SiapkanScene.");
            }
            return;
        }

        if (!siap)
        {
            siap = true;
            AktifkanPeneliti(true);
        }

        MatikanLevel4();
        NonaktifkanBimbingan();
        TerapkanSpriteDuduk();

        dialogMulai = true;
        Debug.Log("[Adegan L5-S2] MC masuk zona, adegan dimulai.");
        StartCoroutine(AlurDialog());
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (langsung mulai)")]
    private void TesAdegan()
    {
        if (dialogMulai || dialogSelesai) return;
        siap = true;
        dialogMulai = true;
        MatikanLevel4();
        NonaktifkanBimbingan();
        TerapkanSpriteDuduk();
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
            if (dudukOtomatis)
            {
                kursiPeneliti.DudukUntukAdegan();
            }
            else
            {
                Debug.Log("[Adegan L5-S2] Menunggu MC duduk sendiri di kursi Research Center.");
                while (!kursiPeneliti.SedangDuduk) yield return null;
            }

            // MC dikunci di kursi selama dialog
            kursiPeneliti.TerkunciEksternal = true;
        }
        else
        {
            Debug.LogWarning("[Adegan L5-S2] Kursi Peneliti belum diisi di Inspector (dan Level 4 Scene 3 juga tidak punya), MC tidak didudukkan.");
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

        Debug.Log("[Adegan L5-S2] MC sudah duduk, dialog dipanggil.");

        // 2. Dua box dialog (Peneliti lalu Pemain)
        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L5-S2] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

        // 3. MC dilepas, ikon E akan muncul di laptop saat MC duduk
        if (kursiPeneliti != null) kursiPeneliti.TerkunciEksternal = false;

        dialogSelesai = true;
        sedangBerjalan = false;

        // Cegah Space / klik terakhir dialog terbaca untuk laptop
        waktuMulaiDuduk = Time.time;

        Debug.Log("[Adegan L5-S2] Dialog selesai, ikon E muncul di laptop.");
    }

    // ---------- Aktivitas laptop ----------
    private IEnumerator AlurLaptop()
    {
        bool pertamaKali = !sudahPernahSelesai;
        sedangBerjalan = true;

        if (ikonE != null) ikonE.SetActive(false);

        // MC tidak bisa berdiri selama aktivitas berjalan
        if (kursiPeneliti != null) kursiPeneliti.TerkunciEksternal = true;

        // 0. Dua box pilihan: jujur (atas) atau bohong (bawah). Bohong mengurangi integritas
        pilihanJujur = 0;
        TampilkanPilihanJujur(true);

        while (pilihanJujur == 0) yield return null;

        TampilkanPilihanJujur(false);

        if (pilihanJujur == 2)
        {
            if (IntegritasManager.Instance != null)
            {
                IntegritasManager.Instance.ReduceIntegritas(penguranganIntegritasBohong);
                Debug.Log("[Adegan L5-S2] Memilih bohong, integritas berkurang " + penguranganIntegritasBohong + ".");
            }
            else
            {
                Debug.LogWarning("[Adegan L5-S2] IntegritasManager.Instance tidak ditemukan di Scene!");
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

        Debug.Log("[Adegan L5-S2] Memilih pilihan " + pilihan + ": bar " + durasi + " detik, poin " + poin + ".");

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
            Debug.Log("[Adegan L5-S2] Pengembangan penelitian selesai! Skor bertambah +" + poin);
        }
        else
        {
            Debug.LogWarning("[Adegan L5-S2] ScoreManager.Instance tidak ditemukan di Scene!");
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
        waktuMulaiDuduk = Time.time;

        // Event untuk scene berikutnya hanya dipanggil sekali
        if (!eventSudahDipanggil)
        {
            eventSudahDipanggil = true;
            saatAdeganSelesai.Invoke();
        }

        Debug.Log(pertamaKali ? "[Adegan L5-S2] Scene 2 Level 5 selesai." : "[Adegan L5-S2] Aktivitas ulang selesai.");
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

        Debug.Log("[Adegan L5-S2] Laptop Research Center siap dipakai lagi.");
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
                Debug.LogWarning("[Adegan L5-S2] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }
}