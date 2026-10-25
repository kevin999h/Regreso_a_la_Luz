using UnityEngine;
using UnityEngine.UI;

public class InteractableObject : MonoBehaviour
{
    [Header("Mensaje Flotante")]
    [SerializeField] private GameObject promptFloatingText;

    [Header("Contenido Educativo")]
    [SerializeField] private GameObject infoDisplayPanel;
    [SerializeField] private Text infoTextComponent;
    [TextArea(3, 8)]
    [SerializeField] private string educationalContent = "Dato educativo.";

    private bool isPlayerNearby = false;
    private bool isPanelOpen = false;

    private void Start()
    {
        if (promptFloatingText != null) promptFloatingText.SetActive(false);
        if (infoDisplayPanel != null) infoDisplayPanel.SetActive(false);
    }

    private void Update()
    {
        if (isPlayerNearby && !NPCDialogue.IsDialogueActive && Input.GetKeyDown(KeyCode.E))
        {
            ToggleInteraction();
        }
    }

    private void ToggleInteraction()
    {
        isPanelOpen = !isPanelOpen;
        if (infoDisplayPanel != null) infoDisplayPanel.SetActive(isPanelOpen);
        if (infoTextComponent != null) infoTextComponent.text = educationalContent;
        if (promptFloatingText != null) promptFloatingText.SetActive(!isPanelOpen);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (promptFloatingText != null && !isPanelOpen)
            {
                promptFloatingText.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            isPanelOpen = false;
            if (promptFloatingText != null) promptFloatingText.SetActive(false);
            if (infoDisplayPanel != null) infoDisplayPanel.SetActive(false);
        }
    }
}
