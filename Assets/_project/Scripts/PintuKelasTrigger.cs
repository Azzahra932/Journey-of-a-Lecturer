using UnityEngine;

public class PintuKelasTrigger : MonoBehaviour
{
    [Header("Referensi Panah")]
    public GameObject panahKeKelas; // Drag objek panahKeKelas ke slot ini
    public GameObject panahKeKursi; // Drag objek panah kursi di dalam kelas ke slot ini (Baru)

    // Menyimpan status global apakah pemain sudah pernah sampai ke kelas
    public static bool sudahPernahKeKelas = false;

    // Tambahkan Awake atau OnEnable untuk mereset status saat game baru dimainkan (Play)
    private void Awake()
    {
        sudahPernahKeKelas = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Cek apakah yang menyentuh area pintu adalah pemain (TPdiam atau ber-Tag Player)
        if (other.CompareTag("Player") || other.name == "TPdiam")
        {
            sudahPernahKeKelas = true; // Kunci status selamanya selama sesi permainan ini

            // Matikan panah penunjuk pintu saat pemain tiba di pintu
            if (panahKeKelas != null)
            {
                panahKeKelas.SetActive(false);
            }

            // Nyalakan panah penunjuk kursi di dalam kelas
            if (panahKeKursi != null)
            {
                panahKeKursi.SetActive(true);
            }
        }
    }
}