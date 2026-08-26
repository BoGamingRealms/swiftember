using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Swiftember.Models;

namespace Swiftember.Services;

public class SwiftemberHistoryService
{
    private readonly string _historyPath;

    public SwiftemberHistoryService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _historyPath = customPath;
        }
        else
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _historyPath = Path.Combine(baseDir, "data", "swiftember_history.json");
        }
    }

    public List<WeeklyLeaderboard> LoadAllWeeks()
    {
        if (!File.Exists(_historyPath))
        {
            string fallback = Path.Combine(Directory.GetCurrentDirectory(), "data", "swiftember_history.json");
            if (File.Exists(fallback))
            {
                return ReadFromFile(fallback);
            }
            return new List<WeeklyLeaderboard>();
        }
        return ReadFromFile(_historyPath);
    }

    private static List<WeeklyLeaderboard> ReadFromFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<WeeklyLeaderboard>>(json);
            return list ?? new List<WeeklyLeaderboard>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Failed to read Swiftember history: {ex.Message}");
            return new List<WeeklyLeaderboard>();
        }
    }

    public void SaveWeek(WeeklyLeaderboard week)
    {
        var allWeeks = LoadAllWeeks();

        int existingIdx = allWeeks.FindIndex(w => w.WeekNumber == week.WeekNumber);
        if (existingIdx >= 0)
        {
            allWeeks[existingIdx] = week;
        }
        else
        {
            allWeeks.Add(week);
        }

        allWeeks = allWeeks.OrderBy(w => w.WeekNumber).ToList();

        string? dir = Path.GetDirectoryName(_historyPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(allWeeks, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_historyPath, json);
    }
}
