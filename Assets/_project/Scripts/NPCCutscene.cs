using System.Collections;
using UnityEngine;
using TMPro;

public class NPCCutscene : MonoBehaviour
{
    public enum Pembicara { Kaprodi, Player }

    [System.Serializable]
    public struct DialogLine
    {
        public Pembicara pembicara;
        [TextArea(2, 4)]
        public string isiKalimat;
    }

    [Header("Pengaturan Pergerakan NPC")]
    public Transform targetPosition;    // Titik berdiri saat bicara
    public Transform exitPosition;      // Titik pintu keluar
    public float moveSpeed = 2f;

    [Header("Pengaturan Box Dialog Kaprodi")]
    public GameObject dialogBoxKaprodi;
    public TextMeshProUGUI isiTextKaprodi;

    [Header("Pengaturan Box Dialog Player")]
    public GameObject dialogBoxPlayer;
    public TextMeshProUGUI isiTextPlayer;

    [Header("Daftar Percakapan")]
    public DialogLine[] daftarDialog;

    private int indexDialog = 0;
    private bool isTalking = false;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        SembunyikanSemuaDialog();
        StartCoroutine(MulaiJalanMasuk());
    }

    IEnumerator MulaiJalanMasuk()
    {
        if (anim != null) anim.SetBool("isWalking", true);

        while (Vector2.Distance(transform.position, targetPosition.position) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPosition.position,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = targetPosition.position;
        if (anim != null) anim.SetBool("isWalking", false);

        MulaiBicara();
    }

    void MulaiBicara()
    {
        if (daftarDialog == null || daftarDialog.Length == 0) return;

        isTalking = true;
        indexDialog = 0;
        TampilkanKalimat();
    }

    void TampilkanKalimat()
    {
        SembunyikanSemuaDialog();
        DialogLine line = daftarDialog[indexDialog];

        if (line.pembicara == Pembicara.Kaprodi)
        {
            if (dialogBoxKaprodi != null) dialogBoxKaprodi.SetActive(true);
            if (isiTextKaprodi != null) isiTextKaprodi.text = line.isiKalimat;
        }
        else if (line.pembicara == Pembicara.Player)
        {
            if (dialogBoxPlayer != null) dialogBoxPlayer.SetActive(true);
            if (isiTextPlayer != null) isiTextPlayer.text = line.isiKalimat;
        }
    }

    void Update()
    {
        if (isTalking && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            LanjutKalimat();
        }
    }

    void LanjutKalimat()
    {
        if (indexDialog < daftarDialog.Length - 1)
        {
            indexDialog++;
            TampilkanKalimat();
        }
        else
        {
            // DIALOG HABIS: Tutup UI dan jalankan Kaprodi keluar
            SembunyikanSemuaDialog();
            isTalking = false;
            StartCoroutine(JalanKeluar());
        }
    }

    IEnumerator JalanKeluar()
    {
        if (anim != null) anim.SetBool("isWalking", true);

        // Kaprodi berjalan menuju exitPosition (pintu)
        if (exitPosition != null)
        {
            while (Vector2.Distance(transform.position, exitPosition.position) > 0.05f)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    exitPosition.position,
                    moveSpeed * Time.deltaTime
                );
                yield return null;
            }
        }

        // Sembunyikan Kaprodi dari Scene
        gameObject.SetActive(false);
    }

    void SembunyikanSemuaDialog()
    {
        if (dialogBoxKaprodi != null) dialogBoxKaprodi.SetActive(false);
        if (dialogBoxPlayer != null) dialogBoxPlayer.SetActive(false);
    }
}