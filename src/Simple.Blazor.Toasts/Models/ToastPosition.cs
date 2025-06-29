namespace Simple.Blazor.Toasts.Models;

public enum ToastPosition
{
    TopLeft,
    TopRight,
    TopCenter,
    BottomLeft,
    BottomRight,
    BottomCenter
}

public enum ToastAnimation
{
    Slide,          // Slide from/to nearest edge based on position
    Fade,           // Simple fade in/out
    SlideAndFade,   // Combination of slide + fade
    Scale,          // Scale up/down from center
    SlideAndScale   // Slide + scale effect
} 