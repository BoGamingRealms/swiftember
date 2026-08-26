using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Swiftember.Models;
using Swiftember.Services;

namespace Swiftember;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=========================================================================================");
        Console.WriteLine("             SWIFTEMBER — STRAVA CLUB CHALLENGE & LEADERBOARD SYSTEM                    ");
        Console.WriteLine("=========================================================================================");

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string configPath = Path.Combine(baseDir, "appsettings.json");
        if (!File.Exists(configPath)) configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        string clubName = "Birmingham Swifts";
        string clubId = "202685";
        string stravaToken = string.Empty;
        string stravaCookie = string.Empty;
        string onlineUrl = string.Empty;
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
                if (root.TryGetProperty("ClubName", out var cn)) clubName = cn.GetString() ?? clubName;
                if (root.TryGetProperty("StravaClubId", out var sci)) clubId = sci.GetString() ?? clubId;
                if (root.TryGetProperty("StravaAccessToken", out var sat)) stravaToken = sat.GetString() ?? stravaToken;
                if (root.TryGetProperty("StravaSessionCookie", out var ssc)) stravaCookie = ssc.GetString() ?? stravaCookie;
                if (root.TryGetProperty("GoogleSheetUrl", out var gsu)) onlineUrl = gsu.GetString() ?? onlineUrl;
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
        bool useApi = false;
        bool useCookie = false;

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
            else if ((arg.Equals("--token", StringComparison.OrdinalIgnoreCase) || arg.Equals("-t", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                stravaToken = args[++i];
                useApi = true;
            }
            else if (arg.Equals("--api", StringComparison.OrdinalIgnoreCase))
            {
                useApi = true;
            }
            else if ((arg.Equals("--cookie", StringComparison.OrdinalIgnoreCase) || arg.Equals("-c", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                stravaCookie = args[++i];
                useCookie = true;
            }
            else if ((arg.Equals("--club", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                clubId = args[++i];
            }
            else if ((arg.Equals("--url", StringComparison.OrdinalIgnoreCase) || arg.Equals("-u", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                onlineUrl = args[++i];
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

        WeeklyLeaderboard currentWeek;

        if (useApi && !string.IsNullOrEmpty(stravaToken))
        {
            Console.WriteLine($"Fetching live data directly via Strava REST API for Club ID: {clubId}...");
            var apiService = new StravaApiService();
            currentWeek = await apiService.FetchClubActivitiesViaApiAsync(clubId, stravaToken, weekNumber);
            currentWeek.ClubName = clubName;
        }
        else if (useCookie && !string.IsNullOrEmpty(stravaCookie))
        {
            Console.WriteLine($"Fetching live leaderboard page directly from Strava for Club ID: {clubId}...");
            var apiService = new StravaApiService();
            currentWeek = await apiService.FetchWebLeaderboardAsync(clubId, stravaCookie, weekNumber);
            currentWeek.ClubName = clubName;
        }
        else if (!string.IsNullOrEmpty(onlineUrl))
        {
            Console.WriteLine($"Downloading live leaderboard data from Google Sheet / Online URL:\n  {onlineUrl}...");
            using var http = new HttpClient();
            string csvContent = await http.GetStringAsync(onlineUrl);
            string tempCsv = Path.GetTempFileName();
            File.WriteAllText(tempCsv, csvContent);
            currentWeek = StravaLeaderboardParser.ParseCsv(tempCsv, weekNumber);
            currentWeek.ClubName = clubName;
            try { File.Delete(tempCsv); } catch { }
        }
        else
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                inputFile = Path.Combine(Directory.GetCurrentDirectory(), "data", $"sample_week{weekNumber}.csv");
                if (!File.Exists(inputFile))
                {
                    inputFile = Path.Combine(baseDir, "data", $"sample_week{weekNumber}.csv");
                }
            }

            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"[Error] Input leaderboard data file not found: {inputFile}");
                Console.WriteLine("\nOptions to load data:");
                Console.WriteLine("  1. Direct Strava API:   dotnet run -- --api --token <your_strava_token> --club <clubId>");
                Console.WriteLine("  2. Direct Web Session:  dotnet run -- --cookie <strava_cookie> --club <clubId>");
                Console.WriteLine("  3. Live Google Sheet:   dotnet run -- --url <google_sheet_csv_url>");
                Console.WriteLine("  4. Local File:          dotnet run -- --file <path_to_csv_or_json>");
                return;
            }

            Console.WriteLine($"Parsing Strava weekly leaderboard from:\n  {inputFile}\n");
            currentWeek = StravaLeaderboardParser.ParseFile(inputFile, weekNumber);
            currentWeek.ClubName = clubName;
        }

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
