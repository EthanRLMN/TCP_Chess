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

    public string IpAddress { get; set; } = "127.0.0.1"; // Use local IP as default
    public int Port { get; set; } = 10147; // Use 10147 as the default port
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

        Debug.Log($"[Server] Listening on {m_localEP.Address}:{m_localEP.Port}");
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


    public void Shutdown()
    {
        Debug.Log("[Server] Shutting down...");

        try
        {
            if (m_clientSocket != null)
            {
                try
                {
                    if (m_clientSocket.Connected)
                    {
                        m_clientSocket.Shutdown(SocketShutdown.Both);
                        Debug.Log("[Server] Client socket shutdown done!");
                    }
                }
                catch (SocketException se)
                {
                    Debug.LogWarning("[Server] Client shutdown skipped (already closed) : " + se.Message);
                }
                finally
                {
                    m_clientSocket.Close();
                    m_clientSocket = null;
                    Debug.Log("[Server] Client socket closed!");
                }
            }
            
            if (m_socket != null)
            {
                try
                {
                    m_socket.Close();
                    Debug.Log("[Server] Listening socket closed!");
                }
                catch (SocketException se)
                {
                    Debug.LogWarning("[Server] Listening socket close error : " + se.Message);
                }
                finally
                {
                    m_socket = null;
                }
            }

            Debug.Log("[Server] Shutdown done!");
        }
        catch (Exception e)
        {
            Debug.LogError("[Server] Unexpected error during shutdown: " + e);
        }
    }


    public void DispatchMessage(string message)
    {
        if (!HasClient)
        {
            Debug.Log("[Server] There's no client to dispatch message to!");
            return;
        }

        byte[] msg = Encoding.UTF8.GetBytes(message);
        try
        {
            m_clientSocket.Send(msg);
            Debug.Log(message);
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
            if (m_clientSocket.Poll(0, SelectMode.SelectRead))
            {
                if (m_clientSocket.Available == 0)
                {
                    Debug.Log("[Server] Client socket closed.");
                    m_clientSocket.Close();
                    m_clientSocket = null;
                    return string.Empty;
                }

                byte[] buffer = new byte[1024];
                int received = m_clientSocket.Receive(buffer);
                return Encoding.UTF8.GetString(buffer, 0, received);
            }
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
