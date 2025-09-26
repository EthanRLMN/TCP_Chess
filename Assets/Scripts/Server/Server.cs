using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


public class Server
{
    #region Variables

    private Socket m_socket, m_clientSocket;
    private IPHostEntry m_host;
    private IPEndPoint m_localEP;

    #endregion


    #region Getters / Setters

    public string IpAddress { get; set; } = "10.2.107.154";
    public int Port { get; set; } = 10147;
    public int Listeners { get; set; } = 2;
    public bool HasClient => m_clientSocket != null && m_clientSocket.Connected;

    #endregion


    #region Custom Functions
    
    public void Initialize()
    {
        Debug.Log("[Server] Initializing...");

        IPAddress ipAddress = IPAddress.Parse(IpAddress);
        m_localEP = new IPEndPoint(ipAddress, Port);
        
        m_socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        m_socket.Blocking = false;
        m_socket.Bind(m_localEP);
        m_socket.Listen(Listeners);

        Debug.Log($"[Server] Listening on {IpAddress}:{Port}");
    }


    public void Update()
    {
        HandleUsersConnection();
        
        string message = ReceiveMessage();
        if (message != string.Empty) 
            Debug.Log("[Server] Received : " + message);
    }


    private void HandleUsersConnection()
    {
        if (m_socket == null)
            return;
        
        try
        {
            // Ensure there's a connection attempt before accepting anything
            if (!m_socket.Poll(0, SelectMode.SelectRead))
                return;
            
            m_clientSocket = m_socket.Accept();
            m_clientSocket.Blocking = false;
                
            Debug.Log("[Server] User connection allowed!");
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)
                Debug.Log("[Server] Connection authorization error : " + se.Message);
        }
    }


    private void Shutdown()
    {
        try
        {
            if (m_clientSocket != null)
            { 
                m_clientSocket.Shutdown(SocketShutdown.Both);
                m_clientSocket.Close();
                m_clientSocket = null;
            }

            if (m_socket != null)
            {
                m_socket.Shutdown(SocketShutdown.Both);
                m_socket.Close();
                m_socket = null;
            }
            
            Debug.Log("[Server] Shutdown done!");
        }
        catch (Exception e)
        {
            Debug.LogError("[Server] Error shutting down server : " + e);
        }
    }


    public void DispatchMessage(string message)
    {
        if (!HasClient)
        {
            Debug.Log("[Server] There's no client to dispatch message to!");
            return;
        }
        
        byte[] msg = Encoding.ASCII.GetBytes(message);
        try
        {
            m_clientSocket.Send(msg);
            Debug.Log("[Server] " + message);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock) 
                Debug.LogError("[Server] Error sending message : " + se.Message);
        }
    }


    private string ReceiveMessage()
    {
        if (!HasClient)
            return string.Empty;
        
        try
        {
            /*if (m_clientSocket.Available == 0)
            {
                Debug.Log("[Server] Not available anymore!");
                return string.Empty;
            }*/

            byte[] messageBytes = new byte[1024];
            int receivedMessage = m_clientSocket.Receive(messageBytes);
            
            Debug.LogWarning("[Server] Has received message!");
            
            return Encoding.ASCII.GetString(messageBytes, 0, receivedMessage);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)
                Debug.LogError("[Server] Error receiving message : " + se.Message);
        }
        return string.Empty;
    }

    #endregion
}