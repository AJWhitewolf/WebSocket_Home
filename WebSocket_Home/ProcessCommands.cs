using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using GitRemote;

namespace WebSocket_Home
{
    //Used for creating objects from JSON
    public class DataBreaker
    {
        public string Command { get; set; }
        public string Data { get; set; }
        public string Other { get; set; }
    }
    class ProcessCommands
    {
        public static void ProcessData(string data, WebSocketClient client)
        {
            try
            {
                DataBreaker c = JsonConvert.DeserializeObject<DataBreaker>(data);
                if (client.clientName == "DefaultUser")
                {
                    if (c.Command == "setUsername")
                    {
                        client.clientName = c.Data;
                    }
                    else
                    {
                        client.SendTo("Username must be set.");
                    }
                }
                else
                {
                    switch (c.Command)
                    {
                        case "git":
                            Console.WriteLine("Git request received, attempting to {0} -> Submitted by {1}", c.Data, client.clientName);
                            var cNode = (from item in Program.commandsDoc.Descendants("item")
                                where item.Attribute("name").Value == c.Data.Split(" ".ToCharArray())[1] &&
                                item.Parent.Attribute("name").Value == "git"
                                select item).FirstOrDefault();
                            GitProgram git = new GitProgram(c.Data.Split(" ".ToCharArray())[0], cNode.Attribute("directory").Value);
                            git.DoIt();
                            break;
                        case "exit":
                            client.Logout();
                            Console.WriteLine("Received exit command from " + client.clientName);
                            break;
                        default:
                            client.SendTo("Command not recognized.");
                            //Program.ChatToAll(data, client.clientName);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                client.SendTo("Command Invalid, " + ex.Message + ex.StackTrace);
            }
        }
    }
}
