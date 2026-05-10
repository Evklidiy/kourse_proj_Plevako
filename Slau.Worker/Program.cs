using System;
using System.Diagnostics;
using Slau.Worker.Services;

namespace Slau.Worker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Вычислительный узел (Worker)";
            Console.WriteLine("=== НАСТРОЙКА ОГРАНИЧЕНИЙ РЕСУРСОВ ===");

            // 1. Ограничение CPU
            int totalCores = Environment.ProcessorCount;
            Console.Write($"Доступно ядер: {totalCores}. Сколько использовать для расчета? (1-{totalCores}): ");
            if (!int.TryParse(Console.ReadLine(), out int cores) || cores < 1) cores = totalCores;

            // 2. Ограничение RAM
            Console.Write("Введите лимит оперативной памяти для этого узла (в МБ, например 2048): ");
            if (!long.TryParse(Console.ReadLine(), out long ramMb) || ramMb < 256) ramMb = 4096;

            // 3. Порт
            Console.Write("Введите порт (по умолчанию 5000): ");
            if (!int.TryParse(Console.ReadLine(), out int port)) port = 5000;

            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine($"ЯДЕР В РАБОТЕ: {cores}");
            Console.WriteLine($"ЛИМИТ ПАМЯТИ: {ramMb} МБ");
            Console.WriteLine($"ПОРТ:          {port}");
            Console.WriteLine("========================================");

            try
            {
                WorkerServer server = new WorkerServer(port);
                // Передаем настройки в сервер
                server.SetupLimits(cores, ramMb);
                server.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[КРИТИЧЕСКАЯ ОШИБКА] {ex.Message}");
                Console.ReadLine();
            }
        }
    }
}