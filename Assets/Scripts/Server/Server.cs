using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


public class Server
{
    #region Variables

    [SerializeField] private string m_ipString = "127.0.0.1";
    [SerializeField] private int m_port = 10147;
    [SerializeField] private int m_listeners = 2;
    private Socket m_socket, m_clientSocket;
    private IPHostEntry m_host;
    private IPEndPoint m_localEP;

    #endregion
    
    
    #region Getters / Setters

    private string IpAddress
    {
        get => m_ipString;
        set => m_ipString = value;
    }


    private int Port
    {
        get => m_port;
        set => m_port = value;
    }


    private int Listeners
    {
        get => m_listeners;
        set => m_listeners = value;
    }
    
    #endregion


    #region Custom Functions
    
    public void Initialize()
    {
        Debug.Log("[Server] Initializing Server...");
        
        IPAddress ipAddress = IPAddress.Parse(m_ipString);
        m_localEP = new IPEndPoint(ipAddress, m_port);
        m_socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        
        Debug.Log("[Server] Server initialized\n IP Address : " + ipAddress + ", Port : " + m_port + ", Listeners : " + m_listeners);
        
        InitHosting();
    }
    
    
    public void InitHosting()
    {
        Binding();

        //string message = ReceiveMessage();
        //Debug.Log("Server has received message : " + message);

        //SendingMessage("Hello from Server");

        //HandleShutdown();
    }


    public void Binding()
    {
        Debug.Log("Starting Server");

        m_socket.Bind(m_localEP);
        m_socket.Listen(m_listeners);

        try
        {
            Debug.Log("Waiting for a connection...");
            // blocking instruction
            m_clientSocket.Blocking = false;
            m_clientSocket = m_socket.Accept();

            Debug.Log("Accepted Client !");
        }
        catch (Exception e)
        {
            Debug.Log("error " + e);
            HandleShutdown();
        }
    }


    public void HandleShutdown()
    {
        if (m_clientSocket != null)
            // shutdown client socket
            try
            {
                m_clientSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                Debug.Log("error " + e);
            }
            finally
            {
                m_clientSocket.Close();
            }

        m_socket?.Close();
    }


    public void SendingMessage(string message)
    {
        byte[] msg = Encoding.ASCII.GetBytes(message);
        try
        {
            m_clientSocket.Send(msg);
        }
        catch (Exception e)
        {
            Debug.Log("error sending message : " + e);
        }
    }


    public string ReceiveMessage()
    {
        try
        {
            byte[] messageReceived = new byte[1024];
            int nbBytes = m_clientSocket.Receive(messageReceived);
            return Encoding.ASCII.GetString(messageReceived, 0, nbBytes);
        }
        catch (Exception e)
        {
            Debug.Log("error receiving message : " + e);
        }

        return string.Empty;
    }

    #endregion
}