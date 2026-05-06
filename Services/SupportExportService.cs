using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using MicVolumeLock.Models;

namespace MicVolumeLock.Services;

public static class SupportExportService
{
    public static string Export(AppConfig config, IEnumerable<MicDeviceInfo> devices, IEnumerable<LogEntry> recentLog)
    {
        Directory.CreateDirectory(ConfigService.ConfigDirectory);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var exportPath = Path.Combine(ConfigService.ConfigDirectory, $"MicVolumeLock-support-{stamp}.zip");
        var tempDir = Path.Combine(ConfigService.ConfigDirectory, $"support-{stamp}");

        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }

        Directory.CreateDirectory(tempDir);

        try
        {
            WriteText(Path.Combine(tempDir, "summary.txt"), BuildClipboardReport(config, devices, recentLog));

            if (File.Exists(ConfigService.ConfigFile))
            {
                File.Copy(ConfigService.ConfigFile, Path.Combine(tempDir, "settings.json"), overwrite: true);
            }

            if (File.Exists(ConfigService.LogFile))
            {
                File.Copy(ConfigService.LogFile, Path.Combine(tempDir, "events.log"), overwrite: true);
            }

            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }

            ZipFile.CreateFromDirectory(tempDir, exportPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            return exportPath;
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Export already finished; temp cleanup failure should not hide the support zip.
            }
        }
    }

    public static string BuildClipboardReport(AppConfig config, IEnumerable<MicDeviceInfo> devices, IEnumerable<LogEntry> recentLog)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Mic Volume Lock support log");
        builder.AppendLine($"Created: {DateTimeOffset.Now:O}");
        builder.AppendLine($"Version: 1.0.1");
        builder.AppendLine($"OS: {Environment.OSVersion}");
        builder.AppendLine($".NET: {Environment.Version}");
        builder.AppendLine($"Machine: {Environment.MachineName}");
        builder.AppendLine($"User: {Environment.UserName}");
        builder.AppendLine($"Process: {Process.GetCurrentProcess().MainModule?.FileName}");
        builder.AppendLine($"Autostart: {StartupService.IsAutoStartEnabled()}");
        builder.AppendLine($"Language: {config.AppLanguage}");
        builder.AppendLine($"Dark theme: {config.UseDarkTheme}");
        builder.AppendLine($"Notifications: {config.ShowNotifications}");
        builder.AppendLine($"Hotkeys: {config.HotkeysEnabled}");
        builder.AppendLine($"Follow default communications mic: {config.FollowDefaultCommunicationsDevice}");
        builder.AppendLine();
        builder.AppendLine("Devices:");
        foreach (var device in devices)
        {
            builder.AppendLine($"- {(device.IsDefaultCommunicationDevice ? "[default] " : string.Empty)}{device.DisplayName}");
            builder.AppendLine($"  {device.Id}");
        }

        builder.AppendLine();
        builder.AppendLine("Recent in-memory log:");
        foreach (var entry in recentLog.Take(100))
        {
            builder.AppendLine(entry.ToString());
        }

        if (File.Exists(ConfigService.LogFile))
        {
            builder.AppendLine();
            builder.AppendLine("events.log:");
            builder.AppendLine(File.ReadAllText(ConfigService.LogFile));
        }

        return builder.ToString();
    }

    private static void WriteText(string path, string text)
    {
        File.WriteAllText(path, text, Encoding.UTF8);
    }
}

