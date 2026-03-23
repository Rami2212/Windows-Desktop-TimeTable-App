using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TimeTableApp.Models;
using TimeTableApp.ViewModels;

namespace TimeTableApp.Services
{
    public sealed class SqliteTaskStore
    {
        private readonly string _dbPath;
        public SqliteTaskStore(string? dbPath = null)
        {
            _dbPath = dbPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TimeTableApp", "timetable.db");
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        }

        public async Task InitializeAsync()
        {
            await using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Tasks(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DayKey TEXT NOT NULL,
    TaskName TEXT NOT NULL,
    Points INTEGER NOT NULL,
    IsDone INTEGER NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Tasks_DayKey_TaskName ON Tasks(DayKey, TaskName);";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<DayTaskStatus>> LoadDayAsync(string dayKey)
        {
            var list = new List<DayTaskStatus>();
            await using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT TaskName, Points, IsDone FROM Tasks WHERE DayKey=@d ORDER BY Id;";
            cmd.Parameters.AddWithValue("@d", dayKey);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var name = r.GetString(0);
                var points = r.GetInt32(1);
                var done = r.GetInt32(2) == 1;
                list.Add(new DayTaskStatus(new Models.Task { Name = name, Points = points }) { IsDone = done });
            }
            return list;
        }

        public async Task UpsertAsync(string dayKey, DayTaskStatus task)
        {
            await using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Tasks(DayKey, TaskName, Points, IsDone)
VALUES(@d,@n,@p,@done)
ON CONFLICT(DayKey, TaskName) DO UPDATE SET Points=@p, IsDone=@done;";
            cmd.Parameters.AddWithValue("@d", dayKey);
            cmd.Parameters.AddWithValue("@n", task.TaskName);
            cmd.Parameters.AddWithValue("@p", task.Points);
            cmd.Parameters.AddWithValue("@done", task.IsDone ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(string dayKey, string taskName)
        {
            await using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"DELETE FROM Tasks WHERE DayKey=@d AND TaskName=@n;";
            cmd.Parameters.AddWithValue("@d", dayKey);
            cmd.Parameters.AddWithValue("@n", taskName);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}