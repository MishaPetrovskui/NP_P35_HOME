using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text.Json;
using System.Drawing;

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

class Server
{
    static TcpListener listener;
    static int port = 5000;
    static int clients = 1;
    static readonly object lockObj = new object();
    static Random rand = new Random();
    private const int maxQuotesPerClient = 5;
    static Dictionary<string, int> clientQuotes = new();
    static string[] quotes = new string[]
    {
        "Жизнь — это то, что с тобой происходит, пока ты строишь планы. Джон Леннон",
        "Стремитесь не к успеху, а к ценностям, которые он дает. Альберт Эйнштейн",
        "Сначала мечты кажутся невозможными, затем неправдоподобными, а потом неизбежными. Кристофер Рив",
        "Лучшая месть — огромный успех. Фрэнк Синатра",
        "Талант — это дар, которому невозможно ни научить, ни научиться. Иммануил Кант",
        "Единственный способ делать свою работу хорошо — это любить ее. Если ты еще не нашел свое любимое дело, продолжай искать. Стив Джобс",
        "Пока ты держишься за свою «стабильность», кто-то рядом воплощает в жизнь твои мечты. Роберт Орбен",
        "Успех — это умение двигаться от неудачи к неудаче, не теряя энтузиазма. Уинстон Черчилль",
        "Успех — дело исключительно случая. Это вам скажет любой неудачник. Эрл Уилсон"
    };

    static void sendMessage(NetworkStream stream, string message, int buffsize = 1024)
    {
        if (stream == null)
            return;
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        stream.Write(buffer, 0, buffer.Length);
    }
    static string GetMessage(NetworkStream stream, int buffsize = 1024)
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


        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Console.WriteLine("Server Started!");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            new Thread(HandleClient).Start(client);
            /*thread.Start(client);*/
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        Console.WriteLine();
        Console.WriteLine("New Client");

        var stream = client.GetStream();
        string clientId = client.Client.RemoteEndPoint.ToString();
        clientQuotes[clientId] = 0;

        try
        {
            while (true)
            {
                var request = JsonSerializer.Deserialize<Message>(GetMessage(stream));
                if (request == null || request.ToString().ToLower() == "exit" || request.Request == RequestType.Exit)
                    break;

                clientQuotes[clientId]++;
                bool limit = clientQuotes[clientId] >= maxQuotesPerClient;

                string quote = quotes[new Random().Next(quotes.Length)];
                var response = new Message
                {
                    Quote = quote,
                    LimitReached = limit
                };

                sendMessage(stream, JsonSerializer.Serialize(response));

                if (limit) break;
            }
        }
        catch (Exception ex) 
        {
            Console.WriteLine("Помилка: " + ex.Message);
        }
        finally
        {
            Console.WriteLine($"Клієнт {clientId} відключився.");
            clientQuotes.Remove(clientId);
            client.Close();
        }
    }
}
