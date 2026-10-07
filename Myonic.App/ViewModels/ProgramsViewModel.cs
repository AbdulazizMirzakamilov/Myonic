using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.ViewModels;

public partial class ProgramsViewModel : ObservableObject
{
    private readonly ITrainingProgramRepository _repository;

    public ProgramsViewModel(ITrainingProgramRepository repository) => _repository = repository;

    public ObservableCollection<ProgramSummary> Programs { get; } = [];

    [RelayCommand]
    private async Task LoadAsync()
    {
        var items = await _repository.GetSummariesAsync();

        Programs.Clear();
        foreach (var item in items)
            Programs.Add(item);
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(nameof(ProgramEditPage));

    [RelayCommand]
    private Task OpenAsync(ProgramSummary item) =>
        Shell.Current.GoToAsync($"{nameof(ProgramEditPage)}?id={item.Id}");
}