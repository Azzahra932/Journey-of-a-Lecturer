using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Pergerakan Player")]
    public float moveSpeed = 5f;

    [Header("Pengaturan Kursi Pemain")]
    public Transform kursiPemain;
    public int sortingOrderDuduk = 1;
    public int sortingOrderJalan = 3;

    [Header("Sistem Cutscene NPC")]
    public NPCCutscene kaprodiCutscene;
    private bool sudahPernahCutscene = false;  // Penanda agar cutscene hanya 1x

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;

    private bool isSitting = false;
    private bool isInChairArea = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isSitting)
            {
                if (isInChairArea)
                {
                    Duduk();
                }
                else
                {
                    Debug.Log("Masuk ke area kursi dulu baru tekan Space!");
                }
            }
            else
            {
                Berdiri();
            }
        }

        if (isSitting)
        {
            movement = Vector2.zero;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (animator != null)
        {
            animator.SetFloat("Speed", movement.sqrMagnitude);
        }
    }

    void FixedUpdate()
    {
        if (!isSitting)
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

        if (rb != null) rb.velocity = Vector2.zero;
        if (animator != null) animator.SetFloat("Speed", 0f);

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrderDuduk;
        }

        // Cek apakah ini pertama kalinya duduk untuk memanggil Kaprodi
        if (!sudahPernahCutscene)
        {
            sudahPernahCutscene = true; // Set jadi true agar tidak terpanggil lagi ke depannya

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