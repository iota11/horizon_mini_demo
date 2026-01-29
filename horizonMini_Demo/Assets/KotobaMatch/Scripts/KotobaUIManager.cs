using UnityEngine;
using TMPro;

/// <summary>
/// Manages UI for KotobaMatch - simpler approach like CubeStack
/// </summary>
public class KotobaUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    private void Awake()
    {
        // Auto-find references if not set
        if (timerText == null)
        {
            var obj = transform.Find("Canvas/TimerText");
            if (obj != null) timerText = obj.GetComponent<TextMeshProUGUI>();
        }
        if (scoreText == null)
        {
            var obj = transform.Find("Canvas/ScoreText");
            if (obj != null) scoreText = obj.GetComponent<TextMeshProUGUI>();
        }
        if (menuPanel == null)
        {
            var obj = transform.Find("Canvas/MenuPanel");
            if (obj != null) menuPanel = obj.gameObject;
        }
        if (gameOverPanel == null)
        {
            var obj = transform.Find("Canvas/GameOverPanel");
            if (obj != null) gameOverPanel = obj.gameObject;
        }
        if (finalScoreText == null && gameOverPanel != null)
        {
            var obj = gameOverPanel.transform.Find("ScoreText");
            if (obj != null) finalScoreText = obj.GetComponent<TextMeshProUGUI>();
        }
    }

    public void ShowMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
    }

    public void HideAll()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        if (timerText != null) timerText.gameObject.SetActive(true);
    }

    public void UpdateTimer(int seconds)
    {
        if (timerText != null)
        {
            timerText.text = "Time: " + seconds.ToString() + "s";
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }

    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);

        if (finalScoreText != null)
        {
            finalScoreText.text = "You matched " + finalScore.ToString() + " pairs!";
        }
    }
}
