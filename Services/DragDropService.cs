using CheapAvaloniaBlazor.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Singleton orchestrator for file drag-and-drop events.
/// Registers handlers with <see cref="PhotinoMessageHandler"/> for JavaScript drag events
/// bridged via the Photino message channel (<c>window.chrome.webview.postMessage</c>).
/// </summary>
internal sealed class DragDropService : IDragDropService
{
    private readonly ILogger<DragDropService> _logger;

    public event Action<IReadOnlyList<DroppedFileInfo>>? FilesDropped;
    public event Action? DragEnter;
    public event Action? DragLeave;
    public bool IsDragOver { get; private set; }

    public DragDropService(PhotinoMessageHandler messageHandler, ILogger<DragDropService> logger)
    {
        _logger = logger;

        messageHandler.RegisterMessageHandler(Constants.DragDrop.DragEnterMessage, OnDragEnter);
        messageHandler.RegisterMessageHandler(Constants.DragDrop.DragLeaveMessage, OnDragLeave);
        messageHandler.RegisterMessageHandler(Constants.DragDrop.FileDropMessage, OnFileDrop);

        _logger.LogDebug("DragDropService: Registered message handlers for drag-and-drop events");
    }

    private Task<string> OnDragEnter(string payload)
    {
        IsDragOver = true;
        InvokeHandlersSafely(() => DragEnter?.Invoke(), "DragEnter");
        return Task.FromResult(string.Empty);
    }

    private Task<string> OnDragLeave(string payload)
    {
        IsDragOver = false;
        InvokeHandlersSafely(() => DragLeave?.Invoke(), "DragLeave");
        return Task.FromResult(string.Empty);
    }

    private Task<string> OnFileDrop(string payload)
    {
        IsDragOver = false;

        try
        {
            var jsFiles = JsonSerializer.Deserialize<List<JsDroppedFile>>(payload, JsonOptions);

            if (jsFiles is not { Count: > 0 })
            {
                _logger.LogDebug("DragDropService: Received file drop with no files");
                return Task.FromResult(string.Empty);
            }

            var droppedFiles = jsFiles
                .Select(MapToDroppedFileInfo)
                .ToList()
                .AsReadOnly();

            _logger.LogInformation("DragDropService: {Count} file(s) dropped: {Names}",
                droppedFiles.Count,
                string.Join(", ", droppedFiles.Select(f => f.Name)));

            InvokeHandlersSafely(() => FilesDropped?.Invoke(droppedFiles), "FilesDropped");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "DragDropService: Failed to deserialize file drop payload");
        }

        return Task.FromResult(string.Empty);
    }

    private static DroppedFileInfo MapToDroppedFileInfo(JsDroppedFile jsFile) => new()
    {
        Name = jsFile.Name ?? "unknown",
        Size = jsFile.Size,
        ContentType = jsFile.Type ?? string.Empty,
        LastModified = DateTimeOffset.FromUnixTimeMilliseconds(jsFile.LastModified),
    };

    private void InvokeHandlersSafely(Action action, string eventName)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DragDropService: {EventName} event handler threw", eventName);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// DTO matching the JSON shape sent by cheap-blazor-interop.js setupFileDrop.
    /// </summary>
    private sealed class JsDroppedFile
    {
        public string? Name { get; set; }
        public long Size { get; set; }
        public string? Type { get; set; }
        public long LastModified { get; set; }
    }
}
