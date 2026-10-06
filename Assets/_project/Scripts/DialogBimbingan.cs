using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Scene 3 - Dialog Bimbingan (2 box: Mahasiswa & Pemain).
/// Dialog muncul OTOMATIS begitu mahasiswa duduk, lanjut dengan Space, selesai setelah baris terakhir.
/// Pasang di objek yang sama dengan NPCBimbingan (ManagerBimbingan).
/// Hubungkan lewat NPCBimbingan > Saat Mahasiswa Duduk () > DialogBimbingan.Mulai
/// </summary>
public class DialogBimbingan : MonoBehaviour
{
    [System.Serializable]
    public class BarisDialog
    {
        public bool pembicaraMahasiswa = true;   // true = box mahasiswa, false = box pemain
        [TextArea(2, 4)] public string teks;
    }

    [Header("Box Dialog Mahasiswa")]
    public GameObject boxMahasiswa;
    public TMP_Text teksMahasiswa;

    [Header("Box Dialog Pemain")]
    public GameObject boxPemain;
    public TMP_Text teksPemain;

    [Header("Isi Dialog (urut dari atas)")]
    public BarisDialog[] baris = new BarisDialog[]
    {
        new BarisDialog { pembicaraMahasiswa = true,  teks = "Permisi, Pak/Bu. Saya ingin meminta arahan mengenai tugas kuliah saya." },
        new BarisDialog { pembicaraMahasiswa = false, teks = "Tentu. Coba jelaskan bagian mana yang masih mengalami kesulitan." }
    };

    [Header("Lanjut Dialog")]
    public KeyCode tombolLanjut = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaTekanPertama = 0.3f;    // cegah Space yang masih "nyangkut" langsung melewati baris pertama

    [Header("Hadiah Setelah Dialog Selesai")]
    public bool beriPoin = true;
    public int poinReward = 5;

    [Header("Bar Memberi Arahan (Opsional, default mati)")]
    public bool pakaiBarSetelahDialog = true;
    public bool butuhTekanTombol = false;    // true = tekan tombol dulu baru bar jalan, false = bar langsung jalan otomatis
    public KeyCode tombolAksi = KeyCode.F;   // jangan E (dipakai laptop dosen)
    public GameObject promptTombol;
    public GameObject canvasBar;
    public Image fillBar;
    public float durasiBar = 3f;

    [Header("Setelah Selesai")]
    public GameObject mahasiswa;             // dinonaktifkan setelah selesai (mahasiswa pergi)
    public float jedaSebelumPergi = 0.8f;

    private bool sedangBerjalan = false;
    private bool sudahSelesai = false;

    void Start()
    {
        SembunyikanSemua();
    }

    // Dipanggil dari NPCBimbingan.saatMahasiswaDuduk
    public void Mulai()
    {
        Debug.Log("[DialogBimbingan] Mulai() dipanggil. sedangBerjalan=" + sedangBerjalan + ", sudahSelesai=" + sudahSelesai);

        if (sedangBerjalan || sudahSelesai) return;
        StartCoroutine(AlurBimbingan());
    }

    private bool TombolLanjutDitekan()
    {
        return Input.GetKeyDown(tombolLanjut) || (bolehKlikMouse && Input.GetMouseButtonDown(0));
    }

    private IEnumerator AlurBimbingan()
    {
        sedangBerjalan = true;

        // ---------- 1. Dialog (otomatis muncul, lanjut dengan Space) ----------
        for (int i = 0; i < baris.Length; i++)
        {
            TampilkanBaris(baris[i]);

            yield return new WaitForSeconds(i == 0 ? jedaTekanPertama : 0.1f);

            while (!TombolLanjutDitekan())
                yield return null;
        }

        SembunyikanBox();

        // ---------- 2. (Opsional) bar memberi arahan ----------
        if (pakaiBarSetelahDialog)
        {
            if (butuhTekanTombol)
            {
                if (promptTombol != null) promptTombol.SetActive(true);

                while (!Input.GetKeyDown(tombolAksi))
                    yield return null;

                if (promptTombol != null) promptTombol.SetActive(false);
            }

            if (canvasBar != null) canvasBar.SetActive(true);
            if (fillBar != null) fillBar.fillAmount = 0f;

            float timer = 0f;
            while (timer < durasiBar)
            {
                timer += Time.deltaTime;
                if (fillBar != null) fillBar.fillAmount = Mathf.Clamp01(timer / durasiBar);
                yield return null;
            }

            if (fillBar != null) fillBar.fillAmount = 1f;
            yield return new WaitForSeconds(0.3f);
            if (canvasBar != null) canvasBar.SetActive(false);
        }

        // ---------- 3. Poin ----------
        if (beriPoin)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(poinReward);
                Debug.Log("[DialogBimbingan] Bimbingan selesai! Skor bertambah +" + poinReward);
            }
            else
            {
                Debug.LogWarning("[DialogBimbingan] ScoreManager.Instance tidak ditemukan di Scene ini!");
            }
        }

        // ---------- 4. Mahasiswa pergi, player bebas lagi ----------
        yield return new WaitForSeconds(jedaSebelumPergi);

        NPCBimbingan np = GetComponent<NPCBimbingan>();
        if (np != null)
        {
            // Mahasiswa bangun dan berjalan keluar lewat jalur, lalu menghilang
            yield return StartCoroutine(np.ProsesKeluar());
            np.KunciPlayer = false;
        }
        else if (mahasiswa != null)
        {
            mahasiswa.SetActive(false);
        }

        sudahSelesai = true;
        sedangBerjalan = false;
    }

    private void TampilkanBaris(BarisDialog b)
    {
        Debug.Log("[DialogBimbingan] Menampilkan baris (" + (b.pembicaraMahasiswa ? "mahasiswa" : "pemain") + "): " + b.teks);

        SembunyikanBox();

        if (b.pembicaraMahasiswa)
        {
            if (boxMahasiswa != null) boxMahasiswa.SetActive(true);
            if (teksMahasiswa != null) teksMahasiswa.text = b.teks;
        }
        else
        {
            if (boxPemain != null) boxPemain.SetActive(true);
            if (teksPemain != null) teksPemain.text = b.teks;
        }
    }

    private void SembunyikanBox()
    {
        if (boxMahasiswa != null) boxMahasiswa.SetActive(false);
        if (boxPemain != null) boxPemain.SetActive(false);
    }

    private void SembunyikanSemua()
    {
        SembunyikanBox();
        if (promptTombol != null) promptTombol.SetActive(false);
        if (canvasBar != null) canvasBar.SetActive(false);
    }
}