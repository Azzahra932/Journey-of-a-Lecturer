using UnityEngine;

public class NPCCutscene : MonoBehaviour
{
    [Header("Jalur Masuk (Waypoints)")]
    public Transform[] waypointsMasuk; // Point1 -> Point2 -> Point3

    [Header("Jalur Keluar (Waypoints)")]
    public Transform[] waypointsKeluar; // Point2 -> Point1 -> ExitKaprodi

    [Header("Pengaturan Pergerakan")]
    public float moveSpeed = 2f;

    [Header("Pengaturan Posisi & Sprite Duduk Kaprodi")]
    public Transform sitPointKaprodi;
    public float sittingOffsetY = -0.3f;
    public int sittingSortingOrder = 1;
    public int defaultSortingOrder = 2;

    [Header("Pengaturan Dialog Manager")]
    public DialogueManager dialogueManager;

    [Header("Pengaturan Animator NPC")]
    public string isWalkingParam = "isWalking";

    private int currentWaypointIndex = 0;
    private bool isWalking = false;
    private bool isExiting = false;

    private Animator npcAnimator;
    private SpriteRenderer npcSpriteRenderer;

    void Awake()
    {
        // Menggunakan komponen khusus milik GameObject NPC ini sahaja
        npcAnimator = GetComponent<Animator>();
        npcSpriteRenderer = GetComponent<SpriteRenderer>();

        // Sembunyikan Kaprodi saat game baru mulai
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isWalking) return;

        Transform[] currentPath = isExiting ? waypointsKeluar : waypointsMasuk;

        if (currentPath != null && currentPath.Length > 0 && currentWaypointIndex < currentPath.Length)
        {
            Transform target = currentPath[currentWaypointIndex];

            // Gerakkan NPC menuju waypoint target
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

            // Jika sudah sangat dekat dengan titik waypoint
            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                currentWaypointIndex++;

                // Jika sudah mencapai titik waypoint terakhir
                if (currentWaypointIndex >= currentPath.Length)
                {
                    isWalking = false;

                    if (npcAnimator != null)
                    {
                        npcAnimator.SetBool(isWalkingParam, false);
                    }

                    if (!isExiting)
                    {
                        // 1. Kaprodi Duduk DULU
                        Duduk();

                        // 2. BARU panggil & munculkan dialog setelah Kaprodi duduk
                        if (dialogueManager != null)
                        {
                            dialogueManager.StartDialogue();
                        }
                    }
                    else
                    {
                        // Selesai jalan keluar -> sembunyikan GameObject Kaprodi
                        gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public void MulaiJalanMasuk()
    {
        // Pindahkan posisi Kaprodi ke Point1 terlebih dahulu
        if (waypointsMasuk != null && waypointsMasuk.Length > 0 && waypointsMasuk[0] != null)
        {
            transform.position = waypointsMasuk[0].position;
        }

        gameObject.SetActive(true);
        isExiting = false;
        currentWaypointIndex = 0;
        isWalking = true;

        if (npcSpriteRenderer != null)
        {
            npcSpriteRenderer.sortingOrder = defaultSortingOrder;
        }

        if (npcAnimator != null)
        {
            npcAnimator.enabled = true;
            npcAnimator.SetBool(isWalkingParam, true);
        }
    }

    private void Duduk()
    {
        if (sitPointKaprodi != null)
        {
            Vector3 targetPos = sitPointKaprodi.position;
            targetPos.y += sittingOffsetY;
            transform.position = targetPos;
        }

        if (npcAnimator != null)
        {
            npcAnimator.SetBool(isWalkingParam, false);
        }

        if (npcSpriteRenderer != null)
        {
            npcSpriteRenderer.sortingOrder = sittingSortingOrder;
        }
    }

    public void MulaiJalanKeluar()
    {
        if (npcSpriteRenderer != null)
        {
            npcSpriteRenderer.sortingOrder = defaultSortingOrder;
        }

        if (npcAnimator != null)
        {
            npcAnimator.enabled = true;
            npcAnimator.SetBool(isWalkingParam, true);
        }

        isExiting = true;
        currentWaypointIndex = 0;
        isWalking = true;
    }
}