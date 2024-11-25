using System;
using System.Collections.Generic;
using System.Linq;

namespace RestaurantBookingSystem
{
    public class Statistics
    {
        public int SuccessfulBookings { get; private set; }
        public int FailedBookings { get; private set; }

        public void IncrementSuccessful() => SuccessfulBookings++;
        public void IncrementFailed() => FailedBookings++;

        public void ShowStatistics()
        {
            Console.WriteLine("\n[Статистика бронювань]");
            Console.WriteLine($"  Успішні бронювання: {SuccessfulBookings}");
            Console.WriteLine($"  Невдалі бронювання: {FailedBookings}");
        }
    }

    public class Restaurant
    {
        public event Action<string> BookingNotification;
        public event Action<string> FreeTableNotification;

        private readonly int TotalTables;
        private readonly Dictionary<string, Dictionary<int, string>> timeSlotBookings = new Dictionary<string, Dictionary<int, string>>();
        private readonly Dictionary<(string, int), DateTime> tableReleaseTimes = new Dictionary<(string, int), DateTime>();

        public Restaurant(int totalTables)
        {
            TotalTables = totalTables;

            for (int hour = 18; hour <= 23; hour++)
            {
                timeSlotBookings[$"{hour:D2}:00"] = new Dictionary<int, string>();
                timeSlotBookings[$"{hour:D2}:30"] = new Dictionary<int, string>();
            }
        }

        public bool BookTable(Client client, out int tableIndex)
        {
            tableIndex = -1;
            if (!timeSlotBookings.ContainsKey(client.Time))
            {
                client.NotifyInvalidTime();
                return false;
            }

            var bookingsForTime = timeSlotBookings[client.Time];
            for (int i = 1; i <= TotalTables; i++)
            {
                if (!bookingsForTime.ContainsKey(i))
                {
                    bookingsForTime[i] = client.Name;
                    tableReleaseTimes[(client.Time, i)] = DateTime.Now.AddSeconds(30); // Столик буде звільнений через 30 секунд
                    tableIndex = i;
                    client.NotifyBookingSuccess(client.Time);
                    BookingNotification?.Invoke($"{client.Name} (VIP: {client.IsVIP}) забронював столик #{i} на {client.Time}.");
                    return true;
                }
            }

            client.NotifyNoTables();
            return false;
        }

        public void CheckAndReleaseTables()
        {
            var keysToRelease = tableReleaseTimes
                .Where(kv => kv.Value <= DateTime.Now)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in keysToRelease)
            {
                timeSlotBookings[key.Item1].Remove(key.Item2);
                tableReleaseTimes.Remove(key);
                FreeTableNotification?.Invoke($"Столик #{key.Item2} на {key.Item1} тепер вільний.");
            }
        }

        public void ShowStatus()
        {
            Console.WriteLine("\n[Статус столиків]");
            foreach (var timeSlot in timeSlotBookings)
            {
                Console.WriteLine($"  Час {timeSlot.Key}:");

                for (int i = 1; i <= TotalTables; i++)
                {
                    if (timeSlot.Value.ContainsKey(i))
                    {
                        // Відображення імені клієнта
                        string clientName = timeSlot.Value[i];
                        Console.WriteLine($"    Столик #{i}: {clientName} ");
                    }
                    else
                    {
                        Console.WriteLine($"    Столик #{i}: вільний");
                    }
                }
            }
        }
    }

    public class Client
    {
        public string Name { get; private set; }
        public string Time { get; private set; }
        public bool IsVIP { get; private set; }

        public Client(string name, string time, bool isVIP = false)
        {
            Name = name;
            Time = time;
            IsVIP = isVIP;
        }

        public void NotifyBookingSuccess(string time)
        {
            Console.WriteLine($"{Name} (VIP: {IsVIP}), ваш столик заброньовано на {time}.");
        }

        public void NotifyNoTables()
        {
            Console.WriteLine($"{Name} (VIP: {IsVIP}), вибачте, але всі столики на {Time} вже зайняті. Приходьте до нас іншого разу.");
        }

        public void NotifyInvalidTime()
        {
            Console.WriteLine($"{Name} (VIP: {IsVIP}), ви ввели некоректний час.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int totalTables;
            while (true)
            {
                Console.WriteLine("Введіть кількість столиків у ресторані:");
                string input = Console.ReadLine();
                if (int.TryParse(input, out totalTables) && totalTables > 0)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Некоректне число. Будь ласка, введіть ціле число більше за 0.");
                }
            }

            var restaurant = new Restaurant(totalTables);
            var statistics = new Statistics();
            var random = new Random();
            List<Client> queue = null;
            int actionsCounter = 0;

            restaurant.BookingNotification += message =>
            {
                Console.WriteLine($"[Ресторан]: {message}");
                statistics.IncrementSuccessful();
            };
            restaurant.FreeTableNotification += message => Console.WriteLine($"[Ресторан]: {message}");

            while (true)
            {
                Console.Clear();
                restaurant.CheckAndReleaseTables();
                Console.WriteLine("Меню:");
                Console.WriteLine("1. Створити чергу клієнтів");
                Console.WriteLine("2. Розпочати розсаджування клієнтів");
                Console.WriteLine("3. Показати статус столиків");
                Console.WriteLine("4. Показати статистику бронювань");
                Console.WriteLine("5. Завершити програму");
                Console.Write("Виберіть опцію: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        queue = GenerateQueue(random);
                        Console.WriteLine("Черга створена. Ось список клієнтів:");
                        ShowQueue(queue);
                        Console.WriteLine("Натисніть будь-яку клавішу для продовження...");
                        Console.ReadKey();
                        break;

                    case "2":
                        if (queue == null || queue.Count == 0)
                        {
                            Console.WriteLine("Черга порожня або не створена. Спочатку створіть чергу клієнтів.");
                            Console.WriteLine("Натисніть будь-яку клавішу для повернення в меню...");
                            Console.ReadKey();
                            break;
                        }

                        Console.WriteLine("Виберіть спосіб розсаджування:");
                        Console.WriteLine("1. Розсаджувати клієнтів вручну");
                        Console.WriteLine("2. Розсаджувати клієнтів автоматично");
                        Console.Write("Ваш вибір: ");
                        string seatingChoice = Console.ReadLine();

                        if (seatingChoice == "1")
                        {
                            while (queue.Count > 0)
                            {
                                actionsCounter++;

                                var client = queue[0];
                                Console.WriteLine($"\nОбробка клієнта: {client.Name} (VIP: {client.IsVIP}, Час: {client.Time})");

                                int tableIndex;
                                bool validInput = false;
                                while (!validInput)
                                {
                                    Console.WriteLine("Виберіть столик для цього клієнта (1- {0}):", totalTables);
                                    string tableInput = Console.ReadLine();

                                    if (int.TryParse(tableInput, out tableIndex) && tableIndex >= 1 && tableIndex <= totalTables)
                                    {
                                        validInput = true;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Некоректне введення. Спробуйте знову.");
                                    }
                                }

                                if (restaurant.BookTable(client, out tableIndex))
                                {
                                    queue.RemoveAt(0);
                                }
                                else
                                {
                                    statistics.IncrementFailed();
                                    queue.RemoveAt(0);
                                }
                            }
                        }
                        else if (seatingChoice == "2")
                        {
                            foreach (var client in queue)
                            {
                                int tableIndex;
                                // Якщо не вдалося забронювати столик для поточного клієнта, просто переходимо до наступного
                                if (!restaurant.BookTable(client, out tableIndex))
                                {
                                    continue; // Продовжуємо з наступним клієнтом
                                }
                            }
                            queue.Clear(); // Очищаємо чергу після автоматичного розсаджування
                        }
                        else
                        {
                            Console.WriteLine("Некоректний вибір. Спробуйте ще раз.");
                        }
                        Console.WriteLine("Натисніть будь-яку клавішу для повернення в меню...");
                        Console.ReadKey();
                        break;

                    case "3":
                        restaurant.ShowStatus();
                        Console.WriteLine("Натисніть будь-яку клавішу для продовження...");
                        Console.ReadKey();
                        break;

                    case "4":
                        statistics.ShowStatistics();
                        Console.WriteLine("Натисніть будь-яку клавішу для продовження...");
                        Console.ReadKey();
                        break;

                    case "5":
                        return;

                    default:
                        Console.WriteLine("Некоректний вибір. Спробуйте ще раз.");
                        break;
                }
            }
        }

        static List<Client> GenerateQueue(Random random)
        {
            string[] names = { "Олег", "Марія", "Дмитро", "Ірина", "Антон", "Оксана", "Юлія", "Богдан", "Наталія", "Максим" };
            string[] times = { "18:00", "18:30", "19:00", "19:30", "20:00", "20:30", "21:00", "21:30", "22:00", "22:30" };

            var queue = new List<Client>();
            int count = random.Next(5, 11);
            for (int i = 0; i < count; i++)
            {
                string name = names[random.Next(names.Length)];
                string time = times[random.Next(times.Length)];
                bool isVIP = random.Next(0, 2) == 0;

                queue.Add(new Client(name, time, isVIP));
            }

            return queue.OrderByDescending(c => c.IsVIP).ToList();
        }

        static void ShowQueue(List<Client> queue)
        {
            if (queue.Count == 0)
            {
                Console.WriteLine("Черга порожня.");
                return;
            }

            Console.WriteLine("Черга клієнтів:");
            foreach (var client in queue)
            {
                Console.WriteLine($"  {client.Name} (VIP: {client.IsVIP}, Час: {client.Time})");
            }
        }
    }
}
