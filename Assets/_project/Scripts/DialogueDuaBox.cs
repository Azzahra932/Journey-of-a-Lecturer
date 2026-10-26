using UnityEngine;

public class DialogueDuaBox : MonoBehaviour
{
    [Header("Box Dosen (teks sudah ada di dalam box)")]
    public GameObject boxDosen;

    [Header("Box Mahasiswa (teks sudah ada di dalam box)")]
    public GameObject boxMahasiswa;

    private int tahap = 0;

    void OnEnable()
    {
        tahap = 0;
        TampilkanDialog();
    }

    void Update()
    {
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