using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Baterai bersegmen. Dipanggil dari LaptopKelas: SetProgress(0..1).
/// Segmen terisi bertahap, warna berubah: merah -> kuning -> hijau.
/// Contoh 6 segmen, cooldown 60 detik:
///   detik 0-9   : kosong
///   detik 10-19 : 1 batang merah, detik 20: 2 batang merah
///   detik 40    : 4 batang kuning
///   detik 60    : 6 batang hijau (penuh), lalu ikon E muncul
/// </summary>
public class IndikatorBaterai : MonoBehaviour
{
    [Header("Segmen Baterai (urut dari kiri ke kanan)")]
    public GameObject[] segmen;            // Tiap segmen: SpriteRenderer ATAU UI Image

    [Header("Warna")]
    public Color warnaMerah = new Color(0.9f, 0.1f, 0.1f);
    public Color warnaKuning = new Color(0.95f, 0.8f, 0.1f);
    public Color warnaHijau = new Color(0.2f, 0.8f, 0.2f);

    [Header("Batas Perubahan Warna (0-1)")]
    [Range(0f, 1f)] public float batasMerah = 0.34f;    // di bawah ini = merah
    [Range(0f, 1f)] public float batasKuning = 0.67f;   // di bawah ini = kuning, di atasnya hijau

    public void SetProgress(float progress)
    {
        if (segmen == null || segmen.Length == 0) return;

        progress = Mathf.Clamp01(progress);

        // Jumlah segmen yang terisi (detik awal = kosong)
        int terisi = Mathf.FloorToInt(progress * segmen.Length);

        Color warna = warnaMerah;
        if (progress >= batasKuning) warna = warnaHijau;
        else if (progress >= batasMerah) warna = warnaKuning;

        for (int i = 0; i < segmen.Length; i++)
        {
            if (segmen[i] == null) continue;

            bool aktif = i < terisi;
            segmen[i].SetActive(aktif);

            if (aktif) UbahWarna(segmen[i], warna);
        }
    }

    private void UbahWarna(GameObject obj, Color warna)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) { sr.color = warna; return; }

        Image img = obj.GetComponent<Image>();
        if (img != null) img.color = warna;
    }
}