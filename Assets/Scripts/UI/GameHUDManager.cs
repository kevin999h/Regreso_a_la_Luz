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
    [SerializeField] private Text levelNameText;
    [SerializeField] private Text finalBodyText;
    [SerializeField] private Text killsText;
    [SerializeField] private Text weaponText;
    [SerializeField] private Text missionText;
    [SerializeField] private Image[] nutSlots;
    [SerializeField] private GameObject missionCompletePanel;
    [SerializeField] private GameObject finalPanel;

    [Header("Meta y progresión")]
    [SerializeField] private int totalRequired = 5;
    [SerializeField] private int levelIndex = 1;
    [SerializeField] private string nextSceneName = "Nivel2";
    [SerializeField] private string objectiveStart = "Misión: recolecta las 5 tuercas del generador";
    [SerializeField] private string objectiveReady = "Misión: lleva el avance al Técnico Amp, junto al generador";
    [SerializeField] private string objectiveDone = "¡Luz restaurada! Nivel completado";
    [SerializeField] private string collectibleLabel = "Tuercas";
    [SerializeField] private string missionMessage = "";
    [SerializeField] private bool combatLevel = false;
    [SerializeField] private bool requireBoss = false;
    [SerializeField] private int totalEnemies = 0;

    [Header("Ambiente: regreso a la luz")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private SpriteRenderer[] backgroundRenderers;
    [SerializeField] private SpriteRenderer generatorRenderer;
    [SerializeField] private Sprite generatorOnSprite;
    [SerializeField] private Color darkColor = new Color(0.02f, 0.03f, 0.07f, 1f);
    [SerializeField] private Color lightColor = new Color(0.55f, 0.82f, 0.98f, 1f);
    [SerializeField] private Color dimTint = new Color(0.4f, 0.45f, 0.6f, 1f);

    private int currentCollected = 0;
    private int kills = 0;
    private bool bossDefeated = false;
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
        if (killsText != null) killsText.text = "";
        if (weaponText != null) weaponText.text = "";
        if (combatLevel) SetWeapon("Sin arma");
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
        AudioManager.Play(Sfx.Collect);

        CheckMission();
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
            counterText.text = collectibleLabel + ": " + currentCollected + " / " + totalRequired;
        }

        if (objectiveText != null && !LevelFinished)
        {
            objectiveText.text = MissionComplete ? objectiveReady : objectiveStart;
        }

        if (combatLevel && killsText != null)
        {
            killsText.text = "Virus: " + kills + " / " + totalEnemies;
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
        if (missionText != null && !string.IsNullOrEmpty(missionMessage)) missionText.text = missionMessage;
        UpdateHUDVisuals();
        AudioManager.Play(Sfx.Powerup);
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

    public void AddKill()
    {
        kills++;
        UpdateHUDVisuals();
        ShowToast("¡Virus eliminado!");
        AudioManager.Play(Sfx.Collect);
    }

    public void SetWeapon(string weaponName)
    {
        if (weaponText != null) weaponText.text = "Arma: " + weaponName;
    }

    public void OnBossDefeated()
    {
        bossDefeated = true;
        ShowToast("¡Gran Cortocircuito derrotado!");
        AudioManager.Play(Sfx.Complete);
        CheckMission();
    }

    private void CheckMission()
    {
        if (MissionComplete) return;

        bool bossOk = !requireBoss || bossDefeated;
        if (currentCollected >= totalRequired && bossOk)
        {
            OnMissionCompleted();
        }
    }

    public void FinishLevel()
    {
        if (LevelFinished) return;
        LevelFinished = true;
        AudioManager.Play(Sfx.Complete);

        SaveSystem.UnlockLevel(Mathf.Min(levelIndex + 1, 2));
        SaveSystem.AddScore(currentCollected);

        if (objectiveText != null) objectiveText.text = objectiveDone;
        if (finalBodyText != null) finalBodyText.text += "\n\n" + collectibleLabel + " recogidos: " + currentCollected + " / " + totalRequired;
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

    public void NextLevel()
    {
        AudioManager.Play(Sfx.Click);
        NPCDialogue.ResetState();
        SceneManager.LoadScene(nextSceneName);
    }

    public void RestartGame()
    {
        NPCDialogue.ResetState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        AudioManager.Play(Sfx.Click);
        NPCDialogue.ResetState();
        SceneManager.LoadScene("MainMenu");
    }
}
