using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LaptopInteraction : MonoBehaviour
{
    [Header("Pengaturan Interaksi")]
    public KeyCode interactKey = KeyCode.E;    // Tombol untuk interaksi
    public float workDuration = 3.0f;           // (Default cadangan jika dipanggil langsung)
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

    [Header("Referensi Pilihan Laptop (Baru)")]
    public LaptopChoiceHandler choiceHandler;   // Drag objek LaptopChoiceManager ke sini

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
                GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                if (taggedPlayer != null) playerTransform = taggedPlayer.transform;
            }
        }

        // Cari otomatis LaptopChoiceHandler jika belum didrag di Inspector
        if (choiceHandler == null)
        {
            choiceHandler = FindObjectOfType<LaptopChoiceHandler>();
        }
    }

    void Update()
    {
        if (isCompleted || isWorking || playerTransform == null) return;

        float distance = Vector2.Distance(interactionPoint.position, playerTransform.position);

        if (distance <= interactionDistance && isDialogDone)
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(true);

            if (Input.GetKeyDown(interactKey))
            {
                if (petunjukTekanE != null) petunjukTekanE.SetActive(false);

                if (choiceHandler != null)
                {
                    choiceHandler.OpenChoices();
                }
                else
                {
                    Debug.LogWarning("[Laptop] LaptopChoiceHandler belum dihubungkan!");
                    StartCoroutine(ProsesMengerjakanRencanaKerja(workDuration));
                }
            }
        }
        else
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        }
    }

    // Menerima durasi dinamis dari pilihan box (20s atau 10s)
    public void MulaiPekerjaanDariPilihan(float customDuration)
    {
        if (!isWorking && !isCompleted)
        {
            StartCoroutine(ProsesMengerjakanRencanaKerja(customDuration));
        }
    }

    private IEnumerator ProsesMengerjakanRencanaKerja(float duration)
    {
        isWorking = true;

        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        if (progressBarCanvas != null) progressBarCanvas.SetActive(true);

        float timer = 0f;

        if (progressCircleFill != null)
        {
            progressCircleFill.fillAmount = 0f;
        }

        // Proses berjalan sesuai durasi pilihan (20 detik atau 10 detik)
        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (progressCircleFill != null)
            {
                progressCircleFill.fillAmount = timer / duration;
            }

            yield return null;
        }

        if (progressCircleFill != null)
        {
            progressCircleFill.fillAmount = 1f;
        }

        yield return new WaitForSeconds(0.3f);

        if (progressBarCanvas != null)
        {
            progressBarCanvas.SetActive(false);
        }

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
        isCompleted = true;

        if (panahKeKelas != null && !PintuKelasTrigger.sudahPernahKeKelas)
        {
            panahKeKelas.SetActive(true);
        }
    }
}