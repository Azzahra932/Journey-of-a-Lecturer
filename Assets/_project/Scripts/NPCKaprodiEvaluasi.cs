using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Scene 5 - Evaluasi Level 1 (Kaprodi menemui player).
/// Pasang di objek yang SELALU AKTIF (mis. empty object "ManagerKaprodiEvaluasi").
/// InteractiveDesk memanggil CobaMulai() tiap player duduk di kursi dosen.
/// Kalau Scene 4 sudah selesai: Kaprodi berjalan masuk dari pintu, berhenti di dekat player,
/// dialog mulai OTOMATIS (lanjut Space), lalu Kaprodi berjalan keluar.
/// </summary>
public class NPCKaprodiEvaluasi : MonoBehaviour
{
    [System.Serializable]
    public class BarisDialog
    {
        public bool pembicaraKaprodi = true;   // true = box kaprodi, false = box pemain
        [TextArea(2, 4)] public string teks;
    }

    [Header("Syarat")]
    public NPCDosenSenior syaratScene4;      // Scene 5 baru terjadi setelah Scene 4 selesai
    public bool wajibScene4Selesai = true;
    public InteractiveDesk kursiDosen;       // dipakai untuk mengunci player (tidak bisa berdiri) selama Scene 5

    [Header("NPC Kaprodi")]
    public GameObject kaprodi;               // objek kaprodi untuk scene ini (nonaktif di awal)
    public Transform[] jalurMasuk;           // urut dari pintu ... sampai titik berdiri (elemen TERAKHIR = tempat berdiri dekat player)
    public float kecepatan = 2.5f;
    public float jedaSebelumMasuk = 1.5f;

    [Header("Animator Kaprodi (Opsional)")]
    public Animator animator;
    public string parameterJalan = "IsMoving";

    [Header("Box Dialog")]
    public GameObject boxKaprodi;
    public TMP_Text teksKaprodi;
    public GameObject boxPemain;
    public TMP_Text teksPemain;

    [Header("Isi Dialog (urut dari atas)")]
    public BarisDialog[] baris = new BarisDialog[]
    {
        new BarisDialog { pembicaraKaprodi = true,  teks = "Bagaimana pengalaman pertama Anda sebagai Tenaga Pengajar?" },
        new BarisDialog { pembicaraKaprodi = false, teks = "Banyak hal baru yang saya pelajari. Saya mulai memahami pentingnya mengatur waktu dan prioritas." },
        new BarisDialog { pembicaraKaprodi = true,  teks = "Bagus. Perjalanan akademik membutuhkan keseimbangan antara mengajar, penelitian, dan pengembangan diri." },
        new BarisDialog { pembicaraKaprodi = true,  teks = "Pertahankan konsistensi Anda. Jika memenuhi target, Anda siap menuju jenjang berikutnya." },
        new BarisDialog { pembicaraKaprodi = false, teks = "Saya siap melanjutkan perjalanan akademik saya." }
    };

    [Header("Lanjut Dialog")]
    public KeyCode tombolLanjut = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaTekanPertama = 0.3f;

    [Header("Setelah Dialog")]
    public bool keluarSetelahDialog = true;
    public float jedaSebelumKeluar = 0.5f;
    public UnityEvent saatDialogSelesai;     // hubungkan ke langkah berikutnya (mis. layar Level Complete)

    private bool sudahTerjadi = false;
    private bool sedangBerjalan = false;

    void Start()
    {
        if (kaprodi != null) kaprodi.SetActive(false);
        if (boxKaprodi != null) boxKaprodi.SetActive(false);
        if (boxPemain != null) boxPemain.SetActive(false);
    }

    // Dipanggil dari InteractiveDesk.SitDown()
    public void CobaMulai()
    {
        if (sudahTerjadi || sedangBerjalan) return;

        if (wajibScene4Selesai && (syaratScene4 == null || !syaratScene4.SudahSelesai))
            return;

        StartCoroutine(AlurEvaluasi());
    }

    private IEnumerator AlurEvaluasi()
    {
        sedangBerjalan = true;
        KunciKursi(true);

        if (kaprodi == null)
        {
            Debug.LogWarning("[NPCKaprodiEvaluasi] Objek kaprodi belum diisi di Inspector!");
            KunciKursi(false);
            sedangBerjalan = false;
            yield break;
        }

        yield return new WaitForSeconds(jedaSebelumMasuk);

        // ---------- Kaprodi berjalan masuk dari pintu ----------
        if (jalurMasuk != null && jalurMasuk.Length > 0 && jalurMasuk[0] != null)
            kaprodi.transform.position = jalurMasuk[0].position;

        kaprodi.SetActive(true);
        SetAnimasiJalan(true);

        SpriteRenderer sr = kaprodi.GetComponentInChildren<SpriteRenderer>();

        if (jalurMasuk != null)
        {
            foreach (Transform titik in jalurMasuk)
            {
                if (titik == null) continue;
                yield return JalanKe(titik.position, sr);
            }
        }

        SetAnimasiJalan(false);

        // ---------- Dialog otomatis ----------
        for (int i = 0; i < baris.Length; i++)
        {
            TampilkanBaris(baris[i]);

            yield return new WaitForSeconds(i == 0 ? jedaTekanPertama : 0.1f);

            while (!(Input.GetKeyDown(tombolLanjut) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;
        }

        SembunyikanBox();

        sudahTerjadi = true;
        KunciKursi(false);

        Debug.Log("[NPCKaprodiEvaluasi] Evaluasi Level 1 selesai.");
        saatDialogSelesai.Invoke();

        // ---------- Kaprodi berjalan keluar ----------
        if (keluarSetelahDialog)
        {
            yield return new WaitForSeconds(jedaSebelumKeluar);

            SetAnimasiJalan(true);

            if (jalurMasuk != null)
            {
                for (int i = jalurMasuk.Length - 1; i >= 0; i--)
                {
                    if (jalurMasuk[i] == null) continue;
                    yield return JalanKe(jalurMasuk[i].position, sr);
                }
            }

            SetAnimasiJalan(false);
            kaprodi.SetActive(false);
        }

        sedangBerjalan = false;
    }

    private void KunciKursi(bool kunci)
    {
        if (kursiDosen != null) kursiDosen.TerkunciEksternal = kunci;
    }

    private void TampilkanBaris(BarisDialog b)
    {
        SembunyikanBox();

        if (b.pembicaraKaprodi)
        {
            if (boxKaprodi != null) boxKaprodi.SetActive(true);
            if (teksKaprodi != null) teksKaprodi.text = b.teks;
        }
        else
        {
            if (boxPemain != null) boxPemain.SetActive(true);
            if (teksPemain != null) teksPemain.text = b.teks;
        }
    }

    private void SembunyikanBox()
    {
        if (boxKaprodi != null) boxKaprodi.SetActive(false);
        if (boxPemain != null) boxPemain.SetActive(false);
    }

    private IEnumerator JalanKe(Vector3 tujuan, SpriteRenderer sr)
    {
        tujuan.z = kaprodi.transform.position.z;

        while (Vector3.Distance(kaprodi.transform.position, tujuan) > 0.02f)
        {
            Vector3 posisi = kaprodi.transform.position;
            Vector3 arah = tujuan - posisi;

            if (sr != null && Mathf.Abs(arah.x) > 0.01f)
                sr.flipX = arah.x < 0f;

            kaprodi.transform.position = Vector3.MoveTowards(posisi, tujuan, kecepatan * Time.deltaTime);
            yield return null;
        }

        kaprodi.transform.position = tujuan;
    }

    private void SetAnimasiJalan(bool jalan)
    {
        if (animator == null || string.IsNullOrEmpty(parameterJalan)) return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == parameterJalan && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterJalan, jalan);
                return;
            }
        }
    }
}