using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class DialogueData
{
    [TextArea(2, 5)]
    public string sentence;         // Teks kalimat dialog
    public Sprite customBoxSprite;   // Sprite Box Dialog (Box Kaprodi / Box Pemain)
}

public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialogueBox;          // Panel Utama Dialog
    public Image dialogueBoxImage;         // Component Image tempat gambar background box
    public TextMeshProUGUI dialogueText;     // Component Teks (IsiDialog)

    [Header("Daftar Dialog")]
    public DialogueData[] dialogueLines;   // Array data dialog

    [Header("Referensi NPC Cutscene")]
    public NPCCutscene kaprodiCutscene;    // Drag GameObject kapdiam ke sini

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

        // 1. Set Isi Teks
        if (dialogueText != null)
        {
            dialogueText.text = currentData.sentence;
        }

        // 2. Ganti Sprite Box Dialog jika dimasukkan sprite khusus
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