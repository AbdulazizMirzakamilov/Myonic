using Myonic.Core.Models;
using Myonic.Core.Repositories;
using Myonic.Data;
using Myonic.Data.Repositories;

namespace Myonic.Tests;

public class ExerciseRepositoryTests : IAsyncLifetime
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"myonic-test-{Guid.NewGuid():N}.db3");
    private MyonicDatabase _db = null!;
    private ExerciseRepository _repo = null!;

    public Task InitializeAsync()
    {
        _db = new MyonicDatabase(_path);
        _repo = new ExerciseRepository(_db);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        File.Delete(_path);
    }

    [Fact]
    public async Task Seed_ContainsDefaultExercises()
    {
        var all = await _repo.GetAllAsync();

        Assert.NotEmpty(all);
        Assert.All(all, e => Assert.False(e.IsCustom));
        Assert.Contains(all, e => e.Name == "Жим лёжа" && e.Type == ExerciseType.Compound);
    }

    [Fact]
    public async Task Add_SetsCustomFlag_AndAssignsId()
    {
        var id = await _repo.AddAsync(new Exercise { Name = "  Моё упражнение  ", Type = ExerciseType.Isolation });

        var loaded = await _repo.GetByIdAsync(id);
        Assert.NotNull(loaded);
        Assert.Equal("Моё упражнение", loaded.Name);
        Assert.True(loaded.IsCustom);
    }

    [Fact]
    public async Task Add_EmptyName_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _repo.AddAsync(new Exercise { Name = "   " }));
    }

    [Fact]
    public async Task Add_DuplicateName_IgnoringCase_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repo.AddAsync(new Exercise { Name = "жим ЛЁЖА" }));
    }

    [Fact]
    public async Task Update_RenameToExistingName_Throws()
    {
        var id = await _repo.AddAsync(new Exercise { Name = "Моё упражнение" });
        var exercise = (await _repo.GetByIdAsync(id))!;
        exercise.Name = "Жим лёжа";

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repo.UpdateAsync(exercise));
    }

    [Fact]
    public async Task Search_IsCaseInsensitive_ForCyrillic()
    {
        var result = await _repo.GetAllAsync(search: "жим");

        Assert.Contains(result, e => e.Name == "Жим лёжа");
    }

    [Fact]
    public async Task Remove_UnusedExercise_DeletesIt()
    {
        var id = await _repo.AddAsync(new Exercise { Name = "Временное" });

        var outcome = await _repo.RemoveAsync(id);

        Assert.Equal(ExerciseRemoval.Deleted, outcome);
        Assert.Null(await _repo.GetByIdAsync(id));
    }

    [Fact]
    public async Task Remove_UsedExercise_ArchivesIt()
    {
        var id = await _repo.AddAsync(new Exercise { Name = "Используемое" });
        var conn = await _db.GetConnectionAsync();

        var session = new WorkoutSession { Title = "Test", StartedAt = DateTime.Now };
        await conn.InsertAsync(session);
        await conn.InsertAsync(new WorkoutExercise { WorkoutSessionId = session.Id, ExerciseId = id, Order = 1 });

        var outcome = await _repo.RemoveAsync(id);

        Assert.Equal(ExerciseRemoval.Archived, outcome);
        Assert.DoesNotContain(await _repo.GetAllAsync(), e => e.Id == id);
        Assert.Contains(await _repo.GetAllAsync(includeArchived: true), e => e.Id == id);
    }
}