using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Server
{
    class Server : MonoBehaviour
    {
        Socket socket;
        int port = 10147; // hard coder
        IPEndPoint localEP;
        Socket clientSocket;
        private IPHostEntry host;
        private int listener = 2;
        public Server()
        {
            //create server socket
            IPAddress ipAdress = IPAddress.Parse("10.2.107.154");
            localEP = new IPEndPoint(ipAdress, port);
            socket = new Socket(ipAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public void InitHosting()
        {
            Server server = new Server();
            server.Binding();

            string message = server.ReceiveMessage();
            Debug.Log("Server has received message : " + message);

            server.SendingMessage("Hello from Server");

            server.Disconnect();
            Console.ReadKey();
        }

        public void Binding()
        {
            Debug.Log("Starting Server");

            socket.Bind(localEP);
            socket.Listen(listener);

            try
            {
                Debug.Log("Waiting for a connection...");
                // blocking instruction
                clientSocket = socket.Accept();

                Debug.Log("Accepted Client !");
            }
            catch (Exception e)
            {
                Debug.Log("error " + e.ToString());
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (clientSocket != null)
            {
                // shutdown client socket
                try
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e)
                {
                    Debug.Log("error " + e.ToString());
                }
                finally
                {
                    clientSocket.Close();
                }
            }

            if (socket != null)
            {
                // server socket : no shutdown necessary
                socket.Close();
            }
        }

        public void SendingMessage(string message)
        {
            byte[] msg = Encoding.ASCII.GetBytes(message);
            try
            {
                clientSocket.Send(msg);
            }
            catch (Exception e)
            {
                Debug.Log("error sending message : " + e.ToString());
            }
        }

        public string ReceiveMessage()
        {
            try
            {
                byte[] messageReceived = new byte[1024];
                int nbBytes = clientSocket.Receive(messageReceived);
                return Encoding.ASCII.GetString(messageReceived, 0, nbBytes);
            }
            catch (Exception e)
            {
                Debug.Log("error receiving message : " + e.ToString());
            }
            return String.Empty;
        }
    }
}

