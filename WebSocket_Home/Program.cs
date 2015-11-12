using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

namespace WebSocket_Home
{
    class Program
    {
        public static List<WebSocketClient> clients = new List<WebSocketClient>();
        public static XDocument commandsDoc;

        private static void Main(string[] args)
        {
            commandsDoc = XDocument.Load("commands.xml");
            TcpListener listener = new TcpListener(IPAddress.Any, 8324);
            TcpClient clientSocket = new TcpClient();
            listener.Start();
            Console.WriteLine("Server listening on port 8324...");
            while (true)
            {
                clientSocket = listener.AcceptTcpClient();
                WebSocketClient client = new WebSocketClient(clientSocket);
                //clients.Add(client);
            }
            clientSocket.Close();
            listener.Stop();
            Console.WriteLine("Exiting...");
            Console.ReadLine();
        }

        public static void ChatToAll(string data, string sender)
        {
            Console.WriteLine("[" + sender + "] >> " + data);
            for (int i = 0; i < clients.Count; i++)
            {
                //if (clients[i].clientName != sender)
                //{
                clients[i].SendTo("[" + sender + "] >> " + data + "\r\n");
                //}
            }
        }
    }

}