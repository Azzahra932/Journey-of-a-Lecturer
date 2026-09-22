using UnityEngine;

public class NPCCutscene : MonoBehaviour
{
    [Header("Pengaturan Pergerakan")]
    public Transform targetPoint; // Titik Kaprodi datang (TargetKaprodi)
    public Transform exitPoint;   // Titik Kaprodi keluar (ExitKaprodi)
    public float moveSpeed = 2f;

    [Header("Pengaturan Dialog Manager")]
    public DialogueManager dialogueManager;

    [Header("Pengaturan Animator")]
    public string isWalkingParam = "isWalking";

    private Transform currentTarget;
    private bool isWalking = false;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isWalking && currentTarget != null)
        {
            // Gerakkan Kaprodi menuju titik sasaran
            transform.position = Vector3.MoveTowards(transform.position, currentTarget.position, moveSpeed * Time.deltaTime);

            // Jika sudah sampai di titik tujuan
            if (Vector3.Distance(transform.position, currentTarget.position) < 0.05f)
            {
                isWalking = false;

                if (animator != null)
                {
                    animator.SetBool(isWalkingParam, false);
                }

                // Jika titik sasarannya adalah targetPoint (berarti baru sampai di meja)
                if (currentTarget == targetPoint)
                {
                    if (dialogueManager != null)
                    {
                        dialogueManager.StartDialogue();
                    }
                }
                // Jika titik sasarannya adalah exitPoint (berarti sudah keluar)
                else if (currentTarget == exitPoint)
                {
                    Debug.Log("Kaprodi telah keluar ruangan.");
                    gameObject.SetActive(false); // Sembunyikan Kaprodi
                }
            }
        }
    }

    public void MulaiJalanMasuk()
    {
        gameObject.SetActive(true);
        currentTarget = targetPoint;
        isWalking = true;

        if (animator != null)
        {
            animator.SetBool(isWalkingParam, true);
        }
    }

    // Fungsi ini dipanggil dari DialogueManager setelah dialog terakhir selesai
    public void MulaiJalanKeluar()
    {
        if (exitPoint != null)
        {
            currentTarget = exitPoint;
            isWalking = true;

            if (animator != null)
            {
                animator.SetBool(isWalkingParam, true);
            }

            Debug.Log("Kaprodi mulai berjalan keluar...");
        }
    }
}