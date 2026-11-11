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

    [Fact]
    public async Task Seed_AllDefaultExercises_HaveAssignedMuscleGroups()
    {
        var all = await _repo.GetAllAsync();

        Assert.NotEmpty(all);
        Assert.All(all, e => Assert.NotEqual(MuscleGroup.None, e.MuscleGroup));

        var bench = all.FirstOrDefault(e => e.Name == "Жим лёжа");
        Assert.NotNull(bench);
        Assert.Equal(MuscleGroup.Chest, bench.MuscleGroup);
        Assert.Equal(MuscleGroup.Triceps, bench.SecondaryMuscleGroup);

        var pullup = all.FirstOrDefault(e => e.Name == "Подтягивания");
        Assert.NotNull(pullup);
        Assert.Equal(MuscleGroup.Back, pullup.MuscleGroup);
        Assert.Equal(MuscleGroup.Biceps, pullup.SecondaryMuscleGroup);

        var squat = all.FirstOrDefault(e => e.Name == "Приседания со штангой");
        Assert.NotNull(squat);
        Assert.Equal(MuscleGroup.Quadriceps, squat.MuscleGroup);
        Assert.Equal(MuscleGroup.Glutes, squat.SecondaryMuscleGroup);

        var rdl = all.FirstOrDefault(e => e.Name == "Румынская тяга");
        Assert.NotNull(rdl);
        Assert.Equal(MuscleGroup.Hamstrings, rdl.MuscleGroup);
        Assert.Equal(MuscleGroup.Glutes, rdl.SecondaryMuscleGroup);

        var overheadPress = all.FirstOrDefault(e => e.Name == "Жим штанги стоя");
        Assert.NotNull(overheadPress);
        Assert.Equal(MuscleGroup.Shoulders, overheadPress.MuscleGroup);
        Assert.Equal(MuscleGroup.Triceps, overheadPress.SecondaryMuscleGroup);

        var calfRaise = all.FirstOrDefault(e => e.Name == "Подъём на носки стоя");
        Assert.NotNull(calfRaise);
        Assert.Equal(MuscleGroup.Calves, calfRaise.MuscleGroup);
    }

    [Fact]
    public async Task FilterByMuscleGroup_ReturnsOnlyTargetGroupExercises()
    {
        var chestExercises = await _repo.GetAllAsync(muscleGroup: MuscleGroup.Chest);

        Assert.NotEmpty(chestExercises);
        Assert.All(chestExercises, e =>
            Assert.True(e.MuscleGroup == MuscleGroup.Chest || e.SecondaryMuscleGroup == MuscleGroup.Chest));

        Assert.Contains(chestExercises, e => e.Name == "Жим лёжа");
        Assert.Contains(chestExercises, e => e.Name == "Разведение гантелей лёжа");
        Assert.DoesNotContain(chestExercises, e => e.Name == "Приседания со штангой");
    }

    [Fact]
    public async Task CombinedFilter_MuscleGroupAndType_FiltersCorrectly()
    {
        var compoundChest = await _repo.GetAllAsync(muscleGroup: MuscleGroup.Chest, type: ExerciseType.Compound);
        Assert.Contains(compoundChest, e => e.Name == "Жим лёжа");
        Assert.DoesNotContain(compoundChest, e => e.Name == "Разведение гантелей лёжа");

        var isolationChest = await _repo.GetAllAsync(muscleGroup: MuscleGroup.Chest, type: ExerciseType.Isolation);
        Assert.Contains(isolationChest, e => e.Name == "Разведение гантелей лёжа");
        Assert.DoesNotContain(isolationChest, e => e.Name == "Жим лёжа");
    }

    [Fact]
    public async Task CombinedFilter_SearchAndMuscleGroup_FiltersCorrectly()
    {
        var shoulderPress = await _repo.GetAllAsync(search: "жим", muscleGroup: MuscleGroup.Shoulders);
        Assert.Contains(shoulderPress, e => e.Name == "Жим штанги стоя");
        Assert.DoesNotContain(shoulderPress, e => e.Name == "Жим лёжа");
    }

    [Fact]
    public async Task CustomExercise_WithAndWithoutMuscleGroup_SavesAndFiltersCorrectly()
    {
        // 1. Without muscle group
        var unassignedId = await _repo.AddAsync(new Exercise
        {
            Name = "Упражнение без группы",
            Type = ExerciseType.Isolation,
            MuscleGroup = MuscleGroup.None
        });

        var loadedUnassigned = await _repo.GetByIdAsync(unassignedId);
        Assert.NotNull(loadedUnassigned);
        Assert.Equal(MuscleGroup.None, loadedUnassigned.MuscleGroup);
        Assert.Null(loadedUnassigned.SecondaryMuscleGroup);

        // 2. With Abs muscle group
        var absId = await _repo.AddAsync(new Exercise
        {
            Name = "Скручивания на пресс",
            Type = ExerciseType.Isolation,
            MuscleGroup = MuscleGroup.Abs
        });

        var loadedAbs = await _repo.GetByIdAsync(absId);
        Assert.NotNull(loadedAbs);
        Assert.Equal(MuscleGroup.Abs, loadedAbs.MuscleGroup);

        var absList = await _repo.GetAllAsync(muscleGroup: MuscleGroup.Abs);
        Assert.Contains(absList, e => e.Id == absId);
        Assert.DoesNotContain(absList, e => e.Id == unassignedId);

        // Update muscle group
        loadedUnassigned.MuscleGroup = MuscleGroup.Forearms;
        await _repo.UpdateAsync(loadedUnassigned);

        var updated = await _repo.GetByIdAsync(unassignedId);
        Assert.NotNull(updated);
        Assert.Equal(MuscleGroup.Forearms, updated.MuscleGroup);
    }

    [Fact]
    public async Task MuscleGroups_SurviveDatabaseReconnection()
    {
        var id = await _repo.AddAsync(new Exercise
        {
            Name = "Пресс с роликом",
            Type = ExerciseType.Compound,
            MuscleGroup = MuscleGroup.Abs,
            SecondaryMuscleGroup = MuscleGroup.Back
        });

        await _db.CloseConnectionAsync();

        var reopenedDb = new MyonicDatabase(_path);
        var reopenedRepo = new ExerciseRepository(reopenedDb);

        var loaded = await reopenedRepo.GetByIdAsync(id);
        Assert.NotNull(loaded);
        Assert.Equal("Пресс с роликом", loaded.Name);
        Assert.Equal(MuscleGroup.Abs, loaded.MuscleGroup);
        Assert.Equal(MuscleGroup.Back, loaded.SecondaryMuscleGroup);

        await reopenedDb.DisposeAsync();
    }
}