using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Swiftember.Models;
using Swiftember.Services;

namespace Swiftember;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=========================================================================================");
        Console.WriteLine("             SWIFTEMBER — STRAVA CLUB CHALLENGE & LEADERBOARD SYSTEM                    ");
        Console.WriteLine("=========================================================================================");

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string configPath = Path.Combine(baseDir, "appsettings.json");
        if (!File.Exists(configPath)) configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        string downloadFolder = "~/Downloads";
        string pdfPattern = "Swiftember_Week_{0}_Leaderboard.pdf";
        string excelPattern = "Swiftember_Week_{0}_Leaderboard.xlsx";

        if (File.Exists(configPath))
        {
            try
            {
                string json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("DownloadFolder", out var df)) downloadFolder = df.GetString() ?? downloadFolder;
                if (root.TryGetProperty("PdfOutputPattern", out var pop)) pdfPattern = pop.GetString() ?? pdfPattern;
                if (root.TryGetProperty("ExcelOutputPattern", out var eop)) excelPattern = eop.GetString() ?? excelPattern;
            }
            catch { }
        }

        string? inputFile = null;
        int weekNumber = 1;
        bool generateExcel = true;
        bool generatePdf = true;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if ((arg.Equals("--file", StringComparison.OrdinalIgnoreCase) || arg.Equals("-f", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                inputFile = args[++i];
            }
            else if ((arg.Equals("--week", StringComparison.OrdinalIgnoreCase) || arg.Equals("-w", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                if (int.TryParse(args[++i], out int w)) weekNumber = w;
            }
            else if (arg.Equals("--no-excel", StringComparison.OrdinalIgnoreCase))
            {
                generateExcel = false;
            }
            else if (arg.Equals("--no-pdf", StringComparison.OrdinalIgnoreCase))
            {
                generatePdf = false;
            }
            else if (!arg.StartsWith("-"))
            {
                inputFile = arg;
            }
        }

        if (string.IsNullOrEmpty(inputFile))
        {
            // Default sample dataset
            inputFile = Path.Combine(Directory.GetCurrentDirectory(), "data", $"sample_week{weekNumber}.csv");
            if (!File.Exists(inputFile))
            {
                inputFile = Path.Combine(baseDir, "data", $"sample_week{weekNumber}.csv");
            }
        }

        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"[Error] Input leaderboard data file not found: {inputFile}");
            Console.WriteLine("Usage: dotnet run -- --file <path-to-csv-or-json> --week <1-5>");
            return;
        }

        Console.WriteLine($"Parsing Strava weekly leaderboard from:\n  {inputFile}\n");

        var currentWeek = StravaLeaderboardParser.ParseFile(inputFile, weekNumber);
        SwiftemberRankingEngine.ApplyWeeklyPoints(currentWeek);

        Console.WriteLine($"Club:                            {currentWeek.ClubName}");
        Console.WriteLine($"Challenge Week:                  Week {currentWeek.WeekNumber}");
        Console.WriteLine($"Active Club Athletes:            {currentWeek.ActiveAthletesCount:N0}");
        Console.WriteLine($"Total Club Distance:             {currentWeek.TotalClubDistanceKm:N1} km");
        Console.WriteLine($"Total Club Elevation:            {currentWeek.TotalClubElevationM:N0} m");
        Console.WriteLine($"Total Club Runs/Activities:      {currentWeek.TotalClubActivities:N0}\n");

        // Save Week to Challenge History
        var historyService = new SwiftemberHistoryService();
        historyService.SaveWeek(currentWeek);

        var allWeeks = historyService.LoadAllWeeks();
        var overallStandings = SwiftemberRankingEngine.ComputeOverallStandings(allWeeks);

        Console.WriteLine("-----------------------------------------------------------------------------------------");
        Console.WriteLine("Pos | Athlete Name              | Distance  | Runs | Longest  | Elev (m) | Points");
        Console.WriteLine("-----------------------------------------------------------------------------------------");

        foreach (var a in currentWeek.Athletes.Take(10))
        {
            Console.WriteLine($"{a.Rank,3} | {a.AthleteName,-25} | {a.DistanceKm,6:N1} km | {a.ActivitiesCount,4} | {a.LongestActivityKm,5:N1} km | {a.ElevationGainM,8:N0} | {a.Points,6:N0}");
        }

        if (currentWeek.Athletes.Count > 10)
        {
            Console.WriteLine($"... and {currentWeek.Athletes.Count - 10} more athletes.");
        }
        Console.WriteLine("-----------------------------------------------------------------------------------------\n");

        string resolvedDownloadDir = Path.GetFullPath(downloadFolder.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
        string pdfPath = Path.Combine(resolvedDownloadDir, string.Format(pdfPattern, weekNumber));
        string excelPath = Path.Combine(resolvedDownloadDir, string.Format(excelPattern, weekNumber));

        if (generatePdf)
        {
            SwiftemberReportGenerator.GeneratePdf(currentWeek, overallStandings, pdfPath);
        }

        if (generateExcel)
        {
            SwiftemberReportGenerator.GenerateExcel(currentWeek, overallStandings, excelPath);
        }

        Console.WriteLine("\nSwiftember leaderboard processed successfully!");
        Console.WriteLine("=========================================================================================");
    }
}
