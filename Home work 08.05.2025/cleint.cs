using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.Json;
enum RequestType
{
    GetQuote,
    Exit
}

class Message
{
    public RequestType Request { get; set; }
    public string Quote { get; set; } = "";
    public bool LimitReached { get; set; } = false;
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
        Console.OutputEncoding = UTF8Encoding.UTF8;
        Console.InputEncoding = UTF8Encoding.UTF8;
        TcpClient tcpClient = new(serverIP, port);
        stream = tcpClient.GetStream();

        Console.WriteLine("Підключено до сервера. Натискайте Enter для отримання цитати або введіть 'exit'.");

        while (true)
        {
            string input = Console.ReadLine();
            if (input.ToLower() == "exit")
            {
                sendMessage(JsonSerializer.Serialize(new Message { Request = RequestType.Exit }));
                break;
            }

            sendMessage(JsonSerializer.Serialize(new Message { Request = RequestType.GetQuote }));

            var response = JsonSerializer.Deserialize<Message>(GetMessage());
            if (response == null) break;

            Console.WriteLine("Цитата: " + response.Quote);

            if (response.LimitReached)
            {
                Console.WriteLine("Досягнуто ліміту цитат. З'єднання завершено.");
                break;
            }
        }
    }
}
