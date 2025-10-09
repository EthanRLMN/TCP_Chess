using System.Net.Sockets;
using UnityEngine;


public class ServerManager : MonoBehaviour
{
    #region Variables
    
    public static ServerManager Instance { get; private set; }
    
    private Server m_server;
    
    public bool IsRunning => m_server != null;
    
    #endregion
    
    
    #region Unity Functions
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    private void LateUpdate()
    {
        if (m_server == null)
            return;
        
        if (IsRunning) 
            m_server.Update();
    }


    private void OnApplicationQuit()
    {
        StopServer();
    }

    #endregion


    #region Custom Functions

    public void StartServer(string ip = "127.0.0.1", int port = 10147, int listeners = 2)
    {
        if (m_server != null)
            StopServer();
        
        InitServer(ip, port, listeners);
    }
    
    
    public void Broadcast(MessageBuilder.MessageType type, string content, Socket except = null)
    {
        if (!IsRunning)
        {
            Debug.LogWarning("[ServerManager] Cannot broadcast : server not running.");
            return;
        }

        m_server.BroadcastMessage(type, content, except);
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
