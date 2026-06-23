using FocusGuard.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace FocusGuard.Local.Tests;

public sealed class DictionaryHistoryRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"FocusGuard.Tests.{Guid.NewGuid():N}");
    private readonly string _databasePath;

    public DictionaryHistoryRepositoryTests()
    {
        Directory.CreateDirectory(_directory);
        _databasePath = Path.Combine(_directory, "history.db");
    }

    [Fact]
    public void InitializeAddGetAndDelete_RoundTripsRecordsNewestFirst()
    {
        var repository = new DictionaryHistoryRepository(_databasePath);
        repository.Initialize();

        int firstId = repository.Add("사전", "focus", "집중");
        int secondId = repository.Add("번역", "stay focused", "집중하세요");

        IReadOnlyList<DictionaryHistoryRecord> records = repository.GetAll();

        Assert.Equal(2, records.Count);
        Assert.Equal(secondId, records[0].rowIndex);
        Assert.Equal("번역", records[0].type);
        Assert.Equal("stay focused", records[0].input);
        Assert.Equal("집중하세요", records[0].output);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$", records[0].time);
        Assert.Equal(firstId, records[1].rowIndex);

        repository.Delete(secondId);

        DictionaryHistoryRecord remaining = Assert.Single(repository.GetAll());
        Assert.Equal(firstId, remaining.rowIndex);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
