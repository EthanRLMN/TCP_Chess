using System;
using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;


public class Client : MonoBehaviour
{
    #region Variables

    [SerializeField] private string m_ipString = "10.2.107.154";
    [SerializeField] private int m_port = 10147;
    private IPAddress m_ipAddress;
    private Socket m_clientSocket;
    private bool m_isConnecting = false, m_isHost = false, m_isConnected = false;

    public bool IsConnected
    {
        get
        {
            if (m_clientSocket == null)
                return false;

            try
            {
                return !(m_clientSocket.Poll(1, SelectMode.SelectRead) && m_clientSocket.Available == 0);
            }
            catch
            {
                return false;
            }
        }
    }

    #endregion
    
    
    #region Unity Methods

    private void Awake()
    {
        SetupClient();
    }


    private void LateUpdate()
    {
        if (m_isConnecting && m_clientSocket.Poll(0, SelectMode.SelectWrite))
        {
            m_isConnecting = false;
            Debug.Log("[Client] Connection completed!");
        }
        
        if (!IsConnected)
            return;
        
        string message = ReceiveChatMessage();
        if (!string.IsNullOrEmpty(message))
            Debug.Log("[Client] Received : " + message);
    }

    #endregion
    
    
    #region Custom Methods
    
    private void SetupClient()
    {
        if (m_clientSocket != null)
        {
            try
            {
                m_clientSocket.Close();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        m_ipAddress = IPAddress.Loopback;
        m_clientSocket = new Socket(m_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        m_clientSocket.Blocking = false;    
    }
    
    
    public void ConnectAttempt()
    {
        SetupClient();
        IPEndPoint ipEndPoint = new IPEndPoint(m_ipAddress, m_port);

        try
        {
            m_clientSocket.Blocking = false;
            m_clientSocket.Connect(ipEndPoint);
            Debug.Log("[Client] Connected to server : " + ipEndPoint);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.WouldBlock || se.SocketErrorCode == SocketError.InProgress)
            {
                m_isConnecting = true;
                Debug.Log("[Client] Connection in progress...");
            }
            else
            {
                Debug.LogWarning("[Client] Connection failed : " + se.SocketErrorCode);
            }
        }
    }
    
    
    public void ConnectAttempt(string ip, int port)
    {
        m_ipString = ip;
        m_port = port;
        
        ConnectAttempt();
    }


    public void Disconnect()
    {
        if (m_clientSocket == null)
            return;
        
        try
        {
            m_clientSocket.Shutdown(SocketShutdown.Both);
        }
        catch {}

        try
        {
            m_clientSocket.Close();
            Debug.Log("[Client] successfully disconnected from server : " + m_ipAddress + "!");
        }
        catch (Exception e)
        {
            Debug.LogError("[Client] Error disconnecting from server : " + e.Message);
        }
        finally
        {
            m_clientSocket = null;
        }
    }


    public void SendChatMessage(string message)
    {
        if (!IsConnected)
            return;

        byte[] messageBytes = Encoding.ASCII.GetBytes(message);

        try
        {
            m_clientSocket.Send(messageBytes);
            Debug.Log("[Client] " + message);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)  // Do not log errors when the message is empty
                Debug.LogError("[Client] Error sending message : " + se.Message);
        }
    }


    private string ReceiveChatMessage()
    {
        if (!IsConnected)
            return string.Empty;

        try
        {
            if (m_clientSocket.Poll(0, SelectMode.SelectRead))
            {
                if (m_clientSocket.Available == 0)
                {
                    Debug.Log("[Client] Connection closed by server!");
                    Disconnect();
                    return string.Empty;
                }

                byte[] buffer = new byte[1024];
                int received = m_clientSocket.Receive(buffer);
                return Encoding.ASCII.GetString(buffer, 0, received);
            }
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)
                Debug.LogError("[Client] Receive error : " + se.Message);
        }

        return string.Empty;
    }


    private IEnumerator PingServer()
    {
        yield return new WaitForSeconds(30);
    }
    
    #endregion
}
