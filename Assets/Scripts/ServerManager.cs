using UnityEngine;


public class ServerManager : MonoBehaviour
{
    private static ServerManager m_instance;
    public static ServerManager Instance => m_instance;

    private Server m_server;
    public Server Server => m_server;
    
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


    public void InitServer()
    {
        m_server = new Server();
        m_server.Initialize();
    }
}
