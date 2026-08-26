using System.Collections.Generic;

namespace Swiftember.Models;

public class WeeklyLeaderboard
{
    public int WeekNumber { get; set; } = 1;
    public string DateRange { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string ClubName { get; set; } = "Birmingham Swifts";
    public string ClubId { get; set; } = string.Empty;
    public double TotalClubDistanceKm { get; set; }
    public double TotalClubElevationM { get; set; }
    public int TotalClubActivities { get; set; }
    public int ActiveAthletesCount => Athletes.Count;
    public List<StravaAthleteRecord> Athletes { get; set; } = new();
}
