using UnityEngine;

// Setelah Scene 5 selesai, panah-panah petunjuk arah yang terdaftar di sini tidak boleh muncul lagi,
// siapa pun yang menyalakannya (LaptopKelas, PanahTujuanTrigger, dll).
public class PenjagaPanahSetelahScene5 : MonoBehaviour
{
    [Header("Panah yang tidak boleh muncul lagi setelah Scene 5")]
    public GameObject[] panahDimatikan;

    private bool aktif = false;

    // Dipasang di event "Saat Dialog Selesai ()" di akhir Scene 5
    public void MulaiPenjagaan()
    {
        aktif = true;
        MatikanSemua();
        Debug.Log("[Panah] Penjagaan aktif: panah petunjuk dimatikan setelah Scene 5.");
    }

    // LateUpdate jalan setelah semua Update, jadi panah yang baru dinyalakan langsung dimatikan
    // sebelum sempat tampil di layar.
    void LateUpdate()
    {
        if (aktif) MatikanSemua();
    }

    private void MatikanSemua()
    {
        if (panahDimatikan == null) return;

        foreach (GameObject p in panahDimatikan)
        {
            if (p != null && p.activeSelf) p.SetActive(false);
        }
    }
}