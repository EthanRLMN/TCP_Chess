using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

using UnityEngine;


public class Client : MonoBehaviour
{
    #region Variables

    [SerializeField] private GameObject m_chatBoxObj;
    
    private string m_ipString = "10.2.107.154";
    private int m_port = 10147;
    
    private IPAddress m_ipAddress;
    private Socket m_clientSocket;
    private bool m_isConnecting = false, m_isHost = false, m_isConnected = false;
    
    private ChatBox m_chatBox;
    
    public string Nickname { get; set; }
    public bool IsConnected => m_clientSocket != null && !(m_clientSocket.Poll(1, SelectMode.SelectRead) && m_clientSocket.Available == 0);
    private string m_receiveBuffer = "";

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
        Nickname = "Player#" + UnityEngine.Random.Range(0001, 9999).ToString("D4");
        SetupClient();
        
        m_chatBox = FindFirstObjectByType<ChatBox>();
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

        string messageChunk = ReceiveChatMessage();
        if (!string.IsNullOrEmpty(messageChunk))
        {
            m_receiveBuffer += messageChunk;

            int newLineIndex;
            while ((newLineIndex = m_receiveBuffer.IndexOf('\n')) != -1)
            {
                string fullMessage = m_receiveBuffer.Substring(0, newLineIndex).Trim();
                m_receiveBuffer = m_receiveBuffer.Substring(newLineIndex + 1);

                if (string.IsNullOrEmpty(fullMessage))
                    continue;

                Debug.Log("[Client] Received : " + fullMessage);

                if (fullMessage.StartsWith("TEAM:"))
                {
                    string teamStr = fullMessage.Substring(5);
                    if (Enum.TryParse(teamStr, out ChessGameManager.EChessTeam assignedTeam))
                    {
                        ChessGameManager.Instance.StartNetworkGame(assignedTeam);
                        Debug.Log($"[Client] Assigned team: {assignedTeam}");
                    }
                    continue;
                }

                if (fullMessage == "SHOW_COLOR_SELECTION")
                {
                    Debug.Log("[Client] Displaying color selection UI");
                    GUIManager.Instance.ShowColorSelection();
                    continue;
                }
                if (fullMessage == "START_GAME")
                {
                    Debug.Log("[Client] Game starting!");
                    continue;
                }

                ChessGameManager.Instance.ApplyNetworkMove(fullMessage);
            }
        }
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


    public void SendMessage(MessageBuilder.MessageType type, string content)
    {
        if (!IsConnected)
            return;

        byte[] messageBytes = Encoding.UTF8.GetBytes(message + "\n");

        try
        {
            m_clientSocket.Send(message);
            Debug.Log("[Client] " + message);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)  // Do not log errors when the message is empty
                Debug.LogError("[Client] Error sending message : " + se.Message);
        }
    }
    
    
    public void SendChatMessage(string message)
    {
        if (!IsConnected)
            return;

        string formattedMessage = $"{Nickname}|{message}";
        byte[] msg = MessageBuilder.BuildMessage(MessageBuilder.MessageType.Chat, Encoding.UTF8.GetBytes(formattedMessage));

        try
        {
            m_clientSocket.Send(msg);
            Debug.Log("[Client] Chat message sent: " + formattedMessage);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode != SocketError.WouldBlock)
                Debug.LogError("[Client] Error sending chat: " + se.Message);
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
                m_chatBox?.AddToChatOutput(content);
                Debug.Log("[Client] Chat : " + content);
                break;

            case MessageBuilder.MessageType.GameState:
                Debug.Log("[Client] GameState : " + content);
                // TODO: Update local board state
                break;

            case MessageBuilder.MessageType.PlayerAction:
                Debug.Log("[Client] PlayerAction : " + content);
                // TODO: Mirror opponent action
                break;

            default:
                Debug.LogWarning("[Client] Unknown message type : " + msg.Type);
                break;
        }
    }
    
    #endregion
}
