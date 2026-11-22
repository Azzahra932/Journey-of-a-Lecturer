using UnityEngine;
using UnityEngine.UI;

// Efek suara klik untuk semua tombol di scene ini (menu utama, pop-up Pengaturan, Tentang Game, dst).
// Pasang script ini di satu objek (mis. objek kosong "SuaraTombol"), isi Suara Klik, selesai.
// Semua Button di scene dicari otomatis saat scene dimuat (termasuk yang ada di pop-up yang nonaktif),
// jadi tidak perlu mengisi On Click satu per satu.
// Suara mengikuti pengaturan: kalau suara dimatikan di Pengaturan (dan sudah disimpan), klik tombol jadi senyap.
public class SuaraTombol : MonoBehaviour
{
    [Header("Suara")]
    public AudioClip suaraKlik;                 // Efek suara klik tombol
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Kosong = dibuat otomatis di objek ini.")]
    public AudioSource sumber;

    [Header("Tombol")]
    [Tooltip("Tombol yang tidak diberi suara klik oleh script ini (mis. yang sudah punya suara sendiri).")]
    public Button[] tombolDikecualikan;

    void Awake()
    {
        if (sumber == null)
        {
            sumber = GetComponent<AudioSource>();
            if (sumber == null) sumber = gameObject.AddComponent<AudioSource>();
        }

        sumber.playOnAwake = false;
        sumber.loop = false;
        sumber.spatialBlend = 0f;   // 2D, tidak terpengaruh posisi
    }

    void Start()
    {
        if (suaraKlik == null)
            Debug.LogWarning("[SuaraTombol] Slot Suara Klik masih kosong di Inspector, tombol tidak akan berbunyi.");

        Button[] semua = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in semua)
        {
            if (b == null || Dikecualikan(b)) continue;
            b.onClick.AddListener(Mainkan);
        }
    }

    private bool Dikecualikan(Button b)
    {
        if (tombolDikecualikan == null) return false;

        foreach (Button x in tombolDikecualikan)
        {
            if (x == b) return true;
        }

        return false;
    }

    // Boleh juga dipanggil dari event lain (mis. suara untuk objek yang bukan Button)
    public void Mainkan()
    {
        if (suaraKlik == null || sumber == null) return;
        if (PengaturanAudio.SuaraMati) return;

        sumber.PlayOneShot(suaraKlik, volume);
    }
}