using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public partial class ProgramsViewModel : ObservableObject
{
    private readonly ITrainingProgramRepository _repository;
    private readonly IWorkoutSessionRepository _sessionRepository;

    [ObservableProperty]
    private ActiveSessionSummary? _activeSession;

    [ObservableProperty]
    private bool _hasActiveSession;

    public Myonic.Services.IActiveWorkoutTracker ActiveWorkoutTracker { get; }

    public ProgramsViewModel(
        ITrainingProgramRepository repository,
        IWorkoutSessionRepository sessionRepository,
        Myonic.Services.IActiveWorkoutTracker? activeWorkoutTracker = null)
    {
        _repository = repository;
        _sessionRepository = sessionRepository;
        ActiveWorkoutTracker = activeWorkoutTracker ?? new Myonic.Services.ActiveWorkoutTracker(sessionRepository);
    }

    public ObservableCollection<ProgramSummary> Programs { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        await ActiveWorkoutTracker.RefreshAsync();
        var active = await _sessionRepository.GetActiveSessionSummaryAsync();
        ActiveSession = active;
        HasActiveSession = active != null;

        var items = await _repository.GetSummariesAsync();
        Programs.Clear();
        foreach (var item in items)
            Programs.Add(item);
    }

    [RelayCommand]
    private async Task ResumeActiveSessionAsync()
    {
        if (ActiveSession == null) return;
        await Shell.Current.GoToAsync(
            nameof(ActiveWorkoutPage),
            new Dictionary<string, object>
            {
                ["sessionId"] = ActiveSession.Id
            });
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(nameof(ProgramEditPage));

    [RelayCommand]
    private Task OpenAsync(ProgramSummary item) =>
        Shell.Current.GoToAsync($"{nameof(ProgramEditPage)}?id={item.Id}");
}
