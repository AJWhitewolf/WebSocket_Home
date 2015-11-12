using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace WebSocket_Home
{
    class WebSocketClient
    {
        private TcpClient soc { set; get; }
        private NetworkStream stream { set; get; }
        public string clientName { set; get; }

        public WebSocketClient(TcpClient socket)
        {
            soc = socket;
            Console.WriteLine("Connected: {0}", soc.Client.RemoteEndPoint.ToString());
            clientName = "DefaultUser";
            Thread t = new Thread(new ThreadStart(Login));
            t.Start();
        }

        private void Login()
        {
            stream = soc.GetStream();
            string username = "";
            //Perform Websocket Handshake
            Byte[] bytesFrom = new byte[soc.Available];
            stream.Read(bytesFrom, 0, bytesFrom.Length);
            string dataFromClient = Encoding.UTF8.GetString(bytesFrom);
            if (dataFromClient.Length > 0)
            {
                if (new Regex("^GET").IsMatch(dataFromClient))
                {
                    Byte[] response =
                        Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                                                + "Connection: Upgrade" + Environment.NewLine
                                                + "Upgrade: websocket" + Environment.NewLine
                                                + "Sec-WebSocket-Protocol: json" + Environment.NewLine
                                                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                                    SHA1.Create().ComputeHash(
                                                        Encoding.UTF8.GetBytes(
                                                            new Regex("Sec-WebSocket-Key: (.*)").Match(dataFromClient)
                                                                .Groups[1].Value.Trim() +
                                                            "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                                            )
                                                        )
                                                    ) + Environment.NewLine
                                                + Environment.NewLine);

                    stream.Write(response, 0, response.Length);
                    stream.Flush();
                }
            }

            //Hand off to chatter
            Program.clients.Add(this);
            StartChat();
        }

        public void SendTo(string data)
        {
            Byte[] sendBytes = DataConversion.Encode(data);
            this.stream.Write(sendBytes, 0, sendBytes.Length);
            stream.Flush();
        }

        private void StartChat()
        {
            while (true)
            {
                try
                {
                    if (soc.Client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (soc.Client.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            this.Logout();
                        }
                    }
                    Byte[] bytesFrom = new byte[soc.Available];
                    this.stream.Read(bytesFrom, 0, bytesFrom.Length);
                    //Console.WriteLine("Received data, bytesFrom.Length = "+bytesFrom.Length);
                    if (bytesFrom.Length > 0)
                    {
                        string data = DataConversion.Decode(bytesFrom, bytesFrom.Length);
                        //Console.WriteLine("Got String: "+data);
                        ProcessCommands.ProcessData(data, this);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" >> " + ex.Message);
                    this.Logout();
                    break;
                }
            }
        }



        internal void Logout()
        {
            this.stream.Close();
            Program.clients.Remove(this);
            Program.ChatToAll(clientName + " logged out.", "SYSTEM");
        }

        private string CleanInput(string strIn)
        {
            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(strIn, @"[^\w\.@-]", "");
            }
            // If we timeout when replacing invalid characters, 
            // we should return Empty.
            catch (Exception ex)
            {
                return String.Empty;
            }
        }
    }
}
