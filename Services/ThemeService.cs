using MudBlazor;
using SSRBlazor.Components.Themes;

namespace SSRBlazor.Services;

public class ThemeService
{
    public bool IsDarkMode { get; private set; }
    public MudTheme CurrentTheme { get; private set; } = SSRIntegrationTheme.DefaultTheme;

    public event Action? OnChange;

    public void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        NotifyStateChanged();
    }
    
    public void SetDarkMode(bool value)
    {
        IsDarkMode = value;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
