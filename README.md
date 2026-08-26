# Swiftember — Strava Club Challenge & Leaderboard System (.NET 9 / C#)

A dedicated C# .NET 9 console application for tracking and calculating weekly and cumulative rankings, point systems, distance, elevation, and participation statistics for the **Swiftember Challenge** (or any Strava running club leaderboard).

---

## Features

- **Flexible Strava Data Ingestion**:
  - Reads Strava Club Leaderboards from **CSV**, **JSON**, or **HTML** exports.
  - Automatically parses athlete names, distances, run counts, longest runs, and elevation gain.
- **Weekly & Cumulative Challenge Rankings**:
  - Distance Leaderboard & Podium.
  - Elevation Gain (“King of the Mountains”) Rankings.
  - Activity & Consistency Streaks.
  - Flexible Point Systems (F1-style + Distance Points + Consistency Bonuses).
- **Automated Report Generation**:
  - **PDF Reports (`QuestPDF`)**: Clean, publication-ready multi-page leaderboard sheets with official Birmingham Swifts branding.
  - **Excel Workbooks (`ClosedXML`)**: `.xlsx` workbooks with dedicated weekly and overall cumulative challenge tabs.
- **Multi-Week Challenge Persistence**:
  - Stores all weekly data across the month of September in `data/swiftember_history.json`.

---

## Quick Start

### 1. Run with Sample Data:
```bash
dotnet run --project /Users/bo.wang/.gemini/antigravity-ide/scratch/swiftember
```

### 2. Process a Specific Week's CSV/JSON Leaderboard:
```bash
dotnet run --project /Users/bo.wang/.gemini/antigravity-ide/scratch/swiftember -- --file /path/to/strava_week1.csv --week 1
```

### 3. Generate Reports:
Reports are automatically saved to your `~/Downloads` folder:
- `~/Downloads/Swiftember_Week_1_Leaderboard.pdf`
- `~/Downloads/Swiftember_Week_1_Leaderboard.xlsx`

---

## Project Structure

```
swiftember/
├── Models/
│   ├── StravaAthleteRecord.cs
│   ├── WeeklyLeaderboard.cs
│   └── SwiftemberOverallStats.cs
├── Services/
│   ├── StravaLeaderboardParser.cs
│   ├── SwiftemberRankingEngine.cs
│   ├── SwiftemberHistoryService.cs
│   └── SwiftemberReportGenerator.cs
├── data/
│   ├── sample_week1.csv
│   └── swiftember_history.json
├── assets/
│   └── birmingham_swifts_logo.jpg
├── appsettings.json
├── Program.cs
├── Swiftember.csproj
└── README.md
```
