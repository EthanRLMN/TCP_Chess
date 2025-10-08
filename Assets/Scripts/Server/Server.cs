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

        Message message = ReceiveMessage(m_clientSocket);
        if (message != null)
            HandleMessage(message);
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


    public void DispatchMessage(MessageBuilder.MessageType type, string content)
    {
        if (!HasClient)
        {
            Debug.Log("[Server] There's no client to dispatch message to!");
            return;
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        byte[] message = MessageBuilder.BuildMessage(type, contentBytes);
        
        try
        {
            m_clientSocket.Send(message);
            Debug.Log($"[Server] Sent {type}: {content}");
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)
                Debug.LogError("[Server] Error sending message : " + se.Message);
        }
    }


    private Message ReceiveMessage(Socket socket)
    {
        if (socket == null || !socket.Connected)
            return null;

        // Make sure the header is available
        if (socket.Available < 8)
            return null;

        // Try to read the header and see if it matches
        byte[] header = new byte[8];
        int headerRead = socket.Receive(header, 0, 8, SocketFlags.None);
        if (headerRead < 8)
        {
            Debug.LogWarning("[Receive] Incomplete header.");
            return null;
        }

        int contentLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 0));
        MessageBuilder.MessageType type = (MessageBuilder.MessageType)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 4));

        // Try to read the message content
        byte[] content = new byte[contentLength];
        int totalRead = 0;
        while (totalRead < contentLength)
        {
            int read = socket.Receive(content, totalRead, contentLength - totalRead, SocketFlags.None);
            if (read <= 0)
                return null;
            
            totalRead += read;
        }
        return new Message(type, content);
    }
    
    
    private void HandleMessage(Message msg)
    {
        string content = Encoding.UTF8.GetString(msg.Content);

        switch (msg.Type)
        {
            case MessageBuilder.MessageType.Chat:
                Debug.Log("[Server] Chat : " + content);
                break;

            case MessageBuilder.MessageType.GameState:
                Debug.Log("[Server] GameState : " + content);
                // TODO: Parse game state and update board
                break;

            case MessageBuilder.MessageType.PlayerAction:
                Debug.Log("[Server] PlayerAction : " + content);
                // TODO: Apply move then send new GameState to client
                break;

            default:
                Debug.LogWarning("[Server] Unknown message type : " + msg.Type);
                break;
        }
    }
    
#endregion
    
    
}
