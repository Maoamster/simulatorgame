using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    // Game mode settings
    public enum GameMode { SinglePlayer, MultiPlayer, Tutorial }
    private GameMode _currentGameMode;
    private string _selectedDeckName;
    private int _difficultyLevel = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void StartSinglePlayerGame(string deckName, int difficulty)
    {
        _currentGameMode = GameMode.SinglePlayer;
        _selectedDeckName = deckName;
        _difficultyLevel = difficulty;

        // Load game scene
        SceneManager.LoadScene("GameScene");
    }

    public void StartMultiPlayerGame(string deckName)
    {
        _currentGameMode = GameMode.MultiPlayer;
        _selectedDeckName = deckName;

        // Load multiplayer lobby scene
        SceneManager.LoadScene("LobbyScene");
    }

    public void StartTutorial()
    {
        _currentGameMode = GameMode.Tutorial;

        // Load tutorial scene
        SceneManager.LoadScene("TutorialScene");
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public GameMode GetCurrentGameMode()
    {
        return _currentGameMode;
    }

    public string GetSelectedDeckName()
    {
        return _selectedDeckName;
    }

    public int GetDifficultyLevel()
    {
        return _difficultyLevel;
    }
}