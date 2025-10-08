using UnityEngine;


public class ServerManager : MonoBehaviour
{
    #region Variables
    
    private static ServerManager m_instance = null;
    private Server m_server;
    public Server Server => m_server;
    
    
    #endregion
    
    
    #region Instance
    public static ServerManager Instance
    {
        get
        {
            if (!m_instance)
                m_instance = FindFirstObjectByType<ServerManager>();
            return m_instance;
        }
    }
    
    #endregion
    
    
    #region Unity Functions
    private void Awake()
    {
        if (!m_instance)
            m_instance = this;
        else
            Destroy(gameObject);
    }


    private void LateUpdate()
    {
        if (m_server == null)
            return;
        
        m_server.Update();
        
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            m_server.DispatchMessage("[Server] Sending message to player!");
        }
    }


    private void OnApplicationQuit()
    {
        if (m_server == null)
            return;
        
        m_server.Shutdown();
        m_server = null;
    }

    #endregion


    #region Custom Functions

    public void StartServer(string ip = "127.0.0.1", int port = 10147, int listeners = 2)
    {
        if (m_server != null)
            StopServer();
        
        InitServer(ip, port, listeners);
    }


    public void StopServer()
    {
        if (m_server == null)
            return;
        
        m_server.Shutdown();
        m_server = null;
    }


    private void InitServer(string ip, int port, int listeners)
    {
        m_server = new Server();
        
        IpAddress = ip;
        Port = port;
        MaxPlayers = listeners;
        
        m_server.Initialize();
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
