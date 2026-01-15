namespace SSRBlazor.Models;

/// <summary>
/// Represents the result of an action execution.
/// </summary>
public class ActionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? NavigateToUrl { get; set; }
    public Dictionary<string, object>? Data { get; set; }

    public static ActionResult SuccessResult(string? message = null, string? navigateToUrl = null)
    {
        return new ActionResult
        {
            Success = true,
            Message = message,
            NavigateToUrl = navigateToUrl
        };
    }

    public static ActionResult ErrorResult(string message)
    {
        return new ActionResult
        {
            Success = false,
            Message = message
        };
    }
}
