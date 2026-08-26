using System.Collections.Generic;

namespace Swiftember.Models;

public class SwiftemberOverallStats
{
    public string AthleteName { get; set; } = string.Empty;
    public double TotalDistanceKm { get; set; }
    public double TotalElevationGainM { get; set; }
    public int TotalActivitiesCount { get; set; }
    public int WeeksParticipated { get; set; }
    public double BestWeeklyDistanceKm { get; set; }
    public int DistanceRank { get; set; }
    public int ElevationRank { get; set; }
    public int ActivityRank { get; set; }
    public double OverallPoints { get; set; }
    public Dictionary<int, double> WeeklyDistanceKm { get; set; } = new();

    // Monthly Target & Effort Metrics
    public double MonthlyTargetKm { get; set; } = 100.0;
    public double TargetProgressPct => (MonthlyTargetKm > 0) ? (TotalDistanceKm / MonthlyTargetKm) * 100.0 : 0.0;
    public int EffortRank { get; set; }
    public double EffortScore { get; set; }
    public string PacingStatus { get; set; } = "On Track";
}
