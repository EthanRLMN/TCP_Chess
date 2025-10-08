using UnityEngine;
using UnityEngine.UI;

/*
 * Simple GUI display : scores and team turn
 */

public class GUIManager : MonoBehaviour
{
    [SerializeField] private GameObject m_colorSelectionPanel;
    [SerializeField] private Button m_whiteButton;
    [SerializeField] private Button m_blackButton;

    private ChessGameManager.EChessTeam m_localTeam;

    #region Singleton
    static GUIManager instance = null;
    public static GUIManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<GUIManager>();
            return instance;
        }
    }
    #endregion

    Transform whiteToMoveTr = null;
    Transform blackToMoveTr = null;
    Text whiteScoreText = null;
    Text blackScoreText = null;

    // Use this for initialization
    void Awake()
    {
        whiteToMoveTr = transform.Find("WhiteTurnText");
        blackToMoveTr = transform.Find("BlackTurnText");

        whiteToMoveTr.gameObject.SetActive(false);
        blackToMoveTr.gameObject.SetActive(false);

        whiteScoreText = transform.Find("WhiteScoreText").GetComponent<Text>();
        blackScoreText = transform.Find("BlackScoreText").GetComponent<Text>();

        ChessGameManager.Instance.OnPlayerTurn += DisplayTurn;
        ChessGameManager.Instance.OnScoreUpdated += UpdateScore;
    }

    public void ShowColorSelection()
    {
        m_colorSelectionPanel.SetActive(true);

        m_whiteButton.onClick.RemoveAllListeners();
        m_blackButton.onClick.RemoveAllListeners();

        m_whiteButton.onClick.AddListener(() => SelectColor(ChessGameManager.EChessTeam.White));
        m_blackButton.onClick.AddListener(() => SelectColor(ChessGameManager.EChessTeam.Black));
    }

    private void SelectColor(ChessGameManager.EChessTeam team)
    {
        m_localTeam = team;
        m_colorSelectionPanel.SetActive(false);

        Debug.Log($"[GUIManager] Local player chose {team}");

        // If you are the host, send back the opposit color to the client 
        if (ServerManager.Instance?.Server != null && ServerManager.Instance.Server.HasClient)
        {
            var clientTeam = (team == ChessGameManager.EChessTeam.White)
                ? ChessGameManager.EChessTeam.Black
                : ChessGameManager.EChessTeam.White;

            Debug.Log($"[GUIManager] Server sending TEAM:{clientTeam}");
            ServerManager.Instance.Server.DispatchMessage($"TEAM:{clientTeam}");

            // Host start game with his chosen color
            ChessGameManager.Instance.StartNetworkGame(team);
        }
        else
        {
            var client = FindFirstObjectByType<Client>();
            if (client != null && client.IsConnected)
            {
                Debug.Log($"[GUIManager] Client sending TEAM:{team}");
                client.SendChatMessage($"TEAM:{team}");
            }
            else
            {
                Debug.LogWarning("[GUIManager] Client not connected, cannot send TEAM message.");
            }
        }
    }

    void DisplayTurn(bool isWhiteMove)
    {
        whiteToMoveTr.gameObject.SetActive(isWhiteMove);
        blackToMoveTr.gameObject.SetActive(!isWhiteMove);
    }

    void UpdateScore(uint whiteScore, uint blackScore)
    {
        whiteScoreText.text = string.Format("White : {0}", whiteScore);
        blackScoreText.text = string.Format("Black : {0}", blackScore);
    }
}
