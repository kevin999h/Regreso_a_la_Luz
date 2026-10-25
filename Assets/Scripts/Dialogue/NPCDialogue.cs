using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct DialogueLine
{
    public string speakerName;
    public Color nameColor;
    [TextArea(2, 5)] public string text;
}

public class NPCDialogue : MonoBehaviour
{
    public static bool IsDialogueActive { get; private set; }

    [Header("Conversación")]
    [SerializeField] private DialogueLine[] conversation;
    [SerializeField] private DialogueLine[] completedConversation;
    [SerializeField] private bool finishesLevel = false;

    [Header("Referencias de Interfaz (UI)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text nameText;
    [SerializeField] private Text contentText;
    [SerializeField] private GameObject interactionPrompt;

    private bool isPlayerInRange = false;
    private bool isThisDialogueActive = false;
    private int currentLineIndex = 0;
    private DialogueLine[] activeLines;

    public static void ResetState()
    {
        IsDialogueActive = false;
    }

    private void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!isPlayerInRange) return;

        bool pressed = Input.GetKeyDown(KeyCode.E) || (isThisDialogueActive && Input.GetKeyDown(KeyCode.Space));
        if (!pressed) return;

        if (!isThisDialogueActive)
        {
            if (!IsDialogueActive)
            {
                StartDialogue();
            }
        }
        else
        {
            NextDialogueLine();
        }
    }

    private void StartDialogue()
    {
        bool missionDone = GameHUDManager.Instance != null && GameHUDManager.Instance.MissionComplete;
        activeLines = (missionDone && completedConversation != null && completedConversation.Length > 0) ? completedConversation : conversation;
        if (activeLines == null || activeLines.Length == 0) return;

        isThisDialogueActive = true;
        IsDialogueActive = true;
        currentLineIndex = 0;
        dialoguePanel.SetActive(true);
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        DisplayCurrentLine();
    }

    private void NextDialogueLine()
    {
        currentLineIndex++;
        if (currentLineIndex < activeLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            bool wasComplete = GameHUDManager.Instance != null && GameHUDManager.Instance.MissionComplete;
            EndDialogue();
            if (finishesLevel && wasComplete && GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.FinishLevel();
            }
        }
    }

    private void DisplayCurrentLine()
    {
        DialogueLine line = activeLines[currentLineIndex];
        nameText.text = line.speakerName;
        if (line.nameColor.a > 0f)
        {
            nameText.color = line.nameColor;
        }
        contentText.text = line.text;
    }

    private void EndDialogue()
    {
        isThisDialogueActive = false;
        IsDialogueActive = false;
        dialoguePanel.SetActive(false);
        if (interactionPrompt != null && isPlayerInRange) interactionPrompt.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (!isThisDialogueActive && interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (isThisDialogueActive)
            {
                EndDialogue();
            }
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
        }
    }
}
