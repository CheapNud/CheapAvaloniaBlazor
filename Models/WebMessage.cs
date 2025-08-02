namespace CheapAvaloniaBlazor.Models;

internal class WebMessage
{
    public string? Type { get; set; }
    public string? Id { get; set; }
    public string? Payload { get; set; }
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
}