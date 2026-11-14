using System.Collections;
using UnityEngine;

public class ChairInteraction : MonoBehaviour
{
    [Header("Pengaturan Kursi & Karakter")]
    public GameObject playerCharacter;    // TPdiam (player yang bisa jalan)
    public GameObject lecturerSitting;    // Tpngajar (player versi duduk)
    public GameObject promptUI;

    [Header("Tombol Duduk")]
    public KeyCode tombolDuduk = KeyCode.Space;     // Tombol untuk duduk di kursi (sebelumnya E)

    [Header("Syarat Scene 1 (kursi baru bisa dipakai setelah Scene 1 selesai)")]
    public bool butuhScene1Selesai = true;          // true = kursi kelas terkunci sampai BukaScene2() dipanggil dari akhir Scene 1
    public bool kunciUlangDiLevel2 = true;          // true = saat level 2 dimulai kursi terkunci lagi sampai BukaScene2() dipanggil dari akhir Scene 1 level 2. Matikan kalau Scene 1 level 2 belum disambungkan

    [Header("Referensi Panah & Dialog")]
    public GameObject panahKeKursi;       // Panah kursi yang akan dimatikan
    public GameObject dialogueBoxUI;      // Box Teks / Dialogue Manager

    [Header("Panah Hanya Untuk Scene 2")]
    public bool panahHanyaSekali = true;            // true = setelah duduk pertama kali, panah tidak muncul lagi walau dinyalakan script lain
    public GameObject[] panahLainDimatikan;         // Opsional: panah lain yang juga tidak boleh muncul lagi setelah duduk pertama kali

    [Header("Pengaturan Berdiri")]
    public Transform titikBerdiri;                        // Opsional: kalau diisi, player berdiri TEPAT di titik ini (mengabaikan Offset Berdiri)
    public Vector2 offsetBerdiri = new Vector2(0f, -1f);  // Posisi player saat berdiri, relatif dari Tpngajar
    public bool blokirBerdiriSaatDialog = false;          // true = tidak bisa berdiri selama dialogueBoxUI aktif
    public float jedaSetelahDuduk = 0.3f;                 // Cegah langsung berdiri tepat setelah duduk

    private bool playerIsClose = false;
    private bool isSitting = false;
    private bool sedangBerdiri = false;

    // Diset script lain (mis. LaptopKelas) untuk mengunci player di kursi selama box tutorial tampil
    public bool TerkunciEksternal { get; set; }

    // True setelah player duduk di kursi ini untuk pertama kali (akhir Scene 2)
    public bool SudahPernahDuduk { get; private set; }

    // True kalau Scene 1 sudah selesai, jadi kursi kelas (Scene 2: mengajar) boleh dipakai
    public bool Scene2Terbuka { get; private set; }

    private bool panahTertunda = false;      // true kalau panah kursi sempat menyala saat kursi masih terkunci (dimunculkan lagi saat Scene 1 selesai)
    private bool level2Aktif = false;        // true setelah SiapkanLevelBaru() dipanggil: panah selalu dimatikan di level 2
    private bool sudahPernahDialog = false;   // true setelah dialog muncul di duduk pertama
    private float waktuDuduk = 0f;

    void Start()
    {
        Scene2Terbuka = !butuhScene1Selesai;

        if (lecturerSitting != null) lecturerSitting.SetActive(false);
        if (promptUI != null) promptUI.SetActive(false);

        // Pastikan box teks tertutup di awal game
        if (dialogueBoxUI != null) dialogueBoxUI.SetActive(false);

        if (playerCharacter == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerCharacter = playerObj;
        }
    }

    // Dipanggil saat level 2 dimulai (lewat event "Saat Masuk Level 2" di PopupLevelSelesai):
    // box percakapan muncul lagi saat duduk pertama kali di kursi ini. Panah tetap mati
    // karena SudahPernahDuduk tidak direset.
    public void SiapkanLevelBaru()
    {
        sudahPernahDialog = false;
        level2Aktif = true;   // panah kursi kelas (dan panah lain di daftar) dimatikan terus di level 2

        // Level 2 punya Scene 1 sendiri: kursi terkunci lagi sampai Scene 1 level 2 selesai
        if (butuhScene1Selesai && kunciUlangDiLevel2) Scene2Terbuka = false;

        Debug.Log("[ChairInteraction] Level 2: box percakapan akan muncul lagi saat duduk (tanpa panah).");
    }

    // Dipanggil dari akhir Scene 1 (level 1 dan level 2), lewat event di Inspector: kursi kelas boleh dipakai
    public void BukaScene2()
    {
        Scene2Terbuka = true;

        // Panah yang sempat dimatikan selama terkunci dimunculkan lagi (kecuali sudah pernah duduk atau sudah level 2)
        if (panahTertunda && !SudahPernahDuduk && !level2Aktif && panahKeKursi != null)
            panahKeKursi.SetActive(true);
        panahTertunda = false;

        Debug.Log("[ChairInteraction] Scene 1 selesai, kursi kelas (Scene 2) sekarang bisa dipakai.");
    }

    // Opsional: mengunci kursi kelas lagi secara manual
    public void KunciScene2()
    {
        Scene2Terbuka = false;
    }

    void Update()
    {
        // Duduk
        if (playerIsClose && !isSitting && Input.GetKeyDown(tombolDuduk))
        {
            if (!Scene2Terbuka)
            {
                Debug.Log("[ChairInteraction] Tombol duduk ditekan, tapi kursi kelas belum bisa dipakai: Scene 1 belum selesai.");
                return;
            }

            DudukKeKursi();
            return;
        }

        // Berdiri
        if (isSitting && !sedangBerdiri && !TerkunciEksternal && TombolGerakDitekan())
        {
            if (Time.time - waktuDuduk < jedaSetelahDuduk) return;

            if (blokirBerdiriSaatDialog && dialogueBoxUI != null && dialogueBoxUI.activeInHierarchy) return;

            StartCoroutine(BerdiriDariKursi());
        }
    }

    // LateUpdate jalan setelah semua Update: panah yang dinyalakan script lain langsung dimatikan lagi
    // sebelum sempat tampil di layar.
    void LateUpdate()
    {
        // Selama Scene 1 belum selesai, panah ke kursi kelas tidak boleh tampil (siapa pun yang menyalakannya)
        if (!Scene2Terbuka && panahKeKursi != null && panahKeKursi.activeSelf)
        {
            panahKeKursi.SetActive(false);
            panahTertunda = true;
        }

        if (!panahHanyaSekali || !(SudahPernahDuduk || level2Aktif)) return;

        if (panahKeKursi != null && panahKeKursi.activeSelf) panahKeKursi.SetActive(false);

        if (panahLainDimatikan != null)
        {
            foreach (GameObject p in panahLainDimatikan)
            {
                if (p != null && p.activeSelf) p.SetActive(false);
            }
        }
    }

    // Berdiri kalau menekan WASD atau tombol panah
    bool TombolGerakDitekan()
    {
        return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
               Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
               Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
               Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);
    }

    void DudukKeKursi()
    {
        isSitting = true;
        SudahPernahDuduk = true;
        waktuDuduk = Time.time;
        if (promptUI != null) promptUI.SetActive(false);

        // 1. Matikan panah kursi
        if (panahKeKursi != null) panahKeKursi.SetActive(false);

        // 2. Ubah karakter player jadi dosen duduk
        if (playerCharacter != null) playerCharacter.SetActive(false);
        if (lecturerSitting != null) lecturerSitting.SetActive(true);

        // 3. Munculkan box teks HANYA saat duduk pertama kali
        if (!sudahPernahDialog)
        {
            sudahPernahDialog = true;
            if (dialogueBoxUI != null) dialogueBoxUI.SetActive(true);
            Debug.Log("Player duduk pertama kali, box teks muncul.");
        }
        else
        {
            Debug.Log("Player duduk lagi, box teks tidak dimunculkan.");
        }
    }

    IEnumerator BerdiriDariKursi()
    {
        sedangBerdiri = true;

        // Tunggu 1 frame supaya TPdiam aktif dengan bersih setelah Tpngajar dimatikan
        yield return null;

        if (playerCharacter != null && lecturerSitting != null)
        {
            // Posisikan player berdiri di dekat kursi
            if (titikBerdiri != null)
            {
                playerCharacter.transform.position = new Vector3(titikBerdiri.position.x, titikBerdiri.position.y, playerCharacter.transform.position.z);
            }
            else
            {
                playerCharacter.transform.position = lecturerSitting.transform.position + (Vector3)offsetBerdiri;
            }
        }

        if (lecturerSitting != null) lecturerSitting.SetActive(false);
        if (playerCharacter != null)
        {
            playerCharacter.SetActive(true);

            PlayerMovement pm = playerCharacter.GetComponent<PlayerMovement>();
            if (pm != null) pm.Berdiri();   // reset isSitting & sorting order di PlayerMovement
        }

        isSitting = false;

        Debug.Log("Player berdiri dari kursi kelas.");

        yield return new WaitForSeconds(0.2f);
        sedangBerdiri = false;
    }

    // Dipanggil TransisiKembaliKeAwal: berdiri langsung (tanpa jeda) lalu player dipindah ke titik awal
    public void PaksaBerdiriSegera()
    {
        if (!isSitting) return;

        if (lecturerSitting != null) lecturerSitting.SetActive(false);
        if (playerCharacter != null)
        {
            playerCharacter.SetActive(true);

            PlayerMovement pm = playerCharacter.GetComponent<PlayerMovement>();
            if (pm != null) pm.Berdiri();
        }

        isSitting = false;
        sedangBerdiri = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.gameObject == playerCharacter)
        {
            playerIsClose = true;
            if (promptUI != null && !isSitting && Scene2Terbuka) promptUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.gameObject == playerCharacter)
        {
            playerIsClose = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }
}