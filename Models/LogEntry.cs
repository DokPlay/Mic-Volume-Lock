using System.Text;

namespace MicVolumeLock.Models;

public sealed class LogEntry
{
    public DateTime Timestamp { get; } = DateTime.Now;

    public string Source { get; init; } = string.Empty;

    public string? EndpointId { get; init; }

    public int? PreviousPercent { get; init; }

    public int? NewPercent { get; init; }

    public string Result { get; init; } = "ok";

    public string? Message { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"[{Timestamp:HH:mm:ss}] ");
        sb.Append(Source);
        if (PreviousPercent.HasValue || NewPercent.HasValue)
        {
            var oldValue = PreviousPercent?.ToString() ?? "-";
            var newValue = NewPercent?.ToString() ?? "-";
            sb.Append($" | {oldValue}% → {newValue}%");
        }

        if (!string.IsNullOrWhiteSpace(Message))
        {
            sb.Append(" | ");
            sb.Append(Message);
        }

        return sb.ToString();
    }
}

