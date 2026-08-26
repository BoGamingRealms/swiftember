namespace Swiftember.Models;

public class StravaAthleteRecord
{
    public int Rank { get; set; }
    public string AthleteName { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public int ActivitiesCount { get; set; }
    public double LongestActivityKm { get; set; }
    public double ElevationGainM { get; set; }
    public string PaceFormatted { get; set; } = string.Empty;
    public string ProfileUrl { get; set; } = string.Empty;
    public double Points { get; set; }

    // Monthly Target & Effort Metrics
    public double MonthlyTargetKm { get; set; } = 100.0;
    public double TargetProgressPct => (MonthlyTargetKm > 0) ? (DistanceKm / MonthlyTargetKm) * 100.0 : 0.0;
    public int EffortRank { get; set; }
    public double EffortScore { get; set; }
    public string PacingStatus { get; set; } = "On Track";
}
