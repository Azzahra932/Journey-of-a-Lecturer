using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class LaptopInteraction : MonoBehaviour
{
    [Header("Pengaturan Interaksi")]
    public KeyCode interactKey = KeyCode.E;    // Tombol untuk interaksi
    public float workDuration = 3.0f;           // (Default cadangan jika dipanggil langsung)
    public float interactionDistance = 1.2f;    // Jarak jangkauan interaksi (meter)
    public int scoreReward = 15;                // Poin hadiah setelah selesai (+15)
    public float biayaEnergi = 15f;             // Energi yang berkurang setelah tugas RPS selesai
    public EnergyBar energyBar;                 // Kosong = dicari otomatis

    [Header("Level 2 (RPS tidak dipakai)")]
    public bool nonaktifDiLevel2 = true;        // true = di level 2 laptop tetap terlihat, tapi RPS dan ikon E tidak berfungsi
    public int targetSkorLevel1 = 150;          // Target skor level 1. Target skor yang lebih besar dari ini berarti level 2 sudah dimulai

    [Header("Titik Acuan Jarak (Opsional)")]
    public Transform interactionPoint;          // Drag objek "TitikInteraksi" ke sini (opsional)

    [Header("Referensi Pemain & Syarat")]
    public Transform playerTransform;           // Drag objek TPdiam / Player ke sini
    public bool isDialogDone = true;            // Di-set ke true oleh script Dialog jika percakapan Kaprodi selesai

    [Header("UI Progress Bar (Circular)")]
    public GameObject progressBarCanvas;        // Drag objek ProgressBarUI ke sini
    public Image progressCircleFill;            // Drag objek ProgressFill (Image Type: Filled) ke sini

    [Header("Sound Effect Mengetik (selama bar bulat berputar)")]
    public AudioClip suaraKetik;                // File mp3 suara mengetik. Kosong = tanpa suara
    [Range(0f, 1f)] public float volumeSuaraKetik = 1f;
    public bool suaraDiulang = true;            // true = suara diulang terus selama bar berputar
    public AudioSource audioSource;             // Kosong = dibuat otomatis di objek ini

    [Header("Gimmick UI / Petunjuk")]
    public GameObject petunjukTekanE;           // UI petunjuk "Tekan [E]"
    public GameObject panahKeKelas;             // Panah penunjuk jalan setelah tugas selesai

    [Header("Referensi Pilihan Laptop (Baru)")]
    public LaptopChoiceHandler choiceHandler;   // Drag objek LaptopChoiceManager ke sini

    [Header("Aktivitas Berulang (Setelah Scene 5)")]
    public float durasiCooldown = 60f;           // Jeda (detik) sebelum RPS bisa dikerjakan lagi
    public GameObject teksLaptopIstirahat;       // Teks "Laptop sedang istirahat" saat E ditekan selama cooldown (opsional)
    public float lamaTeksIstirahat = 1.5f;
    public bool mulaiModeBerulang = false;       // Centang di level 2: aktivitas langsung terbuka berulang sejak awal (tanpa menunggu Scene 5)

    [Header("Cooldown Baterai (Seperti Laptop Kelas, Opsional)")]
    public GameObject ikonBateraiLemah;          // Objek induk baterai, muncul saat cooldown
    public IndikatorBaterai indikatorBaterai;    // Script pengisi segmen baterai (belum dihubungkan di kode ini)
    public TMP_Text teksCountdown;               // Teks di dekat baterai (contoh "Recharging..."), opsional
    public string isiTeksCooldown = "Recharging...";   // Tulisan yang tampil selama cooldown
    public SpriteRenderer spriteLaptop;          // Kosong = ambil dari objek ini
    public Color warnaNormal = Color.white;
    public Color warnaCooldown = new Color(0.45f, 0.45f, 0.45f, 1f);   // Layar redup

    [Header("Box Hasil + Overlay Hitam (Setelah Bar Selesai)")]
    public GameObject overlayHitam;             // Panel hitam transparan satu layar (di belakang box). Kosong = tanpa overlay
    public GameObject boxHasil;                 // Box buatanmu (gambar + teks sudah jadi). Kosong = tanpa box
    public KeyCode tombolTutupBox = KeyCode.Space;    // Space untuk menutup box
    public bool bolehKlikMouse = true;                // Klik kiri mouse juga bisa menutup box

    [Header("Reward (Setelah Tugas Selesai)")]
    public int indexReward = 0;                 // Nomor reward di RewardManager (0 = RPS). -1 = tanpa reward

    [Header("Setelah Tugas RPS Pertama Selesai (Scene 1 Level 1 selesai)")]
    public UnityEvent saatTugasPertamaSelesai;  // Dipanggil sekali, setelah tugas RPS pertama selesai (mis. membuka kursi kelas: ChairInteraction.BukaScene2)

    private bool modeBerulang = false;           // True setelah Scene 5 (RPS bisa diulang dengan cooldown)
    private float cooldownSelesaiPada = 0f;
    private float teksSembunyiPada = 0f;
    private bool cooldownAktif = false;          // True selama tampilan cooldown (baterai) menyala

    private bool isWorking = false;
    private bool isCompleted = false;
    private bool fungsiDimatikan = false;        // True setelah fungsi RPS dimatikan di level 2
    private bool eventPertamaSudahDipanggil = false;   // True setelah event tugas RPS pertama dipanggil

    // True selama tugas RPS berjalan (sampai box hasil ditutup), dipakai pop-up level selesai
    public bool SedangSibuk { get { return isWorking; } }

    void Start()
    {
        // Paksa UI disembunyikan secara otomatis saat Play Game baru dimulai
        if (progressBarCanvas != null) progressBarCanvas.SetActive(false);
        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        if (panahKeKelas != null) panahKeKelas.SetActive(false);

        // Sembunyikan teks istirahat laptop
        if (teksLaptopIstirahat != null) teksLaptopIstirahat.SetActive(false);

        // Sembunyikan tampilan baterai cooldown
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(false);
        if (spriteLaptop == null) spriteLaptop = GetComponent<SpriteRenderer>();
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;

        // Cari EnergyBar otomatis
        if (energyBar == null) energyBar = FindFirstObjectByType<EnergyBar>();

        // Sembunyikan overlay dan box hasil di awal game
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
            choiceHandler = FindFirstObjectByType<LaptopChoiceHandler>();
        }

        // Level 2: langsung dalam mode berulang
        if (mulaiModeBerulang) BukaAktivitasBerulang();
    }

    // ---------- Sound effect mengetik ----------
    private void MulaiSuaraKetik()
    {
        if (suaraKetik == null) return;

        // Buat AudioSource khusus di objek ini (tidak mengganggu AudioSource lain yang mungkin sudah ada)
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;   // suara 2D: terdengar sama di mana pun posisi kamera
        }

        audioSource.clip = suaraKetik;
        audioSource.loop = suaraDiulang;
        audioSource.volume = volumeSuaraKetik;
        audioSource.Play();
    }

    private void HentikanSuaraKetik()
    {
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == suaraKetik)
            audioSource.Stop();
    }

    void OnDisable()
    {
        HentikanSuaraKetik();
    }

    void Update()
    {
        // Level 2: laptop tetap terlihat, tapi RPS tidak berfungsi dan ikon E tidak muncul
        if (nonaktifDiLevel2 && !isWorking && Level2Aktif())
        {
            if (!fungsiDimatikan) MatikanFungsiLevel2();
            return;
        }

        // Teks istirahat hilang otomatis
        if (teksLaptopIstirahat != null && teksLaptopIstirahat.activeSelf && Time.time >= teksSembunyiPada)
            teksLaptopIstirahat.SetActive(false);

        if (isCompleted || isWorking || playerTransform == null) return;

        // Cooldown selesai -> baterai hilang, layar laptop terang lagi
        if (cooldownAktif && Time.time >= cooldownSelesaiPada) SelesaiTampilanCooldown();

        // Hitung mundur selama cooldown
        if (cooldownAktif)
        {
            float sisa = Mathf.Max(0f, cooldownSelesaiPada - Time.time);

            // Segmen baterai terisi bertahap: 0 (kosong) -> 1 (penuh)
            if (indikatorBaterai != null && durasiCooldown > 0f)
                indikatorBaterai.SetProgress(1f - (sisa / durasiCooldown));
        }

        // Selama cooldown (mode berulang), E tidak membuka pilihan
        if (Time.time < cooldownSelesaiPada)
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(false);

            float jarakCooldown = Vector2.Distance(interactionPoint.position, playerTransform.position);
            if (jarakCooldown <= interactionDistance && Input.GetKeyDown(interactKey) && teksLaptopIstirahat != null)
            {
                teksLaptopIstirahat.SetActive(true);
                teksSembunyiPada = Time.time + lamaTeksIstirahat;
            }
            return;
        }

        float distance = Vector2.Distance(interactionPoint.position, playerTransform.position);

        if (distance <= interactionDistance && isDialogDone)
        {
            if (petunjukTekanE != null) petunjukTekanE.SetActive(true);

            if (Input.GetKeyDown(interactKey))
            {
                if (petunjukTekanE != null) petunjukTekanE.SetActive(false);

                // Murni membuka pilihan box saja (tidak ada progress bar otomatis saat tekan E)
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

    // Level 2 dianggap sudah dimulai kalau target skor sudah naik (150 -> 300) setelah pop-up Level Selesai
    private bool Level2Aktif()
    {
        return ScoreManager.Instance != null && ScoreManager.Instance.targetScore > targetSkorLevel1;
    }

    // Menyembunyikan semua tampilan fungsi RPS (ikon E, cooldown, panah). Laptop sendiri tetap terlihat.
    private void MatikanFungsiLevel2()
    {
        fungsiDimatikan = true;
        cooldownAktif = false;

        if (petunjukTekanE != null) petunjukTekanE.SetActive(false);
        if (teksLaptopIstirahat != null) teksLaptopIstirahat.SetActive(false);
        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(false);
        if (panahKeKelas != null) panahKeKelas.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;

        Debug.Log("[Laptop] Level 2: fungsi RPS dimatikan, laptop hanya tampil sebagai dekorasi.");
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

        // Suara mengetik mulai bersamaan dengan bar berputar
        MulaiSuaraKetik();

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

        // Suara berhenti saat bar penuh
        HentikanSuaraKetik();

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
            Debug.Log($"[Laptop] Tugas RPS Selesai! Skor bertambah +{scoreReward}");
        }
        else
        {
            Debug.LogWarning("[Laptop] ScoreManager.Instance tidak ditemukan di Scene!");
        }

        // Tampilkan box hasil dengan overlay hitam, tunggu pemain menutupnya.
        // Hanya untuk pengerjaan pertama (Scene 1). Setelah Scene 5 (mode berulang) dilewati.
        if (!modeBerulang)
        {
            yield return StartCoroutine(TampilkanBoxHasil());
        }

        // Reward muncul langsung setelah tugas selesai, alur menunggu box ditutup
        if (indexReward >= 0 && RewardManager.Instance != null)
        {
            yield return StartCoroutine(RewardManager.Instance.RewardRoutine(indexReward));
        }

        // Energi berkurang setelah tugas selesai, lalu cek apakah habis
        if (energyBar != null)
        {
            energyBar.DecreaseEnergy(biayaEnergi);
            energyBar.CekEnergiHabis();
        }

        isWorking = false;
        isCompleted = true;

        // Mode berulang (setelah Scene 5): tidak terkunci permanen, tapi cooldown dulu
        if (modeBerulang)
        {
            isCompleted = false;
            cooldownSelesaiPada = Time.time + durasiCooldown;

            // Tampilkan baterai + redupkan layar laptop
            cooldownAktif = true;
            if (indikatorBaterai != null) indikatorBaterai.SetProgress(0f);
            if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(true);
            if (teksCountdown != null)
            {
                teksCountdown.text = isiTeksCooldown;
                teksCountdown.gameObject.SetActive(true);
            }
            if (spriteLaptop != null) spriteLaptop.color = warnaCooldown;
        }

        if (panahKeKelas != null && !modeBerulang && !PintuKelasTrigger.sudahPernahKeKelas)
        {
            panahKeKelas.SetActive(true);
        }

        // Tugas RPS pertama selesai (Scene 1 Level 1 selesai): beri tahu script lain, hanya sekali
        if (!modeBerulang && !eventPertamaSudahDipanggil)
        {
            eventPertamaSudahDipanggil = true;
            saatTugasPertamaSelesai.Invoke();
            Debug.Log("[Laptop] Tugas RPS pertama selesai, event Saat Tugas Pertama Selesai dipanggil.");
        }
    }

    // Dipanggil setelah Scene 5 selesai (lewat UnityEvent "Saat Dialog Selesai")
    // RPS di laptop dosen bisa dikerjakan lagi, dengan cooldown antar pengerjaan
    public void BukaAktivitasBerulang()
    {
        modeBerulang = true;
        isCompleted = false;
        isDialogDone = true;
        cooldownSelesaiPada = 0f;

        Debug.Log("[Laptop] Aktivitas RPS terbuka untuk dikerjakan berulang.");
    }

    // Dipanggil saat cooldown selesai: baterai hilang, layar laptop terang lagi
    private void SelesaiTampilanCooldown()
    {
        cooldownAktif = false;

        if (ikonBateraiLemah != null) ikonBateraiLemah.SetActive(false);
        if (teksCountdown != null) teksCountdown.gameObject.SetActive(false);
        if (spriteLaptop != null) spriteLaptop.color = warnaNormal;
    }

    // Box hasil + overlay hitam
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
        if (pm == null) pm = FindFirstObjectByType<PlayerMovement>();
        if (pm == null) return;

        if (kunci && pm.animator != null)
        {
            pm.animator.SetBool("IsMoving", false);
            pm.animator.SetFloat("MoveX", 0f);
            pm.animator.SetFloat("MoveY", 0f);
        }

        pm.enabled = !kunci;
    }
}