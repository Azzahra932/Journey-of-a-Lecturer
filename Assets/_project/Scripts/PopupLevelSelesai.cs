using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Memunculkan pop-up "Status Baru" saat poin mencapai target (150), menampilkan XP dan Integritas.
// Tombol Lanjut (tetap di scene yang sama) akan:
//   1. mengganti tampilan MC (sprite / animator)
//   2. mengganti nama MC di top bar
//   3. mengganti icon MC di top bar
//   4. menaikkan target poin (150 -> 300), jadi top bar tampil 150/300
//   5. membuka ruangan yang terkunci (Ruang C dan D)
public class PopupLevelSelesai : MonoBehaviour
{
    [Header("Panel Pop-up")]
    public GameObject panelPopup;           // Panel "Status Baru"
    public GameObject overlayHitam;         // Panel hitam transparan di belakang pop-up (opsional)
    public TMP_Text teksXP;                 // Teks angka di sebelah ikon bintang
    public TMP_Text teksIntegritas;         // Teks angka di sebelah ikon perisai
    public Button tombolLanjut;             // Tombol "Lanjut" (jangan isi On Click di Inspector, dipasang lewat script)

    [Header("1. Perubahan Karakter MC")]
    public SpriteRenderer playerSpriteRenderer;             // SpriteRenderer pemain (dipakai kalau pemain TIDAK pakai Animator)
    public Sprite spriteMCLevel2;                           // Sprite MC level 2
    public Animator[] animatorPemain;                       // Animator pemain (TPdiam, dst). Dipakai kalau MC digerakkan Animator
    public RuntimeAnimatorController animatorControllerLevel2;   // Animator Controller MC level 2

    [Header("2 & 3. Nama & Icon MC di Top Bar")]
    public TMP_Text teksNamaMCTopBar;       // Teks nama MC di top bar
    public string namaBaruMC = "Asisten Ahli";
    public Image iconTopBar;                // Image icon MC di top bar
    public Sprite iconLevel2;               // Sprite icon level 2

    [Header("4. Target Poin Level 2 (150 / 300)")]
    public int targetSkorLevel2 = 300;      // 0 = target tidak diubah

    [Header("5. Membuka Ruangan C & D")]
    public RuangTerkunci[] ruangYangDibuka; // Isi dengan objek yang memasang script RuangTerkunci (Ruang C dan D)

    [Header("Tutorial")]
    public TutorialManager tutorialManager;     // Kosong = dicari otomatis. Di Level 2, step tutorial "Hanya Level 1" dilewati

    [Header("Waktu Muncul")]
    public float jedaSebelumPopup = 1.0f;       // Jeda (detik) setelah poin 150 tercapai
    public BukuInteraction buku;                // Opsional: pop-up menunggu aktivitas buku selesai
    public LaptopInteraction laptopDosen;       // Opsional: pop-up menunggu tugas RPS selesai
    public bool hentikanWaktuSaatTampil = true; // Game berhenti selama pop-up tampil

    private bool sudahTampil = false;
    private bool sudahLanjut = false;
    private float waktuTercapai = -1f;

    void Start()
    {
        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (tombolLanjut != null) tombolLanjut.onClick.AddListener(Lanjut);
        if (tutorialManager == null) tutorialManager = FindFirstObjectByType<TutorialManager>();
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
            Debug.LogWarning("[Level] Slot Panel Popup masih kosong di Inspector! Pop-up tidak bisa tampil.");
            return;
        }

        // Urutan penting: overlay dulu, baru panel (panel harus tampil di atas overlay)
        if (overlayHitam != null) overlayHitam.SetActive(true);
        panelPopup.SetActive(true);

        if (hentikanWaktuSaatTampil) Time.timeScale = 0f;

        Debug.Log("[Level] Target poin tercapai. Pop-up status baru ditampilkan.");
    }

    // Dipanggil tombol Lanjut: masuk ke level 2 tanpa pindah scene
    public void Lanjut()
    {
        if (sudahLanjut) return;   // cegah klik ganda
        sudahLanjut = true;

        Time.timeScale = 1f;

        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        GantiKarakterMC();
        GantiNamaDanIconTopBar();
        NaikkanTargetSkor();
        BukaRuangan();

        // Tutorial yang bertanda "Hanya Level 1" (mis. penjelasan top bar) tidak muncul lagi
        if (tutorialManager != null) tutorialManager.MasukLevel2();

        Debug.Log("[Level] Masuk Level 2.");
    }

    // 1. Ganti tampilan MC
    private void GantiKarakterMC()
    {
        // Kalau MC digerakkan Animator, sprite harus diganti lewat Animator Controller,
        // kalau tidak, Animator akan menimpa sprite setiap frame.
        if (animatorControllerLevel2 != null && animatorPemain != null)
        {
            foreach (Animator a in animatorPemain)
            {
                if (a != null) a.runtimeAnimatorController = animatorControllerLevel2;
            }
        }

        if (playerSpriteRenderer != null && spriteMCLevel2 != null)
            playerSpriteRenderer.sprite = spriteMCLevel2;
    }

    // 2 & 3. Nama dan icon MC di top bar
    private void GantiNamaDanIconTopBar()
    {
        if (teksNamaMCTopBar != null && !string.IsNullOrEmpty(namaBaruMC))
            teksNamaMCTopBar.text = namaBaruMC;

        if (iconTopBar != null && iconLevel2 != null)
            iconTopBar.sprite = iconLevel2;
    }

    // 4. Target poin jadi 300, skor tetap 150 -> top bar tampil 150/300
    private void NaikkanTargetSkor()
    {
        if (targetSkorLevel2 <= 0 || ScoreManager.Instance == null) return;

        ScoreManager.Instance.targetScore = targetSkorLevel2;

        if (ScoreManager.Instance.scoreSlider != null)
            ScoreManager.Instance.scoreSlider.maxValue = targetSkorLevel2;

        ScoreManager.Instance.AddScore(0);   // perbarui teks dan bar poin
    }

    // 5. Buka ruangan yang terkunci
    private void BukaRuangan()
    {
        if (ruangYangDibuka == null) return;

        foreach (RuangTerkunci r in ruangYangDibuka)
        {
            if (r != null) r.Buka();
        }
    }
}