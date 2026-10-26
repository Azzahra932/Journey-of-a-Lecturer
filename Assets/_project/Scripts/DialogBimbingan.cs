using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DialogBimbingan : MonoBehaviour
{
    [System.Serializable]
    public class BarisDialog
    {
        public bool pembicaraMahasiswa = true;   // true = box mahasiswa, false = box pemain
    }

    [Header("Box Dialog Mahasiswa (Gambar saja)")]
    public GameObject boxMahasiswa;

    [Header("Box Dialog Pemain (Gambar saja)")]
    public GameObject boxPemain;

    [Header("Urutan Tampilan Box (sesuai jumlah baris percakapan)")]
    public BarisDialog[] baris = new BarisDialog[]
    {
        new BarisDialog { pembicaraMahasiswa = true  }, // Giliran 1: Mahasiswa bicara
        new BarisDialog { pembicaraMahasiswa = false }  // Giliran 2: Pemain membalas
    };

    [Header("Lanjut Dialog")]
    public KeyCode tombolLanjut = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaTekanPertama = 0.3f;

    [Header("Hadiah Setelah Dialog Selesai")]
    public bool beriPoin = true;
    public int poinReward = 5;
    public float biayaEnergi = 10f;
    public EnergyBar energyBar;
    public int indexReward = 2;

    [Header("Bar Memberi Arahan (Opsional)")]
    public bool pakaiBarSetelahDialog = true;
    public bool butuhTekanTombol = false;
    public KeyCode tombolAksi = KeyCode.F;
    public GameObject promptTombol;
    public GameObject canvasBar;
    public Image fillBar;
    public float durasiBar = 3f;

    [Header("Tutorial Setelah Bimbingan")]
    public TutorialManager tutorialManager;

    [Header("Setelah Selesai")]
    public GameObject mahasiswa;
    public float jedaSebelumPergi = 0.8f;

    [Header("Langkah Berikutnya")]
    public UnityEvent saatBimbinganSelesai;

    private bool sedangBerjalan = false;
    private bool sudahSelesai = false;

    void Start()
    {
        SembunyikanSemua();

        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();
        if (tutorialManager == null) tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void Mulai()
    {
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

        // ---------- 1. Menampilkan Box Bergantian ----------
        for (int i = 0; i < baris.Length; i++)
        {
            TampilkanBaris(baris[i]);

            yield return new WaitForSeconds(i == 0 ? jedaTekanPertama : 0.1f);

            while (!TombolLanjutDitekan())
                yield return null;
        }

        SembunyikanBox();

        // ---------- 2. Bar Memberi Arahan ----------
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

        // ---------- 3. Poin & Energi ----------
        if (beriPoin && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(poinReward);
        }

        if (energyBar != null) energyBar.DecreaseEnergy(biayaEnergi);

        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));
        }

        // ---------- 4. Tutorial ----------
        if (tutorialManager != null)
        {
            tutorialManager.StartBimbinganTutorial();
        }

        // ---------- 5. Mahasiswa Pergi ----------
        yield return new WaitForSeconds(jedaSebelumPergi);

        NPCBimbingan np = GetComponent<NPCBimbingan>();
        if (np != null)
        {
            yield return StartCoroutine(np.ProsesKeluar());
            np.KunciPlayer = false;
        }
        else if (mahasiswa != null)
        {
            mahasiswa.SetActive(false);
        }

        sudahSelesai = true;
        sedangBerjalan = false;

        if (energyBar != null) energyBar.CekEnergiHabis();

        saatBimbinganSelesai.Invoke();
    }

    private void TampilkanBaris(BarisDialog b)
    {
        bool mahasiswaBicara = b.pembicaraMahasiswa;

        // Hanya menyalakan/mematikan objek gambar box-nya saja
        if (boxMahasiswa != null) boxMahasiswa.SetActive(mahasiswaBicara);
        if (boxPemain != null) boxPemain.SetActive(!mahasiswaBicara);
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