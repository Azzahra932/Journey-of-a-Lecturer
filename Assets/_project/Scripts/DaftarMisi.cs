using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Daftar misi satu level. Pasang SATU script per level (jadi 5 buah) pada objek kosong
/// yang SELALU aktif (jangan di panelnya sendiri, karena panel sering dimatikan).
///
/// Cara pakai:
///  - Panel Misi      : panel gambar misi level ini (yang dibuka/ditutup)
///  - Centang         : 5 objek centang (sprite CHECK BOX) yang diletakkan di tiap kotak, dimatikan dulu
///  - Tombol Buka     : iconlistmisi. Klik = buka/tutup panel (panel tidak muncul sendiri saat misi tercentang)
///  - Tombol Tutup    : tombol X di panel (opsional)
///  - Scene Selesai Misi : untuk tiap misi, nomor scene yang kalau selesai misi itu tercentang
///                         (isi untuk pemulihan lewat Continue)
///  - Panggil CentangMisi(1..5) dari event "Saat Adegan Selesai", di bawah SimpanTitik.
/// </summary>
public class DaftarMisi : MonoBehaviour
{
    [Header("Level")]
    public int level = 2;

    [Header("Tampilan")]
    public GameObject panelMisi;
    public Button tombolBuka;
    public Button tombolTutup;

    [Header("Centang (urut Misi 1 sampai 5)")]
    public GameObject[] centang;

    [Header("Untuk Continue")]
    [Tooltip("Untuk tiap misi: nomor scene yang kalau SELESAI membuat misi tercentang. Panjang sama dengan Centang.")]
    public int[] sceneSelesaiMisi;

    [Header("Pop-up Otomatis Saat Tercentang (default mati)")]
    public bool bukaOtomatisSaatCentang = false;
    public float lamaTampilOtomatis = 2.5f;

    private bool[] sudahCentang;
    private Coroutine otomatis;

    private void Awake()
    {
        sudahCentang = new bool[centang != null ? centang.Length : 0];
        TerapkanSemua();
        if (panelMisi != null) panelMisi.SetActive(false);
    }

    private void Start()
    {
        if (tombolBuka != null) tombolBuka.onClick.AddListener(Toggle);
        if (tombolTutup != null) tombolTutup.onClick.AddListener(Tutup);
    }

    // ---------- Panel ----------
    public void Buka() { if (panelMisi != null) panelMisi.SetActive(true); }
    public void Tutup() { if (panelMisi != null) panelMisi.SetActive(false); }
    // Level yang sedang berjalan. Hanya DaftarMisi dengan level ini yang membuka panel saat ikon diklik.
    public static int levelAktif = 1;

    // Panggil dari event "masuk level N" (mis. popup level selesai -> Saat Masuk) dengan nomor level baru
    public void AturLevelAktif(int levelBaru) { levelAktif = levelBaru; }

    public void Toggle()
    {
        if (panelMisi == null) return;
        if (level != levelAktif)
        {
            panelMisi.SetActive(false);
            return;
        }
        panelMisi.SetActive(!panelMisi.activeSelf);
    }

    // ---------- Centang ----------
    /// <summary>Nomor misi 1 sampai 5. Panggil dari UnityEvent.</summary>
    public void CentangMisi(int nomor)
    {
        int i = nomor - 1;
        if (i < 0 || i >= sudahCentang.Length) { Debug.LogWarning("[DaftarMisi] Nomor misi tidak valid: " + nomor); return; }
        if (sudahCentang[i]) return;

        sudahCentang[i] = true;
        if (centang[i] != null) centang[i].SetActive(true);
        Debug.Log("[DaftarMisi] Level " + level + " misi " + nomor + " dicentang");

        if (bukaOtomatisSaatCentang && panelMisi != null)
        {
            if (otomatis != null) StopCoroutine(otomatis);
            otomatis = StartCoroutine(TampilSebentar());
        }
    }

    private System.Collections.IEnumerator TampilSebentar()
    {
        Buka();
        yield return new WaitForSecondsRealtime(lamaTampilOtomatis);
        Tutup();
        otomatis = null;
    }

    /// <summary>Hapus semua centang (misalnya saat game baru).</summary>
    public void ResetSemua()
    {
        for (int i = 0; i < sudahCentang.Length; i++) sudahCentang[i] = false;
        TerapkanSemua();
    }

    /// <summary>
    /// Dipanggil saat Continue. sceneMulai = scene yang sedang DIMULAI dari checkpoint
    /// (mis. Level 3 Scene 2 -> 2). Misi tercentang kalau scene selesainya lebih kecil dari itu.
    /// Tidak menampilkan pop-up otomatis.
    /// </summary>
    public void PulihkanDariScene(int sceneMulai)
    {
        if (sceneSelesaiMisi == null) return;
        for (int i = 0; i < sudahCentang.Length && i < sceneSelesaiMisi.Length; i++)
            sudahCentang[i] = sceneSelesaiMisi[i] > 0 && sceneSelesaiMisi[i] < sceneMulai;
        TerapkanSemua();
    }

    /// <summary>Pulihkan untuk level yang sudah lewat (semua misi dicentang).</summary>
    public void CentangSemuaTanpaPopup()
    {
        for (int i = 0; i < sudahCentang.Length; i++) sudahCentang[i] = true;
        TerapkanSemua();
    }

    private void TerapkanSemua()
    {
        if (centang == null) return;
        for (int i = 0; i < centang.Length; i++)
            if (centang[i] != null) centang[i].SetActive(i < sudahCentang.Length && sudahCentang[i]);
    }
}