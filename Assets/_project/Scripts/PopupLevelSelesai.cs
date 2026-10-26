using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Memunculkan pop-up "Status Baru" saat poin mencapai target (150), menampilkan XP dan Integritas,
// lalu tombol Lanjut memuat level berikutnya.
public class PopupLevelSelesai : MonoBehaviour
{
    [Header("Panel Pop-up")]
    public GameObject panelPopup;           // Panel "Status Baru" (kosong = tidak ada pop-up)
    public GameObject overlayHitam;         // Panel hitam transparan satu layar di belakang pop-up (kosong = tanpa overlay)
    public TMP_Text teksXP;                 // Teks angka di sebelah ikon bintang
    public TMP_Text teksIntegritas;         // Teks angka di sebelah ikon perisai
    public Button tombolLanjut;             // Tombol "Lanjut" (jangan isi On Click lagi, dipasang lewat script)

    [Header("Level Berikutnya")]
    public string namaSceneLevel2 = "Level2";   // Nama scene level 2 (harus ada di Build Profiles)
    public bool resetSkorSaatLanjut = false;    // false = skor lanjut dari level 1 (tidak mulai dari 0)
    public int targetSkorLevel2 = 0;            // Target skor baru untuk level 2 (0 = tidak diubah). Isi lebih besar dari 150 supaya skor bisa terus bertambah

    [Header("Waktu Muncul")]
    public float jedaSebelumPopup = 1.0f;       // Jeda (detik) setelah poin 150 tercapai
    public BukuInteraction buku;                // Opsional: pop-up menunggu aktivitas buku selesai
    public LaptopInteraction laptopDosen;       // Opsional: pop-up menunggu tugas RPS selesai
    public bool hentikanWaktuSaatTampil = true; // Game berhenti selama pop-up tampil

    private bool sudahTampil = false;
    private float waktuTercapai = -1f;

    void Start()
    {
        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (tombolLanjut != null) tombolLanjut.onClick.AddListener(Lanjut);
    }

    void OnDestroy()
    {
        if (tombolLanjut != null) tombolLanjut.onClick.RemoveListener(Lanjut);
    }

    void Update()
    {
        if (sudahTampil || ScoreManager.Instance == null) return;

        bool tercapai = ScoreManager.Instance.currentScore >= ScoreManager.Instance.targetScore;
        if (!tercapai)
        {
            waktuTercapai = -1f;
            return;
        }

        if (waktuTercapai < 0f) waktuTercapai = Time.time;
        if (Time.time - waktuTercapai < jedaSebelumPopup) return;

        // Tunggu aktivitas yang sedang berjalan selesai (supaya integritas akhir sudah terhitung)
        if (buku != null && buku.SedangSibuk) return;
        if (laptopDosen != null && laptopDosen.SedangSibuk) return;

        Tampilkan();
    }

    private void Tampilkan()
    {
        sudahTampil = true;

        if (teksXP != null)
            teksXP.text = ScoreManager.Instance.currentScore.ToString();

        if (teksIntegritas != null)
        {
            if (IntegritasManager.Instance != null)
                teksIntegritas.text = Mathf.RoundToInt(IntegritasManager.Instance.CurrentIntegritas) + "%";
            else
                teksIntegritas.text = "-";
        }

        if (panelPopup == null)
        {
            Debug.LogWarning("[Level] Slot Panel Popup masih kosong di Inspector! Pop-up tidak bisa tampil (waktu tidak dihentikan).");
            return;
        }

        // Urutan penting: overlay dulu, baru panel (panel harus tampil di atas overlay)
        if (overlayHitam != null) overlayHitam.SetActive(true);
        panelPopup.SetActive(true);

        if (!panelPopup.activeInHierarchy)
        {
            Debug.LogWarning("[Level] Panel '" + panelPopup.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil.");
        }

        if (hentikanWaktuSaatTampil) Time.timeScale = 0f;

        Debug.Log("[Level] Target poin tercapai. Pop-up status baru ditampilkan.");
    }

    public void Lanjut()
    {
        Time.timeScale = 1f;

        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        if (resetSkorSaatLanjut && ScoreManager.Instance != null)
        {
            // AddScore minus = mengembalikan skor ke 0 sekaligus memperbarui bar poin
            ScoreManager.Instance.AddScore(-ScoreManager.Instance.currentScore);
        }

        // Naikkan target skor supaya poin bisa terus bertambah di level 2
        if (targetSkorLevel2 > 0 && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.targetScore = targetSkorLevel2;
            if (ScoreManager.Instance.scoreSlider != null)
                ScoreManager.Instance.scoreSlider.maxValue = targetSkorLevel2;

            ScoreManager.Instance.AddScore(0);   // perbarui teks dan bar poin
        }

        if (string.IsNullOrEmpty(namaSceneLevel2))
        {
            Debug.LogWarning("[Level] Nama scene level 2 belum diisi di Inspector!");
            if (panelPopup != null) panelPopup.SetActive(false);
            return;
        }

        SceneManager.LoadScene(namaSceneLevel2);
    }
}