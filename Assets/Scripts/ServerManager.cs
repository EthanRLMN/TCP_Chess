using UnityEngine;


public class ServerManager : MonoBehaviour
{
    #region Variables
    
    [SerializeField] private string m_ipString = "10.2.107.154";
    [SerializeField] private int m_port = 10147;
    [SerializeField] private int m_listeners = 2;
    
    private static ServerManager m_instance;
    public static ServerManager Instance => m_instance;

    private Server m_server;
    public Server Server => m_server;
    
    
    #endregion
    
    
    #region Unity Functions
    private void Awake()
    {
        if (!m_instance)
        {
            m_instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    #endregion


    #region Custom Functions
    
    public void InitServer()
    {
        m_server = new Server();
        m_server.Initialize();
        
        IpAddress = m_ipString;
        Port = m_port;
        MaxPlayers = m_listeners;
    }

    #endregion

    
    #region Getters / Setters
    
    public string IpAddress
    {
        set => m_server.IpAddress = value;
        get => m_server.IpAddress;
    }
    
    
    public int Port
    {
        set => m_server.Port = value;
        get => m_server.Port;
    }


    public int MaxPlayers
    {
        set => m_server.Listeners = value;
        get => m_server.Listeners;
    }
    
    #endregion
}
