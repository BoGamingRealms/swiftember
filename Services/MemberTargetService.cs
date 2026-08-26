using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Swiftember.Services;

public class MemberTargetService
{
    private readonly Dictionary<string, double> _targets = new(StringComparer.OrdinalIgnoreCase);
    private readonly double _defaultTargetKm = 100.0;
    private readonly string _targetsFilePath;

    public MemberTargetService(string? customPath = null)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _targetsFilePath = customPath ?? Path.Combine(baseDir, "data", "member_targets.json");

        if (!File.Exists(_targetsFilePath))
        {
            string fallback = Path.Combine(Directory.GetCurrentDirectory(), "data", "member_targets.json");
            if (File.Exists(fallback)) _targetsFilePath = fallback;
        }

        LoadTargets();
    }

    private void LoadTargets()
    {
        if (File.Exists(_targetsFilePath))
        {
            try
            {
                string json = File.ReadAllText(_targetsFilePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, double>>(json);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        _targets[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch { }
        }
    }

    public double GetTargetKm(string athleteName)
    {
        if (_targets.TryGetValue(athleteName, out double target) && target > 0)
        {
            return target;
        }
        return _defaultTargetKm;
    }

    public void SetTargetKm(string athleteName, double targetKm)
    {
        _targets[athleteName] = targetKm;
        SaveTargets();
    }

    public void SaveTargets()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_targetsFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(_targets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_targetsFilePath, json);
        }
        catch { }
    }
}
