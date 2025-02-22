using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    public string gameSceneName = "KindaMADscene";  // Name of your main game scene

    [Header("Controls Panel (Optional)")]
    public GameObject controlsPanel; // Drag your "ControlsPanel" here in the Inspector

    private void Awake()
    {
        controlsPanel.SetActive(false);
    }
    // Called by the "Start" button
    public void StartGame()
    {
        // Load the game scene
        SceneManager.LoadScene(gameSceneName);
    }

    // Called by the "Quit" button
    public void QuitGame()
    {
        // Quits the application (won't work in Editor)
        Application.Quit();
        Debug.Log("Game is quitting..."); // For testing in the Editor
    }

    // Called by the "Controls" button
    public void ShowControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
        }
    }

    // Called by a "Back" button on the controls panel, if you have one
    public void HideControls()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
    }
}
