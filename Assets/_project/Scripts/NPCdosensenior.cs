using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Scene 4 - Pengembangan Kompetensi.
/// Pasang di objek yang SELALU AKTIF (mis. empty object "ManagerDosenSenior").
/// Alur:
///  1. MulaiMasuk() dipanggil (dari DialogBimbingan.saatBimbinganSelesai)
///  2. Setelah jeda, Dosen Senior berjalan masuk dari pintu lewat jalurMasuk sampai titik berdiri
///  3. Panah penunjuk ke Dosen Senior menyala
///  4. Player (sudah berdiri) mendekat -> dialog mulai OTOMATIS, lanjut dengan Space
///  5. Dialog selesai -> player bebas, panah ke laptop menyala, Dosen Senior berjalan keluar
/// </summary>
public class NPCDosenSenior : MonoBehaviour
{
    [System.Serializable]
    public class BarisDialog
    {
        public bool pembicaraSenior = true;   // true = box dosen senior, false = box pemain
        [TextArea(2, 4)] public string teks;
    }

    [Header("NPC Dosen Senior")]
    public GameObject dosenSenior;           // objek dosen senior (dinonaktifkan di awal)
    public Transform[] jalurMasuk;           // urut dari pintu ... sampai titik berdiri (elemen TERAKHIR = tempat berdiri)
    public float kecepatan = 2.5f;
    public float jedaSebelumMasuk = 2f;      // jeda setelah mahasiswa keluar

    [Header("Animator Dosen Senior (Opsional)")]
    public Animator animator;
    public string parameterJalan = "IsMoving";

    [Header("Panah Penunjuk")]
    public GameObject panahKeDosenSenior;    // menyala saat dosen senior sudah berdiri, mati saat dialog mulai
    public GameObject panahKeLaptop;         // menyala setelah dialog selesai

    [Header("Pemicu Dialog (Player Mendekat)")]
    public InteractiveDesk kursiDosen;       // objek kursi dosen: dialog baru mulai kalau player SUDAH BERDIRI
    public float jarakMulaiDialog = 1.5f;

    [Header("Box Dialog")]
    public GameObject boxSenior;
    public TMP_Text teksSenior;
    public GameObject boxPemain;
    public TMP_Text teksPemain;

    [Header("Isi Dialog (urut dari atas)")]
    public BarisDialog[] baris = new BarisDialog[]
    {
        new BarisDialog { pembicaraSenior = true,  teks = "Sebagai dosen baru, jangan berhenti belajar. Kompetensi harus terus dikembangkan." },
        new BarisDialog { pembicaraSenior = false, teks = "Baik, Pak/Bu. Apa yang harus saya tingkatkan?" },
        new BarisDialog { pembicaraSenior = true,  teks = "Perbanyak membaca referensi, mengikuti pelatihan, dan berdiskusi dengan dosen lain." },
        new BarisDialog { pembicaraSenior = false, teks = "Saya akan mulai mengembangkan kemampuan saya." }
    };

    [Header("Lanjut Dialog")]
    public KeyCode tombolLanjut = KeyCode.Space;
    public bool bolehKlikMouse = true;
    public float jedaTekanPertama = 0.3f;

    [Header("Setelah Dialog")]
    public bool keluarSetelahDialog = true;  // dosen senior berjalan keluar lewat jalur terbalik
    public float jedaSebelumKeluar = 0.5f;
    public UnityEvent saatDialogSelesai;     // hubungkan ke langkah berikutnya (opsional)

    private enum Tahap { Menunggu, Masuk, MenungguPlayer, Dialog, Selesai }
    private Tahap tahap = Tahap.Menunggu;

    private PlayerMovement playerMovement;

    // True setelah dialog Scene 4 selesai (dipakai Scene 5 sebagai syarat)
    public bool SudahSelesai { get { return tahap == Tahap.Selesai; } }

    void Start()
    {
        if (dosenSenior != null) dosenSenior.SetActive(false);
        if (boxSenior != null) boxSenior.SetActive(false);
        if (boxPemain != null) boxPemain.SetActive(false);
        if (panahKeDosenSenior != null) panahKeDosenSenior.SetActive(false);
        if (panahKeLaptop != null) panahKeLaptop.SetActive(false);

        playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    // Dipanggil dari DialogBimbingan.saatBimbinganSelesai
    public void MulaiMasuk()
    {
        Debug.Log("[NPCDosenSenior] MulaiMasuk() dipanggil. tahap=" + tahap);

        if (tahap != Tahap.Menunggu) return;
        StartCoroutine(ProsesMasuk());
    }

    void Update()
    {
        if (tahap != Tahap.MenungguPlayer) return;

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement == null) return;
        }

        // Player harus sudah berdiri (tidak sedang duduk di kursi dosen)
        if (kursiDosen != null && kursiDosen.SedangDuduk) return;

        float jarak = Vector2.Distance(playerMovement.transform.position, dosenSenior.transform.position);
        if (jarak <= jarakMulaiDialog)
        {
            StartCoroutine(ProsesDialog());
        }
    }

    // ---------- Dosen senior berjalan masuk ----------
    private IEnumerator ProsesMasuk()
    {
        tahap = Tahap.Masuk;

        if (dosenSenior == null)
        {
            Debug.LogWarning("[NPCDosenSenior] Objek dosenSenior belum diisi di Inspector!");
            tahap = Tahap.Menunggu;
            yield break;
        }

        yield return new WaitForSeconds(jedaSebelumMasuk);

        // Muncul di titik pertama jalur (pintu), BUKAN di tempat berdiri
        if (jalurMasuk != null && jalurMasuk.Length > 0 && jalurMasuk[0] != null)
            dosenSenior.transform.position = jalurMasuk[0].position;

        dosenSenior.SetActive(true);
        SetAnimasiJalan(true);

        SpriteRenderer sr = dosenSenior.GetComponentInChildren<SpriteRenderer>();

        if (jalurMasuk != null)
        {
            foreach (Transform titik in jalurMasuk)
            {
                if (titik == null) continue;
                yield return JalanKe(titik.position, sr);
            }
        }

        SetAnimasiJalan(false);

        if (panahKeDosenSenior != null) panahKeDosenSenior.SetActive(true);

        Debug.Log("[NPCDosenSenior] Dosen senior sudah berdiri, menunggu player mendekat.");
        tahap = Tahap.MenungguPlayer;
    }

    // ---------- Dialog ----------
    private IEnumerator ProsesDialog()
    {
        tahap = Tahap.Dialog;

        if (panahKeDosenSenior != null) panahKeDosenSenior.SetActive(false);

        KunciPlayer(true);

        for (int i = 0; i < baris.Length; i++)
        {
            TampilkanBaris(baris[i]);

            yield return new WaitForSeconds(i == 0 ? jedaTekanPertama : 0.1f);

            while (!(Input.GetKeyDown(tombolLanjut) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
                yield return null;
        }

        SembunyikanBox();
        KunciPlayer(false);

        Debug.Log("[NPCDosenSenior] Dialog selesai, panah ke laptop menyala.");

        if (panahKeLaptop != null) panahKeLaptop.SetActive(true);
        saatDialogSelesai.Invoke();

        tahap = Tahap.Selesai;

        // Dosen senior berjalan keluar sementara player sudah bebas
        if (keluarSetelahDialog)
        {
            yield return new WaitForSeconds(jedaSebelumKeluar);
            yield return StartCoroutine(ProsesKeluar());
        }
    }

    private IEnumerator ProsesKeluar()
    {
        SpriteRenderer sr = dosenSenior.GetComponentInChildren<SpriteRenderer>();
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
        dosenSenior.SetActive(false);
    }

    // ---------- Pembantu ----------
    private void KunciPlayer(bool kunci)
    {
        if (playerMovement == null) return;

        if (kunci && playerMovement.animator != null)
        {
            playerMovement.animator.SetBool("IsMoving", false);
            playerMovement.animator.SetFloat("MoveX", 0f);
            playerMovement.animator.SetFloat("MoveY", 0f);
        }

        playerMovement.enabled = !kunci;
    }

    private void TampilkanBaris(BarisDialog b)
    {
        SembunyikanBox();

        if (b.pembicaraSenior)
        {
            if (boxSenior != null) boxSenior.SetActive(true);
            if (teksSenior != null) teksSenior.text = b.teks;
        }
        else
        {
            if (boxPemain != null) boxPemain.SetActive(true);
            if (teksPemain != null) teksPemain.text = b.teks;
        }
    }

    private void SembunyikanBox()
    {
        if (boxSenior != null) boxSenior.SetActive(false);
        if (boxPemain != null) boxPemain.SetActive(false);
    }

    private IEnumerator JalanKe(Vector3 tujuan, SpriteRenderer sr)
    {
        tujuan.z = dosenSenior.transform.position.z;

        while (Vector3.Distance(dosenSenior.transform.position, tujuan) > 0.02f)
        {
            Vector3 posisi = dosenSenior.transform.position;
            Vector3 arah = tujuan - posisi;

            // Hadap kiri/kanan sesuai arah jalan
            if (sr != null && Mathf.Abs(arah.x) > 0.01f)
                sr.flipX = arah.x < 0f;

            dosenSenior.transform.position = Vector3.MoveTowards(posisi, tujuan, kecepatan * Time.deltaTime);
            yield return null;
        }

        dosenSenior.transform.position = tujuan;
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