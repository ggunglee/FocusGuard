#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Dapper;

namespace FocusGuard.Data
{
    public class SessionRecord
    {
        public int id { get; set; }
        public string target_name { get; set; }
        public int planned_study_seconds { get; set; }
        public int actual_focused_seconds { get; set; }
        public int distracted_seconds { get; set; }
        public int total_elapsed_seconds { get; set; }
        public string started_at { get; set; }
        public string memo { get; set; }

        public string DisplayFocusTime
        {
            get
            {
                int seconds = actual_focused_seconds;
                if (seconds == 0) return "0분";
                if (seconds >= 3600) return $"{seconds / 3600}시간 {(seconds % 3600) / 60}분";
                if (seconds >= 60) return $"{seconds / 60}분 {seconds % 60}초";
                return $"{seconds}초";
            }
        }

        public string DisplayDistractTime
        {
            get
            {
                int seconds = distracted_seconds;
                if (seconds == 0) return "0초";
                if (seconds >= 60) return $"{seconds / 60}분 {seconds % 60}초";
                return $"{seconds}초";
            }
        }

        public string DisplayStartedAt
        {
            get
            {
                if (DateTime.TryParse(started_at, out DateTime dt))
                {
                    return dt.ToString("HH:mm:ss");
                }
                return started_at;
            }
        }
    }

    public class MissionRecord
    {
        public int id { get; set; }
        public string title { get; set; }
        public int duration_minutes { get; set; }
        public string target_app { get; set; }
        public string resource_path { get; set; }

        public string DisplayResourcePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(resource_path))
                    return "리소스 없음";

                string raw = resource_path.Trim();
                const string logToken = "[READING_LOG]";
                bool isReadingLog = raw.Contains(logToken);
                string cleanUrl = raw.Replace(logToken, "").Trim();

                string prefix = isReadingLog ? "📚 " : "🔗 ";

                if (cleanUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                    cleanUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var uri = new Uri(cleanUrl);
                        return prefix + uri.Host;
                    }
                    catch
                    {
                        return prefix + cleanUrl;
                    }
                }

                if (cleanUrl.Contains(".") && !cleanUrl.Contains(" ") && !cleanUrl.Contains("\\") && !cleanUrl.Contains("/"))
                {
                    return prefix + cleanUrl;
                }

                try
                {
                    if (cleanUrl.Contains("\\") || cleanUrl.Contains("/"))
                    {
                        return (isReadingLog ? "📚 " : "📂 ") + System.IO.Path.GetFileName(cleanUrl);
                    }
                }
                catch { }

                return prefix + cleanUrl;
            }
        }
    }

    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "focusguard.db");
            _connectionString = $"Data Source={dbPath}";
        }

        public void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                
                // 집중 기록 테이블
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS sessions (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        target_name TEXT,
                        planned_study_seconds INTEGER NOT NULL,
                        actual_focused_seconds INTEGER DEFAULT 0,
                        distracted_seconds INTEGER DEFAULT 0,
                        total_elapsed_seconds INTEGER DEFAULT 0,
                        started_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    );");

                try
                {
                    bool hasMemoColumn = false;
                    bool hasTotalElapsedColumn = false;
                    using (var cmd = new SqliteCommand("PRAGMA table_info(sessions);", connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string name = reader["name"]?.ToString();
                                if (string.Equals(name, "memo", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasMemoColumn = true;
                                }
                                if (string.Equals(name, "total_elapsed_seconds", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasTotalElapsedColumn = true;
                                }
                            }
                        }
                    }
                    if (!hasMemoColumn)
                    {
                        connection.Execute("ALTER TABLE sessions ADD COLUMN memo TEXT;");
                    }
                    if (!hasTotalElapsedColumn)
                    {
                        connection.Execute("ALTER TABLE sessions ADD COLUMN total_elapsed_seconds INTEGER DEFAULT 0;");
                    }
                }
                catch (Exception)
                {
                }
                
                // 과제 설정 테이블
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS missions (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        title TEXT,
                        duration_minutes INTEGER,
                        target_app TEXT,
                        resource_path TEXT
                    );");
            }
        }

        public void SaveSession(string targetName, int planned, int focused, int distracted, int totalElapsed, string memo)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Execute(
                    "INSERT INTO sessions (target_name, planned_study_seconds, actual_focused_seconds, distracted_seconds, total_elapsed_seconds, memo) VALUES (@t, @p, @f, @d, @e, @m)", 
                    new { t = targetName, p = planned, f = focused, d = distracted, e = totalElapsed, m = memo });
            }
        }

        public IEnumerable<SessionRecord> GetTodaySessions()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                return connection.Query<SessionRecord>("SELECT * FROM sessions WHERE date(started_at, 'localtime') = date('now', 'localtime')");
            }
        }

        // 🔥 누락되었던 이번 주 세션 조회 로직
        public IEnumerable<SessionRecord> GetWeeklySessions()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                return connection.Query<SessionRecord>("SELECT * FROM sessions WHERE date(started_at, 'localtime') >= date('now', '-7 days', 'localtime')");
            }
        }

        public IEnumerable<SessionRecord> GetAllSessions()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                return connection.Query<SessionRecord>("SELECT * FROM sessions ORDER BY started_at DESC");
            }
        }

        // 🔥 누락되었던 날짜 추적(Streak) 로직
        public IEnumerable<string> GetStudyDates()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                return connection.Query<string>("SELECT DISTINCT date(started_at, 'localtime') FROM sessions ORDER BY date(started_at, 'localtime') DESC");
            }
        }

        // --- 과제(Mission) 설정 로직 ---
        public IEnumerable<MissionRecord> GetMissions()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                return connection.Query<MissionRecord>("SELECT * FROM missions ORDER BY id DESC");
            }
        }

        public void AddMission(string title, int mins, string app, string res)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Execute("INSERT INTO missions (title, duration_minutes, target_app, resource_path) VALUES (@t, @m, @a, @r)", new { t = title, m = mins, a = app, r = res });
            }
        }

        public void DeleteMission(int id)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Execute("DELETE FROM missions WHERE id = @i", new { i = id });
            }
        }

        public void UpdateMission(int id, string title, int mins, string app, string res)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Execute(
                    "UPDATE missions SET title = @t, duration_minutes = @m, target_app = @a, resource_path = @r WHERE id = @i",
                    new { t = title, m = mins, a = app, r = res, i = id }
                );
            }
        }

        public void DeleteSession(int id)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Execute("DELETE FROM sessions WHERE id = @i", new { i = id });
            }
        }
    }
}
