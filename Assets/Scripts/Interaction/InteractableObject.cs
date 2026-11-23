using UnityEngine;
using UnityEngine.UI;

public class InteractableObject : MonoBehaviour
{
    public static bool IsEducationalPanelActive { get; private set; }

    [Header("Mensaje Flotante")]
    [SerializeField] private GameObject promptFloatingText;

    [Header("Contenido Educativo y Diagrama")]
    [SerializeField] private GameObject infoDisplayPanel;
    [SerializeField] private Text infoTextComponent;
    [SerializeField] private Image diagramImageComponent;
    [SerializeField] private Sprite diagramSprite;
    [TextArea(3, 8)]
    [SerializeField] private string educationalContent = "Dato educativo.";

    [Header("Mini-Reto Interactivo (Teclas 1, 2, 3)")]
    [SerializeField] private Text quizTextComponent;
    [SerializeField] private Text quizFeedbackComponent;
    [SerializeField] private string quizQuestion = "";
    [SerializeField] private string[] quizOptions = new string[3];
    [SerializeField] private int correctOptionIndex = 0;
    [SerializeField] private string correctFeedback = "¡Correcto! +50 puntos.";

    private bool isPlayerNearby = false;
    private bool isPanelOpen = false;
    private bool quizSolved = false;

    public static void ResetState()
    {
        IsEducationalPanelActive = false;
    }

    private void Start()
    {
        if (promptFloatingText != null) promptFloatingText.SetActive(false);
        if (infoDisplayPanel != null) infoDisplayPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isPlayerNearby) return;
        if (GameHUDManager.Instance != null && GameHUDManager.Instance.IsGameOver) return;

        if (!NPCDialogue.IsDialogueActive && Input.GetKeyDown(KeyCode.E))
        {
            ToggleInteraction();
            return;
        }

        if (isPanelOpen && !quizSolved && !string.IsNullOrEmpty(quizQuestion))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SubmitAnswer(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SubmitAnswer(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SubmitAnswer(2);
        }
    }

    private void ToggleInteraction()
    {
        isPanelOpen = !isPanelOpen;
        IsEducationalPanelActive = isPanelOpen;
        AudioManager.Play(Sfx.Click);

        if (infoDisplayPanel != null) infoDisplayPanel.SetActive(isPanelOpen);
        if (promptFloatingText != null) promptFloatingText.SetActive(!isPanelOpen);

        if (isPanelOpen)
        {
            if (infoTextComponent != null) infoTextComponent.text = educationalContent;
            if (diagramImageComponent != null)
            {
                diagramImageComponent.sprite = diagramSprite;
                diagramImageComponent.gameObject.SetActive(diagramSprite != null);
            }
            RefreshQuizUI();
        }
    }

    private void RefreshQuizUI()
    {
        if (quizTextComponent == null) return;

        if (string.IsNullOrEmpty(quizQuestion))
        {
            quizTextComponent.text = "";
            if (quizFeedbackComponent != null) quizFeedbackComponent.text = "";
            return;
        }

        string opts = "";
        for (int i = 0; i < quizOptions.Length; i++)
        {
            if (!string.IsNullOrEmpty(quizOptions[i]))
                opts += "\n[" + (i + 1) + "] " + quizOptions[i];
        }
        quizTextComponent.text = "RETO RÁPIDO (presiona 1, 2 o 3): " + quizQuestion + opts;

        if (quizFeedbackComponent != null)
        {
            quizFeedbackComponent.text = quizSolved
                ? "✔ " + correctFeedback
                : "Responde con las teclas 1, 2 o 3 para ganar +50 pts (y +1 vida si te falta).";
            quizFeedbackComponent.color = quizSolved
                ? new Color(0.35f, 1f, 0.55f, 1f)
                : new Color(1f, 0.88f, 0.4f, 1f);
        }
    }

    private void SubmitAnswer(int index)
    {
        if (index == correctOptionIndex)
        {
            quizSolved = true;
            AudioManager.Play(Sfx.Complete);
            SaveSystem.AddScore(50);

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                PlayerController pc = playerObj.GetComponent<PlayerController>();
                if (pc != null) pc.HealOneLife();
            }

            if (quizFeedbackComponent != null)
            {
                quizFeedbackComponent.text = "✔ " + correctFeedback + " (+50 pts y +1 vida si te faltaba)";
                quizFeedbackComponent.color = new Color(0.35f, 1f, 0.55f, 1f);
            }
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.ShowToast("¡Reto eléctrico superado! +50 pts");
            }
        }
        else
        {
            AudioManager.Play(Sfx.Hurt);
            if (quizFeedbackComponent != null)
            {
                quizFeedbackComponent.text = "✖ Respuesta incorrecta. Observa el diagrama e intenta de nuevo.";
                quizFeedbackComponent.color = new Color(1f, 0.45f, 0.45f, 1f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (promptFloatingText != null && !isPanelOpen) promptFloatingText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            isPanelOpen = false;
            IsEducationalPanelActive = false;
            if (promptFloatingText != null) promptFloatingText.SetActive(false);
            if (infoDisplayPanel != null) infoDisplayPanel.SetActive(false);
        }
    }
}
