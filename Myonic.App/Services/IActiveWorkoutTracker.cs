using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Myonic.Core.Repositories;
using Myonic.Pages;

namespace Myonic.Services;

public interface IActiveWorkoutTracker : INotifyPropertyChanged
{
    bool HasActiveSession { get; }
    int? ActiveSessionId { get; }
    string Title { get; }
    string DurationText { get; }
    ICommand ResumeCommand { get; }

    Task RefreshAsync();
    void NotifySessionStarted(int sessionId, string title);
    void NotifySessionFinished();
}

public class ActiveWorkoutTracker : ObservableObject, IActiveWorkoutTracker
{
    private readonly IWorkoutSessionRepository _sessionRepo;
    private IDispatcherTimer? _timer;
    private DateTime? _startedAt;

    private bool _hasActiveSession;
    public bool HasActiveSession
    {
        get => _hasActiveSession;
        private set => SetProperty(ref _hasActiveSession, value);
    }

    private int? _activeSessionId;
    public int? ActiveSessionId
    {
        get => _activeSessionId;
        private set => SetProperty(ref _activeSessionId, value);
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    private string _durationText = "00:00";
    public string DurationText
    {
        get => _durationText;
        private set => SetProperty(ref _durationText, value);
    }

    public ICommand ResumeCommand { get; }

    public ActiveWorkoutTracker(IWorkoutSessionRepository sessionRepo)
    {
        _sessionRepo = sessionRepo;
        ResumeCommand = new Command(async () =>
        {
            if (ActiveSessionId.HasValue)
            {
                await Shell.Current.GoToAsync($"{nameof(ActiveWorkoutPage)}?sessionId={ActiveSessionId.Value}");
            }
        });
    }

    public async Task RefreshAsync()
    {
        try
        {
            var active = await _sessionRepo.GetActiveSessionSummaryAsync();
            if (active != null)
            {
                _startedAt = active.StartedAt;
                ActiveSessionId = active.Id;
                Title = active.Title;
                HasActiveSession = true;
                UpdateDuration();
                StartTimer();
            }
            else
            {
                StopTimer();
                HasActiveSession = false;
                ActiveSessionId = null;
                Title = string.Empty;
                DurationText = "00:00";
                _startedAt = null;
            }
        }
        catch
        {
        }
    }

    public void NotifySessionStarted(int sessionId, string title)
    {
        _startedAt = DateTime.UtcNow;
        ActiveSessionId = sessionId;
        Title = title;
        HasActiveSession = true;
        UpdateDuration();
        StartTimer();
    }

    public void NotifySessionFinished()
    {
        StopTimer();
        HasActiveSession = false;
        ActiveSessionId = null;
        Title = string.Empty;
        DurationText = "00:00";
        _startedAt = null;
    }

    private void StartTimer()
    {
        if (_timer != null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_timer != null) return;
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.GetForCurrentThread();
            if (dispatcher != null)
            {
                _timer = dispatcher.CreateTimer();
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += (s, e) => UpdateDuration();
                _timer.Start();
            }
        });
    }

    private void StopTimer()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        });
    }

    private void UpdateDuration()
    {
        if (!_startedAt.HasValue) return;

        var elapsed = DateTime.UtcNow - _startedAt.Value;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

        DurationText = (int)elapsed.TotalHours > 0
            ? elapsed.ToString(@"hh\:mm\:ss")
            : elapsed.ToString(@"mm\:ss");
    }
}
