using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Penutup hari: saat jam mencapai waktu tidur (mis. 20.00), MC berjalan keluar sendiri LEWAT JALUR,
/// layar menghitam dengan teks, lalu MC muncul lagi di pagi hari (jam 08.00).
/// HANYA mengubah posisi MC dan jam. Skor, integritas, energi (kecuali dipilih), dan progres scene tidak disentuh.
/// Pasang di objek yang SELALU AKTIF (mis. empty object "ManagerTidurMalam").
/// Dipanggil otomatis oleh GameTimeManager.
/// </summary>
public class TransisiTidurMalam : MonoBehaviour
{
    // Satu rute jalan keluar untuk satu ruangan
    [System.Serializable]
    public class RuteKeluar
    {
        public string namaRuang = "Ruang Dosen";
        public Collider2D areaRuang;      // collider (Is Trigger) yang menutupi seluruh lantai ruangan ini
        public Transform[] titik;          // urut: dari dekat tempat MC -> pintu ruangan -> ... -> pintu keluar terakhir
    }

    [Header("Layar Hitam (boleh sama dengan LayarHitamTransisi)")]
    public Image layarHitam;

    [Header("Teks Saat Layar Hitam")]
    public TMP_Text teksTransisi;            // teks di tengah layar. Harus berada DI ATAS layar hitam di Hierarchy
    [TextArea(2, 3)] public string pesanMalam = "Malam telah tiba...\nWaktunya istirahat.";
    [TextArea(2, 3)] public string pesanPagi = "Keesokan paginya\n08.00";
    public float durasiTeksMalam = 2.5f;
    public float durasiTeksPagi = 2f;

    [Header("Player")]
    public GameObject playerBerjalan;         // TPdiam (kosong = dicari otomatis)
    public Transform titikPagi;               // tempat MC muncul pagi hari. Kosong = posisi awal permainan
    public float kecepatanJalan = 2.5f;

    [Header("Jalur Keluar")]
    public RuteKeluar[] ruteKeluar;          // satu rute per ruangan. Dipilih sesuai ruangan tempat MC berada
    public Transform[] jalurCadangan;        // dipakai kalau MC tidak berada di area ruangan manapun
    public Transform titikKeluar;            // cadangan terakhir: satu titik tujuan (lurus). Kosong = MC langsung menghilang bersama layar hitam

    [Header("Kursi (supaya MC dipaksa berdiri dulu)")]
    public ChairInteraction kursiKelas;
    public InteractiveDesk kursiDosen;
    public InteractiveDesk kursiResearch;    // bangku Research Center (opsional)

    [Header("Fade")]
    public float durasiGelap = 0.8f;
    public float durasiTerang = 0.8f;

    [Header("Waktu & Energi")]
    public GameTimeManager jam;               // kosong = dicari otomatis
    public bool isiEnergiSetelahTidur = false;    // false = energi tetap seperti sebelumnya
    public EnergyBar energyBar;

    [Header("Penunda (transisi menunggu sampai ini tidak aktif)")]
    public GameObject[] objekPenunda;        // drag box dialog dan overlay yang tampil saat adegan
    public TransisiKembaliKeAwal transisiEnergi;

    [Header("Setelah Pagi Tiba (Opsional)")]
    public UnityEvent saatPagiTiba;

    public bool SedangTidur { get; private set; }

    private bool menunggu = false;
    private Vector3 posisiPagi;

    void Start()
    {
        if (playerBerjalan == null) playerBerjalan = GameObject.Find("TPdiam");
        if (jam == null) jam = FindFirstObjectByType<GameTimeManager>();
        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();
        if (transisiEnergi == null) transisiEnergi = FindFirstObjectByType<TransisiKembaliKeAwal>();

        if (titikPagi != null) posisiPagi = titikPagi.position;
        else if (playerBerjalan != null) posisiPagi = playerBerjalan.transform.position;

        if (teksTransisi != null) teksTransisi.gameObject.SetActive(false);
    }

    // Dipanggil GameTimeManager saat jam mencapai waktu tidur
    public void MulaiTidur()
    {
        if (SedangTidur || menunggu) return;
        StartCoroutine(TungguLaluTidur());
    }

    private IEnumerator TungguLaluTidur()
    {
        menunggu = true;

        // Tunda sampai tidak ada adegan atau dialog yang sedang berjalan
        while (!AmanUntukTidur())
            yield return null;

        yield return StartCoroutine(ProsesTidur());

        menunggu = false;
    }

    private bool AmanUntukTidur()
    {
        if (transisiEnergi != null && transisiEnergi.SedangTransisi) return false;

        if (kursiDosen != null && kursiDosen.TerkunciEksternal) return false;
        if (kursiResearch != null && kursiResearch.TerkunciEksternal) return false;
        if (kursiKelas != null && kursiKelas.TerkunciEksternal) return false;

        if (objekPenunda != null)
        {
            foreach (GameObject g in objekPenunda)
                if (g != null && g.activeInHierarchy) return false;
        }

        PlayerMovement pm = playerBerjalan != null ? playerBerjalan.GetComponent<PlayerMovement>() : null;
        if (pm != null && !pm.enabled) return false;   // player sedang dikunci adegan

        return true;
    }

    private IEnumerator ProsesTidur()
    {
        SedangTidur = true;
        if (jam != null) jam.WaktuBerhenti = true;

        PlayerMovement pm = playerBerjalan != null ? playerBerjalan.GetComponent<PlayerMovement>() : null;

        // 1. Kunci kontrol player
        if (pm != null)
        {
            ResetAnimator(pm);
            pm.enabled = false;
        }

        // 2. Kalau sedang duduk, paksa berdiri dulu
        if (kursiKelas != null) kursiKelas.PaksaBerdiriSegera();
        if (kursiDosen != null) kursiDosen.PaksaBerdiri();
        if (kursiResearch != null) kursiResearch.PaksaBerdiri();

        if (playerBerjalan != null) playerBerjalan.SetActive(true);

        // Ambil referensi komponen fisik MC
        Collider2D colMC = playerBerjalan != null ? playerBerjalan.GetComponent<Collider2D>() : null;
        Rigidbody2D rbMC = playerBerjalan != null ? playerBerjalan.GetComponent<Rigidbody2D>() : null;

        // Matikan collider & simulasi rigidbody sementara saat berjalan otomatis agar tidak tersangkut
        if (colMC != null) colMC.enabled = false;
        if (rbMC != null) rbMC.simulated = false;

        // 3. MC berjalan keluar sendiri LEWAT JALUR (titik demi titik)
        if (playerBerjalan != null)
        {
            Transform[] rute = PilihRute();

            if (rute != null)
            {
                foreach (Transform t in rute)
                {
                    if (t == null) continue;
                    yield return JalanKeTitik(playerBerjalan, t.position, pm);
                }
            }
        }

        // 4. Layar menggelap
        yield return Fade(0f, 1f, durasiGelap);

        // 5. Teks malam
        if (teksTransisi != null)
        {
            teksTransisi.text = pesanMalam;
            teksTransisi.gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(durasiTeksMalam);

        // 6. Saat layar hitam: pagi tiba. MC dipindah, jam kembali 08.00
        if (playerBerjalan != null)
        {
            playerBerjalan.transform.position = posisiPagi;

            if (rbMC != null) rbMC.position = posisiPagi;

            Physics2D.SyncTransforms();

            if (pm != null) pm.Berdiri();
        }

        if (jam != null) jam.ResetKeJamPagi();

        if (isiEnergiSetelahTidur && energyBar != null) energyBar.IsiPenuh();

        // 7. Teks pagi
        if (teksTransisi != null) teksTransisi.text = pesanPagi;
        yield return new WaitForSeconds(durasiTeksPagi);

        if (teksTransisi != null) teksTransisi.gameObject.SetActive(false);

        // 8. Nyalakan kembali collider dan rigidbody MC
        if (colMC != null) colMC.enabled = true;
        if (rbMC != null) rbMC.simulated = true;

        // 9. Layar terang kembali
        yield return Fade(1f, 0f, durasiTerang);

        // 10. KONTROL DIKEMBALIKAN (PENTING: Dipastikan aktif kembali di sini)
        if (pm != null)
        {
            pm.enabled = true;
            ResetAnimator(pm);
        }

        if (jam != null) jam.WaktuBerhenti = false;

        Debug.Log("[TransisiTidurMalam] Pagi tiba, permainan dilanjutkan.");

        SedangTidur = false;
        saatPagiTiba.Invoke();
    }

    // Memilih rute sesuai ruangan tempat MC berada saat ini
    private Transform[] PilihRute()
    {
        Vector2 posisiMC = playerBerjalan.transform.position;

        if (ruteKeluar != null)
        {
            foreach (RuteKeluar r in ruteKeluar)
            {
                if (r == null || r.areaRuang == null || r.titik == null || r.titik.Length == 0) continue;

                if (r.areaRuang.OverlapPoint(posisiMC))
                {
                    Debug.Log("[TransisiTidurMalam] MC berada di " + r.namaRuang + ", memakai jalur ruangan itu.");
                    return r.titik;
                }
            }
        }

        if (jalurCadangan != null && jalurCadangan.Length > 0)
        {
            Debug.Log("[TransisiTidurMalam] MC tidak berada di area ruangan manapun, memakai jalur cadangan.");
            return jalurCadangan;
        }

        if (titikKeluar != null)
        {
            Debug.LogWarning("[TransisiTidurMalam] Tidak ada rute yang cocok, MC berjalan lurus ke Titik Keluar.");
            return new Transform[] { titikKeluar };
        }

        return null;
    }

    private IEnumerator JalanKeTitik(GameObject p, Vector3 tujuan, PlayerMovement pm)
    {
        Animator anim = pm != null ? pm.animator : null;
        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();

        float batasWaktu = 12f;   // pengaman per titik supaya tidak macet selamanya
        float t = 0f;

        while (t < batasWaktu && Vector2.Distance(p.transform.position, tujuan) > 0.05f)
        {
            t += Time.deltaTime;

            Vector3 posisi = p.transform.position;
            Vector2 selisih = (Vector2)(tujuan - posisi);

            // Arah hadap animasi mengikuti sumbu yang dominan (kiri/kanan/atas/bawah), supaya sprite tidak menyamping aneh
            Vector2 arahAnim;
            if (Mathf.Abs(selisih.x) > Mathf.Abs(selisih.y))
                arahAnim = new Vector2(Mathf.Sign(selisih.x), 0f);
            else
                arahAnim = new Vector2(0f, Mathf.Sign(selisih.y));

            if (anim != null)
            {
                anim.SetBool("IsMoving", true);
                anim.SetFloat("MoveX", arahAnim.x);
                anim.SetFloat("MoveY", arahAnim.y);
                anim.SetFloat("LastMoveX", arahAnim.x);
                anim.SetFloat("LastMoveY", arahAnim.y);
            }

            Vector3 baru = Vector3.MoveTowards(posisi, new Vector3(tujuan.x, tujuan.y, posisi.z), kecepatanJalan * Time.deltaTime);
            p.transform.position = baru;
            if (rb != null) rb.position = baru;

            yield return null;
        }

        if (pm != null) ResetAnimator(pm);
    }

    private void ResetAnimator(PlayerMovement pm)
    {
        if (pm == null || pm.animator == null) return;

        animatorResetInternal(pm.animator);
    }

    private void animatorResetInternal(Animator anim)
    {
        anim.SetBool("IsMoving", false);
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveY", 0f);
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