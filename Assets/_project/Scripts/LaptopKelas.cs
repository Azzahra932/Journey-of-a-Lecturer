using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LaptopKelas : MonoBehaviour
{
    [Header("UI Progress Bar Kerja (Kelas)")]
    public GameObject canvasProgressBar;   // barbulet (bar kerja 3 detik) - milik KELAS
    public Image circularProgressBar;      // fill (Image Type: Filled)

    [Header("Ikon Status Laptop")]
    public GameObject promptPressE;        // ikon "E" -> muncul saat laptop SIAP
    public GameObject ikonBateraiLemah;    // objek induk baterai -> muncul saat COOLDOWN
    public IndikatorBaterai indikatorBaterai;  // script pengisi segmen baterai (pasang di objek baterai)
    public TMP_Text teksCountdown;         // angka hitung mundur (contoh 0:45), opsional
    public GameObject teksIstirahat;       // teks "Laptop sedang istirahat" saat E ditekan, opsional
    public float lamaTeksIstirahat = 1.5f;

    [Header("Efek Layar Laptop")]
    public SpriteRenderer spriteLaptop;    // kosong = ambil dari objek ini
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);  // layar redup

    [Header("Pengaturan Game")]
    public float durasiKerja = 3f;         // detik bar kerja terisi
    public float durasiCooldown = 60f;     // detik laptop istirahat
    public int jumlahPoinReward = 10;
    public KeyCode tombolInteraksi = KeyCode.E;

    [Header("Syarat Player")]
    [Tooltip("Drag objek Tpngajar (player duduk di kelas). Hanya aktif saat player duduk.")]
    public Transform playerTransform;
    public bool wajibDuduk = true;

    [Header("Deteksi Jarak (Opsional)")]
    public bool cekJarak = false;
    public Transform interactionPoint;
    public float interactionDistance = 3f;

    private enum Status { Siap, Bekerja, Cooldown }
    private Status status = Status.Siap;

    private float progressTimer = 0f;
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;

    void Start()
    {
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
        if (promptPressE != null) promptPressE.SetActive(false);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);

        if (interactionPoint == null) interactionPoint = transform;
        if (spriteLaptop == null) spriteLaptop = GetComponent<SpriteRenderer>();
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;

        // Cari Tpngajar otomatis (termasuk yang nonaktif)
        if (playerTransform == null)
        {
            Transform[] semua = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform t in semua)
            {
                if (t.name == "Tpngajar") { playerTransform = t; break; }
            }
        }

        if (playerTransform == null)
            Debug.LogWarning("[LaptopKelas] Player Transform (Tpngajar) tidak ditemukan! Drag manual di Inspector.");
    }

    bool PlayerBolehInteraksi()
    {
        if (playerTransform == null) return false;
        if (wajibDuduk && !playerTransform.gameObject.activeInHierarchy) return false;

        if (cekJarak)
        {
            float jarak = Vector2.Distance(interactionPoint.position, playerTransform.position);
            if (jarak > interactionDistance) return false;
        }
        return true;
    }

    void Update()
    {
        // Teks "istirahat" hilang otomatis
        if (teksIstirahat != null && teksIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksIstirahat.SetActive(false);

        switch (status)
        {
            case Status.Siap: UpdateSiap(); break;
            case Status.Bekerja: UpdateBekerja(); break;
            case Status.Cooldown: UpdateCooldown(); break;
        }
    }

    // ---------- SIAP: layar terang, ikon E ----------
    void UpdateSiap()
    {
        bool boleh = PlayerBolehInteraksi();
        if (promptPressE != null) promptPressE.SetActive(boleh);

        if (boleh && Input.GetKeyDown(tombolInteraksi))
            MulaiMengajar();
    }

    // ---------- BEKERJA: bar bulat ----------
    void UpdateBekerja()
    {
        // Kalau player berdiri di tengah proses, batalkan (tanpa cooldown, tanpa poin)
        if (!PlayerBolehInteraksi())
        {
            BatalkanKerja();
            return;
        }

        progressTimer += Time.deltaTime;

        if (circularProgressBar != null)
            circularProgressBar.fillAmount = Mathf.Clamp01(progressTimer / durasiKerja);

        if (progressTimer >= durasiKerja)
            SelesaiMengajar();
    }

    // ---------- COOLDOWN: layar redup, baterai lemah, hitung mundur ----------
    void UpdateCooldown()
    {
        float sisa = cooldownSelesaiPada - Time.time;

        if (teksCountdown != null)
        {
            int detik = Mathf.Max(0, Mathf.CeilToInt(sisa));
            teksCountdown.text = (detik / 60) + ":" + (detik % 60).ToString("00");
        }

        // Isi baterai bertahap (0 = kosong, 1 = penuh)
        if (indikatorBaterai != null)
            indikatorBaterai.SetProgress(Mathf.Clamp01(1f - (sisa / durasiCooldown)));

        // Player tekan E saat laptop masih istirahat
        if (PlayerBolehInteraksi() && Input.GetKeyDown(tombolInteraksi))
        {
            if (teksIstirahat != null)
            {
                teksIstirahat.SetActive(true);
                teksSembunyiPada = Time.time + lamaTeksIstirahat;
            }
        }

        if (sisa <= 0f)
            SelesaiCooldown();
    }

    // ---------- Transisi ----------
    void MulaiMengajar()
    {
        Debug.Log("[LaptopKelas] Mulai mengajar.");

        status = Status.Bekerja;
        progressTimer = 0f;

        if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;
        if (promptPressE != null) promptPressE.SetActive(false);
        if (canvasProgressBar != null) canvasProgressBar.SetActive(true);
    }

    void BatalkanKerja()
    {
        status = Status.Siap;
        progressTimer = 0f;

        if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;
        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);
    }

    void SelesaiMengajar()
    {
        if (circularProgressBar != null) circularProgressBar.fillAmount = 1f;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(jumlahPoinReward);
            Debug.Log("[LaptopKelas] Tugas di kelas selesai! Skor bertambah +" + jumlahPoinReward);
        }
        else
        {
            Debug.LogWarning("[LaptopKelas] ScoreManager.Instance tidak ditemukan di Scene ini!");
        }

        if (canvasProgressBar != null) canvasProgressBar.SetActive(false);

        MulaiCooldown();
    }

    void MulaiCooldown()
    {
        status = Status.Cooldown;
        cooldownSelesaiPada = Time.time + durasiCooldown;

        // Layar redup + baterai lemah + angka hitung mundur
        if (promptPressE != null) promptPressE.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaCooldown;
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(true);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(true);
        if (indikatorBaterai != null) indikatorBaterai.SetProgress(0f);
    }

    void SelesaiCooldown()
    {
        Debug.Log("[LaptopKelas] Laptop menyala lagi, siap dipakai.");

        status = Status.Siap;

        // Layar menyala lagi, ikon baterai hilang, ikon E akan muncul di UpdateSiap
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(false);
        if (teksIstirahat != null) teksIstirahat.SetActive(false);
    }
}