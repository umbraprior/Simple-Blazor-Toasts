using System.ComponentModel;

namespace Simple.Blazor.Toasts.Models;

public class ToastItem : INotifyPropertyChanged
{
    private ToastType _type = ToastType.Info;
    private string _title = string.Empty;
    private string _message = string.Empty;
    private bool _isTransitioning = false;
    private int _currentStateIndex = 0;
    private double _progress = 0.0;

    public string Id { get; set; } = GenerateShortId();
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string Message
    {
        get => _message;
        set
        {
            if (_message != value)
            {
                _message = value;
                OnPropertyChanged();
            }
        }
    }
    
    public ToastType Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                _type = value;
                OnPropertyChanged();
            }
        }
    }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public TimeSpan? Timeout { get; set; }
    public List<ToastButton> Buttons { get; set; } = new();
    public ToastSize Size { get; set; } = ToastSize.Medium;
    
    // Legacy compatibility properties
    public int? TimeoutInSeconds 
    { 
        get => Timeout?.TotalSeconds is double seconds ? (int)seconds : null;
        set => Timeout = value.HasValue ? TimeSpan.FromSeconds(value.Value) : null;
    }
    public bool IsVisible { get; set; } = false; // Start invisible for animations
    public bool IsRemoving { get; set; } = false;
    public Dictionary<string, object> Data { get; set; } = new();
    
    // Progress tracking with proper UI notification
    public double Progress 
    { 
        get => _progress;
        set
        {
            if (Math.Abs(_progress - value) > 0.01) // Only update if change is significant
            {
                _progress = value;
                OnPropertyChanged();
            }
        }
    }
    public bool ShowProgress => Timeout.HasValue && !IsStateful;
    
    // Stateful toast properties
    public bool IsStateful => States.Count > 0;
    public List<ToastState> States { get; set; } = new();
    
    // Method to initialize stateful toast with states
    public void InitializeWithStates(List<ToastState> states)
    {
        States = states;
        if (states.Count > 0)
        {
            _currentStateIndex = 0; // Set backing field directly to avoid triggering setter
            UpdateFromCurrentState(); // Explicitly update content from first state
            OnPropertyChanged(nameof(IsStateful));
            OnPropertyChanged(nameof(CurrentState));
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
        }
    }
    public int CurrentStateIndex
    {
        get => _currentStateIndex;
        set
        {
            if (_currentStateIndex != value && value >= 0 && value < States.Count)
            {
                _currentStateIndex = value;
                UpdateFromCurrentState();
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentState));
                OnPropertyChanged(nameof(CanNavigatePrevious));
                OnPropertyChanged(nameof(CanNavigateNext));
            }
        }
    }
    
    public bool IsTransitioning
    {
        get => _isTransitioning;
        set
        {
            if (_isTransitioning != value)
            {
                _isTransitioning = value;
                OnPropertyChanged();
            }
        }
    }

    // Advanced state management
    public HashSet<int> SkippedStates { get; set; } = new();
    public Dictionary<string, object> StateData { get; set; } = new(); // Store data between states
    
    public ToastState? CurrentState => IsStateful && CurrentStateIndex < States.Count ? States[CurrentStateIndex] : null;
    public bool CanNavigatePrevious => IsStateful && GetPreviousNonSkippedStateIndex() >= 0;
    public bool CanNavigateNext => IsStateful && GetNextNonSkippedStateIndex() < States.Count;
    
    // Navigation methods
    public bool NavigateToNext()
    {
        var nextIndex = GetNextNonSkippedStateIndex();
        if (nextIndex < States.Count)
        {
            CurrentStateIndex = nextIndex;
            return true;
        }
        return false;
    }
    
    public bool NavigateToPrevious()
    {
        var prevIndex = GetPreviousNonSkippedStateIndex();
        if (prevIndex >= 0)
        {
            CurrentStateIndex = prevIndex;
            return true;
        }
        return false;
    }

    public bool NavigateToState(int stateIndex)
    {
        if (stateIndex >= 0 && stateIndex < States.Count)
        {
            CurrentStateIndex = stateIndex;
            return true;
        }
        return false;
    }

    public void SkipStates(IEnumerable<int> stateIndices)
    {
        foreach (var index in stateIndices)
        {
            if (index >= 0 && index < States.Count)
            {
                SkippedStates.Add(index);
            }
        }
        OnPropertyChanged(nameof(CanNavigatePrevious));
        OnPropertyChanged(nameof(CanNavigateNext));
    }

    public void SkipState(int stateIndex)
    {
        if (stateIndex >= 0 && stateIndex < States.Count)
        {
            SkippedStates.Add(stateIndex);
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
        }
    }

    public void UnSkipState(int stateIndex)
    {
        SkippedStates.Remove(stateIndex);
        OnPropertyChanged(nameof(CanNavigatePrevious));
        OnPropertyChanged(nameof(CanNavigateNext));
    }

    public bool IsStateSkipped(int stateIndex) => SkippedStates.Contains(stateIndex);

    private int GetNextNonSkippedStateIndex()
    {
        for (int i = CurrentStateIndex + 1; i < States.Count; i++)
        {
            if (!SkippedStates.Contains(i))
                return i;
        }
        return States.Count; // Beyond last state
    }

    private int GetPreviousNonSkippedStateIndex()
    {
        for (int i = CurrentStateIndex - 1; i >= 0; i--)
        {
            if (!SkippedStates.Contains(i))
                return i;
        }
        return -1; // Before first state
    }

    public int GetRemainingStates()
    {
        int count = 0;
        for (int i = CurrentStateIndex + 1; i < States.Count; i++)
        {
            if (!SkippedStates.Contains(i))
                count++;
        }
        return count;
    }

    public int GetCompletedStates()
    {
        int count = 0;
        for (int i = 0; i < CurrentStateIndex; i++)
        {
            if (!SkippedStates.Contains(i))
                count++;
        }
        return count + (SkippedStates.Contains(CurrentStateIndex) ? 0 : 1); // Include current if not skipped
    }

    public List<int> GetActiveStateIndices()
    {
        var activeStates = new List<int>();
        for (int i = 0; i < States.Count; i++)
        {
            if (!SkippedStates.Contains(i))
                activeStates.Add(i);
        }
        return activeStates;
    }
    
    // Legacy compatibility methods
    public void UpdateMessage(string newMessage)
    {
        Message = newMessage;
    }

    public void UpdateTitle(string newTitle)
    {
        Title = newTitle;
    }

    public void UpdateType(ToastType newType)
    {
        Type = newType;
    }

    public void UpdateSize(ToastSize newSize)
    {
        Size = newSize;
        OnPropertyChanged(nameof(Size));
    }

    public void AddButton(ToastButton button)
    {
        Buttons.Add(button);
        OnPropertyChanged(nameof(Buttons));
    }

    public void RemoveButton(string buttonId)
    {
        var button = Buttons.FirstOrDefault(b => b.Id == buttonId);
        if (button != null)
        {
            Buttons.Remove(button);
            OnPropertyChanged(nameof(Buttons));
        }
    }

    public void ClearButtons()
    {
        Buttons.Clear();
        OnPropertyChanged(nameof(Buttons));
    }

    // Legacy stateful navigation methods
    public bool HasNextState() => IsStateful && GetNextNonSkippedStateIndex() < States.Count;
    public bool HasPreviousState() => IsStateful && GetPreviousNonSkippedStateIndex() >= 0;
    
    public void NextState()
    {
        NavigateToNext();
    }
    
    public void PreviousState()
    {
        NavigateToPrevious();
    }
    
    public void GoToState(int stateIndex)
    {
        NavigateToState(stateIndex);
    }
    
    public ToastState? GetCurrentState()
    {
        return CurrentState;
    }
    
    private void UpdateFromCurrentState()
    {
        if (CurrentState != null)
        {
            Title = CurrentState.Title;
            Message = CurrentState.Message;
            Type = CurrentState.Type;
            Buttons = CurrentState.Buttons;
        }
    }
    
    private static string GenerateShortId()
    {
        return Guid.NewGuid().ToString("N")[..5].ToUpper();
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // New property to control navigation visibility
    public bool ShowNavigation { get; set; } = true;
}

public class ToastState
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..5].ToUpper();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
    public int? TimeoutInSeconds { get; set; } = null;
    public List<ToastButton> Buttons { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
    
    // Animation settings for this state
    public string TransitionAnimation { get; set; } = "slideIn"; // slideIn, fadeIn, scaleIn, etc.
    public int TransitionDurationMs { get; set; } = 300;
    
    // State flow control
    public bool AutoAdvanceToNext { get; set; } = false;
    public int AutoAdvanceDelayMs { get; set; } = 2000;
    
    // Legacy compatibility property
    public int? AutoAdvanceAfterSeconds 
    { 
        get => AutoAdvanceToNext ? AutoAdvanceDelayMs / 1000 : null;
        set 
        {
            if (value.HasValue)
            {
                AutoAdvanceToNext = true;
                AutoAdvanceDelayMs = value.Value * 1000;
            }
            else
            {
                AutoAdvanceToNext = false;
            }
        }
    }
}

public enum ToastType
{
    Default,
    Success,
    Error,
    Warning,
    Info
}

public enum ToastSize
{
    Small,
    Medium,
    Large
} 