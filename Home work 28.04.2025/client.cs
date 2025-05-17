using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.Json;
enum Datetime
{
    Date,
    Time
}
class Message
{
    public Datetime date { get; set; }
    public DateTime DateORTime { get; set; }
}
class Client
{
    static string serverIP = "127.0.0.1";
    static int port = 5000;
    static NetworkStream? stream = null;
    static void sendMessage(string message, int buffsize = 1024)
    {
        if (stream == null)
            return;
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        stream.Write(buffer, 0, buffer.Length);
    }
    static string GetMessage(int buffsize = 1024)
    {
        if (stream == null)
            return "";
        byte[] buffer = new byte[buffsize];
        stream.Read(buffer, 0, buffsize);
        string ret = Encoding.UTF8.GetString(buffer).Split(char.MinValue).First();

        return ret;

    }

    static void Main(string[] args)
    {

        Message message = new Message();
        Console.Write("Choose:\n1.Date\n2.Time\n>> ");
        string Choosen = Console.ReadLine();
        if (Choosen == "1")
            message.date = Datetime.Date;
        else
            message.date = Datetime.Time;

        TcpClient tcpClient = new TcpClient(serverIP, port);
        Console.WriteLine("Connection: Succes!");

        stream = tcpClient.GetStream();

        sendMessage(JsonSerializer.Serialize(message));
        var messageFromServer = JsonSerializer.Deserialize<Message>(GetMessage());

        if (Choosen == "1")
            Console.WriteLine(messageFromServer.DateORTime.Date);
        else
            Console.WriteLine(messageFromServer.DateORTime.TimeOfDay);




    }
}
