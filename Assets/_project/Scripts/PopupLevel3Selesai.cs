using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Pop-up "Level 3 selesai" (popuplv3): muncul saat poin mencapai target level 3 (550), menampilkan XP dan Integritas.
// Polanya sama dengan pop-up Level 1 dan Level 2. Tombol Lanjut (tetap di scene yang sama) akan menjalankan, kalau diisi di Inspector:
//   1. mengganti tampilan MC (animator berjalan/diam, dan sprite MC saat duduk)
//   2. mengganti nama MC di top bar
//   3. mengganti icon MC di top bar
//   4. menaikkan target poin
//   5. membuka ruangan yang terkunci
//   6. transisi layar hitam: MC kembali ke ruang dosen, energi penuh, jam 08.00
//   7. box pengantar (opsional), lalu event "Saat Masuk Level Berikutnya"
// Semua bagian yang tidak diisi di Inspector dilewati, jadi pop-up ini bisa dipakai sebagai penutup Level 3 saja,
// lalu disambung ke apa pun yang datang sesudahnya lewat event.
//
// Kalau poin tercapai saat sebuah aktivitas masih berjalan (laptop, adegan Level 3 yang sedang mengunci MC di kursi, tutorial, reward),
// pop-up menunggu sampai aktivitas itu selesai. Pop-up ini TIDAK membaca script adegan Level 3 mana pun, jadi script adegan tidak perlu diubah.
//
// Pasang script ini di satu objek kosong (mis. "ManagerLevel3Selesai").
public class PopupLevel3Selesai : MonoBehaviour
{
    [System.Serializable]
    public class GantiSpriteItem
    {
        public SpriteRenderer target;   // SpriteRenderer objek MC duduk (mis. Tpngajar di kelas, Sit di perpustakaan)
        public Sprite spriteBaru;       // Sprite MC versi duduk untuk objek itu
    }

    [Header("Syarat Muncul")]
    public int targetSkorLevel3 = 550;          // Pop-up muncul saat skor mencapai target ini. Samakan dengan Target Skor Level 3 di ManagerLevel2Selesai
    public float jedaSebelumPopup = 1.0f;       // Jeda (detik) setelah poin tercapai

    [Header("Panel Pop-up (popuplv3)")]
    public GameObject panelPopup;               // Panel pop-up level 3
    public GameObject overlayHitam;             // Panel hitam transparan satu layar di belakang pop-up (opsional)
    public TMP_Text teksXP;                     // Teks angka di sebelah ikon bintang
    public TMP_Text teksIntegritas;             // Teks angka di sebelah ikon perisai
    public Button tombolLanjut;                 // Tombol "Lanjut" (jangan isi On Click di Inspector, dipasang lewat script)
    public bool hentikanWaktuSaatTampil = true; // Game berhenti selama pop-up tampil

    [Header("Menunggu Aktivitas Selesai (kosong = dicari otomatis)")]
    public LaptopKelas laptopKelas;
    public LaptopInteraction laptopDosen;
    public LaptopPerpusInteraction laptopPerpus;

    [Header("0. Box Pengantar (opsional, muncul setelah MC kembali ke ruang dosen)")]
    public GameObject[] boxPengantar;               // Box tampil berurutan setelah transisi. Kosong = langsung lanjut
    public KeyCode tombolLanjutBox = KeyCode.Space; // Menutup box dan lanjut ke box berikutnya
    public bool bolehKlikMouse = true;              // Klik kiri mouse juga bisa

    [Header("1. Perubahan Karakter MC (Berjalan / Diam), opsional")]
    public SpriteRenderer playerSpriteRenderer;             // SpriteRenderer pemain (dipakai kalau pemain TIDAK pakai Animator)
    public Sprite spriteMCBaru;                             // Sprite MC jenjang berikutnya. Kosong = tidak diganti
    public Animator[] animatorPemain;                       // Animator pemain (TPdiam, dst)
    public RuntimeAnimatorController animatorControllerBaru;   // Animator (Override) Controller MC jenjang berikutnya. Kosong = tidak diganti

    [Header("1b. Sprite MC Saat Duduk (Kelas, Perpustakaan, dst), opsional")]
    public GantiSpriteItem[] spriteDuduk;                   // Satu elemen per objek MC duduk

    [Header("1c. Sprite Duduk di Kursi Dosen (InteractiveDesk), opsional")]
    public InteractiveDesk kursiDosen;                      // Objek yang memasang script Interactive Desk (kursi dosen)
    public Sprite spriteDudukDosenBaru;                     // Sprite MC saat duduk di kursi dosen. Kosong = tidak diganti

    [Header("2 & 3. Nama & Icon MC di Top Bar, opsional")]
    public TMP_Text teksNamaMCTopBar;           // Teks nama MC di top bar
    public string namaBaruMC = "";              // Kosong = nama tidak diganti
    public Image iconTopBar;                    // Image icon MC di top bar
    public Sprite iconBaru;                     // Kosong = icon tidak diganti

    [Header("4. Target Poin Berikutnya, opsional")]
    public int targetSkorBerikutnya = 0;        // 0 = target tidak diubah

    [Header("5. Membuka Ruangan, opsional")]
    public RuangTerkunci[] ruangYangDibuka;     // Isi dengan objek RuangTerkunci yang ingin dibuka

    [Header("6. Kembali ke Ruang Dosen (Energi Penuh)")]
    public bool kembaliKeAwalSaatLanjut = true;   // MC dipindah ke titik awal (ruang dosen) lewat transisi layar hitam
    public TransisiKembaliKeAwal transisi;        // Kosong = dicari otomatis
    public EnergyBar energyBar;                   // Kosong = dicari otomatis
    public GameTimeManager timeManager;           // Kosong = dicari otomatis. Jam dikembalikan ke 08.00

    [Header("Saat Tombol Lanjut / Masuk Level Berikutnya")]
    public UnityEvent saatLanjut;               // Dipanggil begitu tombol Lanjut ditekan
    public UnityEvent saatMasukLevelBerikutnya; // Dipanggil setelah MC di titik awal dan box pengantar selesai (sambungkan ke adegan / layar berikutnya)

    private bool sudahTampil = false;
    private bool sudahLanjut = false;
    private float waktuTercapai = -1f;
    private bool sudahDilaporkanMenunggu = false;
    private PlayerMovement pm;

    // Semua kursi di scene: selama sebuah adegan berjalan, kursi MC dikunci (TerkunciEksternal), jadi ini dipakai untuk tahu adegan masih berlangsung
    private InteractiveDesk[] semuaKursiDesk;
    private KursiPerpusInteraction[] semuaKursiPerpus;

    void Start()
    {
        if (panelPopup != null) panelPopup.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxPengantar != null)
        {
            foreach (GameObject b in boxPengantar)
            {
                if (b != null) b.SetActive(false);
            }
        }

        if (tombolLanjut != null) tombolLanjut.onClick.AddListener(Lanjut);

        if (laptopKelas == null) laptopKelas = FindFirstObjectByType<LaptopKelas>();
        if (laptopDosen == null) laptopDosen = FindFirstObjectByType<LaptopInteraction>();
        if (laptopPerpus == null) laptopPerpus = FindFirstObjectByType<LaptopPerpusInteraction>(FindObjectsInactive.Include);
        semuaKursiDesk = FindObjectsByType<InteractiveDesk>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        semuaKursiPerpus = FindObjectsByType<KursiPerpusInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (transisi == null) transisi = FindFirstObjectByType<TransisiKembaliKeAwal>();
        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();
        if (timeManager == null) timeManager = FindFirstObjectByType<GameTimeManager>();
    }

    void OnDestroy()
    {
        if (tombolLanjut != null) tombolLanjut.onClick.RemoveListener(Lanjut);
    }

    void Update()
    {
        if (sudahTampil || ScoreManager.Instance == null) return;

        // Pop-up level 3 hanya aktif setelah target skor naik ke level 3 (supaya tidak ikut muncul di level 1 atau 2)
        bool level3Aktif = ScoreManager.Instance.targetScore >= targetSkorLevel3;
        bool tercapai = level3Aktif && ScoreManager.Instance.currentScore >= ScoreManager.Instance.targetScore;

        if (!tercapai)
        {
            waktuTercapai = -1f;
            sudahDilaporkanMenunggu = false;
            return;
        }

        if (waktuTercapai < 0f) waktuTercapai = Time.unscaledTime;
        if (Time.unscaledTime - waktuTercapai < jedaSebelumPopup) return;

        // Tunggu aktivitas yang sedang berjalan selesai (supaya poin, reward, dan integritas akhir sudah terhitung)
        string alasan = AlasanMasihSibuk();
        if (alasan != null)
        {
            if (!sudahDilaporkanMenunggu && Time.unscaledTime - waktuTercapai > 5f)
            {
                sudahDilaporkanMenunggu = true;
                Debug.Log("[Level 3] Poin sudah tercapai, pop-up menunggu: " + alasan);
            }
            return;
        }

        Tampilkan();
    }

    // Mengembalikan alasan pop-up harus menunggu, atau null kalau tidak ada aktivitas yang berjalan
    private string AlasanMasihSibuk()
    {
        if (Time.timeScale == 0f) return "game sedang berhenti (tutorial atau pop-up lain sedang tampil)";
        if (laptopKelas != null && laptopKelas.SedangSibuk) return "laptop kelas sedang dipakai";
        if (laptopDosen != null && laptopDosen.SedangSibuk) return "laptop dosen sedang dipakai";
        if (laptopPerpus != null && laptopPerpus.SedangSibuk) return "laptop perpustakaan sedang dipakai";

        // Adegan Level 3 mengunci MC di kursi selama berjalan (dialog, bar, poin, reward, tutorial), dan melepasnya setelah selesai
        if (semuaKursiDesk != null)
        {
            foreach (InteractiveDesk d in semuaKursiDesk)
            {
                if (d != null && d.TerkunciEksternal) return "MC sedang dikunci di kursi '" + d.name + "' oleh adegan yang berjalan";
            }
        }

        if (semuaKursiPerpus != null)
        {
            foreach (KursiPerpusInteraction k in semuaKursiPerpus)
            {
                if (k != null && k.TerkunciEksternal) return "MC sedang dikunci di kursi '" + k.name + "' oleh adegan yang berjalan";
            }
        }

        // MC dikunci (adegan atau aktivitas sedang berjalan)
        if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null && !pm.enabled) return "MC sedang dikunci oleh adegan yang berjalan";

        return null;
    }

    // Menu tes: saat Play, klik kanan judul komponen ini di Inspector, lalu pilih menu ini
    [ContextMenu("Tes Tampilkan Pop-up (tanpa menunggu poin)")]
    private void TesTampilkan()
    {
        if (sudahTampil) return;
        Tampilkan();
    }

    [ContextMenu("Tes Lanjut (tanpa pop-up)")]
    private void TesLanjut()
    {
        sudahTampil = true;
        Lanjut();
    }

    private void Tampilkan()
    {
        sudahTampil = true;

        if (teksXP != null && ScoreManager.Instance != null)
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
            Debug.LogWarning("[Level 3] Slot Panel Popup masih kosong di Inspector! Pop-up tidak bisa tampil.");
            return;
        }

        // Urutan penting: overlay dulu, baru panel (panel harus tampil di atas overlay)
        if (overlayHitam != null)
        {
            overlayHitam.SetActive(true);
            overlayHitam.transform.SetAsLastSibling();
        }

        panelPopup.SetActive(true);
        panelPopup.transform.SetAsLastSibling();

        if (!panelPopup.activeInHierarchy)
            Debug.LogWarning("[Level 3] Panel '" + panelPopup.name + "' sudah diaktifkan tapi induknya (Canvas) nonaktif, jadi tidak tampil.");

        if (hentikanWaktuSaatTampil) Time.timeScale = 0f;

        Debug.Log("[Level 3] Target poin level 3 tercapai. Pop-up level 3 ditampilkan.");
    }

    // Dipanggil tombol Lanjut
    public void Lanjut()
    {
        if (sudahLanjut) return;   // cegah klik ganda
        sudahLanjut = true;

        if (panelPopup != null) panelPopup.SetActive(false);

        saatLanjut.Invoke();

        StartCoroutine(AlurLanjut());
    }

    private IEnumerator AlurLanjut()
    {
        // 1. Pop-up dan overlay ditutup, waktu berjalan lagi supaya transisi layar hitam bisa jalan
        Time.timeScale = 1f;
        if (overlayHitam != null) overlayHitam.SetActive(false);

        // 2. Terapkan perubahan (hanya bagian yang diisi di Inspector)
        GantiKarakterMC();
        GantiSpriteMCDuduk();
        GantiNamaDanIconTopBar();
        NaikkanTargetSkor();
        BukaRuangan();

        // Riwayat reward dikosongkan untuk jenjang berikutnya
        if (RewardManager.Instance != null) RewardManager.Instance.MulaiLevelBaru();

        // 3. Transisi layar hitam: MC kembali ke ruang dosen (titik awal)
        if (kembaliKeAwalSaatLanjut && transisi != null && !transisi.SedangTransisi)
        {
            transisi.saatTransisiSelesai.AddListener(SetelahTransisi);
            transisi.MulaiTransisi();
        }
        else
        {
            if (kembaliKeAwalSaatLanjut && transisi == null)
                Debug.LogWarning("[Level 3] TransisiKembaliKeAwal tidak ditemukan, MC tidak dipindah ke titik awal.");

            SetelahTransisi();
        }

        Debug.Log("[Level 3] Tombol Lanjut ditekan.");
        yield break;
    }

    // Dijalankan setelah transisi selesai dan MC berada di titik awal
    private void SetelahTransisi()
    {
        if (transisi != null) transisi.saatTransisiSelesai.RemoveListener(SetelahTransisi);

        // Energi penuh
        if (energyBar != null) energyBar.IsiPenuh();

        // Jam kembali ke 08.00 pagi
        if (timeManager != null) timeManager.ResetKeJamPagi();

        StartCoroutine(AlurBoxPengantar());
    }

    // Setelah transisi: box pengantar (tunggu Space), lalu event masuk level berikutnya
    private IEnumerator AlurBoxPengantar()
    {
        if (boxPengantar != null && boxPengantar.Length > 0)
        {
            // Game berhenti dan layar digelapkan selama box tampil
            Time.timeScale = 0f;
            if (overlayHitam != null)
            {
                overlayHitam.SetActive(true);
                overlayHitam.transform.SetAsLastSibling();
            }

            foreach (GameObject box in boxPengantar)
            {
                if (box == null) continue;

                box.SetActive(true);
                box.transform.SetAsLastSibling();

                // Jeda singkat supaya tombol yang masih tertekan tidak langsung menutup box
                yield return new WaitForSecondsRealtime(0.25f);

                while (!(Input.GetKeyDown(tombolLanjutBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                    yield return null;

                box.SetActive(false);
            }

            if (overlayHitam != null) overlayHitam.SetActive(false);
            Time.timeScale = 1f;
        }

        // Hal tambahan yang dihubungkan lewat Inspector
        saatMasukLevelBerikutnya.Invoke();

        Debug.Log("[Level 3] MC di titik awal, energi penuh, jam 08.00.");
    }

    // 1. Ganti tampilan MC berjalan / diam
    private void GantiKarakterMC()
    {
        // Kalau MC digerakkan Animator, sprite harus diganti lewat Animator Controller,
        // kalau tidak, Animator akan menimpa sprite setiap frame.
        if (animatorControllerBaru != null && animatorPemain != null)
        {
            foreach (Animator a in animatorPemain)
            {
                if (a != null) a.runtimeAnimatorController = animatorControllerBaru;
            }
        }

        if (playerSpriteRenderer != null && spriteMCBaru != null)
            playerSpriteRenderer.sprite = spriteMCBaru;
    }

    // 1b & 1c. Ganti sprite MC versi duduk di tiap kursi
    private void GantiSpriteMCDuduk()
    {
        // Kursi dosen: InteractiveDesk memakai "Sit Sprite" untuk menggantikan gambar pemain saat duduk
        if (kursiDosen != null && spriteDudukDosenBaru != null)
        {
            kursiDosen.sitSprite = spriteDudukDosenBaru;
            Debug.Log("[Level 3] Sprite duduk kursi dosen diganti.");
        }

        if (spriteDuduk == null) return;

        foreach (GantiSpriteItem item in spriteDuduk)
        {
            if (item == null || item.target == null || item.spriteBaru == null) continue;

            item.target.sprite = item.spriteBaru;
            Debug.Log("[Level 3] Sprite duduk '" + item.target.name + "' diganti.");
        }
    }

    // 2 & 3. Nama dan icon MC di top bar
    private void GantiNamaDanIconTopBar()
    {
        if (teksNamaMCTopBar != null && !string.IsNullOrEmpty(namaBaruMC))
            teksNamaMCTopBar.text = namaBaruMC;

        if (iconTopBar != null && iconBaru != null)
            iconTopBar.sprite = iconBaru;
    }

    // 4. Target poin naik, skor tetap
    private void NaikkanTargetSkor()
    {
        if (targetSkorBerikutnya <= 0 || ScoreManager.Instance == null) return;

        ScoreManager.Instance.targetScore = targetSkorBerikutnya;

        if (ScoreManager.Instance.scoreSlider != null)
            ScoreManager.Instance.scoreSlider.maxValue = targetSkorBerikutnya;

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