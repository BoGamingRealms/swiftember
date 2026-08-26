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
                page.Margin(25);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(titleCol =>
                        {
                            if (File.Exists(logoPath))
                            {
                                titleCol.Item().MaxHeight(34).MaxWidth(145).Image(logoPath).FitArea();
                            }
                            else
                            {
                                titleCol.Item().Text("BIRMINGHAM SWIFTS").FontSize(16).Bold().FontColor(Colors.Indigo.Darken3);
                            }

                            titleCol.Item().PaddingTop(2).Text($"SWIFTEMBER CHALLENGE — WEEK {currentWeek.WeekNumber}")
                                .FontSize(12).Bold().FontColor(Colors.Indigo.Darken2);
                        });

                        row.AutoItem().Column(dateCol =>
                        {
                            dateCol.Item().AlignRight().Text(string.IsNullOrEmpty(currentWeek.DateRange) ? $"Week {currentWeek.WeekNumber}" : currentWeek.DateRange)
                                .FontSize(10).Bold().FontColor(Colors.Grey.Darken3);
                            dateCol.Item().AlignRight().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });

                    // Summary Stats Badges
                    column.Item().PaddingTop(8).PaddingBottom(6).Row(statRow =>
                    {
                        statRow.Spacing(8);

                        // Badge 1: Total Club Distance
                        statRow.RelativeItem().Background(Colors.Indigo.Lighten5).Border(1).BorderColor(Colors.Indigo.Lighten3).Padding(6).Column(c =>
                        {
                            c.Item().Text("Club Distance").FontSize(7.5f).FontColor(Colors.Indigo.Darken2).SemiBold();
                            c.Item().Text($"{currentWeek.TotalClubDistanceKm:N1} km").FontSize(12).Bold().FontColor(Colors.Indigo.Darken4);
                        });

                        // Badge 2: Total Activities
                        statRow.RelativeItem().Background(Colors.Teal.Lighten5).Border(1).BorderColor(Colors.Teal.Lighten3).Padding(6).Column(c =>
                        {
                            c.Item().Text("Club Activities").FontSize(7.5f).FontColor(Colors.Teal.Darken2).SemiBold();
                            c.Item().Text($"{currentWeek.TotalClubActivities:N0}").FontSize(12).Bold().FontColor(Colors.Teal.Darken4);
                        });

                        // Badge 3: Elevation
                        statRow.RelativeItem().Background(Colors.Amber.Lighten5).Border(1).BorderColor(Colors.Amber.Lighten3).Padding(6).Column(c =>
                        {
                            c.Item().Text("Elevation Gain").FontSize(7.5f).FontColor(Colors.Amber.Darken3).SemiBold();
                            c.Item().Text($"{currentWeek.TotalClubElevationM:N0} m").FontSize(12).Bold().FontColor(Colors.Amber.Darken4);
                        });

                        // Badge 4: Active Runners
                        statRow.RelativeItem().Background(Colors.Purple.Lighten5).Border(1).BorderColor(Colors.Purple.Lighten3).Padding(6).Column(c =>
                        {
                            c.Item().Text("Active Runners").FontSize(7.5f).FontColor(Colors.Purple.Darken2).SemiBold();
                            c.Item().Text($"{currentWeek.ActiveAthletesCount:N0}").FontSize(12).Bold().FontColor(Colors.Purple.Darken4);
                        });
                    });

                    column.Item().PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    // Section 1: Weekly Leaderboard Table
                    col.Item().Text($"Weekly Leaderboard (Week {currentWeek.WeekNumber})").FontSize(11).Bold().FontColor(Colors.Indigo.Darken3);

                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);   // Rank
                            columns.RelativeColumn(3.5f); // Athlete Name
                            columns.ConstantColumn(65);   // Distance
                            columns.ConstantColumn(50);   // Runs
                            columns.ConstantColumn(65);   // Longest
                            columns.ConstantColumn(60);   // Elevation
                            columns.ConstantColumn(50);   // Points
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(4).AlignCenter().Text("Pos").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(4).Text("Athlete").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(4).AlignCenter().Text("Distance").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(4).AlignCenter().Text("Runs").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(4).AlignCenter().Text("Longest").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(4).AlignCenter().Text("Elev (m)").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Indigo.Darken3).Padding(4).AlignCenter().Text("Points").Bold().FontColor(Colors.White);
                        });

                        for (int i = 0; i < currentWeek.Athletes.Count; i++)
                        {
                            var a = currentWeek.Athletes[i];
                            string bg = (i % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                            table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{a.Rank}").Bold();
                            table.Cell().Background(bg).Padding(3).Text(a.AthleteName).Medium();
                            table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{a.DistanceKm:N1} km").Bold().FontColor(Colors.Indigo.Darken2);
                            table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{a.ActivitiesCount}");
                            table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{a.LongestActivityKm:N1} km");
                            table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{a.ElevationGainM:N0}");
                            table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{a.Points:N0}").Bold().FontColor(Colors.Teal.Darken3);
                        }
                    });

                    // Section 2: Overall Standings (if multi-week data exists)
                    if (overallStandings.Count > 0)
                    {
                        col.Item().PaddingTop(16).Text("Cumulative Swiftember Standings (All Weeks)").FontSize(11).Bold().FontColor(Colors.Indigo.Darken3);

                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);   // Rank
                                columns.RelativeColumn(3.5f); // Athlete Name
                                columns.ConstantColumn(75);   // Total Distance
                                columns.ConstantColumn(50);   // Weeks
                                columns.ConstantColumn(50);   // Runs
                                columns.ConstantColumn(65);   // Total Elev
                                columns.ConstantColumn(55);   // Tot Points
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Teal.Darken3).Padding(4).AlignCenter().Text("Pos").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(4).Text("Athlete").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(4).AlignCenter().Text("Total Dist").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(4).AlignCenter().Text("Weeks").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(4).AlignCenter().Text("Runs").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(4).AlignCenter().Text("Elev (m)").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Teal.Darken3).Padding(4).AlignCenter().Text("Points").Bold().FontColor(Colors.White);
                            });

                            for (int i = 0; i < overallStandings.Count; i++)
                            {
                                var s = overallStandings[i];
                                string bg = (i % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                                table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{s.DistanceRank}").Bold();
                                table.Cell().Background(bg).Padding(3).Text(s.AthleteName).Medium();
                                table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{s.TotalDistanceKm:N1} km").Bold().FontColor(Colors.Teal.Darken3);
                                table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{s.WeeksParticipated}");
                                table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{s.TotalActivitiesCount}");
                                table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{s.TotalElevationGainM:N0}");
                                table.Cell().Background(bg).Padding(3).AlignCenter().Text($"{s.OverallPoints:N0}").Bold().FontColor(Colors.Indigo.Darken2);
                            }
                        });
                    }
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    footer.Item().PaddingTop(4).Row(row =>
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

        // Sheet 1: Weekly Leaderboard
        var wsWeek = workbook.Worksheets.Add($"Week {currentWeek.WeekNumber}");
        wsWeek.Cell(1, 1).Value = "Rank";
        wsWeek.Cell(1, 2).Value = "Athlete Name";
        wsWeek.Cell(1, 3).Value = "Distance (km)";
        wsWeek.Cell(1, 4).Value = "Activities";
        wsWeek.Cell(1, 5).Value = "Longest Run (km)";
        wsWeek.Cell(1, 6).Value = "Elevation Gain (m)";
        wsWeek.Cell(1, 7).Value = "Points";
        wsWeek.Row(1).Style.Font.Bold = true;

        for (int i = 0; i < currentWeek.Athletes.Count; i++)
        {
            var a = currentWeek.Athletes[i];
            int r = i + 2;
            wsWeek.Cell(r, 1).Value = a.Rank;
            wsWeek.Cell(r, 2).Value = a.AthleteName;
            wsWeek.Cell(r, 3).Value = a.DistanceKm;
            wsWeek.Cell(r, 4).Value = a.ActivitiesCount;
            wsWeek.Cell(r, 5).Value = a.LongestActivityKm;
            wsWeek.Cell(r, 6).Value = a.ElevationGainM;
            wsWeek.Cell(r, 7).Value = a.Points;
        }
        wsWeek.Columns().AdjustToContents();

        // Sheet 2: Overall Standings
        if (overallStandings.Count > 0)
        {
            var wsOverall = workbook.Worksheets.Add("Overall Standings");
            wsOverall.Cell(1, 1).Value = "Distance Rank";
            wsOverall.Cell(1, 2).Value = "Athlete Name";
            wsOverall.Cell(1, 3).Value = "Total Distance (km)";
            wsOverall.Cell(1, 4).Value = "Weeks Active";
            wsOverall.Cell(1, 5).Value = "Total Activities";
            wsOverall.Cell(1, 6).Value = "Total Elevation (m)";
            wsOverall.Cell(1, 7).Value = "Best Week (km)";
            wsOverall.Cell(1, 8).Value = "Overall Points";
            wsOverall.Row(1).Style.Font.Bold = true;

            for (int i = 0; i < overallStandings.Count; i++)
            {
                var s = overallStandings[i];
                int r = i + 2;
                wsOverall.Cell(r, 1).Value = s.DistanceRank;
                wsOverall.Cell(r, 2).Value = s.AthleteName;
                wsOverall.Cell(r, 3).Value = s.TotalDistanceKm;
                wsOverall.Cell(r, 4).Value = s.WeeksParticipated;
                wsOverall.Cell(r, 5).Value = s.TotalActivitiesCount;
                wsOverall.Cell(r, 6).Value = s.TotalElevationGainM;
                wsOverall.Cell(r, 7).Value = s.BestWeeklyDistanceKm;
                wsOverall.Cell(r, 8).Value = s.OverallPoints;
            }
            wsOverall.Columns().AdjustToContents();
        }

        workbook.SaveAs(fullPath);
        Console.WriteLine($"[Success] Generated Swiftember Excel Report at: {fullPath}");
    }
}
