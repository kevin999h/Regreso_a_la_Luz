using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameHUDManager : MonoBehaviour
{
    public static GameHUDManager Instance { get; private set; }

    [Header("Textos y Paneles del HUD")]
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
    [SerializeField] private Image[] lifeSlots;
    [SerializeField] private GameObject missionCompletePanel;
    [SerializeField] private GameObject finalPanel;

    [Header("Configuración de Nivel")]
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

    [Header("Escenario e Iluminación")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private SpriteRenderer[] backgroundRenderers;
    [SerializeField] private SpriteRenderer generatorRenderer;
    [SerializeField] private Sprite generatorOnSprite;
    [SerializeField] private Color darkColor = new Color(0.02f, 0.03f, 0.07f, 1f);
    [SerializeField] private Color lightColor = new Color(0.55f, 0.82f, 0.98f, 1f);
    [SerializeField] private Color dimTint = new Color(0.4f, 0.45f, 0.6f, 1f);

    private int currentCollected = 0;
    private int currentKills = 0;
    private bool bossDefeated = false;
    private int currentLives = 3;
    private int maxLives = 3;
    private string currentWeaponDisplay = "Sin arma";
    private Coroutine toastCoroutine;

    public bool IsCombatLevel => combatLevel;

    // En niveles 2 y 3 basta con reunir los 5 chips/núcleos y derrotar al jefe final (o solo los 5 ítems si ya limpió el paso)
    public bool MissionComplete
    {
        get
        {
            if (currentCollected < totalRequired) return false;
            if (combatLevel && requireBoss && !bossDefeated) return false;
            return true;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        NPCDialogue.ResetState();
    }

    private void Start()
    {
        if (worldCamera != null) worldCamera.backgroundColor = darkColor;
        if (backgroundRenderers != null)
        {
            for (int i = 0; i < backgroundRenderers.Length; i++)
            {
                if (backgroundRenderers[i] != null) backgroundRenderers[i].color = dimTint;
            }
        }

        if (toastText != null) toastText.text = "";
        if (boostText != null) boostText.text = "";

        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (counterText != null)
        {
            counterText.text = collectibleLabel + ": " + currentCollected + " / " + totalRequired;
        }

        if (nutSlots != null)
        {
            for (int i = 0; i < nutSlots.Length; i++)
            {
                if (nutSlots[i] != null)
                {
                    nutSlots[i].color = i < currentCollected ? Color.white : new Color(1f, 1f, 1f, 0.2f);
                }
            }
        }

        if (lifeSlots != null)
        {
            for (int i = 0; i < lifeSlots.Length; i++)
            {
                if (lifeSlots[i] != null)
                {
                    lifeSlots[i].gameObject.SetActive(combatLevel);
                    lifeSlots[i].color = i < currentLives ? Color.white : new Color(1f, 1f, 1f, 0.2f);
                }
            }
        }

        if (objectiveText != null)
        {
            objectiveText.text = MissionComplete ? objectiveReady : objectiveStart;
        }

        if (combatLevel)
        {
            if (killsText != null)
            {
                killsText.text = "Vidas: " + currentLives + " / " + maxLives;
            }
            if (weaponText != null)
            {
                weaponText.text = "Arma: " + currentWeaponDisplay;
            }
        }
        else
        {
            if (killsText != null) killsText.text = "";
            if (weaponText != null) weaponText.text = "";
        }
    }

    public void AddCollectible(int amount, string itemName)
    {
        currentCollected = Mathf.Clamp(currentCollected + amount, 0, totalRequired);
        AudioManager.Play(Sfx.Collect);
        ShowToast("+1 " + itemName + " (" + currentCollected + "/" + totalRequired + ")");
        RefreshHUD();
        CheckMissionState();
    }

    public void RegisterEnemyKill()
    {
        currentKills++;
        ShowToast("¡Virus eliminado!");
        RefreshHUD();
    }

    public void RegisterBossDefeated(string bossName)
    {
        bossDefeated = true;
        ShowToast("¡Derrotaste a " + bossName + "!");
        RefreshHUD();
        CheckMissionState();
    }

    public void UpdateLives(int lives, int max)
    {
        currentLives = lives;
        maxLives = max;
        RefreshHUD();
    }

    public void SetWeaponText(string weaponInfo)
    {
        currentWeaponDisplay = weaponInfo;
        RefreshHUD();
    }

    public void SetBoostRemaining(float seconds)
    {
        if (boostText == null) return;
        boostText.text = seconds > 0.05f ? "TURBO: " + seconds.ToString("0.0") + "s" : "";
    }

    private void CheckMissionState()
    {
        if (MissionComplete)
        {
            AudioManager.Play(Sfx.Powerup);
            if (missionCompletePanel != null)
            {
                if (missionText != null && !string.IsNullOrEmpty(missionMessage))
                {
                    missionText.text = missionMessage;
                }
                missionCompletePanel.SetActive(true);
                StartCoroutine(HideMissionPanelAfterDelay(4.5f));
            }
        }
    }

    private IEnumerator HideMissionPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (missionCompletePanel != null) missionCompletePanel.SetActive(false);
    }

    public void ShowToast(string message)
    {
        if (toastText == null) return;
        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;
        yield return new WaitForSeconds(2.6f);
        toastText.text = "";
    }

    public void FinishLevel()
    {
        AudioManager.Play(Sfx.Complete);
        if (missionCompletePanel != null) missionCompletePanel.SetActive(false);
        if (objectiveText != null) objectiveText.text = objectiveDone;

        if (generatorRenderer != null && generatorOnSprite != null)
        {
            generatorRenderer.sprite = generatorOnSprite;
        }

        SaveSystem.UnlockLevel(levelIndex + 1);
        SaveSystem.AddScore(100 * levelIndex);

        StartCoroutine(RestoreLightAndShowFinal());
    }

    private IEnumerator RestoreLightAndShowFinal()
    {
        float duration = 1.2f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            if (worldCamera != null)
            {
                worldCamera.backgroundColor = Color.Lerp(darkColor, lightColor, k);
            }
            if (backgroundRenderers != null)
            {
                for (int i = 0; i < backgroundRenderers.Length; i++)
                {
                    if (backgroundRenderers[i] != null)
                    {
                        backgroundRenderers[i].color = Color.Lerp(dimTint, Color.white, k);
                    }
                }
            }
            yield return null;
        }

        if (finalPanel != null)
        {
            finalPanel.SetActive(true);
        }
    }

    public void NextLevel()
    {
        AudioManager.Play(Sfx.Click);
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void GoToMenu()
    {
        AudioManager.Play(Sfx.Click);
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartGame()
    {
        AudioManager.Play(Sfx.Click);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
