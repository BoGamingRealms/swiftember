using System;
using System.Collections.Generic;
using System.Linq;
using Swiftember.Models;

namespace Swiftember.Services;

public class SwiftemberRankingEngine
{
    public static void ApplyWeeklyMetricsAndEffort(WeeklyLeaderboard week, MemberTargetService targetService)
    {
        // 1. Assign Targets & Calculate Effort
        double weekExpectedFraction = Math.Clamp(week.WeekNumber * 0.25, 0.25, 1.0);

        foreach (var athlete in week.Athletes)
        {
            athlete.MonthlyTargetKm = targetService.GetTargetKm(athlete.AthleteName);

            double expectedKm = athlete.MonthlyTargetKm * weekExpectedFraction;
            double onPaceRatio = (expectedKm > 0) ? (athlete.DistanceKm / expectedKm) * 100.0 : 0.0;
            double progressPct = athlete.TargetProgressPct;

            // Consistency factor (rewarding 3+ runs in a week)
            double consistencyBonus = athlete.ActivitiesCount >= 3 ? 5.0 : 0.0;

            // Effort Score = Progress % towards Monthly Target + On-Pace Bonus + Consistency
            athlete.EffortScore = progressPct + (onPaceRatio * 0.1) + consistencyBonus;

            // Pacing Status
            if (progressPct >= 100.0)
            {
                athlete.PacingStatus = "🏆 Achieved!";
            }
            else if (onPaceRatio >= 115.0)
            {
                athlete.PacingStatus = "🚀 Ahead of Goal";
            }
            else if (onPaceRatio >= 90.0)
            {
                athlete.PacingStatus = "🟢 On Track";
            }
            else if (onPaceRatio >= 60.0)
            {
                athlete.PacingStatus = "🟡 In Progress";
            }
            else
            {
                athlete.PacingStatus = "🔴 Needs Boost";
            }
        }

        // 2. Compute Effort Rankings (Sort by Progress % towards Monthly Target, tiebreak by Target Ambition)
        var byEffort = week.Athletes
            .OrderByDescending(a => a.TargetProgressPct)
            .ThenByDescending(a => a.MonthlyTargetKm)
            .ThenByDescending(a => a.ActivitiesCount)
            .ToList();

        for (int i = 0; i < byEffort.Count; i++)
        {
            byEffort[i].EffortRank = i + 1;
        }

        // 3. Apply F1/Club points based on Effort Rank
        int[] topPoints = { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 };
        for (int i = 0; i < week.Athletes.Count; i++)
        {
            var a = week.Athletes[i];
            double positionPoints = (a.EffortRank <= topPoints.Length) ? topPoints[a.EffortRank - 1] : 0;
            double progressPoints = Math.Round(a.TargetProgressPct, 1);
            double consistencyBonus = a.ActivitiesCount >= 3 ? 5.0 : 0.0;

            a.Points = positionPoints + progressPoints + consistencyBonus;
        }
    }

    public static List<SwiftemberOverallStats> ComputeOverallStandings(List<WeeklyLeaderboard> allWeeks, MemberTargetService targetService)
    {
        var athleteMap = new Dictionary<string, SwiftemberOverallStats>(StringComparer.OrdinalIgnoreCase);

        foreach (var week in allWeeks)
        {
            ApplyWeeklyMetricsAndEffort(week, targetService);

            foreach (var a in week.Athletes)
            {
                if (!athleteMap.TryGetValue(a.AthleteName, out var stats))
                {
                    stats = new SwiftemberOverallStats
                    {
                        AthleteName = a.AthleteName,
                        MonthlyTargetKm = targetService.GetTargetKm(a.AthleteName)
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

        // Calculate Overall Effort, Pacing Status, and Ranks
        int maxWeek = allWeeks.Count > 0 ? allWeeks.Max(w => w.WeekNumber) : 1;
        double overallExpectedFraction = Math.Clamp(maxWeek * 0.25, 0.25, 1.0);

        foreach (var s in standings)
        {
            double expectedKm = s.MonthlyTargetKm * overallExpectedFraction;
            double onPaceRatio = (expectedKm > 0) ? (s.TotalDistanceKm / expectedKm) * 100.0 : 0.0;
            double progressPct = s.TargetProgressPct;

            s.EffortScore = progressPct + (onPaceRatio * 0.1);

            if (progressPct >= 100.0)
            {
                s.PacingStatus = "🏆 Achieved!";
            }
            else if (onPaceRatio >= 115.0)
            {
                s.PacingStatus = "🚀 Ahead of Goal";
            }
            else if (onPaceRatio >= 90.0)
            {
                s.PacingStatus = "🟢 On Track";
            }
            else if (onPaceRatio >= 60.0)
            {
                s.PacingStatus = "🟡 In Progress";
            }
            else
            {
                s.PacingStatus = "🔴 Needs Boost";
            }
        }

        // 1. Distance Ranks
        var byDistance = standings.OrderByDescending(s => s.TotalDistanceKm).ToList();
        for (int i = 0; i < byDistance.Count; i++) byDistance[i].DistanceRank = i + 1;

        // 2. Effort Ranks (Primary Ranking for Swiftember)
        var byEffort = standings
            .OrderByDescending(s => s.TargetProgressPct)
            .ThenByDescending(s => s.MonthlyTargetKm)
            .ThenByDescending(s => s.TotalActivitiesCount)
            .ToList();

        for (int i = 0; i < byEffort.Count; i++)
        {
            byEffort[i].EffortRank = i + 1;
        }

        // 3. Elevation Ranks
        var byElevation = standings.OrderByDescending(s => s.TotalElevationGainM).ToList();
        for (int i = 0; i < byElevation.Count; i++) byElevation[i].ElevationRank = i + 1;

        // 4. Activity Ranks
        var byActivity = standings.OrderByDescending(s => s.TotalActivitiesCount).ToList();
        for (int i = 0; i < byActivity.Count; i++) byActivity[i].ActivityRank = i + 1;

        // Sort by Effort Rank (Target Progress %) by default
        return standings.OrderBy(s => s.EffortRank).ToList();
    }
}
