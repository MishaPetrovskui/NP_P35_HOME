using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

class Dish
{
    public string name { get; set; }
    public List<string> ingredients { get; set; }

    public Dish() { }

    public Dish(string name, List<string> ingredients)
    {
        this.name = name;
        this.ingredients = ingredients;
    }
}

class Message
{
    public string Exception { get; set; } = "";
    public List<Dish> Recipes { get; set; }
    public List<string> Products { get; set; }
}

class User
{
    public string IPAddress { get; set; }
    public int Port { get; set; }
    public List<DateTime> Requests { get; set; }

    public User()
    {
        Requests = new List<DateTime>();
    }

    public bool Equals(IPEndPoint endPoint)
    {
        return IPAddress == endPoint.Address.ToString() && Port == endPoint.Port;
    }
}

class UDPServer
{
    static int port = 5037;
    static List<User> Users = new List<User>();
    static int maxRequests = 10;
    static int cooldownMinutes = 1;

    static List<Dish> recipes = new List<Dish>
    {
        new Dish("borscht", new List<string>{ "beetroot", "cabbage", "potato", "carrot", "onion", "broth", "tomato paste", "dill", "parsley", "green onion" }),
        new Dish("omelette", new List<string>{ "eggs", "milk", "salt", "baking soda", "flour", "pepper", "tomato", "bread" }),
        new Dish("vegetable salad", new List<string>{ "cucumber", "tomato", "onion", "oil", "pepper", "sweet pepper", "radish", "beetroot" }),
        new Dish("french fries", new List<string>{ "potato", "oil", "salt" }),
        new Dish("burger", new List<string>{ "buns", "cutlet", "pickles", "onion", "pickled onion", "cheese", "sauce", "lettuce", "cabbage" }),
        new Dish("pizza", new List<string>{ "dough", "tomato sauce", "cheese", "olives", "tomatoes", "onion", "salami", "mushrooms", "oregano" }),
        new Dish("dumplings with potatoes", new List<string>{ "flour", "water", "egg", "salt", "potato", "onion", "butter" }),
        new Dish("buckwheat with mushrooms", new List<string>{ "buckwheat", "mushrooms", "onion", "oil", "salt", "pepper" }),
        new Dish("pancakes", new List<string>{ "flour", "eggs", "milk", "water", "sugar", "salt", "butter" }),
        new Dish("pasta with sauce", new List<string>{ "pasta", "tomato sauce", "cheese", "basil", "oil", "garlic", "onion" })
    };


    static void ProcessRequest(IPEndPoint clientEndPoint, List<string> ingredients)
    {
        User currentUser = GetOrCreateUser(clientEndPoint);

        CleanupOldRequests(currentUser);

        if (currentUser.Requests.Count >= maxRequests)
        {
            var response = new Message
            {
                Exception = $"Перевищено ліміт запитів ({maxRequests} за {cooldownMinutes} хв). Спробуйте пізніше.",
                Recipes = new List<Dish>()
            };
            SendResponse(response, clientEndPoint);
            Console.WriteLine($"Клієнт {clientEndPoint}: перевищено ліміт запитів");
            return;
        }

        currentUser.Requests.Add(DateTime.Now);

        var validIngredients = ingredients.Where(ing => !string.IsNullOrWhiteSpace(ing)).ToList();

        if (validIngredients.Count == 0)
        {
            var response = new Message
            {
                Exception = "Список продуктів порожній",
                Recipes = new List<Dish>()
            };
            SendResponse(response, clientEndPoint);
            Console.WriteLine($"Клієнт {clientEndPoint}: надіслав порожній список продуктів. Запитів: {currentUser.Requests.Count}/{maxRequests}");
            return;
        }

        var matchedRecipes = GetMatchingRecipes(validIngredients);
        var successResponse = new Message
        {
            Exception = "",
            Recipes = matchedRecipes
        };

        SendResponse(successResponse, clientEndPoint);

        Console.WriteLine($"Клієнт {clientEndPoint}: [{string.Join(", ", validIngredients)}] -> знайдено {matchedRecipes.Count} рецептів. Запросів: {currentUser.Requests.Count}/{maxRequests}");
    }

    static User GetOrCreateUser(IPEndPoint endPoint)
    {
        var existingUser = Users.FirstOrDefault(u => u.Equals(endPoint));
        if (existingUser == null)
        {
            existingUser = new User
            {
                IPAddress = endPoint.Address.ToString(),
                Port = endPoint.Port
            };
            Users.Add(existingUser);
            Console.WriteLine($"Новий клієнт підключено: {endPoint}");
        }
        return existingUser;
    }

    static void CleanupOldRequests(User user)
    {
        var cutoffTime = DateTime.Now.AddMinutes(-cooldownMinutes);
        user.Requests.RemoveAll(requestTime => requestTime < cutoffTime);
    }

    static List<Dish> GetMatchingRecipes(List<string> availableIngredients)
    {
        var matchedRecipes = new List<Dish>();

        var normalizedIngredients = availableIngredients
            .Select(ing => ing.Trim().ToLower())
            .Where(ing => !string.IsNullOrEmpty(ing))
            .ToList();

        foreach (var recipe in recipes)
        {
            bool canMakeRecipe = recipe.ingredients.All(recipeIngredient =>
                normalizedIngredients.Any(availableIngredient =>
                    availableIngredient.Contains(recipeIngredient.ToLower()) ||
                    recipeIngredient.ToLower().Contains(availableIngredient)));

            if (canMakeRecipe)
            {
                matchedRecipes.Add(recipe);
            }
        }

        return matchedRecipes;
    }

    static void SendResponse(Message message, IPEndPoint clientEndPoint)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var jsonResponse = JsonSerializer.Serialize(message, options);
            var responseData = Encoding.UTF8.GetBytes(jsonResponse);
            Server.Send(responseData, clientEndPoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка надсилання відповіді клієнту {clientEndPoint}: {ex.Message}");
        }
    }

    public static UdpClient Server;

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        try
        {
            Server = new UdpClient(port);
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine($"UDP сервер рецептів запущено на порту {port}");
            Console.WriteLine($"Ліміт запитів: {maxRequests} за {cooldownMinutes} хв");
            Console.WriteLine("Очікування підключень...\n");

            while (true)
            {
                try
                {
                    // Получаем данные от клиента
                    var receivedData = Server.Receive(ref remoteEndPoint);
                    var jsonString = Encoding.UTF8.GetString(receivedData);

                    // Десериализуем сообщение
                    var message = JsonSerializer.Deserialize<Message>(jsonString);

                    // Обрабатываем запрос с продуктами
                    if (message?.Products != null)
                    {
                        ProcessRequest(remoteEndPoint, message.Products);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Помилка парсингу JSON від {remoteEndPoint}: {ex.Message}");
                    var errorResponse = new Message
                    {
                        Exception = "Неправильний формат запиту",
                        Recipes = new List<Dish>()
                    };
                    SendResponse(errorResponse, remoteEndPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка обробки запиту від {remoteEndPoint}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критична помилка сервера: {ex.Message}");
        }
        finally
        {
            Server?.Close();
        }
    }
}
