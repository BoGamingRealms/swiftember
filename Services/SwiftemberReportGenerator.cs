using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Swiftember.Models;

namespace Swiftember.Services;

public class SwiftemberReportGenerator
{
    static SwiftemberReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static void GeneratePdf(
        WeeklyLeaderboard currentWeek,
        List<SwiftemberOverallStats> overallStandings,
        string outputPath)
    {
        string fullPath = Path.GetFullPath(outputPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
        string? dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "birmingham_swifts_logo.jpg");
        if (!File.Exists(logoPath)) logoPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "birmingham_swifts_logo.jpg");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(Colors.Grey.Darken3));

                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            if (File.Exists(logoPath))
                            {
                                titleCol.Item().MaxHeight(32).MaxWidth(140).Image(logoPath).FitArea();
                            }
                            else
                            {
                                titleCol.Item().Text("BIRMINGHAM SWIFTS").FontSize(15).Bold().FontColor(Colors.Indigo.Darken3);
                            }

                            titleCol.Item().PaddingTop(2).Text($"SWIFTEMBER CHALLENGE — TARGET & EFFORT LEADERBOARD (WEEK {currentWeek.WeekNumber})")
                                .FontSize(11).Bold().FontColor(Colors.Indigo.Darken2);
                        });

                        row.AutoItem().Column(dateCol =>
                        {
                            dateCol.Item().AlignRight().Text(string.IsNullOrEmpty(currentWeek.DateRange) ? $"Week {currentWeek.WeekNumber}" : currentWeek.DateRange)
                                .FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken3);
                            dateCol.Item().AlignRight().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(7.5f).FontColor(Colors.Grey.Medium);
                        });
                    });

                    // Summary Stats Badges
                    column.Item().PaddingTop(6).PaddingBottom(5).Row(statRow =>
                    {
                        statRow.Spacing(6);

                        // Badge 1: Total Club Distance
                        statRow.RelativeItem().Background(Colors.Indigo.Lighten5).Border(1).BorderColor(Colors.Indigo.Lighten3).Padding(5).Column(c =>
                        {
                            c.Item().Text("Club Distance").FontSize(7f).FontColor(Colors.Indigo.Darken2).SemiBold();
                            c.Item().Text($"{currentWeek.TotalClubDistanceKm:N1} km").FontSize(11).Bold().FontColor(Colors.Indigo.Darken4);
                        });

                        // Badge 2: Total Activities
                        statRow.RelativeItem().Background(Colors.Teal.Lighten5).Border(1).BorderColor(Colors.Teal.Lighten3).Padding(5).Column(c =>
                        {
                            c.Item().Text("Club Runs").FontSize(7f).FontColor(Colors.Teal.Darken2).SemiBold();
                            c.Item().Text($"{currentWeek.TotalClubActivities:N0}").FontSize(11).Bold().FontColor(Colors.Teal.Darken4);
                        });

                        // Badge 3: Elevation
                        statRow.RelativeItem().Background(Colors.Amber.Lighten5).Border(1).BorderColor(Colors.Amber.Lighten3).Padding(5).Column(c =>
                        {
                            c.Item().Text("Elevation Gain").FontSize(7f).FontColor(Colors.Amber.Darken3).SemiBold();
                            c.Item().Text($"{currentWeek.TotalClubElevationM:N0} m").FontSize(11).Bold().FontColor(Colors.Amber.Darken4);
                        });

                        // Badge 4: Active Runners
                        statRow.RelativeItem().Background(Colors.Purple.Lighten5).Border(1).BorderColor(Colors.Purple.Lighten3).Padding(5).Column(c =>
                        {
                            c.Item().Text("Active Runners").FontSize(7f).FontColor(Colors.Purple.Darken2).SemiBold();
                            c.Item().Text($"{currentWeek.ActiveAthletesCount:N0}").FontSize(11).Bold().FontColor(Colors.Purple.Darken4);
                        });
                    });

                    column.Item().PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(6).Column(col =>
                {
                    // Section 1: Weekly Effort & Target Completion Leaderboard
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Effort & Target Leaderboard (Week {currentWeek.WeekNumber})").FontSize(10.5f).Bold().FontColor(Colors.Indigo.Darken3);
                        r.AutoItem().Text("Ranked by % of Monthly Target Completed").FontSize(8f).FontColor(Colors.Grey.Darken1).Italic();
                    });

                    var sortedByEffort = currentWeek.Athletes.OrderBy(a => a.EffortRank).ToList();

                    col.Item().PaddingTop(3).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);   // Effort Rank
                            columns.RelativeColumn(3.2f); // Athlete Name
                            columns.ConstantColumn(52);   // Target
                            columns.ConstantColumn(52);   // Distance
                            columns.ConstantColumn(46);   // Target %
                            columns.ConstantColumn(68);   // Pacing Status
                            columns.ConstantColumn(30);   // Runs
                            columns.ConstantColumn(48);   // Longest
                            columns.ConstantColumn(42);   // Elev
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(3).AlignCenter().Text("Pos").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(3).Text("Athlete").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(3).AlignCenter().Text("Target").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(3).AlignCenter().Text("Distance").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(3).AlignCenter().Text("Goal %").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(3).AlignCenter().Text("Status").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(3).AlignCenter().Text("Runs").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(3).AlignCenter().Text("Longest").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(3).AlignCenter().Text("Elev").Bold().FontColor(Colors.White);
                        });

                        for (int i = 0; i < sortedByEffort.Count; i++)
                        {
                            var a = sortedByEffort[i];
                            string bg = (i % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                            table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{a.EffortRank}").Bold();
                            table.Cell().Background(bg).Padding(2.5f).Text(a.AthleteName).Medium();
                            table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{a.MonthlyTargetKm:N0} km").FontColor(Colors.Grey.Darken2);
                            table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{a.DistanceKm:N1} km").Bold().FontColor(Colors.Indigo.Darken2);
                            table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{a.TargetProgressPct:F1}%").Bold().FontColor(a.TargetProgressPct >= 25.0 ? Colors.Green.Darken3 : Colors.Orange.Darken3);
                            table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text(a.PacingStatus).FontSize(7.5f).SemiBold();
                            table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{a.ActivitiesCount}");
                            table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{a.LongestActivityKm:N1} km");
                            table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{a.ElevationGainM:N0}m");
                        }
                    });

                    // Section 2: Cumulative Standings
                    if (overallStandings.Count > 0)
                    {
                        col.Item().PaddingTop(12).Row(r =>
                        {
                            r.RelativeItem().Text("Cumulative Swiftember Challenge Standings").FontSize(10.5f).Bold().FontColor(Colors.Teal.Darken3);
                            r.AutoItem().Text("Overall Month Progress & Effort").FontSize(8f).FontColor(Colors.Grey.Darken1).Italic();
                        });

                        var overallByEffort = overallStandings.OrderBy(s => s.EffortRank).ToList();

                        col.Item().PaddingTop(3).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(24);   // Effort Rank
                                columns.RelativeColumn(3.2f); // Athlete Name
                                columns.ConstantColumn(52);   // Target
                                columns.ConstantColumn(55);   // Total Dist
                                columns.ConstantColumn(46);   // Target %
                                columns.ConstantColumn(68);   // Status
                                columns.ConstantColumn(35);   // Weeks
                                columns.ConstantColumn(30);   // Runs
                                columns.ConstantColumn(42);   // Elev
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Teal.Darken3).Padding(3).AlignCenter().Text("Pos").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(3).Text("Athlete").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(3).AlignCenter().Text("Target").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(3).AlignCenter().Text("Total Dist").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(3).AlignCenter().Text("Goal %").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(3).AlignCenter().Text("Status").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(3).AlignCenter().Text("Weeks").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(3).AlignCenter().Text("Runs").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(3).AlignCenter().Text("Elev").Bold().FontColor(Colors.White);
                            });

                            for (int i = 0; i < overallByEffort.Count; i++)
                            {
                                var s = overallByEffort[i];
                                string bg = (i % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                                table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{s.EffortRank}").Bold();
                                table.Cell().Background(bg).Padding(2.5f).Text(s.AthleteName).Medium();
                                table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{s.MonthlyTargetKm:N0} km").FontColor(Colors.Grey.Darken2);
                                table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{s.TotalDistanceKm:N1} km").Bold().FontColor(Colors.Teal.Darken3);
                                table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{s.TargetProgressPct:F1}%").Bold().FontColor(s.TargetProgressPct >= 100.0 ? Colors.Green.Darken3 : Colors.Indigo.Darken2);
                                table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text(s.PacingStatus).FontSize(7.5f).SemiBold();
                                table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{s.WeeksParticipated}");
                                table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{s.TotalActivitiesCount}");
                                table.Cell().Background(bg).Padding(2.5f).AlignCenter().Text($"{s.TotalElevationGainM:N0}m");
                            }
                        });
                    }
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    footer.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text("Birmingham Swifts — Swiftember Challenge").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        row.AutoItem().Text(text =>
                        {
                            text.Span("Page ").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                            text.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Darken2).Bold();
                            text.Span(" of ").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                            text.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Darken2).Bold();
                        });
                    });
                });
            });
        }).GeneratePdf(fullPath);

        Console.WriteLine($"[Success] Generated Swiftember PDF Report at: {fullPath}");
    }

    public static void GenerateExcel(
        WeeklyLeaderboard currentWeek,
        List<SwiftemberOverallStats> overallStandings,
        string outputPath)
    {
        string fullPath = Path.GetFullPath(outputPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
        string? dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        using var workbook = new XLWorkbook();

        // Sheet 1: Weekly Leaderboard (Effort Ranked)
        var wsWeek = workbook.Worksheets.Add($"Week {currentWeek.WeekNumber} Effort");
        wsWeek.Cell(1, 1).Value = "Effort Rank";
        wsWeek.Cell(1, 2).Value = "Distance Rank";
        wsWeek.Cell(1, 3).Value = "Athlete Name";
        wsWeek.Cell(1, 4).Value = "Monthly Target (km)";
        wsWeek.Cell(1, 5).Value = "Distance (km)";
        wsWeek.Cell(1, 6).Value = "Monthly Target (%)";
        wsWeek.Cell(1, 7).Value = "Pacing Status";
        wsWeek.Cell(1, 8).Value = "Activities";
        wsWeek.Cell(1, 9).Value = "Longest Run (km)";
        wsWeek.Cell(1, 10).Value = "Elevation Gain (m)";
        wsWeek.Cell(1, 11).Value = "Pace";
        wsWeek.Cell(1, 12).Value = "Points";
        wsWeek.Row(1).Style.Font.Bold = true;

        var sortedByEffort = currentWeek.Athletes.OrderBy(a => a.EffortRank).ToList();
        for (int i = 0; i < sortedByEffort.Count; i++)
        {
            var a = sortedByEffort[i];
            int r = i + 2;
            wsWeek.Cell(r, 1).Value = a.EffortRank;
            wsWeek.Cell(r, 2).Value = a.Rank;
            wsWeek.Cell(r, 3).Value = a.AthleteName;
            wsWeek.Cell(r, 4).Value = a.MonthlyTargetKm;
            wsWeek.Cell(r, 5).Value = a.DistanceKm;
            wsWeek.Cell(r, 6).Value = Math.Round(a.TargetProgressPct, 1);
            wsWeek.Cell(r, 7).Value = a.PacingStatus;
            wsWeek.Cell(r, 8).Value = a.ActivitiesCount;
            wsWeek.Cell(r, 9).Value = a.LongestActivityKm;
            wsWeek.Cell(r, 10).Value = a.ElevationGainM;
            wsWeek.Cell(r, 11).Value = a.PaceFormatted;
            wsWeek.Cell(r, 12).Value = a.Points;
        }
        wsWeek.Columns().AdjustToContents();

        // Sheet 2: Overall Cumulative Standings
        if (overallStandings.Count > 0)
        {
            var wsOverall = workbook.Worksheets.Add("Overall Standings");
            wsOverall.Cell(1, 1).Value = "Effort Rank";
            wsOverall.Cell(1, 2).Value = "Distance Rank";
            wsOverall.Cell(1, 3).Value = "Athlete Name";
            wsOverall.Cell(1, 4).Value = "Monthly Target (km)";
            wsOverall.Cell(1, 5).Value = "Total Distance (km)";
            wsOverall.Cell(1, 6).Value = "Monthly Target (%)";
            wsOverall.Cell(1, 7).Value = "Pacing Status";
            wsOverall.Cell(1, 8).Value = "Weeks Active";
            wsOverall.Cell(1, 9).Value = "Total Activities";
            wsOverall.Cell(1, 10).Value = "Total Elevation (m)";
            wsOverall.Cell(1, 11).Value = "Best Week (km)";
            wsOverall.Cell(1, 12).Value = "Overall Points";
            wsOverall.Row(1).Style.Font.Bold = true;

            var overallByEffort = overallStandings.OrderBy(s => s.EffortRank).ToList();
            for (int i = 0; i < overallByEffort.Count; i++)
            {
                var s = overallByEffort[i];
                int r = i + 2;
                wsOverall.Cell(r, 1).Value = s.EffortRank;
                wsOverall.Cell(r, 2).Value = s.DistanceRank;
                wsOverall.Cell(r, 3).Value = s.AthleteName;
                wsOverall.Cell(r, 4).Value = s.MonthlyTargetKm;
                wsOverall.Cell(r, 5).Value = s.TotalDistanceKm;
                wsOverall.Cell(r, 6).Value = Math.Round(s.TargetProgressPct, 1);
                wsOverall.Cell(r, 7).Value = s.PacingStatus;
                wsOverall.Cell(r, 8).Value = s.WeeksParticipated;
                wsOverall.Cell(r, 9).Value = s.TotalActivitiesCount;
                wsOverall.Cell(r, 10).Value = s.TotalElevationGainM;
                wsOverall.Cell(r, 11).Value = s.BestWeeklyDistanceKm;
                wsOverall.Cell(r, 12).Value = s.OverallPoints;
            }
            wsOverall.Columns().AdjustToContents();
        }

        workbook.SaveAs(fullPath);
        Console.WriteLine($"[Success] Generated Swiftember Excel Report at: {fullPath}");
    }
}
