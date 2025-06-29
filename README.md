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

Take a look at the ***[sample page](https://github.com/umbraprior/Simple-Blazor-Toasts/blob/main/src/Samples/Components/Pages/ToastDemo.razor)***