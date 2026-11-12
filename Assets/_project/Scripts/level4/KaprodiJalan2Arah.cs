using System.Collections;
using UnityEngine;

// Versi 4: arah jalan Atas, Bawah, dan Samping (kiri/kanan lewat flip). Nama class tetap KaprodiJalan2Arah.
// Ganti seluruh isi file KaprodiJalan2Arah.cs di Unity dengan isi file ini.
//
// Arah dipilih otomatis di awal tiap segmen jalan (dari satu titik ke titik berikutnya):
//   - gerak lebih banyak ke samping (kiri/kanan)  -> animasi Samping
//   - gerak lebih banyak ke atas                  -> animasi Atas
//   - gerak lebih banyak ke bawah                 -> animasi Bawah
// Jadi letakkan titik lurus: titik yang segaris tegak = jalan atas/bawah, titik yang segaris datar = jalan samping.
//
// Tambahan versi 4: gambar duduk bisa diletakkan otomatis di posisi badan berdiri (Samakan Posisi Duduk), dan
// Console melaporkan posisi gambar duduk saat Kaprodi duduk, untuk mencari tahu kenapa Kaprodi tidak terlihat.
public class KaprodiJalan2Arah : MonoBehaviour
{
    [Header("Objek Anak")]
    public GameObject objekAtas;            // KaprodiAtas
    public GameObject objekBawah;           // KaprodiBawah
    public GameObject objekSamping;         // KaprodiSamping
    public Animator animatorAtas;
    public Animator animatorBawah;
    public Animator animatorSamping;

    [Header("Samping")]
    public bool spriteSampingMenghadapKanan = true;     // Matikan kalau gambar samping aslinya menghadap kiri

    [Header("Duduk (opsional)")]
    public GameObject objekDuduk;           // KaprodiDuduk: objek anak dengan Sprite Renderer berisi gambar duduk. Kosong = Kaprodi tetap tampil menghadap bawah saat duduk
    [Tooltip("true = saat duduk, gambar duduk dipindah ke posisi lokal yang sama dengan badan berdiri (Objek Bawah), jadi muncul tepat di titik duduk Kaprodi. Matikan kalau posisi gambar duduk sudah diatur sendiri di Inspector.")]
    public bool samakanPosisiDuduk = true;
    public Vector2 geserDuduk = Vector2.zero;           // Geser halus gambar duduk dari posisi badan berdiri (dipakai kalau Samakan Posisi Duduk dicentang)
    [Tooltip("Kalau diisi 0 atau lebih, Order in Layer gambar duduk diset ke angka ini saat duduk (supaya tidak tertutup kursi / meja). -1 = tidak diubah.")]
    public int urutanLayerDuduk = -1;

    [Header("Gerak")]
    public float kecepatan = 2f;
    public bool mulaiMenghadapAtas = false; // arah awal saat belum jalan

    private enum Arah { Atas, Bawah, Samping }

    private Arah arah;
    private bool hadapKanan = true;

    void Awake()
    {
        arah = mulaiMenghadapAtas ? Arah.Atas : Arah.Bawah;
        if (objekDuduk != null) objekDuduk.SetActive(false);
        Tampilkan();
    }

    // Jalan melewati titik berurutan, mis. titik depan MC lalu titik keluar
    public IEnumerator JalanMelalui(Transform[] titik)
    {
        foreach (Transform t in titik)
        {
            if (t != null) yield return StartCoroutine(JalanKe(t.position));
        }
    }

    public IEnumerator JalanKe(Vector2 tujuan)
    {
        // Arah ditentukan sekali di awal segmen, supaya tidak berganti-ganti di tengah jalan
        Vector2 selisih = tujuan - (Vector2)transform.position;

        if (selisih.sqrMagnitude > 0.0025f)
        {
            if (Mathf.Abs(selisih.x) > Mathf.Abs(selisih.y))
            {
                arah = Arah.Samping;
                hadapKanan = selisih.x > 0f;
            }
            else
            {
                arah = selisih.y > 0f ? Arah.Atas : Arah.Bawah;
            }

            Tampilkan();
        }

        SetJalan(true);

        while (Vector2.Distance(transform.position, tujuan) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(transform.position, tujuan, kecepatan * Time.deltaTime);
            yield return null;
        }

        transform.position = tujuan;
        SetJalan(false);
    }

    // Menghadap ke atas atau bawah tanpa jalan (mis. saat dialog)
    public void Hadap(bool atas)
    {
        if (objekDuduk != null) objekDuduk.SetActive(false);
        arah = atas ? Arah.Atas : Arah.Bawah;
        Tampilkan();
        SetJalan(false);
    }

    // Duduk: kalau Objek Duduk diisi, gambar duduk ditampilkan. Kalau kosong, Kaprodi diam menghadap bawah (ke arah MC)
    public void Duduk()
    {
        SetJalan(false);

        if (objekDuduk != null)
        {
            if (objekAtas != null) objekAtas.SetActive(false);
            if (objekBawah != null) objekBawah.SetActive(false);
            if (objekSamping != null) objekSamping.SetActive(false);

            // Gambar duduk diletakkan di posisi badan berdiri (relatif ke objek induk), supaya ikut titik duduk Kaprodi
            if (samakanPosisiDuduk)
            {
                Vector3 lokal = (objekBawah != null) ? objekBawah.transform.localPosition : Vector3.zero;
                lokal.x += geserDuduk.x;
                lokal.y += geserDuduk.y;
                objekDuduk.transform.localPosition = lokal;
            }

            SpriteRenderer sr = objekDuduk.GetComponent<SpriteRenderer>();
            if (sr != null && urutanLayerDuduk >= 0) sr.sortingOrder = urutanLayerDuduk;

            objekDuduk.SetActive(true);

            // Laporan di Console untuk mencari tahu kalau gambar duduk tidak terlihat
            Debug.Log("[Kaprodi] Duduk: objek '" + objekDuduk.name + "' aktif=" + objekDuduk.activeInHierarchy
                + ", posisi dunia=" + objekDuduk.transform.position
                + ", sprite=" + ((sr != null && sr.sprite != null) ? sr.sprite.name : "KOSONG")
                + ", order=" + ((sr != null) ? sr.sortingOrder.ToString() : "-")
                + ", warna alpha=" + ((sr != null) ? sr.color.a.ToString("0.00") : "-"));
        }
        else
        {
            arah = Arah.Bawah;
            Tampilkan();
            Debug.LogWarning("[Kaprodi] Objek Duduk belum diisi di Inspector, Kaprodi tampil menghadap bawah saat duduk. Isi slot Objek Duduk dengan objek anak 'duduk'.");
        }
    }

    // Duduk TEPAT di posisi dunia tertentu (mis. pusat kursi tamu). Seluruh objek Kaprodi dan gambar duduk dipindah ke posisi itu,
    // jadi posisi lokal gambar duduk di Inspector tidak berpengaruh.
    public void DudukDi(Vector2 posisiDunia)
    {
        Vector3 p = transform.position;
        p.x = posisiDunia.x;
        p.y = posisiDunia.y;
        transform.position = p;

        Duduk();

        if (objekDuduk != null)
        {
            Vector3 d = objekDuduk.transform.position;
            d.x = posisiDunia.x;
            d.y = posisiDunia.y;
            objekDuduk.transform.position = d;

            Debug.Log("[Kaprodi] DudukDi: gambar duduk dipasang di posisi dunia " + objekDuduk.transform.position);
        }
    }

    // Berdiri: kembali ke sprite berdiri menghadap bawah, siap jalan lagi
    public void Berdiri()
    {
        if (objekDuduk != null) objekDuduk.SetActive(false);
        arah = Arah.Bawah;
        Tampilkan();
        SetJalan(false);
    }

    private void Tampilkan()
    {
        if (objekAtas != null) objekAtas.SetActive(arah == Arah.Atas);
        if (objekBawah != null) objekBawah.SetActive(arah == Arah.Bawah);
        if (objekSamping != null)
        {
            objekSamping.SetActive(arah == Arah.Samping);

            // Flip kiri/kanan lewat Scale X, jadi satu animasi samping cukup untuk dua arah
            Vector3 s = objekSamping.transform.localScale;
            float besar = Mathf.Abs(s.x);
            bool menghadapKanan = spriteSampingMenghadapKanan ? hadapKanan : !hadapKanan;
            s.x = menghadapKanan ? besar : -besar;
            objekSamping.transform.localScale = s;
        }
    }

    private void SetJalan(bool jalan)
    {
        Animator a = animatorBawah;
        if (arah == Arah.Atas) a = animatorAtas;
        else if (arah == Arah.Samping) a = animatorSamping;

        if (a != null && a.gameObject.activeInHierarchy) a.SetBool("IsMoving", jalan);
    }
}