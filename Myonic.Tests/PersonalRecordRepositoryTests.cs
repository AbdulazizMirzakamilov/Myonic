using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Data;
using Myonic.Data.Repositories;

namespace Myonic.Tests;

public class PersonalRecordRepositoryTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-pr-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private PersonalRecordRepository _repo = null!;
    private WorkoutSessionRepository _sessionRepo = null!;
    private int _exerciseId;
    private int _workoutExerciseId;

    public async Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _repo = new PersonalRecordRepository(_db);
        _sessionRepo = new WorkoutSessionRepository(_db);

        var conn = await _db.GetConnectionAsync();
        var ex = await conn.Table<Exercise>().FirstAsync();
        _exerciseId = ex.Id;

        var sessionId = await _sessionRepo.StartEmptySessionAsync("Тест PR");
        var we = await _sessionRepo.AddExerciseToSessionAsync(sessionId, _exerciseId);
        _workoutExerciseId = we.WorkoutExerciseId;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    [Fact]
    public async Task AddManualRecord_InsertsAndRetrieves()
    {
        var id = await _repo.AddManualRecordAsync(_exerciseId, RecordType.MaxWeight, 120.0, 1, DateTime.UtcNow);
        Assert.True(id > 0);

        var records = await _repo.GetByExerciseAsync(_exerciseId);
        Assert.Single(records);
        Assert.Equal(120.0, records[0].Weight);
        Assert.True(records[0].IsManual);
    }

    [Fact]
    public async Task CheckAndRecordNewPrs_FirstSet_RecordsPrs()
    {
        var set = await _sessionRepo.AddSetAsync(_workoutExerciseId, 100, 5, 8.0);
        var newPrs = await _repo.CheckAndRecordNewPrsAsync(_workoutExerciseId, set.Id, 100, 5);

        // 100x5 should set MaxWeight (100), 1RM (~116.7), and 5RM (100)
        Assert.Equal(3, newPrs.Count);
        Assert.Contains(newPrs, p => p.Type == RecordType.MaxWeight && p.Weight == 100);
        Assert.Contains(newPrs, p => p.Type == RecordType.FiveRepMax && p.Weight == 100);
        Assert.Contains(newPrs, p => p.Type == RecordType.OneRepMax && p.Weight == 116.7);
    }

    [Fact]
    public async Task CheckAndRecordNewPrs_LowerWeight_DoesNotRecordPr()
    {
        var set1 = await _sessionRepo.AddSetAsync(_workoutExerciseId, 100, 5, 8.0);
        await _repo.CheckAndRecordNewPrsAsync(_workoutExerciseId, set1.Id, 100, 5);

        var set2 = await _sessionRepo.AddSetAsync(_workoutExerciseId, 90, 5, 7.5);
        var newPrs = await _repo.CheckAndRecordNewPrsAsync(_workoutExerciseId, set2.Id, 90, 5);

        Assert.Empty(newPrs);
    }

    [Fact]
    public async Task CheckAndRecordNewPrs_HigherWeight_RecordsNewPr()
    {
        var set1 = await _sessionRepo.AddSetAsync(_workoutExerciseId, 100, 5, 8.0);
        await _repo.CheckAndRecordNewPrsAsync(_workoutExerciseId, set1.Id, 100, 5);

        var set2 = await _sessionRepo.AddSetAsync(_workoutExerciseId, 105, 5, 9.0);
        var newPrs = await _repo.CheckAndRecordNewPrsAsync(_workoutExerciseId, set2.Id, 105, 5);

        Assert.NotEmpty(newPrs);
        Assert.Contains(newPrs, p => p.Type == RecordType.MaxWeight && p.Weight == 105);
    }
}
