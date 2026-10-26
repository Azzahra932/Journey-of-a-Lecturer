using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BukuInteraction : MonoBehaviour
{
    [Header("Pengaturan Interaksi")]
    public KeyCode interactKey = KeyCode.E;
    public float interactionDistance = 1.2f;
    public Transform interactionPoint;          // Kosong = pakai posisi objek ini
    public Transform playerTransform;           // Kosong = dicari otomatis (TPdiam / tag Player)

    [Header("Syarat Duduk (Opsional)")]
    public KursiPerpusInteraction kursi;        // Kalau diisi, E hanya aktif saat pemain sedang duduk di kursi

    [Header("Status")]
    public bool logDiagnosis = true;            // Tampilkan log status tiap E ditekan (matikan kalau sudah beres)
    public bool terbuka = false;                // Baru bisa dipakai setelah Scene 5 (lewat BukaAktivitasBerulang)

    [Header("Hadiah & Biaya")]
    public int scoreReward = 5;                 // Poin setelah selesai membaca (+5)
    public float biayaEnergi = 0f;              // Energi yang berkurang (0 = tanpa biaya energi)
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Box Pilihan (2 Box)")]
    public GameObject boxJujur;                 // Box 1: baca sendiri (tombolnya panggil PilihJujur)
    public GameObject boxCurang;                // Box 2: pakai AI (tombolnya panggil PilihCurang)
    public float durasiJujur = 10f;             // Lama membaca kalau jujur (detik)
    public float durasiCurang = 3f;             // Lama membaca kalau curang (detik)

    [Header("Integritas (Kalau Curang)")]
    public UnityEvent saatCurang;               // Hubungkan ke fungsi pengurang integritas (nilai 5)

    [Header("Reward (Setelah Selesai Membaca)")]
    public int indexReward = 3;                 // Nomor reward di RewardManager (3 = Referensi). -1 = tanpa reward

    [Header("UI Progress Membaca (Bulat, Opsional)")]
    public GameObject progressBarCanvas;
    public Image progressCircleFill;            // Image Type: Filled

    [Header("Cooldown (Bar Bulat)")]
    public float durasiCooldown = 45f;          // Jeda (detik) sebelum buku bisa dibaca lagi
    public GameObject barCooldownBulat;         // Objek induk bar bulat cooldown
    public Image isiBarCooldown;                // Image Type: Filled, terisi pelan-pelan saat cooldown
    public GameObject teksBukuIstirahat;        // Opsional: teks "Tunggu sebentar" saat E ditekan selama cooldown
    public float lamaTeksIstirahat = 1.5f;
    public SpriteRenderer spriteBuku;           // Kosong = ambil dari objek ini
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Gimmick UI")]
    public GameObject petunjukTekanE;           // UI petunjuk "Tekan [E]" khusus buku

    private bool sedangMemilih = false;
    private bool sedangMembaca = false;
    private bool cooldownAktif = false;
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;

    // True saat pilihan sedang tampil atau pemain sedang membaca (kursi tidak boleh dibangunkan)
    public bool SedangSibuk { get { return sedangMemilih || sedangMembaca; } }

    void Start()
    {
        if (boxJujur != null) boxJujur.SetActive(false);
        if (boxCurang != null) boxCurang.SetActive(false);
        if (progressBarCanvas != null) progressBarCanvas.SetActive(false);
        if (barCooldownBulat != null) barCooldownBulat.SetActive(false);
        if (teksBukuIstirahat != null) teksBukuIstirahat.SetActive(false);
        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);

        if (spriteBuku == null) spriteBuku = GetComponent<SpriteRenderer>();
        if (spriteBuku != null) spriteBuku.color = warnaNormal;

        if (interactionPoint == null) interactionPoint = transform;
        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.Find("TPdiam");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                if (taggedPlayer != null) playerTransform = taggedPlayer.transform;
            }
        }
    }

    void Update()
    {
        // Teks istirahat hilang otomatis
        if (teksBukuIstirahat != null && teksBukuIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksBukuIstirahat.SetActive(false);

        // Bar bulat cooldown terisi pelan-pelan, lalu hilang saat selesai
        if (cooldownAktif)
        {
            float sisa = Mathf.Max(0f, cooldownSelesaiPada - Time.time);
            if (isiBarCooldown != null && durasiCooldown > 0f)
                isiBarCooldown.fillAmount = 1f - (sisa / durasiCooldown);

            if (Time.time >= cooldownSelesaiPada) SelesaiCooldown();
        }

        if (logDiagnosis && Input.GetKeyDown(interactKey))
        {
            Debug.Log("[Buku] E ditekan. terbuka=" + terbuka
                + ", sedangMemilih=" + sedangMemilih
                + ", sedangMembaca=" + sedangMembaca
                + ", cooldownAktif=" + cooldownAktif
                + ", sisaCooldown=" + Mathf.Max(0f, cooldownSelesaiPada - Time.time).ToString("0.0")
                + ", duduk=" + (kursi != null ? kursi.sedangDuduk.ToString() : "kursi kosong"));
        }

        if (!terbuka || sedangMemilih || sedangMembaca || playerTransform == null)
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
            return;
        }

        float distance = Vector2.Distance(interactionPoint.position, playerTransform.position);
        // Kalau kursi diisi, syaratnya pemain sedang duduk (bukan sekadar dekat)
        bool dekat = (kursi != null) ? kursi.sedangDuduk : (distance <= interactionDistance);

        // Selama cooldown, E tidak membuka pilihan
        if (Time.time < cooldownSelesaiPada)
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(false);

            if (dekat && Input.GetKeyDown(interactKey) && teksBukuIstirahat != null)
            {
                teksBukuIstirahat.SetActive(true);
                teksSembunyiPada = Time.time + lamaTeksIstirahat;
            }
            return;
        }

        if (dekat)
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(true);

            if (Input.GetKeyDown(interactKey))
            {
                if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
                BukaPilihan();
            }
        }
        else
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        }
    }

    // Dipanggil setelah Scene 5 selesai (lewat UnityEvent "Saat Dialog Selesai")
    public void BukaAktivitasBerulang()
    {
        terbuka = true;
        cooldownSelesaiPada = 0f;
        Debug.Log("[Buku] Aktivitas referensi terbuka.");
    }

    private void BukaPilihan()
    {
        if (boxJujur == null && boxCurang == null)
        {
            Debug.LogWarning("[Buku] Box pilihan belum dihubungkan di Inspector!");
            return;
        }

        sedangMemilih = true;
        KunciPlayer(true);

        if (boxJujur != null) boxJujur.SetActive(true);
        if (boxCurang != null) boxCurang.SetActive(true);
    }

    // Dipasang di OnClick tombol Box 1
    public void PilihJujur()
    {
        Debug.Log("[Buku] PilihJujur dipanggil dari tombol.");
        MulaiMembaca(durasiJujur, false);
    }

    // Dipasang di OnClick tombol Box 2
    public void PilihCurang()
    {
        Debug.Log("[Buku] PilihCurang dipanggil dari tombol.");
        MulaiMembaca(durasiCurang, true);
    }

    private void MulaiMembaca(float durasi, bool curang)
    {
        if (!sedangMemilih || sedangMembaca)
        {
            Debug.LogWarning("[Buku] Klik diabaikan. sedangMemilih=" + sedangMemilih + ", sedangMembaca=" + sedangMembaca
                + ". Box harus dibuka lewat tombol E (bukan diaktifkan manual).");
            return;
        }

        sedangMemilih = false;
        sedangMembaca = true;

        if (boxJujur != null) boxJujur.SetActive(false);
        if (boxCurang != null) boxCurang.SetActive(false);
        KunciPlayer(false);

        StartCoroutine(ProsesMembaca(durasi, curang));
    }

    private IEnumerator ProsesMembaca(float durasi, bool curang)
    {
        if (progressBarCanvas != null) progressBarCanvas.SetActive(true);
        if (progressCircleFill != null) progressCircleFill.fillAmount = 0f;

        float timer = 0f;
        while (timer < durasi)
        {
            timer += Time.deltaTime;
            if (progressCircleFill != null) progressCircleFill.fillAmount = timer / durasi;
            yield return null;
        }

        if (progressCircleFill != null) progressCircleFill.fillAmount = 1f;
        yield return new WaitForSeconds(0.3f);
        if (progressBarCanvas != null) progressBarCanvas.SetActive(false);

        // Poin bacaan
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreReward);
            Debug.Log($"[Buku] Selesai membaca. Skor bertambah +{scoreReward}");
        }
        else
        {
            Debug.LogWarning("[Buku] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // Integritas turun kalau curang
        if (curang)
        {
            saatCurang.Invoke();
            Debug.Log("[Buku] Pemain memilih curang (AI). Integritas dikurangi.");
        }

        // Reward muncul langsung setelah selesai membaca, alur menunggu box ditutup
        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));
        }

        // Energi (opsional)
        if (energyBar != null && biayaEnergi > 0f)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        sedangMembaca = false;
        MulaiCooldown();
    }

    private void MulaiCooldown()
    {
        cooldownSelesaiPada = Time.time + durasiCooldown;
        cooldownAktif = true;

        if (isiBarCooldown != null) isiBarCooldown.fillAmount = 0f;
        if (barCooldownBulat != null) barCooldownBulat.SetActive(true);
        if (spriteBuku != null) spriteBuku.color = warnaCooldown;
    }

    private void SelesaiCooldown()
    {
        cooldownAktif = false;

        if (barCooldownBulat != null) barCooldownBulat.SetActive(false);
        if (spriteBuku != null) spriteBuku.color = warnaNormal;
    }

    // Menahan pergerakan player selama box pilihan tampil
    private void KunciPlayer(bool kunci)
    {
        // Saat duduk, pemain biasa sudah dinonaktifkan oleh kursi, jadi tidak perlu dikunci lagi
        if (kursi != null && kursi.sedangDuduk) return;

        PlayerMovement pm = null;

        if (playerTransform != null) pm = playerTransform.GetComponent<PlayerMovement>();
        if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
        if (pm == null) return;

        if (kunci && pm.animator != null)
        {
            pm.animator.SetBool("IsMoving", false);
            pm.animator.SetFloat("MoveX", 0f);
            pm.animator.SetFloat("MoveY", 0f);
        }

        // Kalau masih duduk di kursi, pemain tetap terkunci walau box sudah ditutup
        pm.enabled = !kunci && !(kursi != null && kursi.sedangDuduk);
    }
}