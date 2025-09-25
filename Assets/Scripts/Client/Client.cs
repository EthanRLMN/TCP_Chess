using System;
using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class Client : MonoBehaviour
{
    #region Variables

    private string m_ipString = "10.2.107.154";
    private int m_port = 10147;
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
        PingServer();
    }

    #endregion
    
    
    #region Custom Methods
    
    private void SetupClient()
    {
        m_ipAddress = IPAddress.Parse(m_ipString);
        m_clientSocket = new Socket(m_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    }
    
    
    public void ConnectAttempt()
    {
        SetupClient();
        IPEndPoint ipEndPoint = new IPEndPoint(m_ipAddress, m_port);

        try
        {
            m_clientSocket.Connect(ipEndPoint);
            Debug.Log("Client connected to server : " + ipEndPoint);
        }
        catch (Exception e)
        {
            Debug.LogError("Error connecting to server : " + e.Message);
            Disconnect();
        }
    }


    public void Disconnect()
    {
        // Do nothing if there is no existing socket
        if (m_clientSocket == null || !m_clientSocket.Connected)
            return;

        // Attempt to shut down client connection to server properly
        try
        {
            m_clientSocket.Shutdown(SocketShutdown.Both);
            Debug.Log("Disconnecting client from server " + m_ipAddress + "...");
        }
        catch (Exception e)
        {
            Debug.LogError("Error disconnecting from server : " + e.Message);
        }
        finally
        {
            m_clientSocket.Close();
            Debug.Log("Client successfully disconnected from server : " + m_ipAddress + "!");
        }
    }


    public void SendChatMessage(string message)
    {
        if (m_clientSocket == null || !m_clientSocket.Connected)
            return;
        
        byte[] messageBytes = System.Text.Encoding.ASCII.GetBytes(message);

        try
        {
            m_clientSocket.Send(messageBytes);
            Debug.Log("Client sent message : " + message);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending message : " + e.Message);
        }
    }


    public string ReceiveChatMessage()
    {
        try
        {
            byte[] messageBytes = new byte[1024];
            int message = m_clientSocket.Receive(messageBytes);
            
            return System.Text.Encoding.ASCII.GetString(messageBytes, 0, messageBytes.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("Error receiving message : " + e.Message);
            return null;
        }
    }


    private IEnumerator PingServer()
    {
        yield return new WaitForSeconds(30);
    }
    
    #endregion
}
