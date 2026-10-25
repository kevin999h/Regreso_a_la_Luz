using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameHUDManager : MonoBehaviour
{
    public static GameHUDManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private Text counterText;
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text toastText;
    [SerializeField] private Text boostText;
    [SerializeField] private Image[] nutSlots;
    [SerializeField] private GameObject missionCompletePanel;
    [SerializeField] private GameObject finalPanel;

    [Header("Meta")]
    [SerializeField] private int totalRequired = 5;
    [SerializeField] private string objectiveStart = "Misión: recolecta las 5 tuercas del generador";
    [SerializeField] private string objectiveReady = "Misión: lleva las tuercas al Técnico Amp, junto al generador";
    [SerializeField] private string objectiveDone = "¡Luz restaurada! Nivel 1 completado";

    [Header("Ambiente: regreso a la luz")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private SpriteRenderer[] backgroundRenderers;
    [SerializeField] private SpriteRenderer generatorRenderer;
    [SerializeField] private Sprite generatorOnSprite;
    [SerializeField] private Color darkColor = new Color(0.02f, 0.03f, 0.07f, 1f);
    [SerializeField] private Color lightColor = new Color(0.55f, 0.82f, 0.98f, 1f);
    [SerializeField] private Color dimTint = new Color(0.4f, 0.45f, 0.6f, 1f);

    private int currentCollected = 0;
    private float toastTimer = 0f;
    private PlayerController player;

    public bool MissionComplete { get; private set; }
    public bool LevelFinished { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        NPCDialogue.ResetState();
    }

    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
        if (missionCompletePanel != null) missionCompletePanel.SetActive(false);
        if (finalPanel != null) finalPanel.SetActive(false);
        if (toastText != null) toastText.text = "";
        if (boostText != null) boostText.text = "";
        ApplyAmbient(0f);
        UpdateHUDVisuals();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f && toastText != null) toastText.text = "";
        }

        if (boostText != null && player != null)
        {
            float remaining = player.BoostRemaining;
            boostText.text = remaining > 0f ? "TURBO  " + remaining.ToString("0.0") + " s" : "";
        }
    }

    public void AddCollectible(int amount, string itemName)
    {
        currentCollected += amount;
        UpdateHUDVisuals();
        ShowToast("+" + amount + " " + itemName);

        if (currentCollected >= totalRequired && !MissionComplete)
        {
            OnMissionCompleted();
        }
    }

    public void ShowToast(string message)
    {
        if (toastText == null) return;
        toastText.text = message;
        toastTimer = 2.5f;
    }

    private void UpdateHUDVisuals()
    {
        if (counterText != null)
        {
            counterText.text = "Tuercas: " + currentCollected + " / " + totalRequired;
        }

        if (objectiveText != null && !LevelFinished)
        {
            objectiveText.text = MissionComplete ? objectiveReady : objectiveStart;
        }

        if (nutSlots != null)
        {
            for (int i = 0; i < nutSlots.Length; i++)
            {
                if (nutSlots[i] == null) continue;
                Color c = nutSlots[i].color;
                c.a = i < currentCollected ? 1f : 0.2f;
                nutSlots[i].color = c;
            }
        }
    }

    private void OnMissionCompleted()
    {
        MissionComplete = true;
        UpdateHUDVisuals();
        if (missionCompletePanel != null)
        {
            missionCompletePanel.SetActive(true);
            Invoke(nameof(HideMissionPanel), 6f);
        }
    }

    private void HideMissionPanel()
    {
        if (missionCompletePanel != null) missionCompletePanel.SetActive(false);
    }

    public void FinishLevel()
    {
        if (LevelFinished) return;
        LevelFinished = true;
        if (objectiveText != null) objectiveText.text = objectiveDone;
        if (generatorRenderer != null && generatorOnSprite != null) generatorRenderer.sprite = generatorOnSprite;
        StartCoroutine(LightsOn());
    }

    private IEnumerator LightsOn()
    {
        float duration = 2.5f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            ApplyAmbient(Mathf.Clamp01(t / duration));
            yield return null;
        }
        ApplyAmbient(1f);
        if (finalPanel != null) finalPanel.SetActive(true);
    }

    private void ApplyAmbient(float k)
    {
        if (worldCamera != null)
        {
            worldCamera.backgroundColor = Color.Lerp(darkColor, lightColor, k);
        }

        if (backgroundRenderers != null)
        {
            Color tint = Color.Lerp(dimTint, Color.white, k);
            for (int i = 0; i < backgroundRenderers.Length; i++)
            {
                if (backgroundRenderers[i] != null) backgroundRenderers[i].color = tint;
            }
        }
    }

    public void RestartGame()
    {
        NPCDialogue.ResetState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        NPCDialogue.ResetState();
        SceneManager.LoadScene("MainMenu");
    }
}
