using UnityEngine;

// Sistem simpan sederhana untuk tombol Continue di menu utama (disimpan ke PlayerPrefs, jadi bertahan walau game ditutup).
//
// Cara kerja (checkpoint di AWAL tiap level dan di AWAL tiap scene / adegan):
//   - Awal level: pop-up level selesai memanggil SimpanCheckpoint(levelBerikutnya), lewat PenyimpanCheckpoint
//   - Awal scene: event "Saat Adegan Selesai" tiap scene memanggil SimpanTitikAdegan("L3S4") untuk scene berikutnya, lewat PenyimpanCheckpoint
//     (format id: L + nomor level + S + nomor scene, mis. "L3S4" = Level 3 Scene 4)
//   - Yang disimpan: id titik, level, skor, target, mode tanpa target, integritas, hari ke berapa, jam game, dan pilihan MC di Scene 3 Level 5 (energi tidak disimpan: saat Continue energi penuh)
//   - Continue memuat scene gameplay, lalu PemulihProgres (di scene gameplay) memulihkan keadaan dan memulai scene itu dari awal
//   - Progres di tengah scene TIDAK disimpan: Continue selalu mulai dari awal scene terakhir yang sudah dimulai
//
// Script ini static, jadi tidak dipasang di objek mana pun.
public static class ProgresPermainan
{
    private const string KunciAda = "progres_ada";
    private const string KunciLevel = "progres_level";
    private const string KunciSkor = "progres_skor";
    private const string KunciTarget = "progres_target";
    private const string KunciTanpaTarget = "progres_tanpa_target";
    private const string KunciIntegritas = "progres_integritas";
    private const string KunciHari = "progres_hari";
    private const string KunciTitik = "progres_titik";
    private const string KunciLangkahWaktu = "progres_langkah_waktu";
    private const string KunciPilihanL5S3 = "progres_pilihan_l5s3";

    // True selama Continue dipilih dan scene gameplay belum selesai memulihkan progres (hanya di memori, tidak disimpan)
    public static bool LanjutDipilih = false;

    // Level pada checkpoint terakhir (1 sampai 5)
    public static int LevelTersimpan { get { return PlayerPrefs.GetInt(KunciLevel, 1); } }

    public static int HariTersimpan { get { return PlayerPrefs.GetInt(KunciHari, 1); } }

    // Jam game saat checkpoint, dalam jumlah lompatan waktu sejak 08.00 (GameTimeManager.LangkahWaktu)
    public static int LangkahWaktuTersimpan { get { return PlayerPrefs.GetInt(KunciLangkahWaktu, 0); } }

    // Titik scene terakhir yang dimulai, mis. "L3S4" (kosong = belum ada)
    public static string TitikTersimpan { get { return PlayerPrefs.GetString(KunciTitik, ""); } }

    // Pilihan MC di Scene 3 Level 5 (tawaran Dewan Profesor): 0 = belum memilih, 1 = menerima (Ya), 2 = menolak (Tidak)
    public static int PilihanScene3Tersimpan { get { return PlayerPrefs.GetInt(KunciPilihanL5S3, 0); } }

    public static void SimpanPilihanScene3(int nilai)
    {
        PlayerPrefs.SetInt(KunciPilihanL5S3, nilai);
        PlayerPrefs.Save();
    }

    // Membaca id titik seperti "L3S4" menjadi level 3 dan scene 4. False kalau formatnya salah
    public static bool UraiTitik(string id, out int level, out int scene)
    {
        level = 0;
        scene = 0;
        if (string.IsNullOrEmpty(id)) return false;

        string t = id.Trim().ToUpperInvariant();
        int posS = t.IndexOf('S');
        if (t.Length < 4 || t[0] != 'L' || posS < 2) return false;

        return int.TryParse(t.Substring(1, posS - 1), out level) && int.TryParse(t.Substring(posS + 1), out scene);
    }

    // Ada progres yang bisa dilanjutkan? (butuh minimal satu checkpoint)
    public static bool AdaSave()
    {
        return PlayerPrefs.GetInt(KunciAda, 0) == 1 && LevelTersimpan >= 1;
    }

    // Menyimpan checkpoint di awal level (scene 1 level itu): dipanggil lewat PenyimpanCheckpoint dari pop-up level selesai
    public static void SimpanCheckpoint(int levelDimulai)
    {
        SimpanSemua(levelDimulai, "L" + levelDimulai + "S1");
    }

    // Menyimpan checkpoint di awal sebuah scene / adegan. Id berformat "L3S4" (Level 3 Scene 4)
    public static void SimpanTitikAdegan(string id)
    {
        int level, scene;
        if (!UraiTitik(id, out level, out scene))
        {
            Debug.LogWarning("[Progres] Id titik '" + id + "' tidak valid. Pakai format L + level + S + scene, mis. L3S4.");
            return;
        }

        SimpanSemua(level, id.Trim().ToUpperInvariant());
    }

    private static void SimpanSemua(int level, string titik)
    {
        ScoreManager skor = ScoreManager.Instance;
        IntegritasManager integritas = IntegritasManager.Instance;
        GameTimeManager waktu = Object.FindFirstObjectByType<GameTimeManager>();

        PlayerPrefs.SetInt(KunciAda, 1);
        PlayerPrefs.SetInt(KunciLevel, level);
        PlayerPrefs.SetString(KunciTitik, titik);

        if (skor != null)
        {
            PlayerPrefs.SetInt(KunciSkor, skor.currentScore);
            PlayerPrefs.SetInt(KunciTarget, skor.targetScore);
            PlayerPrefs.SetInt(KunciTanpaTarget, skor.TanpaTarget ? 1 : 0);
        }

        if (integritas != null)
            PlayerPrefs.SetFloat(KunciIntegritas, integritas.CurrentIntegritas);

        PlayerPrefs.SetInt(KunciHari, waktu != null ? waktu.HariKe : 1);
        PlayerPrefs.SetInt(KunciLangkahWaktu, waktu != null ? waktu.LangkahWaktu : 0);

        PlayerPrefs.Save();

        Debug.Log("[Progres] Checkpoint disimpan: " + titik
            + ", skor " + PlayerPrefs.GetInt(KunciSkor, 0)
            + ", integritas " + PlayerPrefs.GetFloat(KunciIntegritas, 100f)
            + ", hari " + PlayerPrefs.GetInt(KunciHari, 1)
            + ", jam langkah " + PlayerPrefs.GetInt(KunciLangkahWaktu, 0) + ".");
    }

    // Dipanggil menu utama saat Continue: skor dan integritas yang tersimpan dipasang ke ScoreManager dan IntegritasManager
    // (dibaca saat scene gameplay dimuat), lalu penanda Continue dinyalakan
    public static void SiapkanContinue()
    {
        ScoreManager.TerapkanProgresTersimpan(
            PlayerPrefs.GetInt(KunciSkor, 0),
            PlayerPrefs.GetInt(KunciTarget, 150),
            PlayerPrefs.GetInt(KunciTanpaTarget, 0) == 1);

        IntegritasManager.TerapkanProgresTersimpan(PlayerPrefs.GetFloat(KunciIntegritas, 100f));

        LanjutDipilih = true;

        Debug.Log("[Progres] Continue disiapkan: titik " + TitikTersimpan + " (Level " + LevelTersimpan + ").");
    }

    // Dipanggil menu utama saat Play (permainan baru): semua data di memori dikembalikan ke awal.
    // Data simpanan di PlayerPrefs sengaja tidak dihapus, jadi Continue tetap bisa dipakai sampai ada checkpoint baru yang menimpanya.
    public static void MulaiBaru()
    {
        ScoreManager.ResetProgres();
        IntegritasManager.ResetProgres();
        LanjutDipilih = false;
    }

    // Menghapus progres tersimpan (mis. saat permainan tamat)
    public static void Hapus()
    {
        PlayerPrefs.DeleteKey(KunciAda);
        PlayerPrefs.DeleteKey(KunciLevel);
        PlayerPrefs.DeleteKey(KunciSkor);
        PlayerPrefs.DeleteKey(KunciTarget);
        PlayerPrefs.DeleteKey(KunciTanpaTarget);
        PlayerPrefs.DeleteKey(KunciIntegritas);
        PlayerPrefs.DeleteKey(KunciHari);
        PlayerPrefs.DeleteKey(KunciTitik);
        PlayerPrefs.DeleteKey(KunciLangkahWaktu);
        PlayerPrefs.DeleteKey(KunciPilihanL5S3);
        PlayerPrefs.Save();

        LanjutDipilih = false;

        Debug.Log("[Progres] Progres tersimpan dihapus.");
    }
}