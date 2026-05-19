using System.IO;
using MicVolumeLock.Models;

namespace MicVolumeLock.Services;

public static class ProcessExclusionService
{
    private static readonly HashSet<string> BlockedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "audiodg"
    };

    public static string Normalize(string? processName)
    {
        return string.IsNullOrWhiteSpace(processName)
            ? string.Empty
            : Path.GetFileNameWithoutExtension(processName.Trim());
    }

    public static bool CanExclude(string? processName)
    {
        var normalized = Normalize(processName);
        return !string.IsNullOrWhiteSpace(normalized) && !BlockedNames.Contains(normalized);
    }

    public static bool Clean(AppConfig config)
    {
        var changed = false;
        config.DiagnosticIgnoredProcesses ??= new List<string>();

        var cleaned = config.DiagnosticIgnoredProcesses
            .Select(Normalize)
            .Where(CanExclude)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (cleaned.Count != config.DiagnosticIgnoredProcesses.Count ||
            !cleaned.SequenceEqual(config.DiagnosticIgnoredProcesses, StringComparer.OrdinalIgnoreCase))
        {
            config.DiagnosticIgnoredProcesses = cleaned;
            changed = true;
        }

        if (!CanExclude(config.DiagnosticSelectedProcessName))
        {
            if (!string.IsNullOrWhiteSpace(config.DiagnosticSelectedProcessName))
            {
                changed = true;
            }

            config.DiagnosticSelectedProcessName = cleaned.FirstOrDefault();
        }
        else
        {
            var normalizedSelected = Normalize(config.DiagnosticSelectedProcessName);
            if (!string.Equals(config.DiagnosticSelectedProcessName, normalizedSelected, StringComparison.Ordinal))
            {
                config.DiagnosticSelectedProcessName = normalizedSelected;
                changed = true;
            }
        }

        return changed;
    }
}
