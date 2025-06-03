using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;

class Dish
{
    public string name { get; set; }
    public List<string> ingredients { get; set; }
}

class Message
{
    public string Exception { get; set; } = "";
    public List<Dish> Recipes { get; set; }
    public List<string> Products { get; set; }
}

class UDPClientApp
{
    static string serverIP = "127.0.0.1";
    static int port = 5037;
    static IPEndPoint serverEndPoint;
    static UdpClient client;
    static bool isRunning = true;

    static void ReadResponsesFromServer()
    {
        while (isRunning)
        {
            try
            {
                var tempEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var responseData = client.Receive(ref tempEndPoint);
                var jsonResponse = Encoding.UTF8.GetString(responseData);

                var serverMessage = JsonSerializer.Deserialize<Message>(jsonResponse);

                if (!string.IsNullOrEmpty(serverMessage.Exception))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nПомилка: {serverMessage.Exception}");
                    Console.ResetColor();
                }
                else if (serverMessage.Recipes != null && serverMessage.Recipes.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nЗнайдено рецептів: {serverMessage.Recipes.Count}");
                    Console.WriteLine("═══════════════════════════════════════");

                    foreach (var recipe in serverMessage.Recipes)
                    {
                        Console.WriteLine($"{recipe.name.ToUpper()}");
                        Console.WriteLine($"Інгредієнти: {string.Join(", ", recipe.ingredients)}");
                        Console.WriteLine("───────────────────────────────────────");
                    }
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nНа жаль, рецептів із зазначеними продуктами не знайдено");
                    Console.ResetColor();
                }

                Console.Write("\nВведіть продукти через кому (або 'exit' для виходу):\n>> ");
            }
            catch (SocketException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (isRunning)
                {
                    Console.WriteLine($"Помилка отримання відповіді: {ex.Message}");
                }
                break;
            }
        }
    }

    static void Main(string[] args)
    {
        Console.OutputEncoding = UTF8Encoding.UTF8;
        Console.InputEncoding = UTF8Encoding.UTF8;

        try
        {
            Console.OutputEncoding = UTF8Encoding.UTF8;
            Console.InputEncoding = UTF8Encoding.UTF8;
            client = new UdpClient(0); 
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);

            Console.WriteLine("Клієнт пошуку рецептів");
            Console.WriteLine("═══════════════════════════");
            Console.WriteLine($"Підключення до сервера {serverIP}:{port}");
            Console.WriteLine("Приклади продуктів: potato, oil, salt");
            Console.WriteLine();

            var responseThread = new Thread(ReadResponsesFromServer);
            responseThread.Start();

            while (true)
            {
                try
                {
                    Console.Write("Введіть продукти через кому (або 'exit' для виходу):\n>> ");
                    string input = Console.ReadLine();
                    Console.WriteLine(input);



                    if (input.ToLower().Trim() == "exit")
                        break;

                    var products = input.Split(',').ToList();
                    foreach(var product in products)
                    {
                        Console.WriteLine(product);
                    }
                    if (products.Count == 0)
                    {
                        Console.WriteLine("Введіть хоча б один продукт!");
                        continue;
                    }

                    var message = new Message { Products = products };

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

                    var jsonRequest = JsonSerializer.Serialize(message, options);
                    var requestData = Encoding.UTF8.GetBytes(jsonRequest);

                    client.Send(requestData, serverEndPoint);
                    Console.WriteLine($"Запит надіслано: [{string.Join(", ", products)}]");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка надсилання запиту: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критична помилка клієнта: {ex.Message}");
        }
        finally
        {
            isRunning = false;
            client?.Close();
            Console.WriteLine("Клієнт вимкнено. До побачення!");
        }
    }
}
