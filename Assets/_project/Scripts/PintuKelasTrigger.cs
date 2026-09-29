using UnityEngine;

public class PintuKelasTrigger : MonoBehaviour
{
    [Header("Referensi Panah")]
    public GameObject panahKeKelas; // Drag objek panahKeKelas ke slot ini

    // Menyimpan status global apakah pemain sudah pernah sampai ke kelas
    public static bool sudahPernahKeKelas = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Cek apakah yang menyentuh area pintu adalah pemain (TPdiam atau ber-Tag Player)
        if (other.CompareTag("Player") || other.name == "TPdiam")
        {
            sudahPernahKeKelas = true; // Kunci status selamanya agar panah tidak bisa muncul lagi

            if (panahKeKelas != null)
            {
                panahKeKelas.SetActive(false); // Matikan panah saat pemain tiba di pintu
            }
        }
    }
}