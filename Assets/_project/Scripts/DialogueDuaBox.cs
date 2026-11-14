using UnityEngine;

public class DialogueDuaBox : MonoBehaviour
{
    [Header("Box Dosen (teks sudah ada di dalam box)")]
    public GameObject boxDosen;

    [Header("Box Mahasiswa (teks sudah ada di dalam box)")]
    public GameObject boxMahasiswa;

    private int tahap = 0;
    private int frameAktif = -1;   // frame saat dialog ini muncul (Spasi untuk duduk tidak ikut melewati dialog)

    void OnEnable()
    {
        tahap = 0;
        frameAktif = Time.frameCount;
        TampilkanDialog();
    }

    void Update()
    {
        // Spasi yang menyalakan dialog ini (duduk di kursi) tidak boleh ikut dihitung untuk lanjut
        if (Time.frameCount == frameAktif) return;

        // Hanya pakai Spasi untuk lanjut
        if (Input.GetKeyDown(KeyCode.Space))
        {
            tahap++;
            TampilkanDialog();
        }
    }

    void TampilkanDialog()
    {
        if (tahap == 0)
        {
            // Tahap 1: Munculkan Dosen, Matikan Mahasiswa
            if (boxDosen != null) boxDosen.SetActive(true);
            if (boxMahasiswa != null) boxMahasiswa.SetActive(false);
        }
        else if (tahap == 1)
        {
            // Tahap 2: Matikan Dosen, Munculkan Mahasiswa
            if (boxDosen != null) boxDosen.SetActive(false);
            if (boxMahasiswa != null) boxMahasiswa.SetActive(true);
        }
        else
        {
            // Tahap Akhir: Tutup semuanya setelah spasi ditekan lagi di mahasiswa
            if (boxDosen != null) boxDosen.SetActive(false);
            if (boxMahasiswa != null) boxMahasiswa.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}