using UnityEngine;

// Pasang di objek yang punya AudioSource supaya mengikuti pengaturan Musik / Suara dari pop-up Pengaturan.
// Musik: pasang di AudioSource musik latar. Suara: pasang di AudioSource efek suara (klik, langkah, dll).
// Pengaturan dibaca saat objek aktif dan otomatis ikut berubah begitu tombol Simpan ditekan.
public class PatuhPengaturanAudio : MonoBehaviour
{
    public enum Jenis { Musik, Suara }

    public Jenis jenis = Jenis.Musik;
    [Tooltip("Kosong = semua AudioSource di objek ini.")]
    public AudioSource[] sumber;

    void Awake()
    {
        if (sumber == null || sumber.Length == 0) sumber = GetComponents<AudioSource>();
    }

    void OnEnable()
    {
        PengaturanAudio.OnBerubah += Terapkan;
        Terapkan();
    }

    void OnDisable()
    {
        PengaturanAudio.OnBerubah -= Terapkan;
    }

    private void Terapkan()
    {
        bool mati = jenis == Jenis.Musik ? PengaturanAudio.MusikMati : PengaturanAudio.SuaraMati;

        if (sumber == null) return;
        foreach (AudioSource s in sumber)
        {
            if (s != null) s.mute = mati;
        }
    }
}