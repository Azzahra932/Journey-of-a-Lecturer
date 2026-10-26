using System.Collections;
using UnityEngine;

// Ruangan yang belum dipakai di level ini: tampil gelap semi-transparan dan tidak bisa dimasuki.
// Pasang di objek "zona pintu" yang punya Collider2D dengan Is Trigger dicentang (area di depan pintu).
public class RuangTerkunci : MonoBehaviour
{
    [Header("Status")]
    public bool terkunciDiAwal = true;          // true = ruangan terkunci saat level dimulai

    [Header("Tampilan Terkunci")]
    public GameObject penutupGelap;             // Sprite hitam semi-transparan yang menutupi ruangan (nonaktif saat dibuka)
    public GameObject[] penghalang;             // Objek berisi Collider2D (BUKAN trigger) di pintu / mulut ruangan, dinonaktifkan saat dibuka
    public GameObject ikonKunci;                // Opsional: ikon gembok di pintu (nonaktif saat dibuka)

    [Header("Pesan Saat Pemain Mendekat")]
    public GameObject pesanTerkunci;            // Opsional: teks/box "Ruangan terkunci" (UI)
    public float lamaPesan = 1.5f;
    public string tagPemain = "Player";

    private bool terkunci;
    private Coroutine rutinPesan;

    void Start()
    {
        if (pesanTerkunci != null) pesanTerkunci.SetActive(false);
        Atur(terkunciDiAwal);
    }

    // Buka ruangan (mis. dipanggil dari event di level berikutnya)
    public void Buka()
    {
        Atur(false);
        Debug.Log("[RuangTerkunci] '" + name + "' dibuka.");
    }

    // Kunci ruangan lagi
    public void Kunci()
    {
        Atur(true);
    }

    private void Atur(bool kunci)
    {
        terkunci = kunci;

        if (penutupGelap != null) penutupGelap.SetActive(kunci);
        if (ikonKunci != null) ikonKunci.SetActive(kunci);

        if (penghalang != null)
        {
            foreach (GameObject p in penghalang)
            {
                if (p != null) p.SetActive(kunci);
            }
        }

        if (!kunci && pesanTerkunci != null) pesanTerkunci.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!terkunci || pesanTerkunci == null) return;
        if (!other.CompareTag(tagPemain)) return;

        if (rutinPesan != null) StopCoroutine(rutinPesan);
        rutinPesan = StartCoroutine(TampilkanPesan());
    }

    private IEnumerator TampilkanPesan()
    {
        pesanTerkunci.SetActive(true);
        yield return new WaitForSeconds(lamaPesan);
        pesanTerkunci.SetActive(false);
        rutinPesan = null;
    }
}