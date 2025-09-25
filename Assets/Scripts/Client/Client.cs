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

    public bool IsConnected => m_clientSocket != null && m_clientSocket.Connected;

    #endregion
    
    
    #region Unity Methods

    private void Awake()
    {
        SetupClient();
    }


    private void LateUpdate()
    {
        if (!IsConnected)
            return;
        
        string message = ReceiveChatMessage();
        if (string.IsNullOrEmpty(message))
            Debug.Log("[Received] Received : " + message);
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

        m_ipAddress = IPAddress.Parse(m_ipString);
        m_clientSocket = new Socket(m_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        m_clientSocket.Blocking = false;    
    }
    
    
    public void ConnectAttempt()
    {
        SetupClient();
        IPEndPoint ipEndPoint = new IPEndPoint(m_ipAddress, m_port);

        try
        {
            m_clientSocket.Connect(ipEndPoint);
            Debug.Log("[Client] Connected to server : " + ipEndPoint);
        }
        catch (SocketException se)
        {
            Debug.LogWarning("[Client] Connection in progress or failed : " + se.SocketErrorCode);
        }
        catch (Exception e)
        {
            Debug.LogError("[Client] Error connecting to server : " + e.Message);
            //Disconnect();
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
        
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        try
        {
            m_clientSocket.Send(messageBytes);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)  // Do not log errors when the message is empty
                Debug.LogError("[Client] Error sending message : " + se.Message);
        }
    }


    private string ReceiveChatMessage()
    {
        if (IsConnected)
            return string.Empty;

        try
        {
            // Ensure the socket is available before trying to send messages
            if (m_clientSocket.Available == 0)
                return string.Empty;

            byte[] messageBytes = new byte[1024];
            int receivedMessage = m_clientSocket.Receive(messageBytes);

            return Encoding.UTF8.GetString(messageBytes, 0, receivedMessage);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)
                Debug.LogWarning("[Client] Message reception failed : " + se.Message);
        }
        catch (Exception e)
        {
            Debug.LogError("[Client] Error receiving message : " + e.Message);
        }
        return string.Empty;
    }


    private IEnumerator PingServer()
    {
        yield return new WaitForSeconds(30);
    }
    
    #endregion
}
