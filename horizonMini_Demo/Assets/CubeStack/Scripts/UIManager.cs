using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI perfectText;

    [Header("Settings")]
    [SerializeField] private float perfectTextDuration = 0.5f;

    private void Awake()
    {
        // Auto-find references if not set
        if (scoreText == null)
        {
            var scoreObj = transform.Find("Canvas/ScoreText");
            if (scoreObj != null) scoreText = scoreObj.GetComponent<TextMeshProUGUI>();
        }
        if (menuPanel == null)
        {
            var menuObj = transform.Find("Canvas/MenuPanel");
            if (menuObj != null) menuPanel = menuObj.gameObject;
        }
        if (gameOverPanel == null)
        {
            var goObj = transform.Find("Canvas/GameOverPanel");
            if (goObj != null) gameOverPanel = goObj.gameObject;
        }
    }

    public void ShowMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
    }

    public void HideAll()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        if (perfectText != null) perfectText.gameObject.SetActive(false);
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    public void ShowGameOver(int finalScore)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (finalScoreText != null)
        {
            finalScoreText.text = "Score: " + finalScore.ToString();
        }
    }

    public void ShowPerfect()
    {
        if (perfectText != null)
        {
            perfectText.gameObject.SetActive(true);
            CancelInvoke(nameof(HidePerfect));
            Invoke(nameof(HidePerfect), perfectTextDuration);
        }
    }

    private void HidePerfect()
    {
        if (perfectText != null)
        {
            perfectText.gameObject.SetActive(false);
        }
    }
}
