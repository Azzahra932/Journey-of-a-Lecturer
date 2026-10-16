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
    public float biayaEnergi = 15f;             // TAMBAHAN BARU: energi yang berkurang setelah tugas RPS selesai
    public EnergyBar energyBar;                 // TAMBAHAN BARU: kosong = dicari otomatis

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

    // ===== TAMBAHAN BARU: Box Hasil + Overlay Hitam (muncul setelah bar bulat selesai) =====
    [Header("Box Hasil + Overlay Hitam (Setelah Bar Selesai)")]
    public GameObject overlayHitam;             // Panel hitam transparan satu layar (di belakang box). Kosong = tanpa overlay
    public GameObject boxHasil;                 // Box buatanmu (gambar + teks sudah jadi). Kosong = tanpa box
    public KeyCode tombolTutupBox = KeyCode.Space;    // Space untuk menutup box
    public bool bolehKlikMouse = true;                // Klik kiri mouse juga bisa menutup box
    // ======================================================================================

    private bool isWorking = false;
    private bool isCompleted = false;

    void Start()
    {
        // Paksa UI disembunyikan secara otomatis saat Play Game baru dimulai
        if (progressBarCanvas != null) progressBarCanvas.SetActive(false);
        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        if (panahKeKelas != null) panahKeKelas.SetActive(false);

        // TAMBAHAN BARU: cari EnergyBar otomatis
        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // TAMBAHAN BARU: sembunyikan overlay dan box hasil di awal game
        if (overlayHitam != null) overlayHitam.SetActive(false);
        if (boxHasil != null) boxHasil.SetActive(false);

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

                // MURNI MEMBUKA PILIHAN BOX SAJA (Tidak ada progress bar otomatis saat tekan E)
                if (choiceHandler != null)
                {
                    choiceHandler.OpenChoices();
                }
                else
                {
                    Debug.LogWarning("[Laptop] LaptopChoiceHandler belum dihubungkan di Inspector!");
                }
            }
        }
        else
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        }
    }

    // Menerima durasi dinamis dari pilihan box (20s atau 3s) yang dipanggil oleh LaptopChoiceHandler
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

        // Proses berjalan sesuai durasi pilihan (20 detik atau 3 detik)
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

        // Menambahkan poin ke ScoreManager pusat (+15)
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreReward);
            Debug.Log($"[Laptop Scene 1] Tugas RPS Selesai! Skor bertambah +{scoreReward}");
        }
        else
        {
            Debug.LogWarning("[Laptop Scene 1] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // TAMBAHAN BARU: tampilkan box hasil dengan overlay hitam, tunggu pemain menutupnya
        yield return StartCoroutine(TampilkanBoxHasil());

        // TAMBAHAN BARU: energi berkurang setelah tugas selesai, lalu cek apakah habis
        if (energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        isWorking = false;
        isCompleted = true;

        if (panahKeKelas != null && !PintuKelasTrigger.sudahPernahKeKelas)
        {
            panahKeKelas.SetActive(true);
        }
    }

    // ===== TAMBAHAN BARU: fungsi box hasil + overlay hitam =====
    private IEnumerator TampilkanBoxHasil()
    {
        // Kalau overlay dan box sama-sama kosong, langkah ini dilewati (alur tetap seperti semula)
        if (overlayHitam == null && boxHasil == null) yield break;

        KunciPlayer(true);

        // Urutan penting: overlay dulu, baru box (box harus tampil di atas overlay)
        if (overlayHitam != null) overlayHitam.SetActive(true);
        if (boxHasil != null) boxHasil.SetActive(true);

        // Jeda singkat supaya tombol yang sedang ditekan tidak langsung menutup box
        yield return new WaitForSeconds(0.3f);

        while (!(Input.GetKeyDown(tombolTutupBox) || (bolehKlikMouse && Input.GetMouseButtonDown(0))))
            yield return null;

        if (boxHasil != null) boxHasil.SetActive(false);
        if (overlayHitam != null) overlayHitam.SetActive(false);

        KunciPlayer(false);
    }

    // Menahan pergerakan player selama box tampil
    private void KunciPlayer(bool kunci)
    {
        PlayerMovement pm = null;

        if (playerTransform != null) pm = playerTransform.GetComponent<PlayerMovement>();
        if (pm == null) pm = FindObjectOfType<PlayerMovement>();
        if (pm == null) return;

        if (kunci && pm.animator != null)
        {
            pm.animator.SetBool("IsMoving", false);
            pm.animator.SetFloat("MoveX", 0f);
            pm.animator.SetFloat("MoveY", 0f);
        }

        pm.enabled = !kunci;
    }
    // ============================================================
}