using System;
using UnityEngine;
using UnityEngine.UI;

// Menyimpan dan membaca pengaturan suara dan musik (PlayerPrefs), dipakai lintas scene.
// Script lain cukup membaca PengaturanAudio.MusikMati / SuaraMati, atau pasang PatuhPengaturanAudio di AudioSource.
public static class PengaturanAudio
{
    private const string KunciMusikMati = "Pengaturan_MusikMati";
    private const string KunciSuaraMati = "Pengaturan_SuaraMati";

    private static bool dimuat = false;
    private static bool musikMati = false;
    private static bool suaraMati = false;

    // Dipanggil setiap kali pengaturan disimpan
    public static event Action OnBerubah;

    public static bool MusikMati { get { Muat(); return musikMati; } }
    public static bool SuaraMati { get { Muat(); return suaraMati; } }

    private static void Muat()
    {
        if (dimuat) return;
        musikMati = PlayerPrefs.GetInt(KunciMusikMati, 0) == 1;
        suaraMati = PlayerPrefs.GetInt(KunciSuaraMati, 0) == 1;
        dimuat = true;
    }

    public static void Simpan(bool musikMatiBaru, bool suaraMatiBaru)
    {
        musikMati = musikMatiBaru;
        suaraMati = suaraMatiBaru;
        dimuat = true;

        PlayerPrefs.SetInt(KunciMusikMati, musikMati ? 1 : 0);
        PlayerPrefs.SetInt(KunciSuaraMati, suaraMati ? 1 : 0);
        PlayerPrefs.Save();

        if (OnBerubah != null) OnBerubah.Invoke();
    }
}

// Menampilkan box di atas overlay, walau keduanya beda induk di Hierarchy.
// Overlay dipindah ke akar Canvas (satu kali), lalu cabang yang memuat box ditaruh paling depan, di atas overlay.
public static class UrutanUI
{
    public static void TampilkanDiAtasOverlay(GameObject overlay, GameObject panel)
    {
        if (panel == null) return;

        Canvas canvas = panel.GetComponentInParent<Canvas>(true);
        Transform akar = canvas != null ? canvas.rootCanvas.transform : panel.transform.root;

        // Cabang di bawah akar Canvas yang memuat box (bisa box itu sendiri)
        Transform cabang = panel.transform;
        while (cabang.parent != null && cabang.parent != akar) cabang = cabang.parent;

        if (overlay != null)
        {
            overlay.SetActive(true);
            if (overlay.transform.parent != akar) overlay.transform.SetParent(akar, false);
            overlay.transform.SetAsLastSibling();
        }

        panel.SetActive(true);
        cabang.SetAsLastSibling();   // Setelah overlay, jadi box tampil di atas overlay
    }
}

// Pop-up "Pengaturan" di menu utama:
//   1. Tombol Settings ditekan -> muncul overlay dan box Pengaturan
//   2. Tombol Suara dan tombol Musik: setiap diklik gambarnya berganti (nyala <-> tercoret) = suara/musik dimatikan atau dinyalakan
//   3. Tombol Simpan menyimpan pilihan (tetap tersimpan walau game ditutup dan dibuka lagi) lalu menutup pop-up
//   4. Tombol X (atau Esc) menutup TANPA menyimpan, perubahan yang belum disimpan dibatalkan
//   5. Tombol tanda tanya membuka pop-up Tentang Game (PopupAbout)
//
// Pasang script ini di satu objek (mis. "MainMenuManager"). Jangan isi On Click di Inspector, semua tombol dipasang lewat script.
public class PopupPengaturan : MonoBehaviour
{
    [Header("Tombol Pembuka")]
    public Button tombolPengaturan;             // Tombol Settings (ikon gerigi) di menu utama

    [Header("Panel")]
    public GameObject overlay;                  // Panel hitam transparan satu layar di belakang box (opsional)
    public GameObject panelPengaturan;          // Box Pengaturan. Nonaktif di awal, diatur script

    [Header("Tombol di dalam Box")]
    public Button tombolTutup;                  // Tombol X
    public Button tombolSimpan;                 // Tombol Simpan
    public Button tombolSuara;                  // Tombol speaker (suara)
    public Button tombolMusik;                  // Tombol not balok (musik)
    public Button tombolTentang;                // Tombol tanda tanya (membuka Tentang Game)
    public PopupAbout popupAbout;               // Script PopupAbout (kosong = dicari otomatis)

    [Header("Gambar Tombol Suara")]
    [Tooltip("Image yang berganti gambar. Kosong = Image milik Tombol Suara.")]
    public Image gambarSuara;
    public Sprite spriteSuaraNyala;             // Speaker bergelombang
    public Sprite spriteSuaraMati;              // Speaker tercoret

    [Header("Gambar Tombol Musik")]
    [Tooltip("Image yang berganti gambar. Kosong = Image milik Tombol Musik.")]
    public Image gambarMusik;
    public Sprite spriteMusikNyala;             // Not balok
    public Sprite spriteMusikMati;              // Not balok tercoret

    [Header("Perilaku")]
    public bool tutupSetelahSimpan = true;
    public KeyCode tombolTutupKeyboard = KeyCode.Escape;

    // Pilihan sementara di pop-up (baru berlaku setelah Simpan)
    private bool musikMatiSementara;
    private bool suaraMatiSementara;

    void Start()
    {
        if (panelPengaturan != null) panelPengaturan.SetActive(false);
        if (overlay != null) overlay.SetActive(false);

        if (gambarSuara == null && tombolSuara != null) gambarSuara = tombolSuara.image;
        if (gambarMusik == null && tombolMusik != null) gambarMusik = tombolMusik.image;
        if (popupAbout == null) popupAbout = FindFirstObjectByType<PopupAbout>(FindObjectsInactive.Include);

        if (tombolPengaturan != null) tombolPengaturan.onClick.AddListener(Buka);
        else Debug.LogWarning("[Pengaturan] Slot Tombol Pengaturan masih kosong di Inspector.");

        if (tombolTutup != null) tombolTutup.onClick.AddListener(TutupTanpaSimpan);
        if (tombolSimpan != null) tombolSimpan.onClick.AddListener(Simpan);
        if (tombolSuara != null) tombolSuara.onClick.AddListener(UbahSuara);
        if (tombolMusik != null) tombolMusik.onClick.AddListener(UbahMusik);
        if (tombolTentang != null) tombolTentang.onClick.AddListener(BukaTentang);

        if (panelPengaturan == null)
            Debug.LogWarning("[Pengaturan] Slot Panel Pengaturan masih kosong di Inspector. Pop-up tidak bisa tampil.");
    }

    void OnDestroy()
    {
        if (tombolPengaturan != null) tombolPengaturan.onClick.RemoveListener(Buka);
        if (tombolTutup != null) tombolTutup.onClick.RemoveListener(TutupTanpaSimpan);
        if (tombolSimpan != null) tombolSimpan.onClick.RemoveListener(Simpan);
        if (tombolSuara != null) tombolSuara.onClick.RemoveListener(UbahSuara);
        if (tombolMusik != null) tombolMusik.onClick.RemoveListener(UbahMusik);
        if (tombolTentang != null) tombolTentang.onClick.RemoveListener(BukaTentang);
    }

    void Update()
    {
        if (panelPengaturan != null && panelPengaturan.activeSelf && Input.GetKeyDown(tombolTutupKeyboard))
            TutupTanpaSimpan();
    }

    public void Buka()
    {
        if (panelPengaturan == null) return;

        // Mulai dari pilihan yang sudah tersimpan
        musikMatiSementara = PengaturanAudio.MusikMati;
        suaraMatiSementara = PengaturanAudio.SuaraMati;
        PerbaruiGambar();

        UrutanUI.TampilkanDiAtasOverlay(overlay, panelPengaturan);
    }

    private void UbahSuara()
    {
        suaraMatiSementara = !suaraMatiSementara;
        PerbaruiGambar();
    }

    private void UbahMusik()
    {
        musikMatiSementara = !musikMatiSementara;
        PerbaruiGambar();
    }

    private void PerbaruiGambar()
    {
        if (gambarSuara != null)
        {
            Sprite s = suaraMatiSementara ? spriteSuaraMati : spriteSuaraNyala;
            if (s != null) gambarSuara.sprite = s;
        }

        if (gambarMusik != null)
        {
            Sprite s = musikMatiSementara ? spriteMusikMati : spriteMusikNyala;
            if (s != null) gambarMusik.sprite = s;
        }
    }

    public void Simpan()
    {
        PengaturanAudio.Simpan(musikMatiSementara, suaraMatiSementara);
        Debug.Log("[Pengaturan] Disimpan. Musik " + (musikMatiSementara ? "mati" : "nyala") + ", suara " + (suaraMatiSementara ? "mati" : "nyala") + ".");

        if (tutupSetelahSimpan) Tutup();
    }

    public void TutupTanpaSimpan()
    {
        Tutup();   // Pilihan sementara dibuang, pop-up berikutnya mulai lagi dari yang tersimpan
    }

    private void Tutup()
    {
        if (panelPengaturan != null) panelPengaturan.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
    }

    // Tombol tanda tanya: Pengaturan ditutup (perubahan yang belum disimpan dibuang), lalu Tentang Game muncul
    private void BukaTentang()
    {
        if (popupAbout == null)
        {
            Debug.LogWarning("[Pengaturan] PopupAbout tidak ditemukan. Pasang script PopupAbout di scene ini.");
            return;
        }

        Tutup();
        popupAbout.Buka();
    }
}