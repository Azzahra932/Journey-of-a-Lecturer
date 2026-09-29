using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Pergerakan Player")]
    public float moveSpeed = 5f;

    [Header("Komponen & Reference")]
    public Animator animator;

    [Header("Pengaturan Kursi Pemain")]
    public Transform kursiPemain;
    public int sortingOrderDuduk = 1;
    public int sortingOrderJalan = 3;

    [Header("Sistem Cutscene NPC")]
    public NPCCutscene kaprodiCutscene;
    private bool sudahPernahCutscene = false;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private Vector2 movement;
    private Vector2 lastMoveDirection = new Vector2(0, -1); // Default menghadap bawah

    [Header("Debug Status (Read Only)")]
    public bool isSitting = false;
    public bool isInChairArea = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
            animator.SetFloat("LastMoveX", lastMoveDirection.x);
            animator.SetFloat("LastMoveY", lastMoveDirection.y);
            animator.SetBool("IsMoving", false);
        }
    }

    void Update()
    {
        // 1. Fitur Duduk / Berdiri saat tekan Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isSitting)
            {
                if (isInChairArea) Duduk();
                else Debug.Log("Masuk ke area kursi dulu baru tekan Space!");
            }
            else Berdiri();
        }

        // 2. Jika sedang duduk, batasi pergerakan
        if (isSitting)
        {
            movement = Vector2.zero;
            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveY", 0f);
            }
            return;
        }

        // 3. Ambil Input Arah (Menggunakan IF-ELSE-IF Eksplisit agar Tidak Saling Membatalkan)
        float inputX = 0f;
        float inputY = 0f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            inputX = 1f;
        }
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            inputX = -1f;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            inputY = 1f;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            inputY = -1f;
        }

        movement = new Vector2(inputX, inputY);

        // 4. Send Parameters to Animator Controller
        if (animator != null)
        {
            // Jika ada arah pergerakan yang aktif
            if (movement.x != 0f || movement.y != 0f)
            {
                // Simpan arah hadap terakhir
                lastMoveDirection = movement.normalized;

                // Aktifkan animasi jalan & kirim nilai realtime
                animator.SetBool("IsMoving", true);
                animator.SetFloat("MoveX", movement.x);
                animator.SetFloat("MoveY", movement.y);
                animator.SetFloat("LastMoveX", lastMoveDirection.x);
                animator.SetFloat("LastMoveY", lastMoveDirection.y);
            }
            else
            {
                // Saat tidak menekan tombol sama sekali
                animator.SetBool("IsMoving", false);
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveY", 0f);

                // Pertahankan arah hadap terakhir saat Idle
                animator.SetFloat("LastMoveX", lastMoveDirection.x);
                animator.SetFloat("LastMoveY", lastMoveDirection.y);
            }
        }
    }

    void FixedUpdate()
    {
        if (!isSitting && (movement.x != 0f || movement.y != 0f))
        {
            rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Chair") || other.gameObject.name == "SitPoint" || other.gameObject.name == "kursi")
        {
            isInChairArea = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Chair") || other.gameObject.name == "SitPoint" || other.gameObject.name == "kursi")
        {
            isInChairArea = false;
        }
    }

    public void Duduk()
    {
        isSitting = true;

        if (kursiPemain != null)
        {
            transform.position = kursiPemain.position;
        }

#if UNITY_6000_0_OR_NEWER
        if (rb != null) rb.linearVelocity = Vector2.zero;
#else
        if (rb != null) rb.velocity = Vector2.zero;
#endif

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrderDuduk;
        }

        if (!sudahPernahCutscene)
        {
            sudahPernahCutscene = true;
            if (kaprodiCutscene != null)
            {
                kaprodiCutscene.MulaiJalanMasuk();
            }
        }
    }

    public void Berdiri()
    {
        isSitting = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrderJalan;
        }
    }
}