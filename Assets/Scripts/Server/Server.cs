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

    #endregion


    #region Custom Functions

    public void ConnectionTest()
    {
        Binding();
        
        string message = ReceiveMessage();
        if (message != string.Empty) 
            Debug.Log("Server has received message : " + message);
    }


    public void Initialize()
    {
        Debug.Log("[Server] Initializing Server...");

        IPAddress ipAddress = IPAddress.Parse(IpAddress);
        m_localEP = new IPEndPoint(ipAddress, Port);
        m_socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        Debug.Log("[Server] Server initialized\n IP Address : " + ipAddress + ", Port : " + Port + ", Listeners : " + Listeners);

        InitHosting();
    }


    public void InitHosting()
    {
        Debug.Log("Starting Server");

        m_socket.Bind(m_localEP);
        m_socket.Listen(Listeners);

        //SendingMessage("Hello from Server");

        //HandleShutdown();
    }


    public void Binding()
    {
        try
        {
            //Debug.Log("Waiting for a connection...");
            // blocking instruction
            m_socket.Blocking = false;
            m_clientSocket = m_socket.Accept();

            //Debug.Log("Accepted Client !");
        }
        catch (Exception e)
        {
            //Debug.Log("error " + e);
            //HandleShutdown();
        }
    }


    public void HandleShutdown()
    {
        if (m_socket == null)
            return;

        // shutdown client socket
        try
        {
            m_socket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception e)
        {
            Debug.Log("error " + e);
        }
        finally
        {
            m_socket.Close();
        }
    }


    public void DispatchMessage(string message)
    {
        Debug.Log("[Server] Dispatching message : " + message);
        
        byte[] msg = Encoding.ASCII.GetBytes(message);
        try
        {
            Debug.Log("[Server] Receiver Information : " + m_clientSocket.RemoteEndPoint);
            m_clientSocket.Send(msg);
            Debug.Log("[Server] Message dispatched successfully!");
        }
        catch (Exception e)
        {
            //Debug.Log("error sending message : " + e);
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
            //Debug.Log("error receiving message : " + e);
        }

        return string.Empty;
    }

    #endregion
}