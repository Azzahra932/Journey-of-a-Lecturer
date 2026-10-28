using System.Collections;
using UnityEngine;

// Ruangan yang belum dipakai di level ini: tampil gelap semi-transparan dan tidak bisa dimasuki.
// Pasang di objek "zona pintu" yang punya Collider2D dengan Is Trigger dicentang (area di depan pintu pertama).
// Pintu ke-2, ke-3, dst. cukup didaftarkan di "Pintu Tambahan" (titik di depan pintu + teks pesannya sendiri).
public class RuangTerkunci : MonoBehaviour
{
    [System.Serializable]
    public class PintuTambahan
    {
        public Transform titik;      // Titik (objek kosong) di depan pintu ini
        public GameObject pesan;     // Teks pesan khusus pintu ini. Kosong = pakai "Pesan Terkunci" di atas
    }

    [Header("Status")]
    public bool terkunciDiAwal = true;          // true = ruangan terkunci saat level dimulai

    [Header("Tampilan Terkunci")]
    public GameObject penutupGelap;             // Sprite hitam semi-transparan yang menutupi ruangan (nonaktif saat dibuka)
    public GameObject[] penghalang;             // Objek berisi Collider2D (BUKAN trigger) di pintu / mulut ruangan, dinonaktifkan saat dibuka
    public GameObject ikonKunci;                // Opsional: ikon gembok di pintu (nonaktif saat dibuka)

    [Header("Pesan Saat Pemain Mendekat (Pintu Pertama)")]
    public GameObject pesanTerkunci;            // Teks/box "Ruangan terkunci"
    public float lamaPesan = 1.5f;
    public string tagPemain = "Player";

    [Header("Pintu Tambahan (Pintu ke-2, ke-3, ...)")]
    public PintuTambahan[] pintuTambahan;
    public float jarakPesan = 1.5f;             // Jarak pemain ke titik pintu supaya pesan muncul

    private bool terkunci;
    private Coroutine rutinPesan;
    private GameObject pesanAktif;
    private bool[] dekatSebelumnya;
    private Transform pemain;

    void Start()
    {
        if (pesanTerkunci != null) pesanTerkunci.SetActive(false);

        if (pintuTambahan != null)
        {
            dekatSebelumnya = new bool[pintuTambahan.Length];
            foreach (PintuTambahan p in pintuTambahan)
            {
                if (p != null && p.pesan != null) p.pesan.SetActive(false);
            }
        }

        Atur(terkunciDiAwal);
    }

    void Update()
    {
        if (!terkunci || pintuTambahan == null || pintuTambahan.Length == 0) return;

        if (pemain == null || !pemain.gameObject.activeInHierarchy)
        {
            GameObject g = GameObject.FindGameObjectWithTag(tagPemain);
            pemain = (g != null) ? g.transform : null;
            if (pemain == null) return;
        }

        for (int i = 0; i < pintuTambahan.Length; i++)
        {
            PintuTambahan p = pintuTambahan[i];
            if (p == null || p.titik == null) continue;

            bool dekat = Vector2.Distance(p.titik.position, pemain.position) <= jarakPesan;

            // Pesan muncul sekali saat pemain baru masuk jangkauan pintu ini
            if (dekat && !dekatSebelumnya[i])
                TampilkanPesanDi(p.pesan != null ? p.pesan : pesanTerkunci);

            dekatSebelumnya[i] = dekat;
        }
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

        if (!kunci)
        {
            if (rutinPesan != null) StopCoroutine(rutinPesan);
            rutinPesan = null;
            pesanAktif = null;

            if (pesanTerkunci != null) pesanTerkunci.SetActive(false);
            if (pintuTambahan != null)
            {
                foreach (PintuTambahan p in pintuTambahan)
                {
                    if (p != null && p.pesan != null) p.pesan.SetActive(false);
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PemainMendekat(other);
    }

    // Dipanggil dari zona di pintu pertama, atau dari zona lain lewat ZonaPintuTerkunci
    public void PemainMendekat(Collider2D other)
    {
        if (!terkunci) return;
        if (!other.CompareTag(tagPemain)) return;

        TampilkanPesanDi(pesanTerkunci);
    }

    private void TampilkanPesanDi(GameObject pesan)
    {
        if (!terkunci || pesan == null) return;

        if (rutinPesan != null) StopCoroutine(rutinPesan);
        if (pesanAktif != null && pesanAktif != pesan) pesanAktif.SetActive(false);

        rutinPesan = StartCoroutine(RutinPesan(pesan));
    }

    private IEnumerator RutinPesan(GameObject pesan)
    {
        pesanAktif = pesan;
        pesan.SetActive(true);

        yield return new WaitForSeconds(lamaPesan);

        pesan.SetActive(false);
        pesanAktif = null;
        rutinPesan = null;
    }
}