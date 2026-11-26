using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pop-up "Pengaturan" di dalam game (tombol Settings di HUD bagian atas).
///
/// Alur:
///  1. Tombol Pengaturan (ikon garis tiga di HUD) ditekan -> overlay + box Pengaturan muncul
///  2. Tombol Suara / Musik: tiap diklik gambarnya berganti (nyala <-> tercoret), sama seperti di Pengaturan menu utama.
///     Perubahan langsung berlaku dan langsung tersimpan (di pop-up ini tidak ada tombol Simpan).
///  3. Tombol Home -> muncul konfirmasi: 1 box kalimat + 2 box tombol (Ya / Tidak)
///       Tidak = konfirmasi ditutup, tetap di game
///       Ya    = keluar ke Main Menu
///  4. Tombol X atau Esc menutup pop-up (Esc saat konfirmasi tampil = sama dengan Tidak)
///
/// Pasang di satu objek yang SELALU AKTIF (mis. "PengaturanManager").
/// Jangan isi On Click tombol-tombol itu di Inspector, semuanya dipasang lewat script.
/// Memakai PengaturanAudio dan UrutanUI dari PopupPengaturan.cs (file itu harus tetap ada di project).
/// </summary>
public class PopupPengaturanGame : MonoBehaviour
{
    [Header("Tombol Pembuka")]
    public Button tombolPengaturan;             // Tombol Settings di HUD (ikon garis tiga)

    [Header("Panel Pengaturan")]
    public GameObject overlay;                  // Panel hitam transparan satu layar di belakang box (opsional)
    public GameObject panelPengaturan;          // Box "PENGATURAN" (induk dari tombol suara, musik, home, X). Nonaktif di awal
    public Button tombolTutup;                  // Tombol X merah
    public Button tombolSuara;                  // Tombol speaker
    public Button tombolMusik;                  // Tombol not balok
    public Button tombolHome;                   // Tombol rumah

    [Header("Tombol Suara - Cara 1: dua objek Image terpisah (dipakai kalau diisi)")]
    [Tooltip("Objek Image saat suara NYALA (boleh kosong kalau hanya ada gambar mati).")]
    public GameObject objekSuaraNyala;
    [Tooltip("Objek Image saat suara MATI (mis. speaker tercoret).")]
    public GameObject objekSuaraMati;

    [Header("Tombol Musik - Cara 1: dua objek Image terpisah (dipakai kalau diisi)")]
    public GameObject objekMusikNyala;
    public GameObject objekMusikMati;

    [Header("Tombol untuk Gambar Mati (opsional)")]
    [Tooltip("Kalau objek gambar mati kamu jadikan Button sendiri, isi di sini (atau biarkan kosong, Button di objek itu dicari otomatis). Klik di tombol ini juga menyalakan lagi.")]
    public Button tombolSuaraMati;
    public Button tombolMusikMati;

    [Header("Tombol Suara - Cara 2: ganti sprite (dipakai kalau Cara 1 kosong)")]
    [Tooltip("Image yang berganti gambar. Kosong = Image milik Tombol Suara.")]
    public Image gambarSuara;
    public Sprite spriteSuaraNyala;
    public Sprite spriteSuaraMati;

    [Header("Tombol Musik - Cara 2: ganti sprite (dipakai kalau Cara 1 kosong)")]
    [Tooltip("Image yang berganti gambar. Kosong = Image milik Tombol Musik.")]
    public Image gambarMusik;
    public Sprite spriteMusikNyala;
    public Sprite spriteMusikMati;

    [Header("Konfirmasi Keluar ke Main Menu")]
    [Tooltip("Overlay gelap khusus konfirmasi (opsional). Kosong = tanpa overlay tambahan.")]
    public GameObject overlayKonfirmasi;
    [Tooltip("Isi 3 objek: box kalimat, box Ya, box Tidak (urutan bebas). Semuanya tampil/hilang bersama.")]
    public GameObject[] boxKonfirmasi;
    public Button tombolYa;                     // Button di dalam box Ya
    public Button tombolTidak;                  // Button di dalam box Tidak

    [Header("Keluar ke Main Menu")]
    public string namaSceneMainMenu = "MainMenu";   // Nama scene menu utama (harus ada di Build Profiles > Scene List)
    [Tooltip("Dipakai kalau diisi 0 atau lebih. -1 = pakai Nama Scene Main Menu.")]
    public int indexSceneMainMenu = -1;
    public UnityEvent saatKeMainMenu;           // Opsional: dipanggil tepat sebelum pindah scene (mis. simpan sesuatu)

    [Header("Perilaku")]
    public bool jedaGameSaatBuka = true;        // true = game berhenti (Time.timeScale = 0) selama pop-up terbuka
    public KeyCode tombolTutupKeyboard = KeyCode.Escape;

    private bool sedangMenjeda = false;
    private float timeScaleSebelumnya = 1f;

    private Button suaraMatiAktif;
    private Button musikMatiAktif;

    private bool KonfirmasiTampil
    {
        get { return boxKonfirmasi != null && boxKonfirmasi.Length > 0 && boxKonfirmasi[0] != null && boxKonfirmasi[0].activeSelf; }
    }

    void Start()
    {
        if (panelPengaturan != null) panelPengaturan.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        SembunyikanKonfirmasi();

        if (gambarSuara == null && tombolSuara != null) gambarSuara = tombolSuara.image;
        if (gambarMusik == null && tombolMusik != null) gambarMusik = tombolMusik.image;

        if (tombolPengaturan != null) tombolPengaturan.onClick.AddListener(Buka);
        else Debug.LogWarning("[PengaturanGame] Slot Tombol Pengaturan masih kosong di Inspector.");

        if (tombolTutup != null) tombolTutup.onClick.AddListener(Tutup);
        if (tombolSuara != null) tombolSuara.onClick.AddListener(UbahSuara);
        if (tombolMusik != null) tombolMusik.onClick.AddListener(UbahMusik);
        if (tombolHome != null) tombolHome.onClick.AddListener(TampilkanKonfirmasi);
        if (tombolYa != null) tombolYa.onClick.AddListener(KeMainMenu);
        if (tombolTidak != null) tombolTidak.onClick.AddListener(SembunyikanKonfirmasi);

        SiapkanKlikGambar();

        if (panelPengaturan == null)
            Debug.LogWarning("[PengaturanGame] Slot Panel Pengaturan masih kosong di Inspector. Pop-up tidak bisa tampil.");

        PerbaruiGambar();
    }

    void OnDestroy()
    {
        if (tombolPengaturan != null) tombolPengaturan.onClick.RemoveListener(Buka);
        if (tombolTutup != null) tombolTutup.onClick.RemoveListener(Tutup);
        if (tombolSuara != null) tombolSuara.onClick.RemoveListener(UbahSuara);
        if (tombolMusik != null) tombolMusik.onClick.RemoveListener(UbahMusik);
        if (tombolHome != null) tombolHome.onClick.RemoveListener(TampilkanKonfirmasi);
        if (tombolYa != null) tombolYa.onClick.RemoveListener(KeMainMenu);
        if (tombolTidak != null) tombolTidak.onClick.RemoveListener(SembunyikanKonfirmasi);
        if (suaraMatiAktif != null) suaraMatiAktif.onClick.RemoveListener(UbahSuara);
        if (musikMatiAktif != null) musikMatiAktif.onClick.RemoveListener(UbahMusik);

        LanjutkanGame();   // jangan sampai game tertinggal dalam keadaan berhenti
    }

    // Gambar nyala/mati jangan menghalangi klik ke tombol. Kalau gambar mati adalah Button sendiri, klik di situ juga mengubah.
    private void SiapkanKlikGambar()
    {
        suaraMatiAktif = tombolSuaraMati != null ? tombolSuaraMati : CariButton(objekSuaraMati);
        musikMatiAktif = tombolMusikMati != null ? tombolMusikMati : CariButton(objekMusikMati);

        if (suaraMatiAktif != null && suaraMatiAktif != tombolSuara) suaraMatiAktif.onClick.AddListener(UbahSuara);
        else suaraMatiAktif = null;

        if (musikMatiAktif != null && musikMatiAktif != tombolMusik) musikMatiAktif.onClick.AddListener(UbahMusik);
        else musikMatiAktif = null;

        LewatkanKlik(objekSuaraNyala);
        LewatkanKlik(objekSuaraMati);
        LewatkanKlik(objekMusikNyala);
        LewatkanKlik(objekMusikMati);
    }

    private Button CariButton(GameObject objek)
    {
        return objek != null ? objek.GetComponent<Button>() : null;
    }

    // Objek yang bukan Button: semua gambarnya dibuat tembus klik, supaya klik sampai ke tombol di bawahnya
    private void LewatkanKlik(GameObject objek)
    {
        if (objek == null || objek.GetComponent<Button>() != null) return;

        foreach (Graphic g in objek.GetComponentsInChildren<Graphic>(true))
        {
            // Lewati gambar yang milik Button yang ada DI DALAM objek ini (Button itu memang harus bisa diklik)
            Button b = g.GetComponentInParent<Button>(true);
            if (b != null && b.transform.IsChildOf(objek.transform)) continue;

            g.raycastTarget = false;
        }
    }

    void Update()
    {
        if (panelPengaturan == null || !panelPengaturan.activeSelf) return;
        if (!Input.GetKeyDown(tombolTutupKeyboard)) return;

        if (KonfirmasiTampil) SembunyikanKonfirmasi();
        else Tutup();
    }

    // ---------- Buka / tutup ----------
    public void Buka()
    {
        if (panelPengaturan == null) return;
        if (panelPengaturan.activeSelf) return;

        PerbaruiGambar();
        UrutanUI.TampilkanDiAtasOverlay(overlay, panelPengaturan);
        JedaGame();
    }

    public void Tutup()
    {
        SembunyikanKonfirmasi();
        if (panelPengaturan != null) panelPengaturan.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        LanjutkanGame();
    }

    // ---------- Suara & musik (langsung tersimpan) ----------
    private void UbahSuara()
    {
        PengaturanAudio.Simpan(PengaturanAudio.MusikMati, !PengaturanAudio.SuaraMati);
        PerbaruiGambar();
    }

    private void UbahMusik()
    {
        PengaturanAudio.Simpan(!PengaturanAudio.MusikMati, PengaturanAudio.SuaraMati);
        PerbaruiGambar();
    }

    private void PerbaruiGambar()
    {
        TerapkanTampilan(PengaturanAudio.SuaraMati, objekSuaraNyala, objekSuaraMati, gambarSuara, spriteSuaraNyala, spriteSuaraMati);
        TerapkanTampilan(PengaturanAudio.MusikMati, objekMusikNyala, objekMusikMati, gambarMusik, spriteMusikNyala, spriteMusikMati);
    }

    // Cara 1 (objek nyala/mati) dipakai kalau salah satunya diisi, kalau tidak pakai Cara 2 (ganti sprite)
    private void TerapkanTampilan(bool mati, GameObject objekNyala, GameObject objekMati, Image gambar, Sprite spriteNyala, Sprite spriteMati)
    {
        if (objekNyala != null || objekMati != null)
        {
            if (objekNyala != null) objekNyala.SetActive(!mati);
            if (objekMati != null) objekMati.SetActive(mati);
            return;
        }

        if (gambar != null)
        {
            Sprite s = mati ? spriteMati : spriteNyala;
            if (s != null) gambar.sprite = s;
        }
    }

    // ---------- Konfirmasi Home ----------
    private void TampilkanKonfirmasi()
    {
        if (boxKonfirmasi == null || boxKonfirmasi.Length == 0)
        {
            Debug.LogWarning("[PengaturanGame] Box Konfirmasi belum diisi di Inspector.");
            return;
        }

        // Pertama: overlay konfirmasi (kalau ada) di atas panel Pengaturan, lalu semua box konfirmasi paling depan
        if (overlayKonfirmasi != null)
        {
            Canvas canvas = panelPengaturan != null ? panelPengaturan.GetComponentInParent<Canvas>(true) : null;
            Transform akar = canvas != null ? canvas.rootCanvas.transform : overlayKonfirmasi.transform.root;
            overlayKonfirmasi.SetActive(true);
            if (overlayKonfirmasi.transform.parent != akar) overlayKonfirmasi.transform.SetParent(akar, false);
            overlayKonfirmasi.transform.SetAsLastSibling();
        }

        foreach (GameObject box in boxKonfirmasi)
        {
            if (box == null) continue;
            box.SetActive(true);
            BawaCabangKeDepan(box);
        }
    }

    private void SembunyikanKonfirmasi()
    {
        if (overlayKonfirmasi != null) overlayKonfirmasi.SetActive(false);
        if (boxKonfirmasi == null) return;

        foreach (GameObject box in boxKonfirmasi)
            if (box != null) box.SetActive(false);
    }

    // Menaruh cabang yang memuat objek ini paling depan di bawah akar Canvas
    private void BawaCabangKeDepan(GameObject objek)
    {
        Canvas canvas = objek.GetComponentInParent<Canvas>(true);
        Transform akar = canvas != null ? canvas.rootCanvas.transform : objek.transform.root;

        Transform cabang = objek.transform;
        while (cabang.parent != null && cabang.parent != akar) cabang = cabang.parent;
        cabang.SetAsLastSibling();
    }

    private void KeMainMenu()
    {
        Debug.Log("[PengaturanGame] Keluar ke Main Menu.");
        saatKeMainMenu.Invoke();

        LanjutkanGame();   // Time.timeScale kembali normal sebelum pindah scene

        if (indexSceneMainMenu >= 0) SceneManager.LoadScene(indexSceneMainMenu);
        else SceneManager.LoadScene(namaSceneMainMenu);
    }

    // ---------- Jeda game ----------
    private void JedaGame()
    {
        if (!jedaGameSaatBuka || sedangMenjeda) return;

        sedangMenjeda = true;
        timeScaleSebelumnya = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void LanjutkanGame()
    {
        if (!sedangMenjeda) return;

        sedangMenjeda = false;
        Time.timeScale = timeScaleSebelumnya > 0f ? timeScaleSebelumnya : 1f;
    }
}