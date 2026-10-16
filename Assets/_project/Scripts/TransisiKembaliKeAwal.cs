using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Transisi layar hitam, lalu player dikembalikan ke titik awal permainan.
/// HANYA memindahkan posisi player. Skor, integritas, dan progres scene tidak disentuh.
/// Pasang di objek yang SELALU AKTIF (mis. empty object "ManagerTransisi").
/// Panggil MulaiTransisi() saat energi habis (dari script energi, atau lewat UnityEvent).
/// </summary>
public class TransisiKembaliKeAwal : MonoBehaviour
{
    [Header("Layar Hitam (UI Image satu layar)")]
    public Image layarHitam;                 // Image hitam stretch penuh di Canvas, paling depan. Alpha diatur oleh script

    [Header("Player")]
    public GameObject playerBerjalan;        // TPdiam (kosong = dicari otomatis dengan nama "TPdiam")
    public Transform titikAwal;              // Opsional. Kosong = posisi TPdiam saat game mulai

    [Header("Kursi (supaya player dipaksa berdiri sebelum dipindah)")]
    public ChairInteraction kursiKelas;      // objek kursikelas (opsional)
    public InteractiveDesk kursiDosen;       // objek kursi (opsional)

    [Header("Durasi (detik)")]
    public float durasiGelap = 0.6f;         // layar menggelap
    public float durasiTahan = 0.8f;         // layar tetap hitam (di sini player dipindahkan)
    public float durasiTerang = 0.6f;        // layar terang kembali

    [Header("Pesan Saat Layar Hitam (Opsional)")]
    public GameObject pesanEnergiHabis;      // teks seperti "Energi habis... kembali ke awal"

    [Header("Setelah Selesai (Opsional)")]
    public UnityEvent saatTransisiSelesai;   // mis. isi ulang energi

    [Header("Uji Coba")]
    public KeyCode tombolTes = KeyCode.None; // isi mis. T untuk mengetes transisi tanpa menunggu energi habis

    public bool SedangTransisi { get; private set; }

    private Vector3 posisiAwal;

    void Start()
    {
        if (playerBerjalan == null) playerBerjalan = GameObject.Find("TPdiam");

        if (titikAwal != null) posisiAwal = titikAwal.position;
        else if (playerBerjalan != null) posisiAwal = playerBerjalan.transform.position;

        if (layarHitam != null)
        {
            Color c = layarHitam.color;
            c.a = 0f;
            layarHitam.color = c;
            layarHitam.gameObject.SetActive(false);
        }

        if (pesanEnergiHabis != null) pesanEnergiHabis.SetActive(false);
    }

    void Update()
    {
        if (tombolTes != KeyCode.None && Input.GetKeyDown(tombolTes))
            MulaiTransisi();
    }

    // Panggil ini saat energi habis
    public void MulaiTransisi()
    {
        if (SedangTransisi) return;
        StartCoroutine(ProsesTransisi());
    }

    private IEnumerator ProsesTransisi()
    {
        SedangTransisi = true;

        PlayerMovement pm = playerBerjalan != null ? playerBerjalan.GetComponent<PlayerMovement>() : null;

        // Kunci pergerakan player selama transisi
        if (pm != null)
        {
            if (pm.animator != null)
            {
                pm.animator.SetBool("IsMoving", false);
                pm.animator.SetFloat("MoveX", 0f);
                pm.animator.SetFloat("MoveY", 0f);
            }
            pm.enabled = false;
        }

        // 1. Layar menggelap
        yield return Fade(0f, 1f, durasiGelap);

        if (pesanEnergiHabis != null) pesanEnergiHabis.SetActive(true);

        // 2. Saat layar hitam: paksa berdiri dari kursi, lalu pindahkan player ke titik awal
        if (kursiKelas != null) kursiKelas.PaksaBerdiriSegera();
        if (kursiDosen != null) kursiDosen.PaksaBerdiri();

        if (playerBerjalan != null)
        {
            playerBerjalan.SetActive(true);
            playerBerjalan.transform.position = posisiAwal;

            Rigidbody2D rb = playerBerjalan.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = posisiAwal;

            Physics2D.SyncTransforms();

            if (pm != null) pm.Berdiri();   // reset status duduk dan sorting order
        }

        yield return new WaitForSeconds(durasiTahan);

        if (pesanEnergiHabis != null) pesanEnergiHabis.SetActive(false);

        // 3. Layar terang kembali
        yield return Fade(1f, 0f, durasiTerang);

        if (pm != null) pm.enabled = true;

        Debug.Log("[TransisiKembaliKeAwal] Player dikembalikan ke titik awal.");

        SedangTransisi = false;
        saatTransisiSelesai.Invoke();
    }

    private IEnumerator Fade(float dari, float ke, float durasi)
    {
        if (layarHitam == null) yield break;

        layarHitam.gameObject.SetActive(true);

        Color c = layarHitam.color;
        float t = 0f;

        while (t < durasi)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(dari, ke, t / durasi);
            layarHitam.color = c;
            yield return null;
        }

        c.a = ke;
        layarHitam.color = c;

        if (ke <= 0f) layarHitam.gameObject.SetActive(false);
    }
}