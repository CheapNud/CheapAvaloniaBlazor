using Avalonia.Input;

namespace CheapAvaloniaBlazor.Models;

/// <summary>
/// Represents a registered global hotkey with its callback.
/// </summary>
public sealed record HotkeyRegistration(
    int Id,
    HotkeyModifiers Modifiers,
    Key Key,
    Action Callback);
