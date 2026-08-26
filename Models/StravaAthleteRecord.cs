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
}
