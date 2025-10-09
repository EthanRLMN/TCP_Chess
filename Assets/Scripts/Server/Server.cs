using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Server
{
    private readonly List<Socket> m_clients = new();
    private ChessGameManager.EChessTeam blackPlayer = ChessGameManager.EChessTeam.None;
    private Socket m_socket;

    private ChessGameManager.EChessTeam whitePlayer = ChessGameManager.EChessTeam.None;

    public string IpAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 10147;
    public int Listeners { get; set; } = 4;
    public bool HasClient => m_clients.Count > 1;

    public void Initialize()
    {
        Debug.Log("[Server] Initializing...");

        IPAddress ipAddress = IPAddress.Parse(IpAddress);
        IPEndPoint localEP = new(ipAddress, Port);

        m_socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        m_socket.Blocking = false;
        m_socket.Bind(localEP);
        m_socket.Listen(Listeners);

        Debug.Log($"[Server] Listening on {localEP.Address}:{localEP.Port}");
    }


    public void Update()
    {
        HandleUsersConnection();

        List<Socket> disconnected = new();

        foreach (Socket client in m_clients)
        {
            if (client == null || !IsConnected(client))
            {
                disconnected.Add(client);
                continue;
            }

            Message msg = ReceiveMessage(client);
            if (msg != null)
                HandleMessage(msg, client);
        }

        foreach (Socket c in disconnected)
            HandleUserDisconnection(c);
    }


    private void HandleUsersConnection()
    {
        if (m_socket == null)
            return;

        try
        {
            if (!m_socket.Poll(0, SelectMode.SelectRead))
                return;

            Socket newClient = m_socket.Accept();
            newClient.Blocking = false;
            m_clients.Add(newClient);

            Debug.Log("[Server] New user connected! Total clients : " + m_clients.Count);

            // Assign team
            ChessGameManager.EChessTeam assignedTeam = ChessGameManager.EChessTeam.Spectator;

            if (whitePlayer == ChessGameManager.EChessTeam.None)
            {
                whitePlayer = ChessGameManager.EChessTeam.White;
                assignedTeam = ChessGameManager.EChessTeam.White;
            }
            else if (blackPlayer == ChessGameManager.EChessTeam.None)
            {
                blackPlayer = ChessGameManager.EChessTeam.Black;
                assignedTeam = ChessGameManager.EChessTeam.Black;
            }

            // Send team assignment via GameState only
            DispatchMessage(newClient, MessageBuilder.MessageType.GameState, $"TEAM:{assignedTeam}");

            // If both players exist, prompt color selection
            if (whitePlayer != ChessGameManager.EChessTeam.None && blackPlayer != ChessGameManager.EChessTeam.None)
                BroadcastMessage(MessageBuilder.MessageType.GameState, "SHOW_COLOR_SELECTION");
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)
                Debug.Log("[Server] Connection error : " + se.Message);
        }
    }


    public void DispatchMessage(Socket target, MessageBuilder.MessageType type, string content)
    {
        if (target == null || !target.Connected) return;

        byte[] msg = MessageBuilder.BuildMessage(type, Encoding.UTF8.GetBytes(content));
        try
        {
            target.Send(msg);
        }
        catch
        {
        }
    }


    public void Shutdown()
    {
        Debug.Log("[Server] Shutting down...");

        BroadcastMessage(MessageBuilder.MessageType.GameState, "SERVER_SHUTDOWN");

        if (ChessGameManager.Instance)
        {
            Debug.Log("[Server] Host cleanup : returning to main menu.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        foreach (Socket client in m_clients)
        {
            try
            {
                if (client.Connected) client.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                client.Close();
            }
            catch
            {
            }
        }

        m_clients.Clear();

        try
        {
            m_socket?.Close();
        }
        catch (SocketException se)
        {
            Debug.LogWarning("[Server] Error closing socket : " + se.Message);
        }
        finally
        {
            m_socket = null;
        }

        Debug.Log("[Server] Shutdown complete!");
    }


    private void HandleUserDisconnection(Socket client)
    {
        if (client == null)
            return;

        Debug.Log($"[Server] Client disconnected : {client.RemoteEndPoint}");

        try
        {
            client.Close();
        }
        catch
        {
        }

        m_clients.Remove(client);

        if (m_clients.Count == 0)
        {
            Debug.Log("[Server] All clients disconnected, closing server.");
            Shutdown();
            return;
        }

        if (client == GetPlayerSocket(ChessGameManager.EChessTeam.White))
        {
            whitePlayer = ChessGameManager.EChessTeam.None;
            BroadcastMessage(MessageBuilder.MessageType.GameState, "SERVER_SHUTDOWN");
            Shutdown();
        }
        else if (client == GetPlayerSocket(ChessGameManager.EChessTeam.Black))
        {
            blackPlayer = ChessGameManager.EChessTeam.None;
            BroadcastMessage(MessageBuilder.MessageType.GameState, "SERVER_SHUTDOWN");
            Shutdown();
        }
        else
        {
            // Spectateur parti -> rien de grave
            Debug.Log("[Server] A spectator has left.");
        }
    }


    private Socket GetPlayerSocket(ChessGameManager.EChessTeam team)
    {
        if (m_clients.Count > 0 && team == ChessGameManager.EChessTeam.White)
            return m_clients[0];

        if (m_clients.Count > 1 && team == ChessGameManager.EChessTeam.Black)
            return m_clients[1];

        return null;
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
        MessageBuilder.MessageType type =
            (MessageBuilder.MessageType)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 4));

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


    public void BroadcastMessage(MessageBuilder.MessageType type, string content, Socket exclude = null)
    {
        if (m_clients.Count == 0) return;

        byte[] msg = MessageBuilder.BuildMessage(type, Encoding.UTF8.GetBytes(content));

        foreach (Socket client in m_clients)
        {
            if (client == null || client == exclude) continue;
            try
            {
                client.Send(msg);
            }
            catch
            {
            }
        }
    }


    private void HandleMessage(Message msg, Socket sender)
    {
        string content = Encoding.UTF8.GetString(msg.Content);
        Debug.Log($"[Server] {msg.Type} from {sender.RemoteEndPoint} : {content}");

        switch (msg.Type)
        {
            case MessageBuilder.MessageType.Chat:
                BroadcastMessage(MessageBuilder.MessageType.Chat, content, sender);
                break;

            case MessageBuilder.MessageType.PlayerAction:
                ChessGameManager.Instance.ApplyNetworkMove(content);
                BroadcastMessage(MessageBuilder.MessageType.PlayerAction, content, sender);
                break;

            case MessageBuilder.MessageType.GameState:
                BroadcastMessage(MessageBuilder.MessageType.GameState, content, sender);
                ChessGameManager.Instance.ProcessNetworkGameCommand(content);
                break;

            default:
                Debug.LogWarning("[Server] Unknown message type: " + msg.Type);
                break;
        }
    }
}