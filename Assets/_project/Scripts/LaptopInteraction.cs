using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LaptopInteraction : MonoBehaviour
{
    [Header("Pengaturan Interaksi")]
    public KeyCode interactKey = KeyCode.E;    // Tombol E
    public float workDuration = 3.0f;           // Lama waktu loading (detik)
    public float interactionDistance = 1.2f;    // Jarak diperkecil (1.2 meter) agar pas di depan meja

    [Header("Titik Acuan Jarak (Opsional)")]
    public Transform interactionPoint;          // Drag objek "TitikInteraksi" ke sini

    [Header("Referensi Pemain")]
    public Transform playerTransform;           // Drag objek TPdiam ke sini

    [Header("UI Progress Bar (Circular)")]
    public GameObject progressBarCanvas;
    public Image progressCircleFill;

    [Header("Gimmick Tambahan")]
    public GameObject panahKeKelas;
    public GameObject petunjukTekanE;

    private bool isWorking = false;
    private bool isCompleted = false;

    void Start()
    {
        if (progressBarCanvas != null) progressBarCanvas.SetActive(false);
        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        if (panahKeKelas != null) panahKeKelas.SetActive(false);

        // Jika interactionPoint tidak diisi manual, gunakan posisi laptop sendiri
        if (interactionPoint == null)
        {
            interactionPoint = transform;
        }

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.Find("TPdiam");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
    }

    void Update()
    {
        if (isCompleted || isWorking || playerTransform == null) return;

        // Hitung jarak dari "TitikInteraksi" ke pemain
        float distance = Vector2.Distance(interactionPoint.position, playerTransform.position);

        // Jika pemain benar-benar dekat (dalam radius interactionDistance)
        if (distance <= interactionDistance)
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(true);

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

        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        if (progressBarCanvas != null) progressBarCanvas.SetActive(true);

        float timer = 0f;

        if (progressCircleFill != null)
        {
            progressCircleFill.fillAmount = 0f;
        }

        while (timer < workDuration)
        {
            timer += Time.deltaTime;

            if (progressCircleFill != null)
            {
                progressCircleFill.fillAmount = timer / workDuration;
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

        isWorking = false;
        isCompleted = true;

        if (panahKeKelas != null)
        {
            panahKeKelas.SetActive(true);
        }
    }
}