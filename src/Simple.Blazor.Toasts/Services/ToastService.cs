using System.Collections.ObjectModel;
using System.Timers;
using Simple.Blazor.Toasts.Models;

namespace Simple.Blazor.Toasts.Services;

public class ToastService : IDisposable
{
    private readonly ObservableCollection<ToastItem> _toasts = new();
    private readonly Queue<ToastItem> _toastQueue = new();
    private readonly Dictionary<string, System.Timers.Timer> _timers = new();
    private readonly Dictionary<string, DateTime> _toastStartTimes = new();
    private readonly object _lock = new object();
    
    // Queue management
    private int _maxVisibleToasts = 5; // Maximum toasts visible at once - updated to 5
    private string? _activeToastId = null; // The toast with active timeout
    
    // Timer system for smooth progress updates and UI batching
    private readonly System.Timers.Timer _masterProgressTimer;
    private const int ProgressUpdateIntervalMs = 16; // Increased to 60fps for ultra-smooth progress bars
    
    // UI update timer for batched re-renders
    private bool _uiUpdatePending = false;
    private readonly System.Timers.Timer _uiUpdateTimer;
    
    // Progress tracking optimization
    private readonly Dictionary<string, double> _lastProgressValues = new();
    
    public IReadOnlyCollection<ToastItem> Toasts => _toasts;
    
    // Position management
    public ToastPosition Position { get; private set; } = ToastPosition.TopRight;
    
    // Animation management
    public ToastAnimation Animation { get; private set; } = ToastAnimation.SlideAndFade;
    
    // Theme management
    public ToastTheme Theme { get; private set; } = ToastTheme.Dark;
    
    // Queue management properties
    public int QueuedCount => _toastQueue.Count;
    public int MaxVisibleToasts 
    { 
        get => _maxVisibleToasts; 
        set => _maxVisibleToasts = Math.Max(1, Math.Min(10, value)); // Limit between 1-10
    }
    public string? ActiveToastId => _activeToastId;
    
    public event Action? OnToastsChanged;

    public ToastService()
    {
        // Initialize master progress timer with reduced frequency
        _masterProgressTimer = new System.Timers.Timer(ProgressUpdateIntervalMs);
        _masterProgressTimer.Elapsed += UpdateAllProgress;
        _masterProgressTimer.Start();
        
        // Initialize UI update batching timer
        _uiUpdateTimer = new System.Timers.Timer(50); // Slightly slower UI updates
        _uiUpdateTimer.Elapsed += (_, _) =>
        {
            if (_uiUpdatePending)
            {
                _uiUpdatePending = false;
                OnToastsChanged?.Invoke();
            }
        };
        _uiUpdateTimer.Start();
        
        // Pre-warm the system without creating visible UI elements
        Task.Run(async () =>
        {
            await Task.Delay(200); // Let the UI settle first
            PreWarmSystem();
        });
    }

    private void PreWarmSystem()
    {
        // Pre-warm timer creation and disposal without adding to UI
        var warmupTimer = new System.Timers.Timer(100);
        warmupTimer.Elapsed += (_, _) => { };
        warmupTimer.Start();
        warmupTimer.Stop();
        warmupTimer.Dispose();
        
        // Pre-warm cleanup operations
        var dummyId = GenerateShortId();
        _toastStartTimes[dummyId] = DateTime.Now;
        _lastProgressValues[dummyId] = 100.0;
        _toastStartTimes.Remove(dummyId);
        _lastProgressValues.Remove(dummyId);
        
        // Pre-warm OnToastsChanged event
        OnToastsChanged?.Invoke();
        
        Console.WriteLine("Toast system pre-warmed successfully");
    }
    
    private static string GenerateShortId()
    {
        return Guid.NewGuid().ToString("N")[..5].ToUpper();
    }

    public string ShowToast(string message, ToastType type = ToastType.Info, string title = "", int? timeoutInSeconds = 5, ToastSize size = ToastSize.Medium)
    {
        var toast = new ToastItem
        {
            Title = title,
            Message = message,
            Type = type,
            Size = size,
            TimeoutInSeconds = timeoutInSeconds
        };

        return ShowToast(toast);
    }

    public string ShowToast(ToastItem toast)
    {
        lock (_lock)
        {
            // Add to queue instead of directly to visible toasts
            _toastQueue.Enqueue(toast);
            
            // Process queue to show toasts up to max visible limit
            ProcessToastQueue();
            
            return toast.Id;
        }
    }

    // New method for toasts with buttons
    public string ShowToastWithButtons(string message, ToastType type, string title, List<ToastButton> buttons, int? timeoutInSeconds = null, ToastSize size = ToastSize.Medium)
    {
        var toast = new ToastItem
        {
            Title = title,
            Message = message,
            Type = type,
            Size = size,
            TimeoutInSeconds = timeoutInSeconds,
            Buttons = buttons
        };

        return ShowToast(toast);
    }

    // Convenience methods for common button scenarios
    public string ShowConfirmation(string message, string title, Action<string> onConfirm, Action<string>? onCancel = null, int? timeoutInSeconds = null, ToastSize size = ToastSize.Medium)
    {
        var buttons = new List<ToastButton>
        {
            ToastButtonPresets.Confirm(onConfirm),
            ToastButtonPresets.Cancel(onCancel ?? (_ => { }))
        };

        return ShowToastWithButtons(message, ToastType.Warning, title, buttons, timeoutInSeconds, size);
    }

    public string ShowUndoAction(string message, string title, Action<string> onUndo, int timeoutInSeconds = 10, ToastSize size = ToastSize.Medium)
    {
        var buttons = new List<ToastButton>
        {
            ToastButtonPresets.Undo(onUndo)
        };

        return ShowToastWithButtons(message, ToastType.Info, title, buttons, timeoutInSeconds, size);
    }

    public string ShowRetryAction(string message, string title, Action<string> onRetry, int? timeoutInSeconds = null, ToastSize size = ToastSize.Medium)
    {
        var buttons = new List<ToastButton>
        {
            ToastButtonPresets.Retry(onRetry)
        };

        return ShowToastWithButtons(message, ToastType.Error, title, buttons, timeoutInSeconds, size);
    }

    public string ShowSuccess(string message, string title = "Success", int? timeoutInSeconds = 5, ToastSize size = ToastSize.Medium)
    {
        return ShowToast(message, ToastType.Success, title, timeoutInSeconds, size);
    }

    public string ShowError(string message, string title = "Error", int? timeoutInSeconds = null, ToastSize size = ToastSize.Medium) // Errors don't auto-dismiss by default
    {
        return ShowToast(message, ToastType.Error, title, timeoutInSeconds, size);
    }

    public string ShowWarning(string message, string title = "Warning", int? timeoutInSeconds = 8, ToastSize size = ToastSize.Medium)
    {
        return ShowToast(message, ToastType.Warning, title, timeoutInSeconds, size);
    }

    public string ShowInfo(string message, string title = "Info", int? timeoutInSeconds = 5, ToastSize size = ToastSize.Medium)
    {
        return ShowToast(message, ToastType.Info, title, timeoutInSeconds, size);
    }

    public string ShowDefault(string message, string title = "Default", int? timeoutInSeconds = 5, ToastSize size = ToastSize.Medium)
    {
        return ShowToast(message, ToastType.Default, title, timeoutInSeconds, size);
    }

    public void RemoveToast(string toastId)
    {
        lock (_lock)
        {
            var toast = _toasts.FirstOrDefault(t => t.Id == toastId);
            if (toast == null) return; // Already removed
            
            // Don't immediately clean up - let exit animation play first
            
            // Mark as removing and trigger exit animation
            toast.IsRemoving = true;
            toast.IsVisible = false; // Trigger exit animation
            
            TriggerUIUpdate();
            
            // Schedule cleanup and removal after animation
            var removalTimer = new System.Timers.Timer(400); // Give animation time to complete
            removalTimer.Elapsed += (_, _) =>
            {
                lock (_lock)
                {
                    var toastToRemove = _toasts.FirstOrDefault(t => t.Id == toastId);
                    if (toastToRemove != null)
                    {
                        _toasts.Remove(toastToRemove);
                        CleanupToast(toastId); // Clean up timers after removal
                        
                        // If this was the active toast, clear active ID
                        if (_activeToastId == toastId)
                        {
                            _activeToastId = null;
                        }
                        
                        // Process queue to show next toast and set new active timeout
                        ProcessToastQueue();
                    }
                }
                removalTimer.Dispose();
            };
            removalTimer.AutoReset = false;
            removalTimer.Start();
        }
    }

    public void RemoveAll()
    {
        lock (_lock)
        {
            // Clear both visible toasts and queue
            var toastIds = _toasts.Select(t => t.Id).ToList();
            foreach (var toastId in toastIds)
            {
                CleanupToast(toastId);
            }
            _toasts.Clear();
            _toastQueue.Clear();
            _lastProgressValues.Clear();
            _activeToastId = null;
            TriggerUIUpdate();
        }
    }

    // Queue management methods
    public void SetMaxVisibleToasts(int maxVisible)
    {
        lock (_lock)
        {
            MaxVisibleToasts = maxVisible;
            ProcessToastQueue(); // Reprocess queue with new limit
        }
    }

    public void ClearQueue()
    {
        lock (_lock)
        {
            _toastQueue.Clear();
            TriggerUIUpdate();
        }
    }

    // Additional queue management convenience methods
    public void SetQueueSize(int size) => SetMaxVisibleToasts(size);
    
    public (int visible, int queued, int total) GetQueueStatus()
    {
        lock (_lock)
        {
            return (_toasts.Count, _toastQueue.Count, _toasts.Count + _toastQueue.Count);
        }
    }

    public void FlushQueue()
    {
        lock (_lock)
        {
            // Show all queued toasts immediately by temporarily increasing limit
            var originalLimit = _maxVisibleToasts;
            _maxVisibleToasts = _toasts.Count + _toastQueue.Count;
            ProcessToastQueue();
            _maxVisibleToasts = originalLimit;
        }
    }

    public bool IsToastActive(string toastId)
    {
        return _activeToastId == toastId;
    }

    public ToastItem? GetToast(string toastId)
    {
        lock (_lock)
        {
            return _toasts.FirstOrDefault(t => t.Id == toastId);
        }
    }

    public bool UpdateToast(string toastId, string? newMessage = null, string? newTitle = null, ToastType? newType = null)
    {
        lock (_lock)
        {
            var toast = GetToast(toastId);
            if (toast == null) return false;

            if (newMessage != null) toast.UpdateMessage(newMessage);
            if (newTitle != null) toast.UpdateTitle(newTitle);
            if (newType != null) toast.UpdateType(newType.Value);

            TriggerUIUpdate();
            return true;
        }
    }

    public bool AddButtonToToast(string toastId, ToastButton button)
    {
        lock (_lock)
        {
            var toast = GetToast(toastId);
            if (toast == null) return false;

            toast.AddButton(button);
            TriggerUIUpdate();
            return true;
        }
    }

    public bool RemoveButtonFromToast(string toastId, string buttonId)
    {
        lock (_lock)
        {
            var toast = GetToast(toastId);
            if (toast == null) return false;

            toast.RemoveButton(buttonId);
            TriggerUIUpdate();
            return true;
        }
    }

    public void ExtendTimeout(string toastId, int additionalSeconds)
    {
        lock (_lock)
        {
            var toast = GetToast(toastId);
            if (toast?.TimeoutInSeconds != null)
            {
                // Reset timers with extended time
                CleanupToast(toastId);
                toast.TimeoutInSeconds += additionalSeconds;
                toast.Progress = 100.0;
                SetupTimeout(toast);
                _toastStartTimes[toast.Id] = DateTime.Now;
                _lastProgressValues[toast.Id] = 100.0;
            }
        }
    }

    public void MakePersistent(string toastId)
    {
        lock (_lock)
        {
            var toast = GetToast(toastId);
            if (toast != null)
            {
                toast.TimeoutInSeconds = null;
                toast.Progress = 100.0;
                CleanupToast(toastId);
                TriggerUIUpdate();
            }
        }
    }

    public void SetPosition(ToastPosition position)
    {
        Position = position;
        TriggerUIUpdate(); // Update UI to reflect new position
    }

    public void SetAnimation(ToastAnimation animation)
    {
        Animation = animation;
        TriggerUIUpdate(); // Update UI to reflect new animation
    }

    public void SetTheme(ToastTheme theme)
    {
        Theme = theme;
        TriggerUIUpdate(); // Update UI to reflect new theme
    }

    // Theme-aware content styling helpers
    public string GetContentBoxStyle(string type = "info")
    {
        return Theme switch
        {
            ToastTheme.Light => GetLightContentBoxStyle(type),
            ToastTheme.Dark => GetDarkContentBoxStyle(type),
            ToastTheme.Colored => GetColoredContentBoxStyle(type),
            _ => GetLightContentBoxStyle(type)
        };
    }

    public string GetSecondaryTextStyle()
    {
        return Theme switch
        {
            ToastTheme.Light => "color: #6b7280;",
            ToastTheme.Dark => "color: #9ca3af;",
            ToastTheme.Colored => "color: rgba(255, 255, 255, 0.8);",
            _ => "color: #6b7280;"
        };
    }

    private string GetLightContentBoxStyle(string type)
    {
        return type switch
        {
            "info" => "background: #f3e5f5; border: 1px solid #ce93d8; border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #4a148c;",
            "warning" => "background: #fff3e0; border: 1px solid #ffb74d; border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #e65100;",
            "success" => "background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #14532d;",
            "error" => "background: #fef2f2; border: 1px solid #fecaca; border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #991b1b;",
            _ => "background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #334155;"
        };
    }

    private string GetDarkContentBoxStyle(string type)
    {
        return type switch
        {
            "info" => "background: #4a148c; border: 1px solid #7b1fa2; border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #e1bee7;",
            "warning" => "background: #e65100; border: 1px solid #ff9800; border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #ffe0b3;",
            "success" => "background: #14532d; border: 1px solid #16a34a; border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #bbf7d0;",
            "error" => "background: #991b1b; border: 1px solid #dc2626; border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #fecaca;",
            _ => "background: #334155; border: 1px solid #475569; border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #cbd5e1;"
        };
    }

    private string GetColoredContentBoxStyle(string type)
    {
        return "background: rgba(255, 255, 255, 0.15); border: 1px solid rgba(255, 255, 255, 0.3); border-radius: 6px; padding: 8px; margin: 8px 0; font-size: 12px; color: #ffffff;";
    }

    private void ProcessToastQueue()
    {
        // Move toasts from queue to visible collection up to max visible limit
        while (_toastQueue.Count > 0 && _toasts.Count < _maxVisibleToasts)
        {
            var toast = _toastQueue.Dequeue();
            _toasts.Add(toast);
            
            // Trigger entrance animation after a brief delay
            Task.Delay(50).ContinueWith(_ =>
            {
                toast.IsVisible = true;
                TriggerUIUpdate();
            });
        }
        
        // Set up active timeout for the first visible toast (if any) that has a timeout
        SetupActiveTimeout();
        
        TriggerUIUpdate();
    }

    private void SetupActiveTimeout()
    {
        // Clear existing active timeout
        if (_activeToastId != null)
        {
            CleanupToast(_activeToastId);
            _activeToastId = null;
        }
        
        // Find first visible toast with timeout that isn't removing
        var activeToast = _toasts.FirstOrDefault(t => t.TimeoutInSeconds.HasValue && !t.IsRemoving);
        if (activeToast != null)
        {
            _activeToastId = activeToast.Id;
            SetupTimeout(activeToast);
            _toastStartTimes[activeToast.Id] = DateTime.Now;
            _lastProgressValues[activeToast.Id] = 100.0;
        }
    }

    private void SetupTimeout(ToastItem toast)
    {
        if (!toast.TimeoutInSeconds.HasValue) return;

        var timer = new System.Timers.Timer(toast.TimeoutInSeconds.Value * 1000);
        timer.Elapsed += (_, _) => 
        {
            timer.Dispose(); // Clean up immediately
            RemoveToast(toast.Id);
        };
        timer.AutoReset = false;
        timer.Start();
        
        _timers[toast.Id] = timer;
    }

    // Single method to update progress bar for the active toast only
    private void UpdateAllProgress(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            var now = DateTime.Now;
            bool progressUpdated = false;
            
            // Only update progress for the active toast
            if (_activeToastId != null)
            {
                var activeToast = _toasts.FirstOrDefault(t => t.Id == _activeToastId && !t.IsStateful && !t.IsRemoving);
                if (activeToast?.TimeoutInSeconds.HasValue == true && _toastStartTimes.TryGetValue(_activeToastId, out var startTime))
                {
                    var elapsed = (now - startTime).TotalSeconds;
                    var totalSeconds = activeToast.TimeoutInSeconds!.Value;
                    var progress = Math.Max(0, 100.0 - (elapsed * 100.0 / totalSeconds));
                    
                    // Update progress more frequently for smoother animation
                    if (!_lastProgressValues.TryGetValue(_activeToastId, out var lastProgress) || 
                        Math.Abs(progress - lastProgress) > 0.1) // Reduced threshold for more frequent updates
                    {
                        activeToast.Progress = progress;
                        _lastProgressValues[_activeToastId] = progress;
                        progressUpdated = true;
                    }
                }
            }
            
            // Trigger UI update if progress was updated
            if (progressUpdated)
            {
                TriggerUIUpdate();
            }
        }
    }

    // Batch UI updates to prevent frequent re-renders
    private void TriggerUIUpdate()
    {
        _uiUpdatePending = true;
    }

    private void CleanupToast(string toastId)
    {
        if (_timers.TryGetValue(toastId, out var timer))
        {
            timer.Stop();
            timer.Dispose();
            _timers.Remove(toastId);
        }

        // Clean up auto-advance timer for stateful toasts
        var advanceKey = $"{toastId}_advance";
        if (_timers.TryGetValue(advanceKey, out var advanceTimer))
        {
            advanceTimer.Stop();
            advanceTimer.Dispose();
            _timers.Remove(advanceKey);
        }

        _toastStartTimes.Remove(toastId);
        _lastProgressValues.Remove(toastId);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            // Stop and dispose master timer
            _masterProgressTimer.Stop();
            _masterProgressTimer.Dispose();
            
            // Stop and dispose UI update timer
            _uiUpdateTimer.Stop();
            _uiUpdateTimer.Dispose();
            
            // Clean up all timeout timers
            foreach (var timer in _timers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }
            _timers.Clear();
            _toastStartTimes.Clear();
            _lastProgressValues.Clear();
        }
    }

    // Stateful toast methods
    public string ShowStatefulToast(List<ToastState> states, bool startImmediately = true, ToastSize size = ToastSize.Large)
    {
        if (states == null || states.Count == 0)
            throw new ArgumentException("Stateful toast must have at least one state", nameof(states));

        var toast = new ToastItem
        {
            TimeoutInSeconds = null, // Stateful toasts should never auto-dismiss at the toast level
            Size = size // Stateful toasts default to large size for better interaction space
        };

        // Initialize with states and properly set up the first state content
        toast.InitializeWithStates(states);
        
        if (!startImmediately)
        {
            toast.CurrentStateIndex = -1;
        }

        lock (_lock)
        {
            // Add stateful toast to queue like regular toasts
            _toastQueue.Enqueue(toast);
            
            // Setup auto-advance if the first state has it enabled
            if (startImmediately)
            {
                // Add a small delay to ensure the toast is fully initialized before setting up auto-advance
                Task.Delay(100).ContinueWith(_ =>
                {
                    SetupAutoAdvance(toast);
                });
            }
            
            // Process queue to show toasts
            ProcessToastQueue();
            
            return toast.Id;
        }
    }

    public bool TransitionToNextState(string toastId)
    {
        lock (_lock)
        {
            var toast = GetToast(toastId);
            if (toast?.IsStateful != true || !toast.HasNextState()) return false;

            // Trigger transition animation
            toast.IsTransitioning = true;
            
            // Perform the transition after a brief delay for animation
            Task.Delay(50).ContinueWith(_ =>
            {
                lock (_lock)
                {
                    toast.NextState();
                    
                    // Clean up any existing timers but don't setup global timeout for stateful toasts
                    CleanupToast(toastId);
                    
                    // Setup auto-advance for the new state
                    SetupAutoAdvance(toast);
                    
                    // End transition
                    Task.Delay(300).ContinueWith(_ =>
                    {
                        toast.IsTransitioning = false;
                        TriggerUIUpdate();
                    });
                    
                    TriggerUIUpdate();
                }
            });
            
            return true;
        }
    }

    public bool TransitionToPreviousState(string toastId)
    {
        lock (_lock)
        {
            var toast = GetToast(toastId);
            if (toast?.IsStateful != true || !toast.HasPreviousState()) return false;

            toast.IsTransitioning = true;
            
            Task.Delay(50).ContinueWith(_ =>
            {
                lock (_lock)
                {
                    toast.PreviousState();
                    
                    // Clean up any existing timers but don't setup global timeout for stateful toasts
                    CleanupToast(toastId);
                    
                    // Setup auto-advance for the new state
                    SetupAutoAdvance(toast);
                    
                    Task.Delay(300).ContinueWith(_ =>
                    {
                        toast.IsTransitioning = false;
                        TriggerUIUpdate();
                    });
                    
                    TriggerUIUpdate();
                }
            });
            
            return true;
        }
    }

    public bool TransitionToState(string toastId, int stateIndex)
    {
        lock (_lock)
        {
            var toast = GetToast(toastId);
            if (toast?.IsStateful != true || stateIndex < 0 || stateIndex >= toast.States.Count) return false;

            toast.IsTransitioning = true;
            
            Task.Delay(50).ContinueWith(_ =>
            {
                lock (_lock)
                {
                    toast.GoToState(stateIndex);
                    
                    // Clean up any existing timers but don't setup global timeout for stateful toasts
                    CleanupToast(toastId);
                    
                    // Setup auto-advance for the new state
                    SetupAutoAdvance(toast);
                    
                    Task.Delay(300).ContinueWith(_ =>
                    {
                        toast.IsTransitioning = false;
                        TriggerUIUpdate();
                    });
                    
                    TriggerUIUpdate();
                }
            });
            
            return true;
        }
    }

    private void SetupAutoAdvance(ToastItem toast)
    {
        if (!toast.IsStateful) return;
        
        var currentState = toast.GetCurrentState();
        if (currentState?.AutoAdvanceToNext == true && toast.HasNextState())
        {
            var advanceTimer = new System.Timers.Timer(currentState.AutoAdvanceDelayMs);
            advanceTimer.Elapsed += (_, _) =>
            {
                TransitionToNextState(toast.Id);
                advanceTimer.Dispose();
            };
            advanceTimer.AutoReset = false;
            advanceTimer.Start();
            
            // Store the advance timer with a special key
            _timers[$"{toast.Id}_advance"] = advanceTimer;
        }
    }

    // Preset stateful toast patterns
    public string ShowWorkflowToast(string title, List<(string message, ToastType type, List<ToastButton>? buttons, bool autoAdvance, int autoAdvanceDelayMs)> steps, ToastSize size = ToastSize.Large)
    {
        var states = new List<ToastState>();
        
        for (int i = 0; i < steps.Count; i++)
        {
            var (message, type, buttons, autoAdvance, autoAdvanceDelayMs) = steps[i];
            
            states.Add(new ToastState
            {
                Title = $"{title} - Step {i + 1}/{steps.Count}",
                Message = message,
                Type = type,
                Buttons = buttons ?? new List<ToastButton>(),
                AutoAdvanceToNext = autoAdvance,
                AutoAdvanceDelayMs = autoAdvanceDelayMs,
                TimeoutInSeconds = null // States in stateful toasts use auto-advance, not timeouts
            });
        }
        
        return ShowStatefulToast(states, true, size);
    }

    public string ShowProgressToast(string title, List<string> steps, bool autoAdvance = true, int stepDelayMs = 2000, ToastSize size = ToastSize.Medium)
    {
        var states = new List<ToastState>();
        
        for (int i = 0; i < steps.Count; i++)
        {
            var isLast = i == steps.Count - 1;
            
            states.Add(new ToastState
            {
                Title = title,
                Message = steps[i],
                Type = isLast ? ToastType.Success : ToastType.Info,
                AutoAdvanceToNext = autoAdvance && !isLast,
                AutoAdvanceDelayMs = stepDelayMs,
                TimeoutInSeconds = null // Use auto-advance instead of timeout
            });
        }
        
        // For the last step, add a button to close or let user close manually
        if (states.Count > 0)
        {
            states[^1].Buttons.Add(new ToastButton 
            { 
                Text = "Done", 
                OnClick = (id) => RemoveToast(id), 
                CloseToastOnClick = true 
            });
        }
        
        return ShowStatefulToast(states, true, size);
    }

    public string ShowQuestionnaireToast(string title, List<(string question, List<ToastButton> answers)> questions, ToastSize size = ToastSize.Large)
    {
        var states = new List<ToastState>();
        
        for (int i = 0; i < questions.Count; i++)
        {
            var (question, answers) = questions[i];
            
            states.Add(new ToastState
            {
                Title = $"{title} - Question {i + 1}/{questions.Count}",
                Message = question,
                Type = ToastType.Info,
                Buttons = answers,
                TimeoutInSeconds = null // Questions should not auto-dismiss
            });
        }
        
        return ShowStatefulToast(states, true, size);
    }

    // Advanced workflow methods
    public string ShowBranchingWorkflow(string title, ToastSize size = ToastSize.Large)
    {
        var states = new List<ToastState>
        {
            new()
            {
                Title = title,
                Message = "Please select your workflow path:",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.JumpToState(1, "Path A: Quick Setup"),
                    ToastButtonPresets.JumpToState(3, "Path B: Advanced Setup"),
                    ToastButtonPresets.JumpToState(5, "Path C: Custom Setup")
                }
            },
            // Path A: Quick Setup (states 1-2)
            new()
            {
                Title = "Quick Setup - Step 1",
                Message = "We'll configure everything with default settings.",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.SkipAndAdvance(new List<int> { 3, 4, 5, 6 }, "Continue with Quick Setup")
                }
            },
            new()
            {
                Title = "Quick Setup Complete",
                Message = "Your setup is complete with default settings!",
                Type = ToastType.Success,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.JumpToState(7, "Finish")
                }
            },
            // Path B: Advanced Setup (states 3-4)
            new()
            {
                Title = "Advanced Setup - Configuration",
                Message = "Please configure your advanced settings:",
                Type = ToastType.Warning,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.NextStep(),
                    ToastButtonPresets.JumpToState(0, "Back to Path Selection")
                }
            },
            new()
            {
                Title = "Advanced Setup - Review",
                Message = "Please review your advanced configuration:",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.SkipAndAdvance(new List<int> { 5, 6 }, "Complete Advanced Setup")
                }
            },
            // Path C: Custom Setup (states 5-6)
            new()
            {
                Title = "Custom Setup - Options",
                Message = "Choose your custom configuration options:",
                Type = ToastType.Warning,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.NextStep(),
                    ToastButtonPresets.ConditionalChoice("Smart Config", toastId => 
                    {
                        // Simulate smart configuration logic
                        return Random.Shared.Next(2) == 0 ? 6 : 7; // Random path for demo
                    }),
                    ToastButtonPresets.JumpToState(0, "Start Over")
                }
            },
            new()
            {
                Title = "Custom Setup - Finalization",
                Message = "Finalizing your custom configuration...",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.NextStep()
                }
            },
            // Final state (state 7)
            new()
            {
                Title = "Setup Complete!",
                Message = "Your workflow has been completed successfully.",
                Type = ToastType.Success,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.Confirm(toastId => RemoveToast(toastId))
                }
            }
        };

        return ShowStatefulToast(states, true, size);
    }

    public string ShowConditionalWorkflow(string title, ToastSize size = ToastSize.Large)
    {
        var states = new List<ToastState>
        {
            new()
            {
                Title = title,
                Message = "Do you have an existing account?",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.JumpToState(1, "Yes, I have an account"),
                    ToastButtonPresets.JumpToState(3, "No, create new account")
                }
            },
            // Existing account flow (states 1-2)
            new()
            {
                Title = "Account Login",
                Message = "Please enter your existing account credentials:",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.NextStep(),
                    ToastButtonPresets.JumpToState(0, "Back")
                }
            },
            new()
            {
                Title = "Login Verification",
                Message = "Verifying your credentials...",
                Type = ToastType.Warning,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.SkipAndAdvance(new List<int> { 3, 4 }, "Login Successful")
                }
            },
            // New account flow (states 3-4)
            new()
            {
                Title = "Create Account",
                Message = "Please provide your information to create a new account:",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.NextStep(),
                    ToastButtonPresets.JumpToState(0, "Back")
                }
            },
            new()
            {
                Title = "Account Setup",
                Message = "Setting up your new account...",
                Type = ToastType.Warning,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.NextStep()
                }
            },
            // Common completion state (state 5)
            new()
            {
                Title = "Welcome!",
                Message = "You're all set up and ready to go.",
                Type = ToastType.Success,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.Confirm(toastId => RemoveToast(toastId))
                }
            }
        };

        return ShowStatefulToast(states, true, size);
    }

    public string ShowMultiPathQuestionnaire(string title, ToastSize size = ToastSize.Large)
    {
        var states = new List<ToastState>
        {
            new()
            {
                Title = title,
                Message = "What type of user are you?",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.JumpToState(1, "Beginner"),
                    ToastButtonPresets.JumpToState(3, "Intermediate"),
                    ToastButtonPresets.JumpToState(5, "Advanced")
                }
            },
            // Beginner path (states 1-2)
            new()
            {
                Title = "Beginner Questions",
                Message = "How often do you use similar applications?",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.ConditionalChoice("Rarely", toastId => 7), // Skip to basic results
                    ToastButtonPresets.NextStep()
                }
            },
            new()
            {
                Title = "Beginner Setup",
                Message = "We'll configure everything for ease of use:",
                Type = ToastType.Success,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.SkipAndAdvance(new List<int> { 3, 4, 5, 6 }, "Apply Beginner Settings")
                }
            },
            // Intermediate path (states 3-4)
            new()
            {
                Title = "Intermediate Questions",
                Message = "Do you want advanced features enabled?",
                Type = ToastType.Warning,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.JumpToState(5, "Yes, enable advanced features"),
                    ToastButtonPresets.NextStep()
                }
            },
            new()
            {
                Title = "Intermediate Setup",
                Message = "Configuring balanced settings for you:",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.SkipAndAdvance(new List<int> { 5, 6 }, "Apply Intermediate Settings")
                }
            },
            // Advanced path (states 5-6)
            new()
            {
                Title = "Advanced Configuration",
                Message = "Select your expert-level preferences:",
                Type = ToastType.Warning,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.NextStep(),
                    ToastButtonPresets.ConditionalChoice("Auto-Configure", toastId => 7)
                }
            },
            new()
            {
                Title = "Expert Settings",
                Message = "Applying advanced configuration...",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.NextStep()
                }
            },
            // Results state (state 7)
            new()
            {
                Title = "Configuration Complete",
                Message = "Your personalized settings have been applied!",
                Type = ToastType.Success,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.Confirm(toastId => RemoveToast(toastId)),
                    ToastButtonPresets.JumpToState(0, "Start Over")
                }
            }
        };

        return ShowStatefulToast(states, true, size);
    }

    public string ShowDataProcessingWorkflow(string title, ToastSize size = ToastSize.Large)
    {
        var states = new List<ToastState>
        {
            new()
            {
                Title = title,
                Message = "Select your data processing option:",
                Type = ToastType.Info,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.JumpToState(1, "Quick Processing"),
                    ToastButtonPresets.JumpToState(3, "Custom Processing"),
                    ToastButtonPresets.ConditionalChoice("Smart Auto", toastId => 
                    {
                        // Simulate data analysis to determine best path
                        var dataSize = Random.Shared.Next(1000);
                        return dataSize > 500 ? 3 : 1; // Large data = custom, small = quick
                    })
                }
            },
            // Quick processing path (states 1-2)
            new()
            {
                Title = "Quick Processing",
                Message = "Processing with standard algorithms...",
                Type = ToastType.Info,
                AutoAdvanceAfterSeconds = 2,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.JumpToState(5, "Skip to Results"),
                    ToastButtonPresets.Cancel(toastId => RemoveToast(toastId))
                }
            },
            new()
            {
                Title = "Quick Processing Complete",
                Message = "Standard processing completed successfully!",
                Type = ToastType.Success,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.SkipAndAdvance(new List<int> { 3, 4 }, "View Results")
                }
            },
            // Custom processing path (states 3-4)  
            new()
            {
                Title = "Custom Processing Setup",
                Message = "Configure your processing parameters:",
                Type = ToastType.Warning,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.NextStep(),
                    ToastButtonPresets.SkipAndAdvance(new List<int> { 4 }, "Use Defaults"),
                    ToastButtonPresets.JumpToState(0, "Back to Options")
                }
            },
            new()
            {
                Title = "Custom Processing Running",
                Message = "Processing with your custom parameters...",
                Type = ToastType.Warning,
                AutoAdvanceAfterSeconds = 3,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.Cancel(toastId => RemoveToast(toastId))
                }
            },
            // Results state (state 5)
            new()
            {
                Title = "Processing Results",
                Message = "Your data has been processed successfully!",
                Type = ToastType.Success,
                Buttons = new List<ToastButton>
                {
                    ToastButtonPresets.Confirm(toastId => RemoveToast(toastId)),
                    ToastButtonPresets.JumpToState(0, "Process More Data")
                }
            }
        };

        return ShowStatefulToast(states, true, size);
    }

    // Helper method for creating custom conditional workflows
    public string ShowCustomConditionalWorkflow(List<ToastState> states, ToastSize size = ToastSize.Large)
    {
        return ShowStatefulToast(states, true, size);
    }
} 