using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Scene 3 - Bimbingan Mahasiswa.
/// Pasang di objek yang SELALU AKTIF (mis. empty object "ManagerBimbingan").
/// InteractiveDesk memanggil CobaMulai() tiap player duduk di kursi ruang dosen.
/// Kalau syarat terpenuhi: mahasiswa berjalan ke meja, duduk, lalu box dialog langsung muncul.
/// </summary>
public class NPCBimbingan : MonoBehaviour
{
    [Header("Syarat")]
    public LaptopKelas laptopKelas;          // drag objek Laptopkelas
    public bool wajibSudahMengajar = true;   // bimbingan baru terjadi setelah mengajar di kelas

    [Header("NPC Mahasiswa")]
    public GameObject mahasiswa;             // objek mahasiswa (akan dinonaktifkan di awal)
    public Transform[] jalur;                // titik jalan berurutan: pintu -> ... -> dekat meja
    public Transform titikDuduk;             // posisi mahasiswa saat duduk (kursi depan meja dosen)
    public float kecepatan = 2.5f;

    [Header("Tampilan Saat Duduk (Opsional)")]
    public Sprite spriteDuduk;               // kosong = sprite tidak diganti
    public int sortingOrderDuduk = 2;
    public float skalaSaatDuduk = 1f;        // pengali ukuran saat sprite duduk dipakai (kecil = < 1, mis. 0.3)

    [Header("Animator Mahasiswa (Opsional)")]
    public Animator animator;
    public string parameterJalan = "IsMoving";   // parameter bool animasi jalan (kalau ada)

    [Header("Dialog")]
    public GameObject boxDialog;             // box dialog, muncul langsung setelah mahasiswa duduk
    public float jedaSebelumDialog = 0.4f;
    public UnityEvent saatMahasiswaDuduk;    // hubungkan ke fungsi mulai dialog (ManagerDialog) lewat Inspector

    private bool sudahTerjadi = false;
    private bool sedangBerjalan = false;

    // True selama bimbingan berlangsung (jalan + dialog): player tidak bisa berdiri dari kursi
    public bool KunciPlayer { get; set; } = false;

    // Tampilan awal mahasiswa (disimpan sebelum diganti sprite duduk, dipakai saat keluar)
    private Sprite spriteAwal;
    private Vector3 skalaAwal = Vector3.one;
    private int sortingAwal = 0;
    private bool spriteDidiganti = false;

    void Start()
    {
        if (mahasiswa != null) mahasiswa.SetActive(false);
        if (boxDialog != null) boxDialog.SetActive(false);
    }

    // Dipanggil dari InteractiveDesk.SitDown()
    public void CobaMulai()
    {
        if (sudahTerjadi || sedangBerjalan) return;

        if (wajibSudahMengajar && (laptopKelas == null || !laptopKelas.SudahMengajar))
            return;

        StartCoroutine(ProsesBimbingan());
    }

    private IEnumerator ProsesBimbingan()
    {
        sedangBerjalan = true;
        KunciPlayer = true;

        if (mahasiswa == null)
        {
            Debug.LogWarning("[NPCBimbingan] Objek mahasiswa belum diisi di Inspector!");
            sedangBerjalan = false;
            KunciPlayer = false;
            yield break;
        }

        // Muncul di titik pertama jalur (pintu)
        if (jalur != null && jalur.Length > 0 && jalur[0] != null)
            mahasiswa.transform.position = jalur[0].position;

        mahasiswa.SetActive(true);
        SetAnimasiJalan(true);

        SpriteRenderer sr = mahasiswa.GetComponentInChildren<SpriteRenderer>();

        // Jalan melewati semua titik, lalu ke titik duduk
        if (jalur != null)
        {
            foreach (Transform titik in jalur)
            {
                if (titik == null) continue;
                yield return JalanKe(titik.position, sr);
            }
        }

        if (titikDuduk != null)
            yield return JalanKe(titikDuduk.position, sr);

        // Sampai: duduk
        SetAnimasiJalan(false);

        if (sr != null)
        {
            if (spriteDuduk != null)
            {
                if (animator != null) animator.enabled = false;  // supaya sprite duduk tidak tertimpa animasi
                spriteAwal = sr.sprite;
                skalaAwal = sr.transform.localScale;
                spriteDidiganti = true;
                sr.sprite = spriteDuduk;
                sr.transform.localScale = sr.transform.localScale * skalaSaatDuduk;
            }
            sortingAwal = sr.sortingOrder;
            sr.sortingOrder = sortingOrderDuduk;
        }

        yield return new WaitForSeconds(jedaSebelumDialog);

        // Dialog langsung muncul setelah mahasiswa duduk
        if (boxDialog != null) boxDialog.SetActive(true);
        saatMahasiswaDuduk.Invoke();

        // Kalau tidak ada dialog yang terhubung, jangan kunci player selamanya
        if (saatMahasiswaDuduk.GetPersistentEventCount() == 0) KunciPlayer = false;

        sudahTerjadi = true;
        sedangBerjalan = false;

        Debug.Log("[NPCBimbingan] Mahasiswa duduk, dialog bimbingan dimulai.");
    }

    private IEnumerator JalanKe(Vector3 tujuan, SpriteRenderer sr)
    {
        tujuan.z = mahasiswa.transform.position.z;

        while (Vector3.Distance(mahasiswa.transform.position, tujuan) > 0.02f)
        {
            Vector3 posisi = mahasiswa.transform.position;
            Vector3 arah = tujuan - posisi;

            // Hadap kiri/kanan sesuai arah jalan
            if (sr != null && Mathf.Abs(arah.x) > 0.01f)
                sr.flipX = arah.x < 0f;

            mahasiswa.transform.position = Vector3.MoveTowards(posisi, tujuan, kecepatan * Time.deltaTime);
            yield return null;
        }

        mahasiswa.transform.position = tujuan;
    }

    // Mahasiswa bangun dari kursi lalu berjalan keluar lewat jalur (urutan terbalik), lalu menghilang.
    // Dipanggil dari DialogBimbingan setelah poin masuk.
    public IEnumerator ProsesKeluar()
    {
        if (mahasiswa == null) yield break;

        SpriteRenderer sr = mahasiswa.GetComponentInChildren<SpriteRenderer>();

        // Kembalikan tampilan berjalan (sprite, ukuran, layer)
        if (sr != null && spriteDidiganti)
        {
            sr.sprite = spriteAwal;
            sr.transform.localScale = skalaAwal;
            sr.sortingOrder = sortingAwal;
        }

        if (animator != null) animator.enabled = true;
        SetAnimasiJalan(true);

        if (jalur != null)
        {
            for (int i = jalur.Length - 1; i >= 0; i--)
            {
                if (jalur[i] == null) continue;
                yield return JalanKe(jalur[i].position, sr);
            }
        }

        SetAnimasiJalan(false);
        mahasiswa.SetActive(false);

        Debug.Log("[NPCBimbingan] Mahasiswa sudah keluar ruangan.");
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