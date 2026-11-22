using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public string menuScene;
    public GameObject howToPlayPanel;

    [Header("UI")]
    public GameObject pauseMenu;

    [Header("Gameplay Scripts To Disable")]
    public MonoBehaviour[] gameplayScripts;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        pauseMenu.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        IsPaused = true;

        Time.timeScale = 0f;

        pauseMenu.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        foreach (MonoBehaviour script in gameplayScripts)
        {
            if (script != null)
                script.enabled = false;
        }
    }

    public void ResumeGame()
    {
        IsPaused = false;

        Time.timeScale = 1f;

        pauseMenu.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        foreach (MonoBehaviour script in gameplayScripts)
        {
            if (script != null)
                script.enabled = true;
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(menuScene);
    }

    public void OpenHowToPlay()
    {
        howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        howToPlayPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}