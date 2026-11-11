using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Level 4 - Scene 1 (Onboarding dan Pendidikan S3), Lokasi: Ruang Dosen -> Titik Panah
//   1. Level 4 dimulai (event "Saat Masuk Level Berikutnya" di PopupLevel3Selesai) -> Scene 1 siap
//   2. MC duduk di kursi dosen (sendiri, atau otomatis kalau "Duduk Otomatis" dicentang)
//   3. Kaprodi masuk menghampiri (jalur waypoint yang sama dengan Level 3 Scene 1) dan duduk
//   4. Empat box dialog (Kaprodi, Pemain, Kaprodi, Pemain), Space untuk lanjut
//   5. Kaprodi berjalan keluar. Box tutorial (opsional) lalu tampil
//   6. MC dibebaskan, panah petunjuk muncul. PEMAIN berjalan sendiri sampai di Titik Pintu Keluar (ujung panah)
//   7. Begitu sampai: MC dikunci, panah hilang, layar menghitam (transisi)
//   8. Box kalimat tampil selama "Durasi Box Kalimat" (3 detik), tanpa perlu ditekan
//   9. Layar terang lagi, posisi MC tetap seperti sebelumnya (tidak dipindah)
//  10. MC dibebaskan
//
// Script ini dipasang di objek apa saja (tidak perlu collider).
// Cara menyambung: di PopupLevel3Selesai, event "Saat Masuk Level Berikutnya",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Bagian yang tidak diisi di Inspector (mis. tutorial, poin, reward) dilewati.
public class AdeganLevel4Scene1 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa melewati pop-up Level 3 dulu

    [Header("Referensi")]
    public InteractiveDesk kursiDosen;          // Kursi dosen di ruang dosen (meja kerja MC)
    public NPCCutscene kaprodi;                 // Kaprodi (kapdiam), jalur waypoint sama seperti Level 3 Scene 1
    public bool dudukOtomatis = false;          // false = MC berjalan ke kursi dan duduk sendiri. true = MC langsung didudukkan saat Scene 1 mulai
    public bool lepasEvaluasiLevel1 = true;     // true = duduk di kursi dosen tidak lagi memicu evaluasi / bimbingan level 1

    [Header("Dijeda Selama Adegan (opsional)")]
    [Tooltip("Script adegan Level 3 yang masih aktif dan bisa ikut membaca tombol E / Space di ruang dosen (mis. AdeganLevel3Scene5, AdeganLevel3Scene4). Dimatikan selama adegan ini, dinyalakan lagi di akhir.")]
    public Behaviour[] scriptDijeda;
    [Tooltip("Objek tampilan lama yang harus tersembunyi selama adegan ini (mis. ikon E laptop Level 3, lingkaran / baterai cooldown).")]
    public GameObject[] objekDisembunyikan;

    [Header("Box Dialog (urut: Kaprodi, Pemain, Kaprodi, Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumKaprodi = 0.8f;     // Jeda (detik) setelah MC duduk sebelum Kaprodi masuk
    public float batasWaktuAdegan = 120f;       // Pengaman: kalau Kaprodi macet, adegan dilanjutkan setelah sekian detik

    [Header("Box Tutorial (opsional, muncul setelah Kaprodi pergi)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;            // Kosong = tanpa tutorial
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Panah dan Titik Tujuan")]
    public GameObject[] panahKePintu;           // Panah petunjuk, muncul setelah Kaprodi pergi dan hilang saat MC sampai di titik tujuan
    public Transform titikPintuKeluar;          // Objek kosong di UJUNG panah (tempat pemain harus lewat). Lingkaran kuning terlihat di Scene saat objek ini dipilih
    public float jarakPintu = 1.5f;             // Jarak MC ke titik supaya dianggap sudah sampai

    [Header("Transisi Layar dan Box Kalimat")]
    public CanvasGroup layarHitam;              // Panel hitam satu layar dengan komponen CanvasGroup (nonaktif di awal juga boleh)
    public float durasiFade = 1f;               // Lama layar menggelap / menerang (detik)
    public GameObject boxKalimat;               // Box berisi tulisan yang tampil di layar hitam (harus satu Canvas dengan Layar Hitam)
    public float durasiBoxKalimat = 3f;         // Lama box kalimat tampil (detik), tanpa perlu ditekan
    public bool hentikanWaktuSaatTransisi = true;   // true = jam game berhenti selama layar hitam

    [Header("Poin dan Reward (opsional)")]
    public int jumlahPoin = 0;                  // Poin yang didapat setelah adegan (0 = tanpa poin)
    public int indexReward = -1;                // Nomor reward di RewardManager. -1 = tanpa reward

    [Header("Setelah Adegan Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil setelah semuanya selesai (untuk menyambung ke adegan Level 4 berikutnya)

    private bool siap = false;
    private bool sudahJalan = false;
    private bool selesaiSemua = false;
    private bool kaprodiTiba = false;
    private bool peringatanKursiDitampilkan = false;

    private PlayerMovement pm;
    private readonly List<Behaviour> interaksiDimatikan = new List<Behaviour>();
    private readonly List<Behaviour> scriptDijedaAktif = new List<Behaviour>();

    // Penahan posisi: selama dialog, MC dikembalikan ke tempat duduknya kalau ada yang menggesernya
    private bool tahanPosisi = false;
    private Vector3 posisiTahan;
    private bool sudahDilaporkanPindah = false;
    private TransisiKembaliKeAwal transisi;
    private bool pemainDikunci = false;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorial);
        SembunyikanSemua(panahKePintu);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxKalimat != null) boxKalimat.SetActive(false);
        if (layarHitam != null)
        {
            layarHitam.alpha = 0f;
            layarHitam.gameObject.SetActive(false);
        }

        transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel4Scene1[] semua = FindObjectsByType<AdeganLevel4Scene1>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L4-S1] Ada " + semua.Length + " objek yang memasang script AdeganLevel4Scene1. Pakai satu saja.");
    }

    // Lingkaran kuning di tab Scene (saat objek ini dipilih): area MC dianggap sudah sampai di titik tujuan
    void OnDrawGizmosSelected()
    {
        if (titikPintuKeluar == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(titikPintuKeluar.position, jarakPintu);
    }

    // Dipanggil dari event "Saat Masuk Level Berikutnya" di PopupLevel3Selesai
    public void SiapkanScene()
    {
        siap = true;
        LepasEvaluasiLevel1();
        Debug.Log("[Adegan L4-S1] Scene 1 Level 4 siap, menunggu MC duduk di kursi dosen.");
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

    // Menu tes: langsung ke bagian panah (tanpa Kaprodi dan dialog)
    [ContextMenu("Tes Langsung ke Panah (tanpa Kaprodi)")]
    private void TesLangsungKePanah()
    {
        if (sudahJalan) return;
        siap = true;
        sudahJalan = true;
        StartCoroutine(AlurTanpaKaprodi());
    }

    // Menunggu MC duduk di kursi dosen (atau langsung mulai kalau Duduk Otomatis dicentang)
    void Update()
    {
        if (sudahJalan) return;

        // "Langsung Siap" dibaca di sini (bukan hanya di Start) supaya juga berlaku kalau dicentang saat Play
        if (!siap && !langsungSiap) return;

        if (kursiDosen == null)
        {
            if (!peringatanKursiDitampilkan)
            {
                peringatanKursiDitampilkan = true;
                Debug.LogWarning("[Adegan L4-S1] Kursi Dosen belum diisi di Inspector, adegan tidak bisa dimulai.");
            }
            return;
        }

        if (dudukOtomatis || kursiDosen.SedangDuduk)
        {
            sudahJalan = true;
            Debug.Log("[Adegan L4-S1] Adegan dimulai.");
            StartCoroutine(Alur());
        }
    }

    // LateUpdate jalan setelah semua Update: kalau script lain menggeser MC dari kursi (mis. karena Space),
    // MC langsung dikembalikan ke tempat duduknya. Objek tampilan lama juga dijaga tetap tersembunyi.
    void LateUpdate()
    {
        if (objekDisembunyikan != null && (sudahJalan && !selesaiSemua))
        {
            foreach (GameObject o in objekDisembunyikan)
            {
                if (o != null && o.activeSelf) o.SetActive(false);
            }
        }

        if (!tahanPosisi || pm == null) return;

        // Energi habis: MC dipindah oleh transisi, jadi MC tidak ditahan lagi
        if (transisi != null && transisi.SedangTransisi)
        {
            tahanPosisi = false;
            return;
        }

        if (Vector2.Distance(pm.transform.position, posisiTahan) > 0.01f)
        {
            if (!sudahDilaporkanPindah)
            {
                sudahDilaporkanPindah = true;
                Debug.LogWarning("[Adegan L4-S1] Ada script yang menggeser MC dari kursi dosen (posisi " + pm.transform.position
                    + "). MC dikembalikan.");
            }

            pm.transform.position = posisiTahan;
        }
    }

    private void SaatKaprodiTiba()
    {
        kaprodiTiba = true;
    }

    private void SembunyikanSemua(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    // Kursi dosen dipakai untuk adegan level 4, jadi duduk di sini tidak boleh memicu evaluasi / bimbingan level 1 lagi
    private void LepasEvaluasiLevel1()
    {
        if (!lepasEvaluasiLevel1 || kursiDosen == null) return;

        if (kursiDosen.evaluasiScript != null) kursiDosen.evaluasiScript = null;
        if (kursiDosen.bimbinganScript != null) kursiDosen.bimbinganScript = null;
    }

    // ---------- Alur lengkap ----------
    private IEnumerator Alur()
    {
        LepasEvaluasiLevel1();
        JedaScriptLain(true);

        // 1. MC duduk di kursi dosen (otomatis kalau diatur begitu), lalu dikunci di kursi
        if (dudukOtomatis && !kursiDosen.SedangDuduk)
            kursiDosen.DudukUntukAdegan();

        kursiDosen.TerkunciEksternal = true;
        MatikanInteraksiLain();

        pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            // Tunggu satu frame supaya posisi duduk sudah terpasang sebelum posisinya ditahan
            yield return null;

            posisiTahan = pm.transform.position;
            sudahDilaporkanPindah = false;
            tahanPosisi = true;
        }

        yield return new WaitForSeconds(jedaSebelumKaprodi);

        if (kaprodi == null)
        {
            Debug.LogWarning("[Adegan L4-S1] Kaprodi belum diisi di Inspector, bagian Kaprodi dilewati.");
        }
        else
        {
            yield return StartCoroutine(AlurKaprodi());
        }

        // MC dibebaskan dari kursi
        tahanPosisi = false;
        kursiDosen.TerkunciEksternal = false;
        NyalakanInteraksiLain();

        yield return StartCoroutine(AlurPanahDanTransisi());
    }

    // Versi tes: langsung ke bagian panah
    private IEnumerator AlurTanpaKaprodi()
    {
        JedaScriptLain(true);
        pm = FindFirstObjectByType<PlayerMovement>();
        yield return StartCoroutine(AlurPanahDanTransisi());
    }

    // Kaprodi masuk, empat dialog, Kaprodi keluar, tutorial (opsional)
    private IEnumerator AlurKaprodi()
    {
        // Dialog bawaan level 1 dimatikan sementara (dikembalikan lagi setelah adegan)
        DialogueManager dialogAsli = kaprodi.dialogueManager;
        kaprodi.dialogueManager = null;

        // Kunci Kaprodi: perintah keluar dari script adegan lain diabaikan selama adegan ini
        kaprodi.KunciAdegan(true);

        kaprodiTiba = false;
        kaprodi.SaatTibaDanDuduk += SaatKaprodiTiba;
        kaprodi.MulaiJalanMasuk();

        float mulai = Time.time;
        while (!kaprodiTiba && Time.time - mulai < batasWaktuAdegan)
            yield return null;

        if (kaprodiTiba)
        {
            // Empat box dialog
            if (boxDialog == null || boxDialog.Length == 0)
                Debug.LogWarning("[Adegan L4-S1] Box Dialog masih kosong di Inspector, dialog dilewati.");
            else
                yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

            // Kaprodi berjalan keluar
            yield return StartCoroutine(KaprodiKeluar());
        }
        else
        {
            Debug.LogWarning("[Adegan L4-S1] Kaprodi tidak sampai ke tujuan sebelum batas waktu, adegan dilanjutkan tanpa dialog.");
        }

        // Lepas kunci dan kembalikan pengaturan asli Kaprodi
        kaprodi.SaatTibaDanDuduk -= SaatKaprodiTiba;
        kaprodi.KunciAdegan(false);
        kaprodi.dialogueManager = dialogAsli;

        // Tutorial (opsional)
        yield return StartCoroutine(TampilkanTutorial());
    }

    // Kaprodi berjalan keluar, tunggu sampai objeknya dinonaktifkan oleh NPCCutscene
    private IEnumerator KaprodiKeluar()
    {
        // Pakai method khusus adegan (bukan MulaiJalanKeluar) supaya tidak terblokir kunci
        kaprodi.MulaiJalanKeluarAdegan();

        float mulai = Time.time;
        while (kaprodi.gameObject.activeSelf && Time.time - mulai < batasWaktuAdegan)
            yield return null;

        if (kaprodi.gameObject.activeSelf)
            Debug.LogWarning("[Adegan L4-S1] Batas waktu habis sebelum Kaprodi selesai keluar, adegan dilanjutkan.");
    }

    // ---------- Panah, transisi hitam, box kalimat ----------
    private IEnumerator AlurPanahDanTransisi()
    {
        if (titikPintuKeluar == null)
        {
            Debug.LogWarning("[Adegan L4-S1] Titik Pintu Keluar belum diisi di Inspector, bagian panah dilewati.");
            Selesai();
            yield break;
        }

        // 1. Panah petunjuk muncul. MC sudah bebas, jadi pemain berjalan sendiri
        if (panahKePintu != null)
        {
            foreach (GameObject p in panahKePintu)
            {
                if (p != null) p.SetActive(true);
            }
        }

        // 2. Tunggu pemain sampai di titik tujuan (ujung panah)
        yield return StartCoroutine(TungguPemainSampaiTitik());

        // 3. MC dikunci begitu sampai, panah hilang
        KunciPemain(true);
        SembunyikanSemua(panahKePintu);

        // 4. Layar menghitam
        yield return StartCoroutine(Fade(0f, 1f));

        bool waktuDihentikan = false;
        if (hentikanWaktuSaatTransisi)
        {
            Time.timeScale = 0f;
            waktuDihentikan = true;
        }

        // 5. Box kalimat tampil beberapa detik (otomatis hilang, tanpa perlu ditekan)
        if (boxKalimat != null)
        {
            boxKalimat.SetActive(true);
            boxKalimat.transform.SetAsLastSibling();   // harus di atas layar hitam

            if (!boxKalimat.activeInHierarchy)
                Debug.LogWarning("[Adegan L4-S1] Box Kalimat sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil.");
        }
        else
        {
            Debug.LogWarning("[Adegan L4-S1] Box Kalimat belum diisi di Inspector, bagian ini dilewati.");
        }

        yield return new WaitForSecondsRealtime(durasiBoxKalimat);

        if (boxKalimat != null) boxKalimat.SetActive(false);
        if (waktuDihentikan) Time.timeScale = 1f;

        // 6. Layar terang lagi. Posisi MC tidak dipindah
        yield return StartCoroutine(Fade(1f, 0f));
        if (layarHitam != null) layarHitam.gameObject.SetActive(false);

        // 7. Poin dan reward (opsional)
        if (jumlahPoin > 0 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoin);
            Debug.Log("[Adegan L4-S1] Skor bertambah +" + jumlahPoin);
        }

        if (indexReward >= 0 && RewardManager.Instance != null)
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));

        Selesai();
    }

    // Menunggu pemain berjalan sendiri sampai dekat Titik Pintu Keluar
    private IEnumerator TungguPemainSampaiTitik()
    {
        while (true)
        {
            if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();

            if (pm != null && Vector2.Distance(titikPintuKeluar.position, pm.transform.position) <= jarakPintu)
                yield break;

            yield return null;
        }
    }

    // Layar hitam menggelap (0 -> 1) atau menerang (1 -> 0). Pakai waktu nyata supaya tetap jalan walau game sedang berhenti
    private IEnumerator Fade(float dari, float ke)
    {
        if (layarHitam == null)
        {
            yield return new WaitForSecondsRealtime(durasiFade);
            yield break;
        }

        layarHitam.gameObject.SetActive(true);
        layarHitam.transform.SetAsLastSibling();
        layarHitam.blocksRaycasts = true;
        layarHitam.alpha = dari;

        float t = 0f;
        while (t < durasiFade)
        {
            t += Time.unscaledDeltaTime;
            layarHitam.alpha = Mathf.Lerp(dari, ke, durasiFade > 0f ? t / durasiFade : 1f);
            yield return null;
        }

        layarHitam.alpha = ke;

        if (ke <= 0f) layarHitam.blocksRaycasts = false;
    }

    // ---------- Pembantu ----------
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
    private IEnumerator TampilkanBoxBerurutan(GameObject[] daftar, bool keDepan)
    {
        foreach (GameObject box in daftar)
        {
            if (box == null) continue;

            box.SetActive(true);

            // Box tutorial dipaksa tampil paling depan di Canvas-nya, di atas overlay hitam
            if (keDepan) box.transform.SetAsLastSibling();

            if (!box.activeInHierarchy)
                Debug.LogWarning("[Adegan L4-S1] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }

    // Menahan atau membebaskan pergerakan MC (dipakai saat transisi hitam dan box kalimat)
    private void KunciPemain(bool kunci)
    {
        if (kunci)
        {
            if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
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

    // Script adegan Level 3 yang bisa ikut membaca E / Space dimatikan selama adegan ini, dinyalakan lagi di akhir
    private void JedaScriptLain(bool jeda)
    {
        if (jeda)
        {
            if (scriptDijeda == null) return;

            foreach (Behaviour b in scriptDijeda)
            {
                if (b != null && b.enabled)
                {
                    b.enabled = false;
                    scriptDijedaAktif.Add(b);
                }
            }
        }
        else
        {
            foreach (Behaviour b in scriptDijedaAktif)
            {
                if (b != null) b.enabled = true;
            }

            scriptDijedaAktif.Clear();
        }
    }

    // Mematikan sementara script kursi dan laptop lain yang ikut membaca tombol Space (kursi dosen sendiri tetap menyala)
    private void MatikanInteraksiLain()
    {
        foreach (InteractiveDesk d in FindObjectsByType<InteractiveDesk>(FindObjectsSortMode.None))
        {
            if (d != kursiDosen) TambahDimatikan(d);
        }

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

    private void Selesai()
    {
        // MC tidak ditahan lagi, bebas bergerak dan berdiri
        tahanPosisi = false;
        KunciPemain(false);
        if (pm != null && !pm.enabled) pm.enabled = true;
        if (kursiDosen != null) kursiDosen.TerkunciEksternal = false;

        // Pastikan Kaprodi tidak tertinggal dalam keadaan terkunci
        if (kaprodi != null) kaprodi.KunciAdegan(false);

        // Interaksi kursi dan laptop dinyalakan lagi, objek tampilan lama tidak dijaga lagi
        NyalakanInteraksiLain();
        selesaiSemua = true;
        JedaScriptLain(false);

        saatAdeganSelesai.Invoke();

        Debug.Log("[Adegan L4-S1] Scene 1 Level 4 selesai.");
    }
}