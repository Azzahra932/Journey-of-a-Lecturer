using UnityEngine;
using UnityEngine.UI;

// Pop-up "Tentang Game" di menu utama:
//   1. Tombol About ditekan -> muncul overlay (panel hitam transparan satu layar) dan box Tentang Game di atasnya
//   2. Tombol X di box (atau tombol Esc) menutup pop-up
//
// Pasang script ini di satu objek (mis. "MainMenuManager").
// Overlay dan box dibuat di Hierarchy (satu Canvas dengan tombol menu), lalu di-drag ke slot di Inspector.
public class PopupAbout : MonoBehaviour
{
    [Header("Tombol")]
    public Button tombolAbout;                  // Tombol "About" di menu utama (jangan isi On Click di Inspector, dipasang lewat script)
    public Button tombolTutup;                  // Tombol X di dalam box Tentang Game (jangan isi On Click di Inspector, dipasang lewat script)

    [Header("Panel")]
    public GameObject overlay;                  // Panel hitam transparan satu layar di belakang box (opsional)
    public GameObject panelAbout;               // Box Tentang Game (judul, teks, tombol X). Nonaktif di awal, diatur script

    [Header("Keyboard")]
    public KeyCode tombolTutupKeyboard = KeyCode.Escape;

    void Start()
    {
        if (panelAbout != null) panelAbout.SetActive(false);
        if (overlay != null) overlay.SetActive(false);

        if (tombolAbout != null) tombolAbout.onClick.AddListener(Buka);
        else Debug.LogWarning("[About] Slot Tombol About masih kosong di Inspector.");

        if (tombolTutup != null) tombolTutup.onClick.AddListener(Tutup);
        else Debug.LogWarning("[About] Slot Tombol Tutup (X) masih kosong di Inspector.");

        if (panelAbout == null)
            Debug.LogWarning("[About] Slot Panel About masih kosong di Inspector. Pop-up tidak bisa tampil.");
    }

    void OnDestroy()
    {
        if (tombolAbout != null) tombolAbout.onClick.RemoveListener(Buka);
        if (tombolTutup != null) tombolTutup.onClick.RemoveListener(Tutup);
    }

    void Update()
    {
        if (panelAbout != null && panelAbout.activeSelf && Input.GetKeyDown(tombolTutupKeyboard))
            Tutup();
    }

    // Dipanggil tombol About
    public void Buka()
    {
        if (panelAbout == null) return;

        // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
        if (overlay != null)
        {
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
        }

        panelAbout.SetActive(true);
        panelAbout.transform.SetAsLastSibling();
    }

    // Dipanggil tombol X
    public void Tutup()
    {
        if (panelAbout != null) panelAbout.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
    }
}