using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI Dialog Boxes")]
    public GameObject dialogBoxKaprodi;
    public GameObject boxPlayer;

    [Header("Komponen Teks")]
    public TextMeshProUGUI textKaprodi;

    [Header("Daftar Teks Kaprodi")]
    [TextArea(2, 4)] public string kaprodiTeks1;
    [TextArea(2, 4)] public string kaprodiTeks3;
    [TextArea(2, 4)] public string kaprodiTeks4;

    [Header("Referensi NPC Cutscene")]
    public NPCCutscene kaprodiCutscene; // Drag objek kapdiam ke sini

    private int step = 0;
    private bool isDialogueActive = false;

    void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            AdvanceDialogue();
        }
    }

    public void StartDialogue()
    {
        isDialogueActive = true;
        step = 1;
        ShowCurrentStep();
    }

    void AdvanceDialogue()
    {
        step++;
        ShowCurrentStep();
    }

    void ShowCurrentStep()
    {
        switch (step)
        {
            case 1:
                if (dialogBoxKaprodi != null) dialogBoxKaprodi.SetActive(true);
                if (boxPlayer != null) boxPlayer.SetActive(false);
                if (textKaprodi != null) textKaprodi.text = kaprodiTeks1;
                break;

            case 2:
                if (dialogBoxKaprodi != null) dialogBoxKaprodi.SetActive(false);
                if (boxPlayer != null) boxPlayer.SetActive(true);
                break;

            case 3:
                if (dialogBoxKaprodi != null) dialogBoxKaprodi.SetActive(true);
                if (boxPlayer != null) boxPlayer.SetActive(false);
                if (textKaprodi != null) textKaprodi.text = kaprodiTeks3;
                break;

            case 4:
                if (dialogBoxKaprodi != null) dialogBoxKaprodi.SetActive(true);
                if (boxPlayer != null) boxPlayer.SetActive(false);
                if (textKaprodi != null) textKaprodi.text = kaprodiTeks4;
                break;

            default:
                // Dialog Selesai -> Sembunyikan UI Dialog
                if (dialogBoxKaprodi != null) dialogBoxKaprodi.SetActive(false);
                if (boxPlayer != null) boxPlayer.SetActive(false);
                isDialogueActive = false;

                // Perintahkan Kaprodi untuk berjalan keluar!
                if (kaprodiCutscene != null)
                {
                    kaprodiCutscene.MulaiJalanKeluar();
                }

                Debug.Log("Percakapan selesai, Kaprodi pergi.");
                break;
        }
    }
}