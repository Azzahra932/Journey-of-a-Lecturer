using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Level 4 - Scene 6 (Evaluasi Menuju Guru Besar), Lokasi: Ruang Rektor
//   1. Scene sebelumnya selesai -> Scene 6 siap
//   2. MC duduk di kursi rektor. MC dikunci di kursi sampai tutorial selesai
//   3. Kaprodi otomatis dipanggil: muncul, berjalan ke kursi tamu (jalur yang sama dengan Scene 4), lalu duduk
//   4. Empat box dialog (Kaprodi, Pemain, Kaprodi, Pemain), Space untuk lanjut
//   5. Kaprodi berdiri dan berjalan keluar
//   6. Setelah Kaprodi keluar: box tutorial (satu atau beberapa box berurutan, Space untuk lanjut)
//   7. MC dibebaskan, Scene 6 selesai
//
// Kursi rektor boleh memakai script sendiri (KursiRektorInteraction), InteractiveDesk, atau KursiPerpusInteraction. Isi salah satu saja.
// Script ini dipasang di objek apa saja (tidak perlu collider).
// Cara menyambung: di scene sebelumnya (mis. AdeganLevel4Scene4), event "Saat Adegan Selesai",
// tambahkan objek ini dan pilih fungsi SiapkanScene.
// Untuk tes tanpa melewati scene sebelumnya: centang "Langsung Siap".
// Script Scene 4 dimatikan sejak Scene 6 siap (isi slot Script Scene 4), supaya ikon E dan cooldown Scene 4 tidak muncul lagi di kursi yang sama.
public class AdeganLevel4Scene6 : MonoBehaviour
{
    [Header("Syarat Mulai")]
    public bool langsungSiap = false;           // Centang untuk tes tanpa menyelesaikan scene sebelumnya

    [Header("Script Scene 4 (dimatikan sejak Scene 6 siap)")]
    public AdeganLevel4Scene4 scriptScene4;     // Script Scene 4 yang memakai kursi rektor yang sama. Kosong = tidak dimatikan

    [Header("Kursi Rektor (isi SALAH SATU sesuai komponen di kursinya)")]
    public KursiRektorInteraction kursiRektor;          // Kalau kursinya memakai script sendiri (KursiRektorInteraction)
    public InteractiveDesk kursiRektorDesk;             // Kalau kursinya memakai komponen InteractiveDesk
    public KursiPerpusInteraction kursiRektorPerpus;    // Kalau kursinya memakai komponen KursiPerpusInteraction
    public float jedaSetelahDuduk = 0.5f;               // Jeda (detik) setelah MC duduk sebelum Kaprodi muncul

    [Header("Kaprodi (boleh objek yang sama dengan Scene 4)")]
    public KaprodiJalan2Arah kaprodi;           // Objek induk Kaprodi (yang punya script KaprodiJalan2Arah)
    public Transform titikAwal;                 // Tempat Kaprodi muncul
    public Transform[] titikMasuk;              // Jalur ke kursi tamu, urut
    public Transform kursiTamu;                 // Objek kursi tamu (mis. kursikaprek). Setelah sampai di titik terakhir, Kaprodi langsung duduk di sini
    public Vector2 offsetDudukDiKursi = Vector2.zero;   // Geser posisi duduk dari pusat kursi tamu
    public Transform[] titikKeluar;             // Jalur keluar, urut. Titik terakhir = tempat Kaprodi menghilang
    public float jedaSetelahDudukKaprodi = 0.5f;

    [Header("Box Dialog (urut: Kaprodi, Pemain, Kaprodi, Pemain)")]
    public GameObject[] boxDialog;
    public KeyCode tombolLanjutBox = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaSebelumDialog = 0.3f;

    [Header("Box Tutorial (muncul setelah Kaprodi keluar, satu atau beberapa box berurutan)")]
    public GameObject overlayHitam;             // Opsional: panel hitam transparan di belakang box tutorial
    public GameObject[] boxTutorial;            // Isi Size sesuai jumlah box tutorial. Kosong = tanpa tutorial
    public bool hentikanWaktuSaatTutorial = true;

    [Header("Setelah Scene 6 Selesai")]
    public UnityEvent saatAdeganSelesai;        // Dipanggil sekali, setelah tutorial selesai dan MC dibebaskan

    private bool siap = false;
    private bool sudahJalan = false;
    private bool scene4Dimatikan = false;
    private bool peringatanKursi = false;
    private bool peringatanBelumSiap = false;
    private bool dudukSebelumnya = false;
    private float waktuMulaiDuduk = 0f;

    void Start()
    {
        siap = langsungSiap;

        SembunyikanSemua(boxDialog);
        SembunyikanSemua(boxTutorial);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        // Pengaman: script ini hanya boleh dipasang di SATU objek
        AdeganLevel4Scene6[] semua = FindObjectsByType<AdeganLevel4Scene6>(FindObjectsSortMode.None);
        if (semua.Length > 1)
            Debug.LogWarning("[Adegan L4-S6] Ada " + semua.Length + " objek yang memasang script AdeganLevel4Scene6. Pakai satu saja.");
    }

    // Dipanggil dari event "Saat Adegan Selesai" di scene sebelumnya
    public void SiapkanScene()
    {
        siap = true;
        MatikanScene4();
        Debug.Log("[Adegan L4-S6] Scene 6 siap, menunggu MC duduk di kursi rektor.");
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Adegan (langsung mulai)")]
    private void TesAdegan()
    {
        if (sudahJalan) return;
        siap = true;
        sudahJalan = true;
        MatikanScene4();
        StartCoroutine(Alur());
    }

    // Script Scene 4 dimatikan, dan tampilan sisanya (ikon E, baterai, layar redup) dibersihkan
    private void MatikanScene4()
    {
        if (scene4Dimatikan || scriptScene4 == null) return;
        scene4Dimatikan = true;

        scriptScene4.enabled = false;

        if (scriptScene4.ikonE != null) scriptScene4.ikonE.SetActive(false);
        if (scriptScene4.ikonBateraiLemah != null) scriptScene4.ikonBateraiLemah.SetActive(false);
        if (scriptScene4.teksCooldown != null) scriptScene4.teksCooldown.gameObject.SetActive(false);
        if (scriptScene4.teksIstirahat != null) scriptScene4.teksIstirahat.SetActive(false);
        if (scriptScene4.spriteLaptop != null) scriptScene4.spriteLaptop.color = scriptScene4.warnaNormal;

        Debug.Log("[Adegan L4-S6] Script Scene 4 dimatikan.");
    }

    void Update()
    {
        if (sudahJalan) return;

        // "Langsung Siap" dibaca di sini supaya bisa dicentang saat game sedang Play
        if (!siap && !langsungSiap)
        {
            if (!peringatanBelumSiap && SedangDudukDiKursiRektor())
            {
                peringatanBelumSiap = true;
                Debug.Log("[Adegan L4-S6] MC duduk di kursi rektor, tapi Scene 6 belum siap. Centang 'Langsung Siap' untuk tes, atau sambungkan event 'Saat Adegan Selesai' scene sebelumnya ke SiapkanScene.");
            }
            return;
        }

        if (kursiRektor == null && kursiRektorDesk == null && kursiRektorPerpus == null)
        {
            if (!peringatanKursi)
            {
                peringatanKursi = true;
                Debug.LogWarning("[Adegan L4-S6] Kursi Rektor belum diisi di Inspector (isi salah satu), adegan tidak bisa mulai.");
            }
            return;
        }

        MatikanScene4();

        bool duduk = SedangDudukDiKursiRektor();
        if (duduk && !dudukSebelumnya) waktuMulaiDuduk = Time.time;
        dudukSebelumnya = duduk;

        // MC duduk di kursi rektor: Kaprodi otomatis dipanggil
        if (duduk && Time.time - waktuMulaiDuduk >= jedaSetelahDuduk)
        {
            sudahJalan = true;
            Debug.Log("[Adegan L4-S6] MC duduk di kursi rektor, adegan dimulai.");
            StartCoroutine(Alur());
        }
    }

    private bool SedangDudukDiKursiRektor()
    {
        bool d = false;
        if (kursiRektor != null) d = kursiRektor.SedangDuduk;
        if (kursiRektorDesk != null) d = d || kursiRektorDesk.SedangDuduk;
        if (kursiRektorPerpus != null) d = d || kursiRektorPerpus.sedangDuduk;
        return d;
    }

    private void KunciKursi(bool kunci)
    {
        if (kursiRektor != null) kursiRektor.TerkunciEksternal = kunci;
        if (kursiRektorDesk != null) kursiRektorDesk.TerkunciEksternal = kunci;
        if (kursiRektorPerpus != null) kursiRektorPerpus.TerkunciEksternal = kunci;
    }

    private void SembunyikanSemua(GameObject[] daftar)
    {
        if (daftar == null) return;

        foreach (GameObject b in daftar)
        {
            if (b != null) b.SetActive(false);
        }
    }

    // ---------- Alur lengkap ----------
    private IEnumerator Alur()
    {
        // MC dikunci di kursi sampai tutorial selesai
        KunciKursi(true);

        // 1. Kaprodi datang dan duduk
        if (kaprodi == null)
        {
            Debug.LogWarning("[Adegan L4-S6] Kaprodi belum diisi di Inspector, bagian Kaprodi dilewati.");
        }
        else
        {
            if (titikAwal != null) kaprodi.transform.position = titikAwal.position;
            kaprodi.gameObject.SetActive(true);
            kaprodi.Berdiri();   // reset tampilan (kalau sebelumnya dipakai di Scene 4)

            if (titikMasuk == null || titikMasuk.Length == 0)
                Debug.LogWarning("[Adegan L4-S6] Titik Masuk masih kosong di Inspector, Kaprodi langsung duduk di tempat.");
            else
                yield return StartCoroutine(kaprodi.JalanMelalui(titikMasuk));

            // Sampai di titik terakhir: langsung duduk, gambar duduk dipasang tepat di posisi kursi tamu
            if (kursiTamu != null)
            {
                kaprodi.DudukDi((Vector2)kursiTamu.position + offsetDudukDiKursi);
            }
            else
            {
                Debug.LogWarning("[Adegan L4-S6] Kursi Tamu belum diisi di Inspector, Kaprodi duduk di titik terakhir Titik Masuk.");
                kaprodi.Duduk();
            }

            yield return new WaitForSeconds(jedaSetelahDudukKaprodi);
        }

        // 2. Empat box dialog
        yield return new WaitForSeconds(jedaSebelumDialog);

        if (boxDialog == null || boxDialog.Length == 0)
            Debug.LogWarning("[Adegan L4-S6] Box Dialog masih kosong di Inspector, dialog dilewati.");
        else
            yield return StartCoroutine(TampilkanBoxBerurutan(boxDialog, false));

        // 3. Kaprodi berdiri dan berjalan keluar
        yield return StartCoroutine(KaprodiKeluar());

        // 4. Box tutorial: muncul setelah Kaprodi keluar
        yield return StartCoroutine(TampilkanTutorial());

        // 5. MC dibebaskan, scene selesai
        KunciKursi(false);

        saatAdeganSelesai.Invoke();

        Debug.Log("[Adegan L4-S6] Scene 6 (Evaluasi Menuju Guru Besar) selesai.");
    }

    // Kaprodi berdiri, berjalan ke titik keluar, lalu menghilang
    private IEnumerator KaprodiKeluar()
    {
        if (kaprodi == null) yield break;

        kaprodi.Berdiri();

        if (titikKeluar == null || titikKeluar.Length == 0)
            Debug.LogWarning("[Adegan L4-S6] Titik Keluar masih kosong di Inspector, Kaprodi langsung menghilang.");
        else
            yield return StartCoroutine(kaprodi.JalanMelalui(titikKeluar));

        kaprodi.gameObject.SetActive(false);
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
                Debug.LogWarning("[Adegan L4-S6] Box '" + box.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil. Aktifkan induknya di Hierarchy.");

            // Jeda singkat (waktu nyata) supaya tombol yang masih tertekan tidak langsung menutup box
            yield return new WaitForSecondsRealtime(0.25f);

            while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;

            box.SetActive(false);
        }
    }
}