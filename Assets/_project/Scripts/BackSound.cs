using System.Collections;
using System.Reflection;
using UnityEngine;

// Musik latar (backsound) permainan.
// Cara pakai:
//   1. Masukkan file musik (mp3 / wav / ogg) ke folder Assets.
//   2. Buat objek kosong di Hierarchy (mis. "BackSound"), pasang script ini.
//   3. Seret file musik ke slot "Musik". Atur "Volume" sesuai selera.
//   4. Play: musik mulai dan berulang terus (loop). Musik tetap berbunyi walau game sedang berhenti
//      (pop-up, tutorial, dll), karena suara tidak ikut Time.timeScale.
//
// Ganti musik di tengah permainan (mis. saat level berganti): panggil GantiLagu dari UnityEvent
// atau dari script lain. Contoh: BackSound.GantiLagu(laguLevel2).
public class BackSound : MonoBehaviour
{
    [Header("Musik")]
    public AudioClip musik;                         // File musik latar. Kosong = tidak ada musik
    [Range(0f, 1f)] public float volume = 0.4f;     // Sebaiknya lebih pelan dari sound effect supaya tidak menutupi suara lain
    public bool mainkanSaatMulai = true;            // true = musik mulai begitu scene dimuat
    public float lamaFadeIn = 2f;                   // Musik naik pelan-pelan (detik). 0 = langsung

    [Header("Pengaturan dari Menu")]
    [Tooltip("true = ikut tombol musik di panel pengaturan (kalau musik dimatikan di menu, backsound ikut senyap).")]
    public bool ikutPengaturanMusik = true;

    [Header("Audio Source")]
    public AudioSource audioSource;                 // Kosong = dibuat otomatis di objek ini

    private Coroutine fadeBerjalan;
    private bool pengaturanTidakDitemukanDilaporkan = false;

    // Nama field / properti static di PengaturanAudio yang menandai musik dimatikan.
    // Dicari otomatis supaya script ini tetap bisa dikompilasi walau namanya berbeda.
    private static readonly string[] namaPenandaMusikMati = { "MusikMati", "MusicMati", "BacksoundMati", "BackSoundMati", "BgmMati" };
    private FieldInfo fieldMusikMati;
    private PropertyInfo propertiMusikMati;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;   // suara 2D: sama keras di mana pun posisi kamera

        CariPenandaMusikMati();
    }

    void Start()
    {
        if (mainkanSaatMulai) Mainkan();
    }

    void Update()
    {
        // Ikut pengaturan musik dari menu (dicek terus supaya langsung berlaku saat tombol di panel pengaturan ditekan)
        if (ikutPengaturanMusik && audioSource != null)
            audioSource.mute = MusikSedangDimatikan();
    }

    // Mulai memainkan musik (naik pelan-pelan sesuai Lama Fade In)
    public void Mainkan()
    {
        if (musik == null)
        {
            Debug.LogWarning("[BackSound] Slot Musik masih kosong di Inspector, tidak ada musik yang dimainkan.");
            return;
        }

        audioSource.clip = musik;
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.Play();

        JalankanFade(volume, lamaFadeIn, null);
    }

    // Hentikan musik pelan-pelan
    public void Hentikan(float lamaFade = 1f)
    {
        JalankanFade(0f, lamaFade, () => audioSource.Stop());
    }

    // Ganti ke lagu lain: lagu lama turun pelan-pelan, lagu baru naik pelan-pelan
    public void GantiLagu(AudioClip laguBaru)
    {
        if (laguBaru == null || laguBaru == audioSource.clip) return;
        StartCoroutine(ProsesGantiLagu(laguBaru));
    }

    private IEnumerator ProsesGantiLagu(AudioClip laguBaru)
    {
        if (fadeBerjalan != null) StopCoroutine(fadeBerjalan);

        float lama = Mathf.Max(0.01f, lamaFadeIn * 0.5f);
        float awal = audioSource.volume;
        for (float t = 0f; t < lama; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(awal, 0f, t / lama);
            yield return null;
        }

        musik = laguBaru;
        audioSource.clip = laguBaru;
        audioSource.volume = 0f;
        audioSource.Play();

        JalankanFade(volume, lamaFadeIn, null);
    }

    private void JalankanFade(float tujuan, float lama, System.Action setelahSelesai)
    {
        if (fadeBerjalan != null) StopCoroutine(fadeBerjalan);
        fadeBerjalan = StartCoroutine(ProsesFade(tujuan, lama, setelahSelesai));
    }

    // Fade memakai waktu nyata, jadi tetap jalan walau Time.timeScale = 0
    private IEnumerator ProsesFade(float tujuan, float lama, System.Action setelahSelesai)
    {
        float awal = audioSource.volume;

        if (lama > 0f)
        {
            for (float t = 0f; t < lama; t += Time.unscaledDeltaTime)
            {
                audioSource.volume = Mathf.Lerp(awal, tujuan, t / lama);
                yield return null;
            }
        }

        audioSource.volume = tujuan;
        fadeBerjalan = null;

        if (setelahSelesai != null) setelahSelesai();
    }

    // ---------- Pengaturan musik dari menu ----------
    private void CariPenandaMusikMati()
    {
        System.Type tipe = typeof(PengaturanAudio);
        BindingFlags bendera = BindingFlags.Public | BindingFlags.Static;

        foreach (string nama in namaPenandaMusikMati)
        {
            FieldInfo f = tipe.GetField(nama, bendera);
            if (f != null && f.FieldType == typeof(bool)) { fieldMusikMati = f; return; }

            PropertyInfo p = tipe.GetProperty(nama, bendera);
            if (p != null && p.PropertyType == typeof(bool) && p.CanRead) { propertiMusikMati = p; return; }
        }
    }

    private bool MusikSedangDimatikan()
    {
        if (fieldMusikMati != null) return (bool)fieldMusikMati.GetValue(null);
        if (propertiMusikMati != null) return (bool)propertiMusikMati.GetValue(null);

        if (!pengaturanTidakDitemukanDilaporkan)
        {
            pengaturanTidakDitemukanDilaporkan = true;
            Debug.LogWarning("[BackSound] Penanda 'musik dimatikan' tidak ditemukan di PengaturanAudio, jadi backsound tidak ikut tombol musik di menu. " +
                             "Kirim script PengaturanAudio supaya disambungkan dengan benar.");
        }

        return false;
    }
}