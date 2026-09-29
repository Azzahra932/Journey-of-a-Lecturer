using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LaptopInteraction : MonoBehaviour
{
    [Header("Pengaturan Interaksi")]
    public KeyCode interactKey = KeyCode.E;    // Tombol untuk interaksi
    public float workDuration = 3.0f;           // Lama waktu proses pembuatan RPS (detik)
    public float interactionDistance = 1.2f;    // Jarak jangkauan interaksi (meter)
    public int scoreReward = 15;                // Poin hadiah setelah selesai (+15)

    [Header("Titik Acuan Jarak (Opsional)")]
    public Transform interactionPoint;          // Drag objek "TitikInteraksi" ke sini (opsional)

    [Header("Referensi Pemain & Syarat")]
    public Transform playerTransform;           // Drag objek TPdiam / Player ke sini
    public bool isDialogDone = true;            // Di-set ke true oleh script Dialog jika percakapan Kaprodi selesai

    [Header("UI Progress Bar (Circular)")]
    public GameObject progressBarCanvas;        // Drag objek ProgressBarUI ke sini
    public Image progressCircleFill;            // Drag objek ProgressFill (Image Type: Filled) ke sini

    [Header("Gimmick UI / Petunjuk")]
    public GameObject petunjukTekanE;           // UI petunjuk "Tekan [E]"
    public GameObject panahKeKelas;             // Panah penunjuk jalan setelah tugas selesai

    private bool isWorking = false;
    private bool isCompleted = false;

    void Start()
    {
        // Paksa UI disembunyikan secara otomatis saat Play Game baru dimulai
        if (progressBarCanvas != null) progressBarCanvas.SetActive(false);
        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        if (panahKeKelas != null) panahKeKelas.SetActive(false);

        // Jika interactionPoint kosong, gunakan posisi objek Laptop ini sendiri
        if (interactionPoint == null)
        {
            interactionPoint = transform;
        }

        // Cari otomatis objek player jika slot di Inspector belum diisi
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.Find("TPdiam");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                // Fallback cari berdasarkan Tag
                GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                if (taggedPlayer != null) playerTransform = taggedPlayer.transform;
            }
        }
    }

    void Update()
    {
        // Jika tugas sudah selesai, sedang bekerja, atau player tidak ada, hentikan pengecekan
        if (isCompleted || isWorking || playerTransform == null) return;

        // Hitung jarak dari titik interaksi ke pemain
        float distance = Vector2.Distance(interactionPoint.position, playerTransform.position);

        // SYARAT: Jarak harus dekat DAN dialog percakapan Kaprodi sudah selesai
        if (distance <= interactionDistance && isDialogDone)
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(true);

            // Tekan E untuk mulai membuat RPS
            if (Input.GetKeyDown(interactKey))
            {
                StartCoroutine(ProsesMengerjakanRencanaKerja());
            }
        }
        else
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        }
    }

    private IEnumerator ProsesMengerjakanRencanaKerja()
    {
        isWorking = true;

        // Sembunyikan petunjuk [E] dan tampilkan Canvas Progress Bar
        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        if (progressBarCanvas != null) progressBarCanvas.SetActive(true);

        float timer = 0f;

        if (progressCircleFill != null)
        {
            progressCircleFill.fillAmount = 0f;
        }

        // Animasi bar bulat berputar mengisi 0% -> 100%
        while (timer < workDuration)
        {
            timer += Time.deltaTime;

            if (progressCircleFill != null)
            {
                progressCircleFill.fillAmount = timer / workDuration;
            }

            yield return null;
        }

        // Pastikan visual bar penuh 100%
        if (progressCircleFill != null)
        {
            progressCircleFill.fillAmount = 1f;
        }

        // Jeda sejenak agar pemain melihat indikator penuh
        yield return new WaitForSeconds(0.3f);

        // Sembunyikan Progress Bar
        if (progressBarCanvas != null)
        {
            progressBarCanvas.SetActive(false);
        }

        // --- PANGGIL TAMBAH 15 POIN KE SCORE MANAGER ---
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreReward);
            Debug.Log($"[Laptop] Tugas RPS Selesai! Skor bertambah +{scoreReward}");
        }
        else
        {
            Debug.LogWarning("[Laptop] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        isWorking = false;
        isCompleted = true; // Mengunci laptop agar tidak bisa diulang

        // Munculkan panah penunjuk jalan HANYA BILA pemain belum pernah sampai ke pintu kelas
        if (panahKeKelas != null && !PintuKelasTrigger.sudahPernahKeKelas)
        {
            panahKeKelas.SetActive(true);
        }
    }
}