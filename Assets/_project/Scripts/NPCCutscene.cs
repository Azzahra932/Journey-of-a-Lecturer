using UnityEngine;

public class NPCCutscene : MonoBehaviour
{
    [Header("Jalur Masuk (Waypoints)")]
    public Transform[] waypointsMasuk;

    [Header("Jalur Keluar (Waypoints)")]
    public Transform[] waypointsKeluar;

    public float moveSpeed = 2f;

    [Header("Pengaturan Posisi & Sprite Duduk Kaprodi")]
    public Transform sitPointKaprodi;   // Drag SitPointKaprodi ke sini
    public Sprite kaprodiSitSprite;     // Sprite Kaprodi tampak belakang / duduk
    public int sittingSortingOrder = 1; // Layer saat duduk (di belakang meja)
    public int defaultSortingOrder = 3; // Layer saat jalan/berdiri

    [Header("Pengaturan Dialog Manager")]
    public DialogueManager dialogueManager;

    [Header("Pengaturan Animator")]
    public string isWalkingParam = "isWalking";

    private int currentWaypointIndex = 0;
    private bool isWalking = false;
    private bool isExiting = false;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!isWalking) return;

        Transform[] currentPath = isExiting ? waypointsKeluar : waypointsMasuk;

        if (currentPath != null && currentPath.Length > 0 && currentWaypointIndex < currentPath.Length)
        {
            Transform target = currentPath[currentWaypointIndex];

            // 1. Jalan menuju titik waypoint aktif
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

            // 2. Jika sudah sampai di titik waypoint aktif
            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                currentWaypointIndex++;

                // Jika sudah sampai di titik paling ujung/terakhir
                if (currentWaypointIndex >= currentPath.Length)
                {
                    isWalking = false;

                    if (animator != null)
                    {
                        animator.SetBool(isWalkingParam, false);
                    }

                    if (!isExiting)
                    {
                        // Selesai jalan masuk -> Duduk di kursi & mulai dialog
                        Duduk();

                        if (dialogueManager != null)
                        {
                            dialogueManager.StartDialogue();
                        }
                    }
                    else
                    {
                        // Selesai jalan keluar -> Sembunyikan objek
                        gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public void MulaiJalanMasuk()
    {
        gameObject.SetActive(true);
        isExiting = false;
        currentWaypointIndex = 0;
        isWalking = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = defaultSortingOrder;
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool(isWalkingParam, true);
        }
    }

    private void Duduk()
    {
        // Pindahkan posisi pas ke titik kursi
        if (sitPointKaprodi != null)
        {
            transform.position = sitPointKaprodi.position;
        }

        // Matikan animasi jalan
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Ganti ke sprite duduk & ubah order layer ke belakang meja
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
            if (kaprodiSitSprite != null)
            {
                spriteRenderer.sprite = kaprodiSitSprite;
            }
            spriteRenderer.sortingOrder = sittingSortingOrder;
        }
    }

    public void MulaiJalanKeluar()
    {
        // Balikkan sprite & order layer ke mode berdiri/jalan
        if (spriteRenderer != null)
        {
            if (originalSprite != null)
            {
                spriteRenderer.sprite = originalSprite;
            }
            spriteRenderer.sortingOrder = defaultSortingOrder;
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool(isWalkingParam, true);
        }

        isExiting = true;
        currentWaypointIndex = 0;
        isWalking = true;
    }
}