using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using HtmlAgilityPack;
using Swiftember.Models;

namespace Swiftember.Services;

public class StravaLeaderboardParser
{
    public static WeeklyLeaderboard ParseFile(string filePath, int weekNumber = 1)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".json")
        {
            return ParseJson(filePath, weekNumber);
        }
        else if (ext == ".csv")
        {
            return ParseCsv(filePath, weekNumber);
        }
        else if (ext == ".tsv" || ext == ".txt")
        {
            return ParseTsv(filePath, weekNumber);
        }
        else if (ext == ".html" || ext == ".htm")
        {
            return ParseHtml(filePath, weekNumber);
        }
        else
        {
            // Default attempt TSV / CSV / text
            string content = File.ReadAllText(filePath);
            if (content.Contains('\t')) return ParseTsv(filePath, weekNumber);
            return ParseCsv(filePath, weekNumber);
        }
    }

    public static WeeklyLeaderboard ParseTsv(string tsvPath, int weekNumber = 1)
    {
        var leaderboard = new WeeklyLeaderboard { WeekNumber = weekNumber };
        var lines = File.ReadAllLines(tsvPath);
        int rank = 1;

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine)) continue;
            var parts = rawLine.Split('\t').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
            if (parts.Count < 2) continue;

            // Skip header if first column is "Rank"
            if (parts[0].Equals("Rank", StringComparison.OrdinalIgnoreCase)) continue;

            int parsedRank = rank++;
            string name = parts.Count > 1 ? CleanAthleteName(parts[1]) : $"Athlete {parsedRank}";
            string distStr = parts.Count > 2 ? parts[2] : "0";
            string runsStr = parts.Count > 3 ? parts[3] : "1";
            string longestStr = parts.Count > 4 ? parts[4] : "0";
            string paceStr = string.Empty;
            string elevStr = "0";

            if (parts.Count > 5)
            {
                if (parts[5].Contains("/km") || parts[5].Contains("/mi"))
                {
                    paceStr = parts[5];
                    if (parts.Count > 6) elevStr = parts[6];
                }
                else
                {
                    elevStr = parts[5];
                    if (parts.Count > 6) paceStr = parts[6];
                }
            }

            double dist = ParseNumeric(distStr);
            int runs = int.TryParse(Regex.Replace(runsStr, @"[^\d]", ""), out int r) ? r : 1;
            double longest = ParseNumeric(longestStr);
            double elev = ParseNumeric(elevStr);

            leaderboard.Athletes.Add(new StravaAthleteRecord
            {
                Rank = parsedRank,
                AthleteName = name,
                DistanceKm = dist,
                ActivitiesCount = runs,
                LongestActivityKm = longest,
                ElevationGainM = elev,
                PaceFormatted = paceStr
            });
        }

        leaderboard.Athletes = leaderboard.Athletes.OrderByDescending(a => a.DistanceKm).ToList();
        for (int i = 0; i < leaderboard.Athletes.Count; i++)
        {
            leaderboard.Athletes[i].Rank = i + 1;
        }

        RecalculateTotals(leaderboard);
        return leaderboard;
    }

    public static string CleanAthleteName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Athlete";
        raw = raw.Trim();

        string[] words = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2 && words.Length % 2 == 0)
        {
            int half = words.Length / 2;
            bool isDuplicate = true;
            for (int i = 0; i < half; i++)
            {
                if (!words[i].Equals(words[i + half], StringComparison.OrdinalIgnoreCase))
                {
                    isDuplicate = false;
                    break;
                }
            }
            if (isDuplicate)
            {
                return string.Join(" ", words.Take(half));
            }
        }
        return raw;
    }

    public static WeeklyLeaderboard ParseJson(string jsonPath, int weekNumber = 1)
    {
        string json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var leaderboard = new WeeklyLeaderboard { WeekNumber = weekNumber };

        if (root.TryGetProperty("ClubName", out var cn)) leaderboard.ClubName = cn.GetString() ?? "Birmingham Swifts";
        if (root.TryGetProperty("DateRange", out var dr)) leaderboard.DateRange = dr.GetString() ?? string.Empty;
        if (root.TryGetProperty("StartDate", out var sd)) leaderboard.StartDate = sd.GetString() ?? string.Empty;
        if (root.TryGetProperty("EndDate", out var ed)) leaderboard.EndDate = ed.GetString() ?? string.Empty;

        if (root.TryGetProperty("Athletes", out var athletesArray) && athletesArray.ValueKind == JsonValueKind.Array)
        {
            int rank = 1;
            foreach (var elem in athletesArray.EnumerateArray())
            {
                var athlete = new StravaAthleteRecord
                {
                    Rank = elem.TryGetProperty("Rank", out var r) ? r.GetInt32() : rank++,
                    AthleteName = elem.TryGetProperty("AthleteName", out var an) ? an.GetString() ?? "Athlete" : "Athlete",
                    DistanceKm = elem.TryGetProperty("DistanceKm", out var d) ? d.GetDouble() : 0.0,
                    ActivitiesCount = elem.TryGetProperty("ActivitiesCount", out var a) ? a.GetInt32() : 0,
                    LongestActivityKm = elem.TryGetProperty("LongestActivityKm", out var l) ? l.GetDouble() : 0.0,
                    ElevationGainM = elem.TryGetProperty("ElevationGainM", out var eg) ? eg.GetDouble() : 0.0,
                    PaceFormatted = elem.TryGetProperty("PaceFormatted", out var p) ? p.GetString() ?? string.Empty : string.Empty,
                    ProfileUrl = elem.TryGetProperty("ProfileUrl", out var pu) ? pu.GetString() ?? string.Empty : string.Empty
                };
                leaderboard.Athletes.Add(athlete);
            }
        }

        RecalculateTotals(leaderboard);
        return leaderboard;
    }

    public static WeeklyLeaderboard ParseCsv(string csvPath, int weekNumber = 1)
    {
        var leaderboard = new WeeklyLeaderboard { WeekNumber = weekNumber };
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, config);

        var records = new List<StravaAthleteRecord>();
        int rank = 1;

        if (csv.Read())
        {
            csv.ReadHeader();
            while (csv.Read())
            {
                string name = csv.GetField("Athlete") ?? csv.GetField("Name") ?? csv.GetField("AthleteName") ?? $"Athlete {rank}";
                string distStr = csv.GetField("Distance") ?? csv.GetField("DistanceKm") ?? "0";
                string actStr = csv.GetField("Activities") ?? csv.GetField("Runs") ?? csv.GetField("Count") ?? "0";
                string longStr = csv.GetField("Longest") ?? csv.GetField("LongestRun") ?? "0";
                string elevStr = csv.GetField("Elevation") ?? csv.GetField("ElevationGain") ?? "0";
                string pace = csv.GetField("Pace") ?? csv.GetField("AvgPace") ?? string.Empty;

                double dist = ParseNumeric(distStr);
                int acts = int.TryParse(Regex.Replace(actStr, @"[^\d]", ""), out int a) ? a : 0;
                double longest = ParseNumeric(longStr);
                double elev = ParseNumeric(elevStr);

                records.Add(new StravaAthleteRecord
                {
                    Rank = rank++,
                    AthleteName = name,
                    DistanceKm = dist,
                    ActivitiesCount = acts,
                    LongestActivityKm = longest,
                    ElevationGainM = elev,
                    PaceFormatted = pace
                });
            }
        }

        leaderboard.Athletes = records.OrderByDescending(a => a.DistanceKm).ToList();
        for (int i = 0; i < leaderboard.Athletes.Count; i++)
        {
            leaderboard.Athletes[i].Rank = i + 1;
        }

        RecalculateTotals(leaderboard);
        return leaderboard;
    }

    public static WeeklyLeaderboard ParseHtml(string htmlPath, int weekNumber = 1)
    {
        var leaderboard = new WeeklyLeaderboard { WeekNumber = weekNumber };
        string html = File.ReadAllText(htmlPath);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Find leaderboard table
        var rows = doc.DocumentNode.SelectNodes("//table[contains(@class, 'leaderboard')]//tr")
                   ?? doc.DocumentNode.SelectNodes("//table//tr");

        if (rows != null)
        {
            int rank = 1;
            foreach (var tr in rows)
            {
                var tds = tr.SelectNodes("td");
                if (tds == null || tds.Count < 3) continue;

                string name = tds[1].InnerText.Trim();
                string distStr = tds[2].InnerText.Trim();
                string actsStr = tds.Count > 3 ? tds[3].InnerText.Trim() : "1";
                string longStr = tds.Count > 4 ? tds[4].InnerText.Trim() : "0";
                string elevStr = tds.Count > 5 ? tds[5].InnerText.Trim() : "0";

                double dist = ParseNumeric(distStr);
                int acts = int.TryParse(Regex.Replace(actsStr, @"[^\d]", ""), out int a) ? a : 1;
                double longest = ParseNumeric(longStr);
                double elev = ParseNumeric(elevStr);

                leaderboard.Athletes.Add(new StravaAthleteRecord
                {
                    Rank = rank++,
                    AthleteName = name,
                    DistanceKm = dist,
                    ActivitiesCount = acts,
                    LongestActivityKm = longest,
                    ElevationGainM = elev
                });
            }
        }

        leaderboard.Athletes = leaderboard.Athletes.OrderByDescending(a => a.DistanceKm).ToList();
        for (int i = 0; i < leaderboard.Athletes.Count; i++)
        {
            leaderboard.Athletes[i].Rank = i + 1;
        }

        RecalculateTotals(leaderboard);
        return leaderboard;
    }

    private static double ParseNumeric(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return 0.0;
        string cleaned = Regex.Replace(str, @"[^\d\.\,]", "").Replace(",", "");
        return double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out double val) ? val : 0.0;
    }

    private static void RecalculateTotals(WeeklyLeaderboard lb)
    {
        lb.TotalClubDistanceKm = lb.Athletes.Sum(a => a.DistanceKm);
        lb.TotalClubElevationM = lb.Athletes.Sum(a => a.ElevationGainM);
        lb.TotalClubActivities = lb.Athletes.Sum(a => a.ActivitiesCount);
    }
}
