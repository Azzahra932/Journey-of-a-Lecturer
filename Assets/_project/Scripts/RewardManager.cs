using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[System.Serializable]
public class RewardItem
{
    public string nama = "Reward";          // Hanya pengingat di Inspector (contoh: RPS, Ngajar, Bimbingan, Referensi)
    public GameObject boxReward;            // Box + teks reward yang muncul sendiri saat aktivitas selesai
    public GameObject itemRiwayat;          // "Cetakan" entri di panel riwayat (biarkan nonaktif). Tiap reward diterima, salinannya ditambahkan ke riwayat

    [HideInInspector] public int jumlah = 0;
}

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance;

    [Header("Daftar Reward (urutan = nomor index)")]
    [Tooltip("Urutan: 0 = RPS, 1 = Ngajar, 2 = Bimbingan, 3 = Referensi (buku), 4 = Penelitian, 5 = Karya Ilmiah, 6 = Seminar, 7 = Pengabdian. Nomor mulai dari 0.")]
    public RewardItem[] daftarReward;

    [Header("Box Reward (Muncul Sendiri, Hilang Sendiri)")]
    public float lamaTampil = 1.2f;                 // Detik box reward tampil sebelum hilang otomatis
    public bool alurMenungguReward = false;         // false = aktivitas langsung lanjut (cepat), reward tampil sambil jalan. true = aktivitas menunggu box hilang dulu
    public GameObject overlayHitam;                 // Opsional: panel hitam transparan di belakang box reward (kosongkan kalau tidak mau)

    [Header("Ikon Pesan & Riwayat")]
    public Button tombolIkonPesan;                  // Ikon pesan di HUD
    public GameObject panelRiwayat;                 // Panel yang berisi entri riwayat reward
    public Button tombolTutupRiwayat;               // Opsional: tombol tutup di panel riwayat
    public ScrollRect scrollRiwayat;                // Opsional: Scroll View di panel riwayat (dikembalikan ke atas tiap panel dibuka)
    public GameObject tandaBaru;                    // Opsional: titik/tanda di ikon saat ada reward baru
    public int maksEntriRiwayat = 8;                // Jumlah entri maksimal di panel (yang terlama dihapus). 0 = tanpa batas

    private bool sedangTampil = false;
    private readonly List<GameObject> entriRiwayat = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (daftarReward != null)
        {
            foreach (RewardItem r in daftarReward)
            {
                if (r.boxReward != null) r.boxReward.SetActive(false);
                if (r.itemRiwayat != null) r.itemRiwayat.SetActive(false);
            }
        }

        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (panelRiwayat != null) panelRiwayat.SetActive(false);
        if (tandaBaru != null) tandaBaru.SetActive(false);

        if (tombolIkonPesan != null) tombolIkonPesan.onClick.AddListener(BukaTutupRiwayat);
        if (tombolTutupRiwayat != null) tombolTutupRiwayat.onClick.AddListener(TutupRiwayat);

        // Tombol hanya bisa diklik mouse: Space/Enter/panah tidak ikut menekannya
        MatikanNavigasi(tombolIkonPesan);
        MatikanNavigasi(tombolTutupRiwayat);
    }

    void OnDestroy()
    {
        if (tombolIkonPesan != null) tombolIkonPesan.onClick.RemoveListener(BukaTutupRiwayat);
        if (tombolTutupRiwayat != null) tombolTutupRiwayat.onClick.RemoveListener(TutupRiwayat);
    }

    // Versi sederhana: panggil dari script mana pun, langsung jalan (tidak ditunggu)
    public void TampilkanReward(int index)
    {
        StartCoroutine(RewardRoutine(index));
    }

    // Versi coroutine: dipakai dengan "yield return StartCoroutine(...)" supaya alur aktivitas menunggu box hilang
    public IEnumerator RewardRoutine(int index)
    {
        if (daftarReward == null || index < 0 || index >= daftarReward.Length)
        {
            int jumlah = (daftarReward == null) ? 0 : daftarReward.Length;
            Debug.LogWarning("[Reward] Nomor reward " + index + " tidak ada. Daftar Reward hanya berisi " + jumlah
                + " elemen, jadi nomor yang valid adalah 0 sampai " + (jumlah - 1) + ". Reward tidak ditampilkan.");
            yield break;
        }

        RewardItem r = daftarReward[index];

        // Catat ke riwayat
        r.jumlah++;
        Debug.Log("[Reward] Riwayat dicatat: " + r.nama + " (x" + r.jumlah + "). Slot Item Riwayat terisi: " + (r.itemRiwayat != null)
            + ", Slot Box Reward terisi: " + (r.boxReward != null));
        TambahEntriRiwayat(r);
        if (tandaBaru != null && (panelRiwayat == null || !panelRiwayat.activeSelf)) tandaBaru.SetActive(true);

        if (r.boxReward == null) yield break;

        if (alurMenungguReward)
        {
            // Alur aktivitas menunggu sampai box reward hilang
            yield return StartCoroutine(TampilkanBoxRoutine(r));
        }
        else
        {
            // Box reward berjalan sendiri, alur aktivitas langsung lanjut (lebih cepat)
            StartCoroutine(TampilkanBoxRoutine(r));
        }
    }

    // Menampilkan box reward lalu menyembunyikannya sendiri
    private IEnumerator TampilkanBoxRoutine(RewardItem r)
    {

        // Kalau ada reward lain yang sedang tampil, tunggu giliran
        // (dengan batas waktu: kalau reward sebelumnya tersangkut, tidak boleh membuat aktivitas macet selamanya)
        float batasTunggu = lamaTampil + 1f;
        float sudahMenunggu = 0f;
        while (sedangTampil && sudahMenunggu < batasTunggu)
        {
            sudahMenunggu += Time.unscaledDeltaTime;
            yield return null;
        }

        if (sedangTampil)
        {
            Debug.LogWarning("[Reward] Reward sebelumnya tersangkut. Dipaksa selesai supaya tidak macet.");
            foreach (RewardItem lain in daftarReward)
            {
                if (lain.boxReward != null) lain.boxReward.SetActive(false);
            }
            if (overlayHitam != null) overlayHitam.SetActive(false);
        }

        sedangTampil = true;

        TutupRiwayat();

        // Urutan penting: overlay dulu, baru box
        if (overlayHitam != null) overlayHitam.SetActive(true);
        r.boxReward.SetActive(true);

        yield return new WaitForSecondsRealtime(lamaTampil);

        r.boxReward.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        sedangTampil = false;
    }

    // Tiap reward diterima, salinan "cetakan" entri ditambahkan ke panel riwayat (terbaru di paling atas)
    private void TambahEntriRiwayat(RewardItem r)
    {
        if (r.itemRiwayat == null) return;

        Transform induk = r.itemRiwayat.transform.parent;
        GameObject entri = Instantiate(r.itemRiwayat, induk);
        entri.name = "Riwayat_" + r.nama + "_" + r.jumlah;
        entri.SetActive(true);
        entri.transform.SetSiblingIndex(0);

        entriRiwayat.Add(entri);

        // Buang entri terlama kalau sudah melebihi batas
        while (maksEntriRiwayat > 0 && entriRiwayat.Count > maksEntriRiwayat)
        {
            if (entriRiwayat[0] != null) Destroy(entriRiwayat[0]);
            entriRiwayat.RemoveAt(0);
        }
    }

    // Dipanggil saat masuk level baru: riwayat dan hitungan reward level sebelumnya dikosongkan
    public void MulaiLevelBaru()
    {
        foreach (GameObject entri in entriRiwayat)
        {
            if (entri != null) Destroy(entri);
        }
        entriRiwayat.Clear();

        if (daftarReward != null)
        {
            foreach (RewardItem r in daftarReward) r.jumlah = 0;
        }

        TutupRiwayat();
        if (tandaBaru != null) tandaBaru.SetActive(false);

        Debug.Log("[Reward] Riwayat dikosongkan untuk level baru.");
    }

    public void BukaTutupRiwayat()
    {
        LepasFokusTombol();

        if (panelRiwayat == null || sedangTampil) return;

        bool buka = !panelRiwayat.activeSelf;
        panelRiwayat.SetActive(buka);

        if (buka && scrollRiwayat != null)
        {
            // Entri terbaru ada di paling atas, jadi tampilkan dari atas
            Canvas.ForceUpdateCanvases();
            scrollRiwayat.verticalNormalizedPosition = 1f;
        }

        if (buka && tandaBaru != null) tandaBaru.SetActive(false);
    }

    public void TutupRiwayat()
    {
        LepasFokusTombol();
        if (panelRiwayat != null) panelRiwayat.SetActive(false);
    }

    // Mematikan navigasi keyboard pada tombol, supaya Space/Enter tidak bisa "menekan" tombol yang sedang terpilih
    private void MatikanNavigasi(Button tombol)
    {
        if (tombol == null) return;

        Navigation nav = tombol.navigation;
        nav.mode = Navigation.Mode.None;
        tombol.navigation = nav;
    }

    // Setelah diklik, tombol tidak lagi menjadi objek terpilih di EventSystem
    private void LepasFokusTombol()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // ----- Menu tes: saat Play, klik kanan judul komponen Reward Manager di Inspector -----
    [ContextMenu("Tes Reward 0 (RPS)")]
    private void Tes0() { TampilkanReward(0); }

    [ContextMenu("Tes Reward 1 (Ngajar)")]
    private void Tes1() { TampilkanReward(1); }

    [ContextMenu("Tes Reward 2 (Bimbingan)")]
    private void Tes2() { TampilkanReward(2); }

    [ContextMenu("Tes Reward 3 (Referensi)")]
    private void Tes3() { TampilkanReward(3); }

    [ContextMenu("Tes Reward 4 (Penelitian)")]
    private void Tes4() { TampilkanReward(4); }

    [ContextMenu("Tes Reward 5 (Karya Ilmiah)")]
    private void Tes5() { TampilkanReward(5); }

    [ContextMenu("Tes Reward 6 (Seminar)")]
    private void Tes6() { TampilkanReward(6); }

    [ContextMenu("Tes Reward 7 (Pengabdian)")]
    private void Tes7() { TampilkanReward(7); }
}