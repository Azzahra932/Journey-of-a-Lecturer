using UnityEngine;
using TMPro;

public class GameTimeManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI timeText;

    [Header("Pengaturan Waktu")]
    private int startHour = 8;         // Jam mulai (08.00)
    private int startMinute = 0;       // Menit mulai
    private int intervalMinutes = 90;  // Jarak per lompatan waktu (90 menit)

    [Header("Pengaturan Otomatis")]
    public float realSecondsPerStep = 3f; // Berapa detik waktu nyata untuk 1 kali lompatan (60 detik = 1 menit)
    private float timer = 0f;

    // ===== TAMBAHAN BARU: Tidur malam =====
    [Header("Tidur Malam (Penutup Hari)")]
    public TransisiTidurMalam transisiMalam;   // drag objek ManagerTidurMalam (kosong = dicari otomatis)
    public int jamTidur = 20;                  // jam tidur (20.00 -> jam 20, menit 0)
    public int menitTidur = 0;

    public int HariKe { get; private set; } = 1;       // hari ke berapa (bertambah tiap tidur malam)
    public bool WaktuBerhenti { get; set; }            // true selama transisi tidur: jam tidak berjalan

    private bool sudahDipicuHariIni = false;
    // ======================================

    // ===== TAMBAHAN BARU: Level terakhir (tanpa tidur malam) =====
    // Di level terakhir, jam 20.00 TIDAK memicu transisi tidur. Jam berhenti dan SudahJamPenutup jadi true,
    // lalu pop-up penutup (PopupLevel5Selesai) yang muncul.
    public bool LevelTerakhirAktif { get; private set; }   // true setelah AktifkanModeLevelTerakhir() dipanggil
    public bool SudahJamPenutup { get; private set; }      // true saat jam level terakhir mencapai jam tidur (20.00)
    // =============================================================

    // Menyimpan total langkah waktu yang sudah berjalan
    private int currentTimeStep = 0;

    void Start()
    {
        if (transisiMalam == null) transisiMalam = FindFirstObjectByType<TransisiTidurMalam>();

        UpdateClockUI();
    }

    void Update()
    {
        // TAMBAHAN BARU: jam berhenti selama transisi tidur, dan setelah jam penutup level terakhir tercapai
        if (WaktuBerhenti || SudahJamPenutup) return;

        // Menghitung waktu berjalan secara otomatis
        timer += Time.deltaTime;

        if (timer >= realSecondsPerStep)
        {
            timer = 0f;
            AdvanceTime();
        }
    }

    // Panggil fungsi ini jika ingin memajukan waktu secara manual (misal lewat interaksi/dialog)
    public void AdvanceTime()
    {
        currentTimeStep++;
        UpdateClockUI();

        // TAMBAHAN BARU: cek apakah sudah waktunya tidur
        CekWaktuTidur();
    }

    void UpdateClockUI()
    {
        // Hitung total menit dari awal jam 08:00
        int totalMinutes = (startHour * 60) + startMinute + (currentTimeStep * intervalMinutes);

        // Ubah kembali ke format Jam dan Menit (Format 24 jam)
        int hour = (totalMinutes / 60) % 24;
        int minute = totalMinutes % 60;

        // Ubah ke format teks
        if (timeText != null)
        {
            timeText.text = $"{hour:D2}.{minute:D2}";
        }
    }

    // ===== TAMBAHAN BARU =====

    // Dipanggil saat level terakhir dimulai (dari PopupLevel4Selesai): jam 20.00 tidak lagi memicu tidur malam
    public void AktifkanModeLevelTerakhir()
    {
        LevelTerakhirAktif = true;
        SudahJamPenutup = false;
        Debug.Log("[GameTimeManager] Mode level terakhir aktif: jam " + jamTidur.ToString("D2") + "." + menitTidur.ToString("D2") + " memunculkan pop-up penutup, bukan tidur malam.");
    }

    // Dipakai untuk mengecek waktu tidur: jam sekarang >= jam tidur (>= supaya tidak terlewat bila lompatan jam tidak pas 19.30)
    private void CekWaktuTidur()
    {
        if (sudahDipicuHariIni) return;

        int totalMinutes = (startHour * 60) + startMinute + (currentTimeStep * intervalMinutes);
        int batasTidur = (jamTidur * 60) + menitTidur;

        if (totalMinutes < batasTidur) return;

        // Level terakhir: tidak tidur. Hari berakhir, jam berhenti, pop-up penutup yang muncul
        if (LevelTerakhirAktif)
        {
            sudahDipicuHariIni = true;
            SudahJamPenutup = true;
            Debug.Log("[GameTimeManager] Level terakhir: jam penutup tercapai, pop-up penutup akan muncul.");
            return;
        }

        if (transisiMalam == null) return;

        sudahDipicuHariIni = true;
        Debug.Log("[GameTimeManager] Sudah waktunya tidur, memulai transisi malam.");
        transisiMalam.MulaiTidur();
    }

    // Dipanggil TransisiTidurMalam saat pagi tiba: jam kembali 08.00. Data permainan lain TIDAK disentuh.
    public void ResetKeJamPagi()
    {
        currentTimeStep = 0;
        timer = 0f;
        HariKe++;
        sudahDipicuHariIni = false;
        SudahJamPenutup = false;
        UpdateClockUI();
    }

    // TAMBAHAN BARU (Continue): memulihkan hari ke berapa dari data simpanan
    public void AturHariKe(int hari)
    {
        HariKe = Mathf.Max(1, hari);
        Debug.Log("[GameTimeManager] Hari diatur ke " + HariKe + ".");
    }

    // TAMBAHAN BARU (Continue): jam game disimpan sebagai jumlah lompatan waktu sejak 08.00 (0 = 08.00, 1 = 09.30, dst)
    public int LangkahWaktu { get { return currentTimeStep; } }

    // TAMBAHAN BARU (Continue): jam dipulihkan ke langkah tersimpan, jadi jam lanjut dari waktu terakhir (tidak kembali 08.00)
    public void AturLangkahWaktu(int langkah)
    {
        currentTimeStep = Mathf.Max(0, langkah);
        timer = 0f;
        sudahDipicuHariIni = false;
        SudahJamPenutup = false;
        UpdateClockUI();
        Debug.Log("[GameTimeManager] Jam dipulihkan ke langkah " + currentTimeStep + ".");
    }
}