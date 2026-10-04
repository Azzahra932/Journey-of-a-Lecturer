using UnityEngine;
using TMPro;

public class DialogueDuaBox : MonoBehaviour
{
    [Header("Box Dosen")]
    public GameObject boxDosen;
    public TextMeshProUGUI textDosen;
    [TextArea(2, 4)] public string kalimatDosen;

    [Header("Box Mahasiswa")]
    public GameObject boxMahasiswa;
    public TextMeshProUGUI textMahasiswa;
    [TextArea(2, 4)] public string kalimatMahasiswa;

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
            if (textDosen != null) textDosen.text = kalimatDosen;
        }
        else if (tahap == 1)
        {
            // Tahap 2: Matikan Dosen, Munculkan Mahasiswa
            if (boxDosen != null) boxDosen.SetActive(false);
            if (boxMahasiswa != null) boxMahasiswa.SetActive(true);
            if (textMahasiswa != null) textMahasiswa.text = kalimatMahasiswa;
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