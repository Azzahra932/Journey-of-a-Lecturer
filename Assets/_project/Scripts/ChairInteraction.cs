using System.Collections;
using UnityEngine;

public class ChairInteraction : MonoBehaviour
{
    [Header("Pengaturan Kursi & Karakter")]
    public GameObject playerCharacter;    // TPdiam (player yang bisa jalan)
    public GameObject lecturerSitting;    // Tpngajar (player versi duduk)
    public GameObject promptUI;

    [Header("Referensi Panah & Dialog")]
    public GameObject panahKeKursi;       // Panah kursi yang akan dimatikan
    public GameObject dialogueBoxUI;      // Box Teks / Dialogue Manager

    [Header("Pengaturan Berdiri")]
    public Vector2 offsetBerdiri = new Vector2(0f, -1f);  // Posisi player saat berdiri, relatif dari Tpngajar
    public bool blokirBerdiriSaatDialog = false;          // true = tidak bisa berdiri selama dialogueBoxUI aktif
    public float jedaSetelahDuduk = 0.3f;                 // Cegah langsung berdiri tepat setelah duduk

    private bool playerIsClose = false;
    private bool isSitting = false;
    private bool sedangBerdiri = false;
    private bool sudahPernahDialog = false;   // true setelah dialog muncul di duduk pertama
    private float waktuDuduk = 0f;

    void Start()
    {
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

    void Update()
    {
        // Duduk
        if (playerIsClose && !isSitting && Input.GetKeyDown(KeyCode.E))
        {
            DudukKeKursi();
            return;
        }

        // Berdiri
        if (isSitting && !sedangBerdiri && TombolGerakDitekan())
        {
            if (Time.time - waktuDuduk < jedaSetelahDuduk) return;

            if (blokirBerdiriSaatDialog && dialogueBoxUI != null && dialogueBoxUI.activeInHierarchy) return;

            StartCoroutine(BerdiriDariKursi());
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
            playerCharacter.transform.position = lecturerSitting.transform.position + (Vector3)offsetBerdiri;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.gameObject == playerCharacter)
        {
            playerIsClose = true;
            if (promptUI != null && !isSitting) promptUI.SetActive(true);
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