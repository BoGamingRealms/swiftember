using System;
using System.Collections.Generic;
using System.Linq;
using Swiftember.Models;

namespace Swiftember.Services;

public class SwiftemberRankingEngine
{
    public static void ApplyWeeklyPoints(WeeklyLeaderboard week)
    {
        // Standard F1/Club points table for top positions, +1 point per km
        int[] topPoints = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };

        for (int i = 0; i < week.Athletes.Count; i++)
        {
            var athlete = week.Athletes[i];
            double positionPoints = (i < topPoints.Length) ? topPoints[i] : 0;
            double distancePoints = Math.Round(athlete.DistanceKm, 1);
            double consistencyBonus = athlete.ActivitiesCount >= 3 ? 5.0 : 0.0;

            athlete.Points = positionPoints + distancePoints + consistencyBonus;
        }
    }

    public static List<SwiftemberOverallStats> ComputeOverallStandings(List<WeeklyLeaderboard> allWeeks)
    {
        var athleteMap = new Dictionary<string, SwiftemberOverallStats>(StringComparer.OrdinalIgnoreCase);

        foreach (var week in allWeeks)
        {
            ApplyWeeklyPoints(week);

            foreach (var a in week.Athletes)
            {
                if (!athleteMap.TryGetValue(a.AthleteName, out var stats))
                {
                    stats = new SwiftemberOverallStats
                    {
                        AthleteName = a.AthleteName
                    };
                    athleteMap[a.AthleteName] = stats;
                }

                stats.TotalDistanceKm += a.DistanceKm;
                stats.TotalElevationGainM += a.ElevationGainM;
                stats.TotalActivitiesCount += a.ActivitiesCount;
                stats.WeeksParticipated++;
                stats.OverallPoints += a.Points;

                if (a.DistanceKm > stats.BestWeeklyDistanceKm)
                {
                    stats.BestWeeklyDistanceKm = a.DistanceKm;
                }

                stats.WeeklyDistanceKm[week.WeekNumber] = a.DistanceKm;
            }
        }

        var standings = athleteMap.Values.ToList();

        // Compute Distance Ranks
        var byDistance = standings.OrderByDescending(s => s.TotalDistanceKm).ToList();
        for (int i = 0; i < byDistance.Count; i++) byDistance[i].DistanceRank = i + 1;

        // Compute Elevation Ranks
        var byElevation = standings.OrderByDescending(s => s.TotalElevationGainM).ToList();
        for (int i = 0; i < byElevation.Count; i++) byElevation[i].ElevationRank = i + 1;

        // Compute Activity Ranks
        var byActivity = standings.OrderByDescending(s => s.TotalActivitiesCount).ToList();
        for (int i = 0; i < byActivity.Count; i++) byActivity[i].ActivityRank = i + 1;

        // Final sorting by Total Distance (or Overall Points)
        return standings.OrderByDescending(s => s.TotalDistanceKm).ToList();
    }
}
