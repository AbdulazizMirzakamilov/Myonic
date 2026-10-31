using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Myonic
{
    public partial class AppShell : Shell, INotifyPropertyChanged
    {
        private bool _isScheduleActive = true;
        private bool _isTrainingActive;
        private bool _isAnalysisActive;
        private bool _isOneRepMaxActive;
        private bool _isProfileActive;

        public bool IsScheduleActive
        {
            get => _isScheduleActive;
            set => SetProperty(ref _isScheduleActive, value);
        }

        public bool IsTrainingActive
        {
            get => _isTrainingActive;
            set => SetProperty(ref _isTrainingActive, value);
        }

        public bool IsAnalysisActive
        {
            get => _isAnalysisActive;
            set => SetProperty(ref _isAnalysisActive, value);
        }

        public bool IsOneRepMaxActive
        {
            get => _isOneRepMaxActive;
            set => SetProperty(ref _isOneRepMaxActive, value);
        }

        public bool IsProfileActive
        {
            get => _isProfileActive;
            set => SetProperty(ref _isProfileActive, value);
        }

        public ICommand NavigateDesktopCommand { get; }

        public AppShell()
        {
            InitializeComponent();
            BindingContext = this;
            NavigateDesktopCommand = new Command<string>(async route => await NavigateDesktopAsync(route));

            Routing.RegisterRoute(nameof(Pages.ExerciseDetailPage), typeof(Pages.ExerciseDetailPage));
            Routing.RegisterRoute(nameof(Pages.ExercisesPage), typeof(Pages.ExercisesPage));
            Routing.RegisterRoute(nameof(Pages.PersonalRecordsPage), typeof(Pages.PersonalRecordsPage));
            Routing.RegisterRoute(nameof(Pages.ExerciseEditPage), typeof(Pages.ExerciseEditPage));
            Routing.RegisterRoute(nameof(Pages.ProgramEditPage), typeof(Pages.ProgramEditPage));
            Routing.RegisterRoute(nameof(Pages.WorkoutEditPage), typeof(Pages.WorkoutEditPage));
            Routing.RegisterRoute(nameof(Pages.ProgramExerciseEditPage), typeof(Pages.ProgramExerciseEditPage));
            Routing.RegisterRoute(nameof(Pages.ActiveWorkoutPage), typeof(Pages.ActiveWorkoutPage));
            Routing.RegisterRoute(nameof(Pages.WorkoutSessionDetailPage), typeof(Pages.WorkoutSessionDetailPage));
            Routing.RegisterRoute(nameof(Pages.SettingsPage), typeof(Pages.SettingsPage));
        }

        public async void OnNavButtonClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.ClassId is string route)
            {
                await NavigateDesktopAsync(route);
            }
        }

        private async Task NavigateDesktopAsync(string? route)
        {
            if (string.IsNullOrWhiteSpace(route)) return;

            try
            {
                UpdateActiveDesktopTab(route);
                // Absolute routing ("//Route") switches to target tab and resets child navigation stack to root
                await GoToAsync($"//{route}");
                ResetSubpagesToRoot();
            }
            catch
            {
            }
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            if (args.Source == ShellNavigationSource.ShellItemChanged ||
                args.Source == ShellNavigationSource.ShellSectionChanged ||
                args.Source == ShellNavigationSource.ShellContentChanged)
            {
                ResetSubpagesToRoot();
            }
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);
            UpdateActiveDesktopTab(args.Current?.Location?.ToString() ?? string.Empty);
        }

        private void UpdateActiveDesktopTab(string location)
        {
            var loc = location.ToLowerInvariant();
            IsScheduleActive = loc.Contains("schedule");
            IsTrainingActive = loc.Contains("program") || loc.Contains("exercise") || loc.Contains("workoutedit");
            IsAnalysisActive = loc.Contains("workouthistory") || loc.Contains("workoutsessiondetail");
            IsOneRepMaxActive = loc.Contains("onerepmax");
            IsProfileActive = loc.Contains("profile") || loc.Contains("setting");

            // Default fallback to Schedule if none matched
            if (!IsScheduleActive && !IsTrainingActive && !IsAnalysisActive && !IsOneRepMaxActive && !IsProfileActive)
            {
                IsScheduleActive = true;
            }
        }

        public void ResetSubpagesToRoot()
        {
            try
            {
                foreach (var item in Items)
                {
                    if (item?.Items == null) continue;
                    foreach (var section in item.Items)
                    {
                        var nav = section?.Navigation;
                        if (nav?.NavigationStack != null && nav.NavigationStack.Count > 1)
                        {
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                try
                                {
                                    await nav.PopToRootAsync(false);
                                }
                                catch
                                {
                                }
                            });
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public void NotifyThemeChanged()
        {
            OnPropertyChanged(nameof(IsScheduleActive));
            OnPropertyChanged(nameof(IsTrainingActive));
            OnPropertyChanged(nameof(IsAnalysisActive));
            OnPropertyChanged(nameof(IsOneRepMaxActive));
            OnPropertyChanged(nameof(IsProfileActive));
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
