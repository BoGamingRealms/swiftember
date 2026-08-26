using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Swiftember.Models;

namespace Swiftember.Services;

public class StravaApiService
{
    private readonly HttpClient _httpClient;

    public StravaApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    /// <summary>
    /// Fetches leaderboard directly via Strava REST API using an Access Token or Client credentials
    /// </summary>
    public async Task<WeeklyLeaderboard> FetchClubActivitiesViaApiAsync(string clubId, string accessToken, int weekNumber = 1)
    {
        var leaderboard = new WeeklyLeaderboard
        {
            ClubId = clubId,
            WeekNumber = weekNumber
        };

        string url = $"https://www.strava.com/api/v3/clubs/{clubId}/activities?per_page=200";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Strava API request failed ({response.StatusCode}): {errorContent}");
        }

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var athleteMap = new Dictionary<string, StravaAthleteRecord>(StringComparer.OrdinalIgnoreCase);

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var act in doc.RootElement.EnumerateArray())
            {
                string type = act.TryGetProperty("type", out var t) ? t.GetString() ?? "Run" : "Run";
                if (!type.Equals("Run", StringComparison.OrdinalIgnoreCase)) continue;

                string athleteName = "Athlete";
                if (act.TryGetProperty("athlete", out var athleteObj))
                {
                    string fname = athleteObj.TryGetProperty("firstname", out var fn) ? fn.GetString() ?? "" : "";
                    string lname = athleteObj.TryGetProperty("lastname", out var ln) ? ln.GetString() ?? "" : "";
                    athleteName = $"{fname} {lname}".Trim();
                }

                double distanceMeters = act.TryGetProperty("distance", out var d) ? d.GetDouble() : 0.0;
                double distanceKm = Math.Round(distanceMeters / 1000.0, 2);
                double totalElevation = act.TryGetProperty("total_elevation_gain", out var eg) ? eg.GetDouble() : 0.0;

                if (!athleteMap.TryGetValue(athleteName, out var record))
                {
                    record = new StravaAthleteRecord
                    {
                        AthleteName = athleteName
                    };
                    athleteMap[athleteName] = record;
                }

                record.DistanceKm += distanceKm;
                record.ActivitiesCount++;
                record.ElevationGainM += totalElevation;
                if (distanceKm > record.LongestActivityKm)
                {
                    record.LongestActivityKm = distanceKm;
                }
            }
        }

        leaderboard.Athletes = athleteMap.Values
            .OrderByDescending(a => a.DistanceKm)
            .ToList();

        for (int i = 0; i < leaderboard.Athletes.Count; i++)
        {
            leaderboard.Athletes[i].Rank = i + 1;
        }

        leaderboard.TotalClubDistanceKm = leaderboard.Athletes.Sum(a => a.DistanceKm);
        leaderboard.TotalClubElevationM = leaderboard.Athletes.Sum(a => a.ElevationGainM);
        leaderboard.TotalClubActivities = leaderboard.Athletes.Sum(a => a.ActivitiesCount);

        return leaderboard;
    }

    /// <summary>
    /// Fetches the live web leaderboard directly using Strava web session cookies
    /// </summary>
    public async Task<WeeklyLeaderboard> FetchWebLeaderboardAsync(string clubId, string sessionCookie, int weekNumber = 1)
    {
        string url = $"https://www.strava.com/clubs/{clubId}/leaderboard";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", $"_strava4_session={sessionCookie}");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch Strava club leaderboard page: {response.StatusCode}");
        }

        string html = await response.Content.ReadAsStringAsync();
        return StravaLeaderboardParser.ParseHtml(html, weekNumber);
    }
}
