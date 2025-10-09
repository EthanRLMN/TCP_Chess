using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class Server
{
    private Socket m_socket;
    private List<Socket> m_clients = new List<Socket>();
    private string m_receiveBuffer = "";

    private ChessGameManager.EChessTeam whitePlayer = ChessGameManager.EChessTeam.None;
    private ChessGameManager.EChessTeam blackPlayer = ChessGameManager.EChessTeam.None;

    public string IpAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 10147;
    public int Listeners { get; set; } = 4;
    public bool HasClient => m_clients.Count > 0;

    public void Initialize()
    {
        Debug.Log("[Server] Initializing...");

        IPAddress ipAddress = IPAddress.Parse(IpAddress);
        IPEndPoint localEP = new IPEndPoint(ipAddress, Port);

        m_socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        m_socket.Blocking = false;
        m_socket.Bind(localEP);
        m_socket.Listen(Listeners);

        Debug.Log($"[Server] Listening on {localEP.Address}:{localEP.Port}");
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

        foreach (var client in m_clients)
        {
            if (client == null || !client.Connected)
            {
                disconnected.Add(client);
                continue;
            }

            try
            {
                if (!client.Poll(0, SelectMode.SelectRead))
                    continue;

                if (client.Available == 0)
                {
                    Debug.Log("[Server] Client disconnected.");
                    disconnected.Add(client);
                    continue;
                }

                byte[] buffer = new byte[1024];
                int received = client.Receive(buffer);
                string messageChunk = Encoding.UTF8.GetString(buffer, 0, received);
                m_receiveBuffer += messageChunk;

                int newLineIndex;
                while ((newLineIndex = m_receiveBuffer.IndexOf('\n')) != -1)
                {
                    string fullMessage = m_receiveBuffer.Substring(0, newLineIndex).Trim();
                    m_receiveBuffer = m_receiveBuffer.Substring(newLineIndex + 1);

                    if (string.IsNullOrEmpty(fullMessage))
                        continue;

                    Debug.Log("[Server] Received : " + fullMessage);

                    if (fullMessage.StartsWith("TEAM:"))
                    {
                        string clientTeam = fullMessage.Substring(5);
                        var senderSocket = client; 
                        var parsedTeam = (ChessGameManager.EChessTeam)Enum.Parse(typeof(ChessGameManager.EChessTeam), clientTeam);

                        string assignedTeam = "Spectator";

                        if (parsedTeam == ChessGameManager.EChessTeam.White && whitePlayer == ChessGameManager.EChessTeam.None)
                        {
                            whitePlayer = ChessGameManager.EChessTeam.White;
                            assignedTeam = "White";
                        }
                        else if (parsedTeam == ChessGameManager.EChessTeam.Black && blackPlayer == ChessGameManager.EChessTeam.None)
                        {
                            blackPlayer = ChessGameManager.EChessTeam.Black;
                            assignedTeam = "Black";
                        }
                        else
                        {
                            Debug.Log($"[Server] Requested team {clientTeam} already taken, assigning Spectator.");
                        }

                        DispatchMessage(senderSocket, $"TEAM:{assignedTeam}");
                        Debug.Log($"[Server] Assigned {assignedTeam} to client");

                        BroadcastMessage($"TEAM_TAKEN:{assignedTeam}", senderSocket);

                        // Optionally start the game automatically once both players have joined
                        if (whitePlayer != ChessGameManager.EChessTeam.None && blackPlayer != ChessGameManager.EChessTeam.None)
                        {
                            Debug.Log("[Server] Both teams ready, starting game!");
                            ChessGameManager.Instance.StartNetworkGame(ChessGameManager.EChessTeam.None); // server spectates
                            BroadcastMessage("START_GAME");
                        }

                        if(fullMessage.StartsWith("TEAM_TAKEN:"))
                        {
                            string teamTaken = fullMessage.Substring(11);
                            GUIManager.Instance.DisableTeamButton(teamTaken);
                            continue;
                        }

                        continue;
                    }

                    // Move broadcast
                    ChessGameManager.Instance.ApplyNetworkMove(fullMessage);
                    BroadcastMessage(fullMessage, client); // Send to everyone excepte himself
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode != SocketError.WouldBlock)
                    Debug.LogError("[Server] Error receiving message : " + se.Message);
            }
        }
        foreach (var c in disconnected)
        {
            c.Close();
            m_clients.Remove(c);
        }
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
            Debug.Log("[Server] New user connected! Total clients: " + m_clients.Count);
            DispatchMessage(newClient, "SHOW_COLOR_SELECTION");
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)
                Debug.Log("[Server] Connection error : " + se.Message);
        }
    }

    public void DispatchMessage(Socket target, string message)
    {
        try
        {
            byte[] msg = Encoding.UTF8.GetBytes(message + "\n");
            target.Send(msg);
        }
        catch { }
    }

    public void BroadcastMessage(string message, Socket exclude = null)
    {
        byte[] msg = Encoding.UTF8.GetBytes(message + "\n");
        foreach (var c in m_clients)
        {
            if (c == null || !c.Connected || c == exclude)
                continue;
            try { c.Send(msg); } catch { }
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
