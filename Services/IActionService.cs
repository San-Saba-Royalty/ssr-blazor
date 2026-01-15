using SSRBlazor.Models;

namespace SSRBlazor.Services;

/// <summary>
/// Service interface for handling toolbar and menu actions.
/// Coordinates with other services to execute actions and manage navigation.
/// </summary>
public interface IActionService
{
    /// <summary>
    /// Executes the specified action command.
    /// </summary>
    /// <param name="actionCommand">The action command constant from ActionCommands</param>
    /// <param name="parameters">Optional parameters for the action</param>
    /// <returns>ActionResult indicating success/failure and any navigation</returns>
    Task<ActionResult> ExecuteActionAsync(string actionCommand, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Determines if the specified action is enabled for the current context.
    /// </summary>
    /// <param name="actionCommand">The action command to check</param>
    /// <param name="currentModule">The current page module (e.g., "Acquisition", "Filter", "View")</param>
    /// <returns>True if the action should be enabled</returns>
    bool IsActionEnabled(string actionCommand, string? currentModule = null);

    /// <summary>
    /// Gets the URL for the specified action without executing it.
    /// </summary>
    /// <param name="actionCommand">The action command</param>
    /// <param name="parameters">Optional parameters for URL generation</param>
    /// <returns>The URL string, or null if not applicable</returns>
    string? GetActionUrl(string actionCommand, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Sets the last viewed acquisition ID for "Back" navigation.
    /// </summary>
    void SetLastAcquisitionId(string acquisitionId);

    /// <summary>
    /// Sets the last viewed letter agreement ID for "Back" navigation.
    /// </summary>
    void SetLastLetterAgreementId(string letterAgreementId);

    /// <summary>
    /// Gets the last viewed acquisition ID.
    /// </summary>
    string? GetLastAcquisitionId();

    /// <summary>
    /// Gets the last viewed letter agreement ID.
    /// </summary>
    string? GetLastLetterAgreementId();
    /// <summary>
    /// Initializes the action service with user-specific data.
    /// </summary>
    Task InitializeAsync(int userId);

    /// <summary>
    /// Gets the current state of actions (enabled/disabled, visibility).
    /// </summary>
    ActionState CurrentState { get; }

    /// <summary>
    /// Event triggered when the action state changes.
    /// </summary>
    event Action OnStateChange;

    // specific action events for pages to subscribe to
    event Func<Task> OnSaveRequested;
    event Func<Task> OnDeleteRequested;
    event Func<Task> OnCopyRequested;
    event Func<Task> OnPrintRequested;

    /// <summary>
    /// Updates the global action state.
    /// </summary>
    /// <param name="state">The new state to apply</param>
    void UpdateState(ActionState state);
}

/// <summary>
/// Represents the state of toolbar actions for the current context.
/// </summary>
public record ActionState
{
    public bool IsEditMode { get; init; }
    public bool IsDirty { get; init; }
    public bool CanSave { get; init; }
    public bool CanDelete { get; init; }
    public bool CanCopy { get; init; }
    public bool CanPrint { get; init; }

    // Static defaults
    public static ActionState Default => new()
    {
        IsEditMode = false,
        IsDirty = false,
        CanSave = false,
        CanDelete = false,
        CanCopy = false,
        CanPrint = false
    };

    public static ActionState Edit(bool isDirty = false) => new()
    {
        IsEditMode = true,
        IsDirty = isDirty,
        CanSave = true,
        CanDelete = true,
        CanCopy = true,
        CanPrint = true
    };

    public static ActionState Index => new()
    {
        IsEditMode = false,
        IsDirty = false,
        CanSave = false,
        CanDelete = false,
        CanCopy = false,
        CanPrint = true
    };
}
