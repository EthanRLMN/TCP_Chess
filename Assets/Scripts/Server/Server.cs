using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


public class Server
{
    #region Variables

    private Socket m_socket;
    private List<Socket> m_clients = new();
    private IPHostEntry m_host;
    private IPEndPoint m_localEP;

    #endregion


    #region Getters / Setters

    public string IpAddress { get; set; } = "127.0.0.1"; // Use local IP as default
    public int Port { get; set; } = 10147; // Use 10147 as the default port
    public int Listeners { get; set; } = 2;
    public int ClientCount => m_clients.Count;
    public IEnumerable<Socket> Clients => m_clients;

    public bool HasClient => m_clients != null && m_clients[0].Connected;

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

        for (int i = m_clients.Count - 1; i >= 0; --i)
        {
            Socket client = m_clients[i];
            if (client == null || !client.Connected)
            {
                m_clients.RemoveAt(i);
                continue;
            }

            Message msg = ReceiveMessage(client);
            if (msg != null)
                HandleMessage(msg, client);
        }
    }


    private void HandleUsersConnection()
    {
        if (m_socket == null)
            return;

        try
        {
            while (m_socket.Poll(0, SelectMode.SelectRead))
            {
                Socket newClient = m_socket.Accept();
                newClient.Blocking = false;
                m_clients.Add(newClient);

                Debug.Log($"[Server] New client connected ({m_clients.Count} total)");
            }
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)
                Debug.Log("[Server] Connection error : " + se.Message);
        }
    }


    public void Shutdown()
    {
        Debug.Log("[Server] Shutting down...");

        foreach (Socket client in m_clients)
        {
            try
            {
                if (client.Connected)
                    client.Shutdown(SocketShutdown.Both);
            }
            catch { }

            try
            {
                client.Close();
            }
            catch { }
        }

        m_clients.Clear();

        try
        {
            m_socket?.Close();
        }
        catch (SocketException se)
        {
            Debug.LogWarning("[Server] Error closing listening socket : " + se.Message);
        }
        finally
        {
            m_socket = null;
        }

        Debug.Log("[Server] Shutdown complete!");
    }
    
    
    private static bool IsConnected(Socket socket)
    {
        try
        {
            return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
        }
        catch
        {
            return false;
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
    
    
    public void BroadcastMessage(MessageBuilder.MessageType type, string content, Socket except = null)
    {
        if (m_clients.Count == 0)
        {
            Debug.Log("[Server] No clients to broadcast to.");
            return;
        }

        byte[] msg = MessageBuilder.BuildMessage(type, Encoding.UTF8.GetBytes(content));

        foreach (Socket client in m_clients)
        {
            if (client == null || client == except)
                continue;

            try
            {
                client.Send(msg);
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode != SocketError.WouldBlock)
                    Debug.LogWarning("[Server] Broadcast send error : " + se.Message);
            }
        }
        Debug.Log($"[Server] Broadcasted {type} : {content}");
    }
    
    
    private void HandleMessage(Message msg, Socket sender)
    {
        string content = Encoding.UTF8.GetString(msg.Content);
        Debug.Log($"[Server] {msg.Type} from {sender.RemoteEndPoint} : {content}");


        switch (msg.Type)
        {
            case MessageBuilder.MessageType.Chat:
                BroadcastMessage(MessageBuilder.MessageType.Chat, content, sender);
                Debug.Log("[Server] Chat relayed : " + content);
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
