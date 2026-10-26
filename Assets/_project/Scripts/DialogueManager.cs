using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DialogueData
{
    public Sprite customBoxSprite;   // Sprite Box Dialog (gambar box yang sudah berisi teks, contoh Box Kaprodi / Box Pemain)
}

public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialogueBox;          // Panel Utama Dialog
    public Image dialogueBoxImage;          // Component Image tempat gambar box (gambar sudah berisi teks)

    [Header("Daftar Dialog (urut dari atas, satu sprite per baris)")]
    public DialogueData[] dialogueLines;    // Array data dialog

    [Header("Referensi NPC Cutscene")]
    public NPCCutscene kaprodiCutscene;     // Drag GameObject kapdiam ke sini

    private int currentLineIndex = 0;
    private bool isDialogueActive = false;

    void Start()
    {
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }
    }

    void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            NextLine();
        }
    }

    public void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;

        isDialogueActive = true;
        currentLineIndex = 0;

        if (dialogueBox != null)
        {
            dialogueBox.SetActive(true);
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (currentLineIndex >= dialogueLines.Length) return;

        DialogueData currentData = dialogueLines[currentLineIndex];

        // Ganti gambar box dialog sesuai baris. Kalau sprite baris ini kosong, gambar sebelumnya dipertahankan.
        if (dialogueBoxImage != null && currentData.customBoxSprite != null)
        {
            dialogueBoxImage.sprite = currentData.customBoxSprite;
        }
    }

    public void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < dialogueLines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;

        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (kaprodiCutscene != null)
        {
            kaprodiCutscene.MulaiJalanKeluar();
        }
    }
}