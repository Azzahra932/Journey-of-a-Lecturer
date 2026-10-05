using UnityEngine;

/// <summary>
/// Pasang di objek dengan Collider2D (Is Trigger).
/// Saat player masuk area ini:
///  - "panah" dimatikan
///  - "panahBerikutnya" (opsional) dinyalakan -> untuk rantai panah A -> B -> C
/// </summary>
public class PanahTujuanTrigger : MonoBehaviour
{
    [Header("Panah yang dimatikan saat player sampai")]
    public GameObject panah;

    [Header("Panah yang dinyalakan setelahnya (opsional)")]
    public GameObject panahBerikutnya;

    [Header("Pengaturan")]
    public string tagPlayer = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(tagPlayer)) return;

        // Hanya bereaksi kalau panah ini memang sedang tampil
        if (panah != null && !panah.activeSelf) return;

        if (panah != null) panah.SetActive(false);
        if (panahBerikutnya != null) panahBerikutnya.SetActive(true);

        Debug.Log("[PanahTujuanTrigger] Player sampai. Panah dimatikan" +
                  (panahBerikutnya != null ? ", panah berikutnya dinyalakan." : "."));
    }
}