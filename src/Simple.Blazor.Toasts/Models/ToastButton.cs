namespace Simple.Blazor.Toasts.Models;

public class ToastButton
{
    public string Id { get; set; } = GenerateShortId();
    public string Text { get; set; } = string.Empty;
    public string CssClass { get; set; } = "btn btn-primary btn-sm";
    public Action<string>? OnClick { get; set; } // Passes the toast ID
    public bool CloseToastOnClick { get; set; } = true;
    public bool IsDisabled { get; set; } = false;
    public string Icon { get; set; } = string.Empty; // For future icon support

    // Advanced state navigation
    public int? TargetStateIndex { get; set; } = null; // Jump to specific state
    public List<int> SkipStates { get; set; } = new(); // States to mark as skipped
    public Func<string, int>? ConditionalTarget { get; set; } = null; // Dynamic state targeting
    public bool AdvanceToNext { get; set; } = false; // Simple next state advancement

    /// <summary>
    /// Generates a short 5-character ID using GUID for uniqueness
    /// </summary>
    private static string GenerateShortId()
    {
        return Guid.NewGuid().ToString("N")[..5].ToUpper();
    }
}

public static class ToastButtonPresets
{
    public static ToastButton Confirm(Action<string> onConfirm) => new()
    {
        Text = "Confirm",
        CssClass = "btn btn-success btn-sm",
        OnClick = onConfirm,
        CloseToastOnClick = true
    };

    public static ToastButton Cancel(Action<string>? onCancel = null) => new()
    {
        Text = "Cancel",
        CssClass = "btn btn-secondary btn-sm",
        OnClick = onCancel,
        CloseToastOnClick = true
    };

    public static ToastButton Delete(Action<string> onDelete) => new()
    {
        Text = "Delete",
        CssClass = "btn btn-danger btn-sm",
        OnClick = onDelete,
        CloseToastOnClick = true
    };

    public static ToastButton Save(Action<string> onSave) => new()
    {
        Text = "Save",
        CssClass = "btn btn-primary btn-sm",
        OnClick = onSave,
        CloseToastOnClick = true
    };

    public static ToastButton Retry(Action<string> onRetry) => new()
    {
        Text = "Retry",
        CssClass = "btn btn-warning btn-sm",
        OnClick = onRetry,
        CloseToastOnClick = false // Keep toast open for potential retry
    };

    public static ToastButton ViewDetails(Action<string> onViewDetails) => new()
    {
        Text = "View Details",
        CssClass = "btn btn-info btn-sm",
        OnClick = onViewDetails,
        CloseToastOnClick = false
    };

    public static ToastButton Undo(Action<string> onUndo) => new()
    {
        Text = "Undo",
        CssClass = "btn btn-outline-primary btn-sm",
        OnClick = onUndo,
        CloseToastOnClick = true
    };

    // Advanced workflow buttons
    public static ToastButton NextStep(Action<string>? onNext = null) => new()
    {
        Text = "Next",
        CssClass = "btn btn-primary btn-sm",
        OnClick = onNext,
        CloseToastOnClick = false,
        AdvanceToNext = true
    };

    public static ToastButton SkipStep(int targetStateIndex, Action<string>? onSkip = null) => new()
    {
        Text = "Skip",
        CssClass = "btn btn-outline-secondary btn-sm",
        OnClick = onSkip,
        CloseToastOnClick = false,
        TargetStateIndex = targetStateIndex
    };

    public static ToastButton JumpToState(int targetStateIndex, string buttonText, Action<string>? onClick = null) => new()
    {
        Text = buttonText,
        CssClass = "btn btn-info btn-sm",
        OnClick = onClick,
        CloseToastOnClick = false,
        TargetStateIndex = targetStateIndex
    };

    public static ToastButton ConditionalChoice(string buttonText, Func<string, int> conditionalTarget, Action<string>? onClick = null) => new()
    {
        Text = buttonText,
        CssClass = "btn btn-warning btn-sm",
        OnClick = onClick,
        CloseToastOnClick = false,
        ConditionalTarget = conditionalTarget
    };

    public static ToastButton SkipAndAdvance(List<int> skipStates, string buttonText = "Skip Steps", Action<string>? onClick = null) => new()
    {
        Text = buttonText,
        CssClass = "btn btn-outline-warning btn-sm",
        OnClick = onClick,
        CloseToastOnClick = false,
        SkipStates = skipStates,
        AdvanceToNext = true
    };
} 