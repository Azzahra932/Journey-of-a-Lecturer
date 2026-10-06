using UnityEngine;

public class InteractiveDesk : MonoBehaviour
{
    [Header("Referensi Objek")]
    public GameObject arrowIndicator;  // Drag objek 'panah' ke sini
    public Transform sitPoint;          // Drag objek 'SitPoint' ke sini
    public NPCCutscene kaprodiScript;   // Drag objek Kaprodi (kapdiam) ke sini

    [Header("Panah Tambahan yang Dimatikan Saat Duduk")]
    public GameObject[] panahDimatikanSaatDuduk;

    [Header("Scene 3 - Bimbingan Mahasiswa")]
    public NPCBimbingan bimbinganScript;   // Drag objek yang memasang NPCBimbingan (boleh kosong)   // Panah yang harus hilang tiap kali player duduk (mis. panahKeKursiDosen)

    [Header("Pengaturan Interaksi")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Pengaturan Sorting Layer Player")]
    public int defaultSortingOrder = 3; // Order sprite saat berdiri/jalan
    public int sittingSortingOrder = 2; // Order sprite saat duduk (di belakang meja)

    [Header("Sprite Khusus Duduk")]
    public Sprite sitSprite; // Asset sprite tampak belakang/duduk

    private bool isPlayerNear = false;
    private bool isSitting = false;
    private bool hasSatOnce = false;
    private GameObject playerObj;
    private Sprite originalSprite; // Menyimpan sprite asli saat berdiri

    void Update()
    {
        // 1. Jika dekat kursi dan menekan tombol E
        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            if (!isSitting)
            {
                SitDown();
            }
            else
            {
                if (!BimbinganTerkunci()) StandUp(); // Bangun lewat tombol E (diblokir saat bimbingan)
            }
        }

        // 2. Jika sedang duduk lalu menekan tombol arah (WASD / Panah), player otomatis bangun
        if (isSitting)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            if (moveX != 0 || moveY != 0)
            {
                if (!BimbinganTerkunci()) StandUp(); // Bangun lewat tombol gerak (diblokir saat bimbingan)
            }
        }
    }

    // True selama Scene 3 (mahasiswa berjalan + dialog) sedang berlangsung
    private bool BimbinganTerkunci()
    {
        return bimbinganScript != null && bimbinganScript.KunciPlayer;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            playerObj = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }

    private void SitDown()
    {
        isSitting = true;

        // Matikan panah tambahan SETIAP kali duduk (bukan hanya yang pertama)
        if (panahDimatikanSaatDuduk != null)
        {
            foreach (GameObject p in panahDimatikanSaatDuduk)
            {
                if (p != null) p.SetActive(false);
            }
        }

        // Scene 3: coba mulai bimbingan mahasiswa (hanya jalan kalau syaratnya terpenuhi)
        if (bimbinganScript != null)
        {
            bimbinganScript.CobaMulai();
        }

        // Onboarding / Kaprodi cuma jalan di interaksi pertama
        if (!hasSatOnce)
        {
            if (arrowIndicator != null)
            {
                arrowIndicator.SetActive(false);
            }

            if (kaprodiScript != null)
            {
                kaprodiScript.MulaiJalanMasuk();
            }

            hasSatOnce = true;
        }

        // Matikan Fisik & Collider Player agar tidak berbenturan
        Collider2D playerCol = playerObj.GetComponent<Collider2D>();
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();

        if (playerCol != null) playerCol.enabled = false;
        if (playerRb != null) playerRb.simulated = false;

        // Matikan Animator agar sprite duduk tidak tertimpa animasi jalan
        Animator playerAnim = playerObj.GetComponent<Animator>();
        if (playerAnim != null)
        {
            playerAnim.enabled = false;
        }

        // Ubah Sprite Karakter ke Sprite Duduk & Atur Order in Layer
        SpriteRenderer playerSr = playerObj.GetComponent<SpriteRenderer>();
        if (playerSr != null)
        {
            originalSprite = playerSr.sprite; // Simpan sprite awal
            if (sitSprite != null)
            {
                playerSr.sprite = sitSprite;  // Pakai sprite duduk
            }
            playerSr.sortingOrder = sittingSortingOrder; // Pindah ke layer belakang kursi
            playerSr.flipX = false;
        }

        // Pindahkan Posisi Player ke SitPoint
        if (sitPoint != null && playerObj != null)
        {
            playerObj.transform.position = sitPoint.position;
        }

        Debug.Log("Player duduk di kursi.");
    }

    private void StandUp()
    {
        isSitting = false;

        if (playerObj != null)
        {
            // 1. Geser sedikit ke bawah saat berdiri agar tidak tersangkut di collider kursi
            playerObj.transform.position += new Vector3(0, -0.6f, 0);

            // 2. Kembalikan Sprite & Order in Layer ke semula
            SpriteRenderer playerSr = playerObj.GetComponent<SpriteRenderer>();
            if (playerSr != null)
            {
                if (originalSprite != null)
                {
                    playerSr.sprite = originalSprite;
                }
                playerSr.sortingOrder = defaultSortingOrder; // Depan kursi
            }

            // 3. Nyalakan kembali Animator agar karakter bisa beranimasi jalan lagi
            Animator playerAnim = playerObj.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.enabled = true;
            }

            // 4. Nyalakan kembali Collider & Rigidbody agar fisiknya aktif lagi
            Collider2D playerCol = playerObj.GetComponent<Collider2D>();
            Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();

            if (playerCol != null) playerCol.enabled = true;
            if (playerRb != null) playerRb.simulated = true;
        }

        Debug.Log("Player berdiri dari kursi.");
    }
}