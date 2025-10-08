using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ServerSelector : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject m_menuRoot;
    [SerializeField] private TMP_InputField m_ipInputField;
    [SerializeField] private Button m_connectDisconnectButton;
    [SerializeField] private Button m_hostButton;

    private bool m_isHost = false, m_isConnected = false, m_isOpen = false;

    private string m_ipAddress = "127.0.0.1";
    private int m_port = 10147;

    private Client m_client;


    private void Awake()
    {
        m_client = FindFirstObjectByType<Client>();
        
        if (m_ipInputField)
            m_ipInputField.text = $"{m_ipAddress}:{m_port}";
        
        if (m_connectDisconnectButton)
            m_connectDisconnectButton.onClick.AddListener(OnConnectDisconnectClicked);

        if (m_hostButton)
            m_hostButton.onClick.AddListener(OnHostButtonClicked);

        RefreshUI();
    }


    private void LateUpdate()
    {
        if (!m_isOpen)
            return;

        if (!m_ipInputField)
            return;
        
        string input = m_ipInputField.text;
        string[] parts = input.Split(':');
        if (parts.Length != 2) 
            return;
        
        m_ipAddress = parts[0];
        if (!int.TryParse(parts[1], out m_port))
            m_port = 10147; // Default port
    }


    private void RefreshUI()
    {
        if (m_ipInputField)
            m_ipInputField.interactable = !m_isConnected && !m_isHost;
        
        if (m_connectDisconnectButton)
        {
            TMP_Text connectBtnTxt = m_connectDisconnectButton.GetComponentInChildren<TMP_Text>();
            if (connectBtnTxt)
                connectBtnTxt.text = m_isConnected ? "Disconnect" : "Connect";
            
            m_connectDisconnectButton.interactable = !m_isHost;
        }
        
        if (!m_hostButton)
            return;
        
        TMP_Text hostBtnTxt = m_hostButton.GetComponentInChildren<TMP_Text>();
        if (hostBtnTxt)
            hostBtnTxt.text = m_isHost ? "Shutdown" : "Host";
        
        m_hostButton.interactable = !m_isConnected || m_isHost; // Disables the host button if the player is already on another server or is already hosting a game
    }
    
    
    private void OnConnectDisconnectClicked()
    {
        if (m_isConnected)
            DisconnectFromServer();
        else
            ConnectToServer();
    }


    private void ConnectToServer()
    {
        Debug.Log($"[ServerSelector] Trying to connect to {m_ipAddress}:{m_port}...");
        
        m_client.ConnectAttempt(m_ipAddress, m_port);
        m_isConnected = true;

        RefreshUI();
    }


    private void DisconnectFromServer()
    {
        Debug.Log("[ServerSelector] Disconnecting from server...");
        // TODO : Call the client disconnection function
        m_isConnected = false;

        RefreshUI();
    }

    
    private void OnHostButtonClicked()
    {
        if (m_isConnected && !m_isHost)
        {
            Debug.LogWarning("[ServerSelector] Cannot host a server while connected to another server!");
            return;
        }

        if (m_isHost)
        {
            Debug.Log("[ServerSelector] Shutting down server...");
            
            ServerManager.Instance.StopServer();
            m_isHost = false;
            m_isConnected = false;
        }
        else
        {
            Debug.Log("[ServerSelector] Starting host server...");
            
            ServerManager.Instance.StartServer(m_ipAddress, m_port);
            
            m_isHost = true;
            m_isConnected = true;
        }

        RefreshUI();
    }
    
    
    public void ToggleMenu()
    {
        SetMenuOpen(!m_isOpen);
    }

    
    public void SetMenuOpen(bool open)
    {
        m_isOpen = open;

        if (m_menuRoot)
            m_menuRoot.SetActive(m_isOpen);
            
        Debug.Log($"[ServerSelector] Menu is now {(m_isOpen ? "Opened" : "Closed")}");

        if (m_isOpen)
            RefreshUI();
    }
}
