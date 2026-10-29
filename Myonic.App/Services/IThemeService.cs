namespace Myonic.Services;

public enum AppThemeMode
{
    Dark = 0,
    Light = 1,
    System = 2
}

public interface IThemeService
{
    AppThemeMode CurrentThemeMode { get; }
    bool IsDark { get; }
    event Action<bool>? ThemeChanged;
    void Initialize();
    void SetThemeMode(AppThemeMode mode);
}
