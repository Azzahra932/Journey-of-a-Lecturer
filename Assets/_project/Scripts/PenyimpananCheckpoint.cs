using UnityEngine;

// Jembatan antara event di Inspector dan ProgresPermainan (yang static, jadi tidak bisa dipilih langsung di event).
// Pasang script ini di satu objek kosong di scene gameplay (mis. "PenyimpanCheckpoint").
//
// Cara menyambung (tanpa mengubah script adegan atau pop-up):
//   A. Awal level: di tiap pop-up level selesai, event "Saat Masuk Level Berikutnya": klik +, drag objek ini, pilih
//      PenyimpanCheckpoint > SimpanAwalLevel (isi angka level yang baru dimulai):
//        pop-up Level 1 selesai -> 2,   pop-up Level 2 selesai -> 3,   pop-up Level 3 selesai -> 4,   pop-up Level 4 selesai -> 5
//   B. Awal scene / adegan: di event "Saat Adegan Selesai" tiap scene (yang menyambung ke scene berikutnya), klik +, drag objek ini, pilih
//      PenyimpanCheckpoint > SimpanTitik (isi id scene BERIKUTNYA, format L + level + S + scene):
//        mis. di Saat Adegan Selesai Scene 3 Level 3, isi "L3S4"
//   Taruh panggilan ini PALING BAWAH di daftar event, setelah semua fungsi lain (mis. SiapkanScene scene berikutnya),
//   supaya keadaan yang disimpan sudah lengkap.
//   C. Saat permainan tamat (mis. event tombol di pop-up akhir Level 5): pilih PenyimpanCheckpoint > HapusProgres.
public class PenyimpanCheckpoint : MonoBehaviour
{
    [Tooltip("Scene 3 Level 5 (tawaran Dewan Profesor). Pilihan MC di sana ikut disimpan supaya dialog Scene 4 dan penutup Level 5 tetap benar setelah Continue. Kosong = dicari otomatis.")]
    public AdeganLevel5Scene3 adeganScene3;

    // Menyimpan checkpoint di awal level yang baru dimulai (isi angka level di event Inspector: 2, 3, 4, atau 5)
    public void SimpanAwalLevel(int levelDimulai)
    {
        CatatPilihanScene3();
        ProgresPermainan.SimpanCheckpoint(levelDimulai);
    }

    // Menyimpan checkpoint di awal sebuah scene. Isi id di event Inspector, format L + level + S + scene (mis. L3S4)
    public void SimpanTitik(string idTitik)
    {
        CatatPilihanScene3();
        ProgresPermainan.SimpanTitikAdegan(idTitik);
    }

    // Menghapus data simpanan (Continue tidak aktif lagi)
    public void HapusProgres()
    {
        ProgresPermainan.Hapus();
    }

    // Pilihan MC di Scene 3 Level 5 (kalau sudah memilih) disimpan ikut checkpoint
    private void CatatPilihanScene3()
    {
        if (adeganScene3 == null) adeganScene3 = FindFirstObjectByType<AdeganLevel5Scene3>(FindObjectsInactive.Include);
        if (adeganScene3 == null || !adeganScene3.SudahMemilih) return;

        ProgresPermainan.SimpanPilihanScene3(adeganScene3.MenerimaTawaran ? 1 : 2);
    }
}