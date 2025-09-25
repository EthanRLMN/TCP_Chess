using System;
using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class Client : MonoBehaviour
{
    #region Variables

    [SerializeField] private string m_ipString = "10.2.107.154";
    [SerializeField] private int m_port = 10147;
    private IPAddress m_ipAddress;
    private Socket m_clientSocket;

    public bool IsConnected => m_clientSocket.Connected;

    #endregion
    
    
    #region Unity Methods

    private void Awake()
    {
        SetupClient();
    }


    private void LateUpdate()
    {
        string message = ReceiveChatMessage();
        if(message != string.Empty)
        {
            Debug.Log(message);
        }
    }

    #endregion
    
    
    #region Custom Methods
    
    private void SetupClient()
    {
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
        catch (Exception e)
        {
            Debug.LogError("[Client] Error connecting to server : " + e.Message);
            //Disconnect();
        }
    }
    
    
    public void ConnectAttempt(string ip, int port)
    {
        SetupClient();
        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

        try
        {
            m_clientSocket.Connect(ipEndPoint);
            Debug.Log("[Client] Connected to server : " + ipEndPoint);
        }
        catch (Exception e)
        {
            Debug.LogError("[Client] Error connecting to server : " + e.Message);
            //Disconnect();
        }
    }


    public void Disconnect()
    {
        // Attempt to shut down client connection to server properly
        try
        {
            m_clientSocket.Shutdown(SocketShutdown.Both);
            Debug.Log("[Client] Disconnecting client from server " + m_ipAddress + "...");
        }
        catch (Exception e)
        {
            Debug.LogError("[Client] Error disconnecting from server : " + e.Message);
        }
        finally
        {
            m_clientSocket.Close();
            Debug.Log("[Client] successfully disconnected from server : " + m_ipAddress + "!");
        }
    }


    public void SendChatMessage(string message)
    {
        byte[] messageBytes = System.Text.Encoding.ASCII.GetBytes(message);

        try
        {
            m_clientSocket.Send(messageBytes);
            Debug.Log("[Client] sent message : " + message);
        }
        catch (Exception e)
        {
            Debug.LogError("[Client] Error sending message : " + e.Message);
        }
    }


    private string ReceiveChatMessage()
    {
        try
        {
            byte[] messageBytes = new byte[1024];
            int message = m_clientSocket.Receive(messageBytes);
            
            return System.Text.Encoding.ASCII.GetString(messageBytes, 0, message);
        }
        catch (Exception e)
        {
            //Debug.LogError("[Client] Error receiving message : " + e.Message);
        }
        return string.Empty;
    }


    private IEnumerator PingServer()
    {
        yield return new WaitForSeconds(30);
    }
    
    #endregion
}
