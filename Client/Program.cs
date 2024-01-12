using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    class Program
    {
        public static string Username;

        static void Main(string[] args)
        {
            // Benutzernamen abfragen
            Console.Write("Bitte geben Sie einen Benutzernamen an: ");
            Username = Console.ReadLine();

            // TCP-Verbindung zum Server herstellen
            TcpClient client = new TcpClient();
            client.Connect(IPAddress.Parse("192.168.8.59"), 3543);

            if (client.Connected)
            {
                // Thread für das Lesen von Serverbefehlen starten
                Thread Reader = new Thread(() => { ReadServerCommands(client); });
                Reader.Start();

                // StreamWriter verwenden, um Nachrichten an den Server zu senden
                using (StreamWriter writer = new StreamWriter(client.GetStream()))
                {
                    // Verbindungsnachricht an den Server senden
                    SendConnectMessage(writer);

                    // Endlosschleife für die Benutzereingabe
                    while (true)
                    {
                        if (!client.Connected)
                            break;

                        // Benutzereingabe abfragen
                        string eingabe = Console.ReadLine();

                        if (eingabe.Equals("exit"))
                        {
                            // Bei "exit" die Verbindung trennen und die Schleife beenden
                            client.Close();
                            break;
                        }
                        else
                        {
                            // Nachricht an den Server senden
                            string messasge = String.Format("{0}: {1}", Username, eingabe);
                            JsonClasses.CMD cmd = new JsonClasses.CMD();
                            cmd.TypeName = "clientmessage";
                            JsonClasses.CMD_SendMessage jmessage = new JsonClasses.CMD_SendMessage();
                            jmessage.Message = messasge;
                            cmd.Command = JsonConvert.SerializeObject(jmessage);
                            string SendString = JsonConvert.SerializeObject(cmd);

                            writer.WriteLine(SendString);
                            writer.Flush();
                        }

                        // Kurze Pause, um unnötige CPU-Auslastung zu vermeiden
                        Thread.Sleep(100);
                    }
                }
            }
            else
            {
                Console.WriteLine("Der Server konnte nicht erreicht werden!");
            }

            Console.Read();
        }

        static void ReadServerCommands(TcpClient client)
        {
            // StreamReader verwenden, um Nachrichten vom Server zu lesen
            using (StreamReader reader = new StreamReader(client.GetStream()))
            {
                // Endlosschleife für das Lesen von Serverbefehlen
                while (true)
                {
                    try
                    {
                        if (!client.Connected)
                        {
                            // Bei Verbindungsverlust Benachrichtigung ausgeben und Schleife beenden
                            Console.WriteLine("Verbindung verloren: {0}", client.Client.RemoteEndPoint.ToString());
                            break;
                        }

                        // Serverbefehl lesen und verarbeiten
                        string GetRead = reader.ReadLine();
                        HandelCommand(client, GetRead);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }

        static void HandelCommand(TcpClient client, string Command)
        {
            try
            {
                // Serverbefehl deserialisieren und verarbeiten
                JsonClasses.CMD cmd_command = JsonConvert.DeserializeObject<JsonClasses.CMD>(Command);
                if (cmd_command.TypeName.Equals("servermessage"))
                {
                    JsonClasses.CMD_SendMessage cmd = JsonConvert.DeserializeObject<JsonClasses.CMD_SendMessage>(cmd_command.Command);
                    Console.WriteLine(cmd.Message);
                }
            }
            catch { }
        }

        static void SendConnectMessage(StreamWriter writer)
        {
            // Verbindungsnachricht erstellen und an den Server senden
            JsonClasses.CMD cmd = new JsonClasses.CMD();
            cmd.TypeName = "connect";
            JsonClasses.CMD_Connected jmessage = new JsonClasses.CMD_Connected();
            jmessage.Name = Username;
            cmd.Command = JsonConvert.SerializeObject(jmessage);
            string SendString = JsonConvert.SerializeObject(cmd);
            writer.WriteLine(SendString);
            writer.Flush();
        }
    }
}