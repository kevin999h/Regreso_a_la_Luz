using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles de Interfaz")]
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private GameObject continueButton;

    private void Start()
    {
        if (continueButton != null)
        {
            continueButton.SetActive(SaveSystem.Load().levelUnlocked > 1);
        }
    }

    public void PlayGame()
    {
        AudioManager.Play(Sfx.Click);
        SceneManager.LoadScene("Nivel1");
    }

    public void ContinueGame()
    {
        AudioManager.Play(Sfx.Click);
        int level = Mathf.Clamp(SaveSystem.Load().levelUnlocked, 1, 3);
        SceneManager.LoadScene("Nivel" + level);
    }

    public void OpenInstructions()
    {
        AudioManager.Play(Sfx.Click);
        if (instructionsPanel != null) instructionsPanel.SetActive(true);
    }

    public void CloseInstructions()
    {
        AudioManager.Play(Sfx.Click);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
    }

    public void OpenCredits()
    {
        AudioManager.Play(Sfx.Click);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        AudioManager.Play(Sfx.Click);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        AudioManager.Play(Sfx.Click);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
