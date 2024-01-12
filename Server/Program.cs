using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    class Program
    {
        // TcpListener für eingehende Verbindungen
        public static TcpListener listen;
        // Dictionary zur Verwaltung der verbundenen Clients
        public static Dictionary<int, TcpClient> Clients = new Dictionary<int, TcpClient>();

        static void Main(string[] args)
        {
            // TcpListener auf localhost und Port 3543 initialisieren
            listen = new TcpListener(IPAddress.Parse("192.168.8.59"), 3543);
            listen.Start();

            // Thread für den Serverstart starten
            Thread RunThread = new Thread(() => { RunServer(); });
            RunThread.Start();

            // Hauptschleife für die Serversteuerung
            while (true)
            {
                // Benutzereingabe abfragen
                string s = Console.ReadLine();

                if (s.Equals("exit"))
                {
                    // Bei "exit" den Server stoppen und das Programm beenden
                    listen.Stop();
                    Environment.Exit(0);
                }
                else if (s.Equals("test"))
                {
                    // Testnachricht ausgeben
                    Console.WriteLine("Das ist ein Test!");
                }
            }
        }

        // Methode für den Serverbetrieb
        static void RunServer()
        {
            // Endlosschleife für das Akzeptieren von Client-Verbindungen
            while (true)
            {
                TcpClient client = listen.AcceptTcpClient();
                Clients.Add(Convert.ToInt32(client.Client.RemoteEndPoint.ToString().Split(':')[1]), client);
                Console.WriteLine("Incomming connection: {0}", client.Client.RemoteEndPoint.ToString());

                // Thread für die Bearbeitung des Clients starten
                Thread T_ClientHandler = new Thread(() => { ClientHandler(Convert.ToInt32(client.Client.RemoteEndPoint.ToString().Split(':')[1])); });
                T_ClientHandler.Start();
            }
        }

        // Methode für die Behandlung eines einzelnen Clients
        static void ClientHandler(int clientID)
        {
            TcpClient client = Clients[clientID];
            StreamReader reader = new StreamReader(client.GetStream());

            // Endlosschleife für das Lesen von Client-Nachrichten
            while (true)
            {
                try
                {
                    if (client.Connected)
                    {
                        string GetRead = reader.ReadLine();
                        HandelCommand(clientID, GetRead);
                    }
                    else
                    {
                        // Bei Verbindungsverlust den Client entfernen und die Schleife beenden
                        Console.WriteLine("[ClientHandler IF] Connection Lost: {0}", clientID);
                        Clients.Remove(clientID);
                        break;
                    }
                }
                catch
                {
                    // Bei Ausnahme den Client entfernen und die Schleife beenden
                    Console.WriteLine("[ClientHandler TRY] Connection Lost: {0}", clientID);
                    Clients.Remove(clientID);
                    break;
                }
            }
        }

        // Methode zur Verarbeitung von Client-Befehlen
        static void HandelCommand(int clientID, string Command)
        {
            TcpClient client = Clients[clientID];
            try
            {
                // Client-Befehl deserialisieren und verarbeiten
                JsonClasses.CMD cmd_command = JsonConvert.DeserializeObject<JsonClasses.CMD>(Command);
                if (cmd_command.TypeName.Equals("connect"))
                {
                    // Thread für die Verarbeitung der Verbindung starten
                    Thread T_Command = new Thread(() => { Command_Connected(clientID, cmd_command.Command); });
                    T_Command.Start();
                }
                if (cmd_command.TypeName.Equals("clientmessage"))
                {
                    // Thread für die Verarbeitung von Client-Nachrichten starten
                    Thread T_Command = new Thread(() =>
                    {
                        JsonClasses.CMD_SendMessage message = JsonConvert.DeserializeObject<JsonClasses.CMD_SendMessage>(cmd_command.Command);
                        ServerCommand_SendMessageToAllOther(clientID, message.Message);
                    });
                    T_Command.Start();
                }
            }
            catch { }
        }

        // Methode für die Verarbeitung der Verbindung eines Clients
        static void Command_Connected(int clientID, string Command)
        {
            JsonClasses.CMD_Connected cmd_command = JsonConvert.DeserializeObject<JsonClasses.CMD_Connected>(Command);
            Console.WriteLine("{0} ist verbunden", cmd_command.Name);
            ServerCommand_SendMessageToAll(String.Format("{0} ist dem Server beigetreten!", cmd_command.Name));
        }

        // Methode für das Senden einer Nachricht an alle Clients
        static void ServerCommand_SendMessageToAll(string Message)
        {
            Console.WriteLine(Message);
            for (int i = 0; i < Clients.Keys.ToArray<int>().Length; i++)
            {
                int connecid = Clients.Keys.ToArray<int>()[i];
                if (Clients[connecid].Connected)
                {
                    StreamWriter writer = new StreamWriter(Clients[connecid].GetStream());
                    JsonClasses.CMD_SendMessage message = new JsonClasses.CMD_SendMessage();
                    message.Message = Message;

                    JsonClasses.CMD cmd = new JsonClasses.CMD();
                    cmd.TypeName = "servermessage";
                    cmd.Command = JsonConvert.SerializeObject(message);

                    writer.WriteLine(JsonConvert.SerializeObject(cmd));
                    writer.Flush();
                }
                else
                {
                    // Bei Verbindungsverlust den Client entfernen
                    Console.WriteLine("[ServerCommand_SendMessageToAll IF] Connection Lost: {0}", connecid);
                    Clients.Remove(connecid);
                }
            }
        }

        // Methode für das Senden einer Nachricht an alle anderen Clients
        static void ServerCommand_SendMessageToAllOther(int clientID, string Message)
        {
            Console.WriteLine(Message);
            for (int i = 0; i < Clients.Keys.ToArray<int>().Length; i++)
            {
                int connecid = Clients.Keys.ToArray<int>()[i];
                if (!(connecid == clientID))
                {
                    if (Clients[connecid].Connected)
                    {
                        StreamWriter writer = new StreamWriter(Clients[connecid].GetStream());
                        JsonClasses.CMD_SendMessage message = new JsonClasses.CMD_SendMessage();
                        message.Message = Message;

                        JsonClasses.CMD cmd = new JsonClasses.CMD();
                        cmd.TypeName = "servermessage";
                        cmd.Command = JsonConvert.SerializeObject(message);

                        writer.WriteLine(JsonConvert.SerializeObject(cmd));
                        writer.Flush();
                    }
                    else
                    {
                        // Bei Verbindungsverlust den Client entfernen
                        Console.WriteLine("[ServerCommand_SendMessageToAll IF] Connection Lost: {0}", connecid);
                        Clients.Remove(connecid);
                    }
                }
            }
        }
    }
}