#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Dapper;

namespace FocusGuard.Data
{
    public class SessionRecord
    {
        public string target_name { get; set; }
        public int planned_study_seconds { get; set; }
        public int actual_focused_seconds { get; set; }
        public int distracted_seconds { get; set; }
        public string started_at { get; set; }
    }

    public class MissionRecord
    {
        public int id { get; set; }
        public string title { get; set; }
        public int duration_minutes { get; set; }
        public string target_app { get; set; }
        public string resource_path { get; set; }
    }

    public class DatabaseHelper
    {
        private readonly string _connectionString = "Data Source=focusguard.db";

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
                        started_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    );");
                
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

        public void SaveSession(string targetName, int planned, int focused, int distracted)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Execute(
                    "INSERT INTO sessions (target_name, planned_study_seconds, actual_focused_seconds, distracted_seconds) VALUES (@t, @p, @f, @d)", 
                    new { t = targetName, p = planned, f = focused, d = distracted });
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
    }
}
