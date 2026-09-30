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
    public float realSecondsPerStep = 60f; // Berapa detik waktu nyata untuk 1 kali lompatan (60 detik = 1 menit)
    private float timer = 0f;

    // Menyimpan total langkah waktu yang sudah berjalan
    private int currentTimeStep = 0;

    void Start()
    {
        UpdateClockUI();
    }

    void Update()
    {
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
}