using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FocusGuard.Infrastructure;
using Microsoft.Data.Sqlite;

namespace FocusGuard.Data;

public sealed record DictionaryHistoryRecord(
    int rowIndex,
    string time,
    string type,
    string input,
    string output);

public sealed class DictionaryHistoryRepository
{
    private readonly string _connectionString;

    public DictionaryHistoryRepository()
        : this(UserDataPaths.DatabasePath)
    {
    }

    public DictionaryHistoryRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        string? directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();
    }

    public void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS dictionary_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                searched_at TEXT NOT NULL,
                type TEXT NOT NULL,
                input TEXT NOT NULL,
                output TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public int Add(string type, string input, string output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentNullException.ThrowIfNull(output);

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dictionary_history (searched_at, type, input, output)
            VALUES ($searchedAt, $type, $input, $output);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$searchedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$type", type);
        command.Parameters.AddWithValue("$input", input);
        command.Parameters.AddWithValue("$output", output);
        return checked((int)(long)(command.ExecuteScalar() ?? throw new InvalidOperationException("기록 ID를 가져오지 못했습니다.")));
    }

    public IReadOnlyList<DictionaryHistoryRecord> GetAll()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, searched_at, type, input, output
            FROM dictionary_history
            ORDER BY id DESC;
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        var records = new List<DictionaryHistoryRecord>();
        while (reader.Read())
        {
            records.Add(new DictionaryHistoryRecord(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)));
        }

        return records;
    }

    public void Delete(int id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dictionary_history WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
