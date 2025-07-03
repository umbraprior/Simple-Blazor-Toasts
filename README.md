# Simple Blazor Toasts

A comprehensive toast notification system for Blazor applications featuring:

- 🎯 **5 Toast Types**: Success, Error, Warning, Info, Default
- 📏 **3 Sizes**: Small (280px), Medium (400px), Large (600px)  
- 🎭 **5 Animation Styles**: Slide, Fade, SlideAndFade, Scale, SlideAndScale
- 🌈 **3 Themes**: Light, Dark, Colored
- 📍 **6 Positions**: All corners and centers
- 🔄 **Stateful Workflows**: Multi-step processes with navigation
- ⏱️ **Smart Queueing**: Configurable limits with automatic processing
- 🎨 **Interactive Buttons**: Custom actions with presets
- 📊 **Progress Tracking**: Visual progress bars and timers

Check out the twin [modal dialog](https://github.com/umbraprior/Simple-Blazor-Dialogs) library!

## Preview Gallery

<details>
<summary><strong>🎨 Toast Types & Themes</strong></summary>

![Mixed](https://github.com/user-attachments/assets/3fa0aca0-c153-40e3-ac71-a16696811e7b)


![Sizes](https://github.com/user-attachments/assets/13c6d453-2de7-437a-b6bb-5c131bc2445d)


![Stateful](https://github.com/user-attachments/assets/8d27792a-0dec-406b-873a-2e1be1241eb8)


![scale](https://github.com/user-attachments/assets/8593fe74-40d6-462b-ac98-74b0753e7cf9) ![slidefade](https://github.com/user-attachments/assets/d3b3ad8d-0ad2-407c-b568-dbdad800ffd1)

</details>

## Installation

```bash
dotnet add package Simple.Blazor.Toasts
```

## Quick Start

### 1. Register Services

```csharp
// Program.cs
using Simple.Blazor.Toasts.Extensions;

builder.Services.AddSimpleBlazorToasts();
```

### 2. Add CSS Reference

```html
<!-- index.html or _Host.cshtml -->
<link href="_content/Simple.Blazor.Toasts/css/toast.css" rel="stylesheet" />
```

### 3. Add Toast Container

```razor
<!-- MainLayout.razor -->
@using Simple.Blazor.Toasts.Components
<ToastContainer />
```

### 4. Use in Components

```csharp
@using Simple.Blazor.Toasts.Services
@inject ToastService ToastService

<button @onclick="ShowToast">Show Toast</button>

@code {
    void ShowToast()
    {
        ToastService.ShowSuccess("Hello World!", "Success");
    }
}
```

## Programmatic API

<details>
<summary><strong>📝 Basic Toast Methods</strong></summary>

```csharp
// Simple success toast
string toastId = ToastService.ShowSuccess("Operation completed!", "Success");

// Error toast (no auto-dismiss by default)
ToastService.ShowError("Something went wrong", "Error");

// Warning with custom timeout
ToastService.ShowWarning("Please review your data", "Warning", timeoutInSeconds: 10);

// Info toast with specific size
ToastService.ShowInfo("Processing started", "Info", timeoutInSeconds: 5, size: ToastSize.Large);

// Default toast
ToastService.ShowDefault("General message", "Default", timeoutInSeconds: 3);
```

</details>

<details>
<summary><strong>🎛️ Advanced Toast Creation</strong></summary>

```csharp
// Custom toast with all parameters
var toastId = ToastService.ShowToast(
    message: "Custom toast message",
    type: ToastType.Warning,
    title: "Custom Title",
    timeoutInSeconds: 8,
    size: ToastSize.Medium
);

// Toast object with full control
var toast = new ToastItem
{
    Title = "Custom Toast",
    Message = "Advanced configuration",
    Type = ToastType.Info,
    Size = ToastSize.Large,
    TimeoutInSeconds = 10,
    Data = new Dictionary<string, object> { ["userId"] = 123 }
};
var id = ToastService.ShowToast(toast);
```

</details>

<details>
<summary><strong>⚙️ Toast Configuration</strong></summary>

```csharp
// Global settings
ToastService.SetPosition(ToastPosition.TopLeft);
ToastService.SetAnimation(ToastAnimation.SlideAndFade);
ToastService.SetTheme(ToastTheme.Dark);
ToastService.SetMaxVisibleToasts(3);

// Position options: TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight
// Animation options: Slide, Fade, SlideAndFade, Scale, SlideAndScale
// Theme options: Light, Dark, Colored
// Size options: Small (280px), Medium (400px), Large (600px)
```

</details>

<details>
<summary><strong>🎯 Interactive Toasts with Buttons</strong></summary>

```csharp
// Confirmation dialog
ToastService.ShowConfirmation(
    message: "Are you sure you want to delete this item?",
    title: "Confirm Delete",
    onConfirm: (toastId) => { /* Delete logic */ },
    onCancel: (toastId) => { /* Cancel logic */ },
    timeoutInSeconds: 15
);

// Undo action
ToastService.ShowUndoAction(
    message: "Item deleted successfully",
    title: "Deleted",
    onUndo: (toastId) => { /* Restore logic */ },
    timeoutInSeconds: 10
);

// Retry action
ToastService.ShowRetryAction(
    message: "Failed to save. Click retry to try again.",
    title: "Save Failed",
    onRetry: (toastId) => { /* Retry save logic */ }
);

// Custom buttons
var buttons = new List<ToastButton>
{
    new ToastButton
    {
        Text = "View Details",
        CssClass = "btn btn-primary btn-sm",
        OnClick = (toastId) => { /* Navigate to details */ },
        CloseToastOnClick = false
    },
    new ToastButton
    {
        Text = "Dismiss",
        CssClass = "btn btn-secondary btn-sm",
        OnClick = (toastId) => { ToastService.RemoveToast(toastId); },
        CloseToastOnClick = true
    }
};

ToastService.ShowToastWithButtons(
    message: "New notification received",
    type: ToastType.Info,
    title: "Notification",
    buttons: buttons,
    timeoutInSeconds: 20
);
```

</details>

<details>
<summary><strong>🔧 Toast Management</strong></summary>

```csharp
// Remove specific toast
ToastService.RemoveToast(toastId);

// Remove all toasts
ToastService.RemoveAll();

// Update existing toast
ToastService.UpdateToast(
    toastId: toastId,
    newMessage: "Updated message",
    newTitle: "Updated Title",
    newType: ToastType.Success
);

// Extend timeout
ToastService.ExtendTimeout(toastId, additionalSeconds: 5);

// Make persistent (never auto-dismiss)
ToastService.MakePersistent(toastId);

// Add button to existing toast
ToastService.AddButtonToToast(toastId, ToastButtonPresets.Retry(onRetry));

// Check if toast exists
bool isActive = ToastService.IsToastActive(toastId);

// Get toast instance
ToastItem? toast = ToastService.GetToast(toastId);
```

</details>

<details>
<summary><strong>📋 Queue Management</strong></summary>

```csharp
// Queue status
var (visible, queued, total) = ToastService.GetQueueStatus();

// Configure queue
ToastService.SetMaxVisibleToasts(5); // Max 1-10
ToastService.ClearQueue(); // Clear waiting toasts
ToastService.FlushQueue(); // Show all queued toasts immediately

// Queue properties
int queuedCount = ToastService.QueuedCount;
int maxVisible = ToastService.MaxVisibleToasts;
string? activeToastId = ToastService.ActiveToastId;
```

</details>

<details>
<summary><strong>🔄 Stateful/Workflow Toasts</strong></summary>

```csharp
// Multi-step workflow
var states = new List<ToastState>
{
    new ToastState
    {
        Title = "Step 1",
        Message = "Starting process...",
        Type = ToastType.Info,
        AutoAdvanceToNext = true,
        AutoAdvanceDelayMs = 3000
    },
    new ToastState
    {
        Title = "Step 2", 
        Message = "Processing data...",
        Type = ToastType.Warning,
        Buttons = new List<ToastButton>
        {
            ToastButtonPresets.NextStep(),
            ToastButtonPresets.Cancel()
        }
    },
    new ToastState
    {
        Title = "Complete",
        Message = "Process finished successfully!",
        Type = ToastType.Success,
        TimeoutInSeconds = 5
    }
};

string workflowId = ToastService.ShowStatefulToast(states, startImmediately: true, size: ToastSize.Large);
var toast = ToastService.GetToast(toastId);
if (toast != null)
{
    toast.ShowNavigation = false;
}

// Navigate workflow programmatically
ToastService.TransitionToNextState(workflowId);
ToastService.TransitionToPreviousState(workflowId);
ToastService.TransitionToState(workflowId, stateIndex: 2);
```

</details>

<details>
<summary><strong>🎨 Button Presets</strong></summary>

```csharp
// Available button presets
ToastButtonPresets.Confirm(onConfirm);
ToastButtonPresets.Cancel(onCancel);
ToastButtonPresets.Delete(onDelete);
ToastButtonPresets.Save(onSave);
ToastButtonPresets.Retry(onRetry);
ToastButtonPresets.Undo(onUndo);
ToastButtonPresets.ViewDetails(onViewDetails);

// Workflow navigation buttons
ToastButtonPresets.NextStep(onNext);
ToastButtonPresets.SkipStep(targetStateIndex, onSkip);
ToastButtonPresets.JumpToState(targetStateIndex, "Go to Step 3", onClick);
ToastButtonPresets.ConditionalChoice("Smart Choice", conditionalTargetFunc, onClick);
```

</details>

<details>
<summary><strong>🚀 Service Configuration (Program.cs)</strong></summary>

```csharp
// Basic registration
builder.Services.AddSimpleBlazorToasts();

// With configuration
builder.Services.AddSimpleBlazorToasts(config =>
{
    config.MaxVisibleToasts = 3;
    config.DefaultPosition = ToastPosition.TopRight;
    config.DefaultTheme = ToastTheme.Dark;
    config.DefaultAnimation = ToastAnimation.SlideAndFade;
});
```

</details>

<details>
<summary><strong>📊 Events and Monitoring</strong></summary>

```csharp
// Subscribe to toast changes
ToastService.OnToastsChanged += () =>
{
    Console.WriteLine($"Toast count: {ToastService.Toasts.Count}");
    Console.WriteLine($"Queue size: {ToastService.QueuedCount}");
};

// Access all active toasts
IReadOnlyCollection<ToastItem> activeToasts = ToastService.Toasts;
```

</details>

Take a look at the ***[sample page](https://github.com/umbraprior/Simple-Blazor-Toasts/blob/main/src/Samples/Components/Pages/ToastDemo.razor)***
