using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Memulihkan keadaan permainan saat pemain memilih Continue di menu utama, dan melanjutkan dari AWAL SCENE terakhir yang sudah dimulai
// (mis. pemain berhenti di Level 3 Scene 4 -> Continue memulai Level 3 Scene 4 dari awal).
// Pasang script ini di satu objek kosong di scene gameplay (mis. "PemulihProgres").
//
// Cara kerja (dijalankan sekali saat scene gameplay dimuat, hanya kalau Continue dipilih):
//   1. Skor, target, dan integritas sudah dipulihkan oleh ScoreManager dan IntegritasManager (dipasang menu utama sebelum scene dimuat)
//   2. Awal permainan (tutorial awal, dst) dimatikan supaya tidak diulang
//   3. Perubahan tiap level yang sudah dilewati dipulihkan (sprite MC, nama dan icon top bar, ruangan terbuka, objek diganti, mode level terakhir)
//      lewat PulihkanPerubahan() milik pop-up Level 1 sampai 4
//   4. Pilihan MC di Scene 3 Level 5 dipulihkan
//   5. Hari ke berapa dan jam game dipulihkan (jam lanjut dari waktu terakhir), energi diisi penuh
//   6. Scene tersimpan dimulai: event "Persiapan Level N" dijalankan, lalu event milik titik itu (daftar Titik Pulih, mis. SiapkanScene scene itu)
//      Untuk scene 1 sebuah level, event "Saat Masuk ..." milik pop-up level sebelumnya yang dijalankan (seperti saat tombol Lanjut)
//
// [DefaultExecutionOrder(-100)] supaya awal permainan sempat dimatikan sebelum mulai berjalan.
[DefaultExecutionOrder(-100)]
public class PemulihProgres : MonoBehaviour
{
    [System.Serializable]
    public class TitikPulih
    {
        [Tooltip("Id scene, format L + level + S + scene, mis. L3S4. Harus sama dengan id yang dipakai di PenyimpanCheckpoint > SimpanTitik.")]
        public string id;
        [Tooltip("Fungsi yang dijalankan untuk memulai scene itu dari awal, biasanya SiapkanScene milik scene tersebut. Boleh lebih dari satu.")]
        public UnityEvent saatDipulihkan;
    }

    [Header("Pop-up Level (untuk memulihkan perubahan tiap level)")]
    public PopupLevelSelesai popupLevel1;       // Pop-up Level 1 selesai (ManagerLevel1Selesai): perubahan menuju Level 2
    public PopupLevel2Selesai popupLevel2;      // Pop-up Level 2 selesai (ManagerLevel2Selesai): perubahan menuju Level 3
    public PopupLevel3Selesai popupLevel3;      // Pop-up Level 3 selesai (ManagerLevel3Selesai): perubahan menuju Level 4
    public PopupLevel4Selesai popupLevel4;      // Pop-up Level 4 selesai (ManagerLevel4Selesai): perubahan menuju Level 5

    [Header("Titik Scene (isi satu elemen per scene yang bisa dilanjutkan, selain scene 1 tiap level)")]
    public TitikPulih[] titikPulih;

    [Header("Persiapan Level (dijalankan kalau Continue di tengah level, sebelum event titik scene)")]
    [Tooltip("Hal yang HARUS aktif di level ini apa pun scene-nya, mis. MulaiLevel4 milik AdeganLevel4Scene2 untuk Level 4. Jangan isi SiapkanScene scene 1 di sini.")]
    public UnityEvent persiapanLevel1;
    public UnityEvent persiapanLevel2;
    public UnityEvent persiapanLevel3;
    public UnityEvent persiapanLevel4;
    public UnityEvent persiapanLevel5;

    [Header("Pilihan Scene 3 Level 5")]
    [Tooltip("Kosong = dicari otomatis. Pilihan MC (menerima atau menolak tawaran) dipulihkan supaya dialog Scene 4 dan penutup Level 5 tetap benar.")]
    public AdeganLevel5Scene3 adeganScene3;

    [Header("Tutorial Awal")]
    [Tooltip("Kosong = dicari otomatis. Tutorial awal (yang otomatis jalan saat scene dimuat) dimatikan, dan step 'hanya Level 1' dilewati kalau Continue ke Level 2 atau lebih.")]
    public TutorialManager tutorialManager;

    [Header("Dimatikan Saat Continue (awal permainan yang tidak boleh diulang)")]
    [Tooltip("Objek yang dinonaktifkan saat Continue, mis. Kaprodi onboarding, panah petunjuk awal.")]
    public GameObject[] objekDimatikan;
    [Tooltip("Script yang dimatikan saat Continue, mis. script adegan Level 1 yang otomatis mulai saat scene dimuat. JANGAN isi TutorialManager di sini (sudah ditangani di atas).")]
    public Behaviour[] scriptDimatikan;
    [Tooltip("Kursi InteractiveDesk yang pemicu adegan level 1-nya dilepas (Kaprodi Script, Bimbingan Script, Evaluasi Script dikosongkan), supaya duduk di kursi tidak mengulang adegan Level 1.")]
    public InteractiveDesk[] kursiDibersihkan;

    [Header("Dinyalakan Saat Continue (opsional)")]
    public GameObject[] objekDinyalakan;

    [Header("Tes Tanpa Menu (opsional)")]
    [Tooltip("Hanya untuk tes di Editor: isi id titik, mis. L3S4, untuk menjalankan pemulihan seolah Continue ke titik itu tanpa lewat menu utama. Skor dan integritas TIDAK dipulihkan (tetap nilai sekarang). Kosong = mati.")]
    public string tesPulihkanKeTitik = "";

    private int levelTujuan = 0;
    private int sceneTujuan = 0;
    private string titikTujuan = "";

    void Awake()
    {
        levelTujuan = 0;
        sceneTujuan = 0;
        titikTujuan = "";

        if (ProgresPermainan.LanjutDipilih)
        {
            titikTujuan = ProgresPermainan.TitikTersimpan;
            levelTujuan = ProgresPermainan.LevelTersimpan;

            int lv, sc;
            if (ProgresPermainan.UraiTitik(titikTujuan, out lv, out sc))
            {
                levelTujuan = lv;
                sceneTujuan = sc;
            }
            else
            {
                sceneTujuan = 1;   // data simpanan lama (hanya level): mulai dari scene 1 level itu
            }
        }
        else if (!string.IsNullOrEmpty(tesPulihkanKeTitik))
        {
            int lv, sc;
            if (ProgresPermainan.UraiTitik(tesPulihkanKeTitik, out lv, out sc))
            {
                titikTujuan = tesPulihkanKeTitik.Trim().ToUpperInvariant();
                levelTujuan = lv;
                sceneTujuan = sc;
            }
            else
            {
                Debug.LogWarning("[Pemulih] Tes Pulihkan Ke Titik '" + tesPulihkanKeTitik + "' tidak valid. Pakai format L3S4.");
            }
        }

        if (levelTujuan < 1) return;

        Debug.Log("[Pemulih] Continue ke awal Level " + levelTujuan + " Scene " + sceneTujuan + ": awal permainan dimatikan.");

        // Tutorial awal: tidak dijalankan lagi. Step "hanya Level 1" dilewati kalau sudah di level 2 atau lebih
        if (tutorialManager == null) tutorialManager = FindFirstObjectByType<TutorialManager>();
        if (tutorialManager != null)
        {
            tutorialManager.steps = new TutorialManager.TutorialStep[0];
            if (levelTujuan >= 2) tutorialManager.MasukLevel2();
        }

        // Awal permainan (adegan awal, dst) dimatikan sebelum sempat berjalan
        if (objekDimatikan != null)
        {
            foreach (GameObject o in objekDimatikan)
            {
                if (o != null) o.SetActive(false);
            }
        }

        if (scriptDimatikan != null)
        {
            foreach (Behaviour b in scriptDimatikan)
            {
                if (b != null) b.enabled = false;
            }
        }
    }

    void Start()
    {
        if (levelTujuan < 1) return;

        StartCoroutine(Pulihkan());
    }

    private IEnumerator Pulihkan()
    {
        // Tunggu dua frame supaya semua script lain (pop-up, manager, kursi) selesai Awake dan Start
        yield return null;
        yield return null;

        int level = levelTujuan;

        // 1. Perubahan tiap level yang sudah dilewati, berurutan: Level 1 selesai, Level 2 selesai, dst
        if (level >= 2) { if (popupLevel1 != null) popupLevel1.PulihkanPerubahan(); else PeringatanKosong("Popup Level 1"); }
        if (level >= 3) { if (popupLevel2 != null) popupLevel2.PulihkanPerubahan(); else PeringatanKosong("Popup Level 2"); }
        if (level >= 4) { if (popupLevel3 != null) popupLevel3.PulihkanPerubahan(); else PeringatanKosong("Popup Level 3"); }
        if (level >= 5) { if (popupLevel4 != null) popupLevel4.PulihkanPerubahan(); else PeringatanKosong("Popup Level 4"); }

        // 2. Pilihan MC di Scene 3 Level 5 (kalau sudah pernah memilih)
        int pilihan = ProgresPermainan.LanjutDipilih ? ProgresPermainan.PilihanScene3Tersimpan : 0;
        if (pilihan != 0)
        {
            if (adeganScene3 == null) adeganScene3 = FindFirstObjectByType<AdeganLevel5Scene3>(FindObjectsInactive.Include);

            if (adeganScene3 != null)
                adeganScene3.PulihkanPilihan(pilihan);
            else
                Debug.LogWarning("[Pemulih] AdeganLevel5Scene3 tidak ditemukan, pilihan Scene 3 tidak dipulihkan.");
        }

        // 3. Riwayat reward dikosongkan (scene dimulai dari awal)
        if (RewardManager.Instance != null) RewardManager.Instance.MulaiLevelBaru();

        // 4. Hari ke berapa dan jam game dipulihkan (hanya kalau Continue dari menu): jam lanjut dari waktu terakhir. Energi diisi penuh
        GameTimeManager waktu = FindFirstObjectByType<GameTimeManager>();
        if (waktu != null && ProgresPermainan.LanjutDipilih)
        {
            waktu.AturHariKe(ProgresPermainan.HariTersimpan);
            waktu.AturLangkahWaktu(ProgresPermainan.LangkahWaktuTersimpan);
        }

        EnergyBar energi = FindFirstObjectByType<EnergyBar>();
        if (energi != null) energi.IsiPenuh();

        // 5. Pemicu adegan level 1 dilepas dari kursi, supaya duduk di kursi tidak mengulang adegan Level 1
        if (kursiDibersihkan != null)
        {
            foreach (InteractiveDesk d in kursiDibersihkan)
            {
                if (d == null) continue;

                d.kaprodiScript = null;
                d.bimbinganScript = null;
                d.evaluasiScript = null;
            }
        }

        if (objekDinyalakan != null)
        {
            foreach (GameObject o in objekDinyalakan)
            {
                if (o != null) o.SetActive(true);
            }
        }

        // 6. Mulai scene tersimpan dari awal
        MulaiScene(level);

        ProgresPermainan.LanjutDipilih = false;

        Debug.Log("[Pemulih] Pemulihan selesai, Level " + level + " Scene " + sceneTujuan + " dimulai dari awal.");
    }

    private void MulaiScene(int level)
    {
        // Scene 1 sebuah level (atau titik yang tidak terdaftar): event "Saat Masuk ..." milik pop-up level sebelumnya, seperti saat tombol Lanjut
        TitikPulih titik = CariTitik(titikTujuan);

        if (sceneTujuan > 1 && titik == null)
        {
            Debug.LogWarning("[Pemulih] Titik '" + titikTujuan + "' belum didaftarkan di daftar Titik Pulih, jadi Level " + level
                + " dimulai dari Scene 1. Tambahkan elemen dengan id ini dan isi event-nya.");
        }

        if (sceneTujuan > 1 && titik != null)
        {
            JalankanPersiapanLevel(level);
            titik.saatDipulihkan.Invoke();
            return;
        }

        switch (level)
        {
            case 2:
                if (popupLevel1 != null) popupLevel1.saatMasukLevel2.Invoke();
                break;
            case 3:
                if (popupLevel2 != null) popupLevel2.MulaiDariContinue();   // box pengantar Level 3 tampil dulu, lalu Saat Masuk Level 3
                break;
            case 4:
                if (popupLevel3 != null) popupLevel3.MulaiDariContinue();   // box pengantar Level 4 tampil dulu, lalu Saat Masuk Level Berikutnya
                break;
            case 5:
                if (popupLevel4 != null) popupLevel4.saatMasukLevelBerikutnya.Invoke();
                break;
            default:
                // Level 1 scene 1: permainan baru dimulai oleh adegan awal biasa, tidak ada yang perlu dijalankan
                break;
        }
    }

    private void JalankanPersiapanLevel(int level)
    {
        switch (level)
        {
            case 1: persiapanLevel1.Invoke(); break;
            case 2: persiapanLevel2.Invoke(); break;
            case 3: persiapanLevel3.Invoke(); break;
            case 4: persiapanLevel4.Invoke(); break;
            case 5: persiapanLevel5.Invoke(); break;
        }
    }

    private TitikPulih CariTitik(string id)
    {
        if (titikPulih == null || string.IsNullOrEmpty(id)) return null;

        foreach (TitikPulih t in titikPulih)
        {
            if (t != null && !string.IsNullOrEmpty(t.id) && t.id.Trim().ToUpperInvariant() == id.Trim().ToUpperInvariant())
                return t;
        }

        return null;
    }

    private void PeringatanKosong(string nama)
    {
        Debug.LogWarning("[Pemulih] Slot " + nama + " masih kosong di Inspector, perubahannya tidak dipulihkan.");
    }
}