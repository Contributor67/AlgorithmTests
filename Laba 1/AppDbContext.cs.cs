using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Laba_1
{
    // Модель сессии замера для отображения в выпадающем списке
    public class ExperimentSession
    {
        public DateTime ExperimentDate { get; set; }
        public string AlgorithmName { get; set; } = string.Empty;
        public int MinN { get; set; }
        public int MaxN { get; set; }
        public int TotalRecords { get; set; }

        // Оформление элемента в выпадающем списке (Дата | Алгоритм | Диапазон N)
        public string DisplayText => $"{ExperimentDate.ToLocalTime():dd.MM.yyyy HH:mm:ss} | {AlgorithmName} (N={MinN}..{MaxN})";

        public override string ToString() => DisplayText;
    }

    // Схема таблицы результатов замеров
    public class BenchmarkResult
    {
        public int Id { get; set; }
        public string AlgorithmName { get; set; } = string.Empty; // Название алгоритма
        public int N { get; set; }                               // Размер входных данных (N)
        public int RunNumber { get; set; }                       // Номер запуска (1..5)
        public double ExecutionTimeMs { get; set; }              // Затраченное время (мс)
        public long? StepCount { get; set; }                    // Количество шагов
        public DateTime ExperimentDate { get; set; } = DateTime.UtcNow; // Дата эксперимента
    }

    public class AppDbContext : DbContext
    {
        public DbSet<BenchmarkResult> BenchmarkResults { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Укажите свои данные для подключения к PostgreSQL
            string connectionString = "Host=localhost;Port=5432;Database=AlgorithmBenchmarksDb;Username=postgres;Password=your_password";
            optionsBuilder.UseNpgsql(connectionString);
        }

        public static async Task InitDatabaseAsync()
        {
            using var db = new AppDbContext();
            await db.Database.EnsureCreatedAsync();
        }

        public static async Task SaveResultsAsync(IEnumerable<BenchmarkResult> results)
        {
            using var db = new AppDbContext();
            await db.BenchmarkResults.AddRangeAsync(results);
            await db.SaveChangesAsync();
        }

        // 1. Получить список всех сохраненных сессий замеров (для выпадающего списка)
        public static async Task<List<ExperimentSession>> GetExperimentSessionsAsync()
        {
            using var db = new AppDbContext();

            var rawData = await db.BenchmarkResults
                .Select(r => new { r.ExperimentDate, r.AlgorithmName, r.N })
                .ToListAsync();

            return rawData
                .GroupBy(r => new { r.ExperimentDate, r.AlgorithmName })
                .Select(g => new ExperimentSession
                {
                    ExperimentDate = g.Key.ExperimentDate,
                    AlgorithmName = g.Key.AlgorithmName,
                    MinN = g.Min(x => x.N),
                    MaxN = g.Max(x => x.N),
                    TotalRecords = g.Count()
                })
                .OrderByDescending(s => s.ExperimentDate)
                .ToList();
        }

        // 2. Получить замеры для конкретной выбранной сессии
        public static async Task<List<BenchmarkResult>> GetResultsForSessionAsync(DateTime date, string algorithmName)
        {
            using var db = new AppDbContext();
            return await db.BenchmarkResults
                .Where(r => r.ExperimentDate == date && r.AlgorithmName == algorithmName)
                .OrderBy(r => r.N)
                .ThenBy(r => r.RunNumber)
                .ToListAsync();
        }
    }
}