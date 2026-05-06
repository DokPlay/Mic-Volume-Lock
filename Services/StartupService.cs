using System.IO;
using Microsoft.Win32;

namespace MicVolumeLock.Services;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "MicVolumeLock";

    public static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RunKeyPath, writable: false);
            var value = key?.GetValue(RunValue) as string;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch
        {
            return false;
        }
    }

    public static void SetAutoStart(bool enabled, string exePath)
    {
        using var key = Registry.LocalMachine.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Could not open machine startup registry key.");

        if (enabled)
        {
            key.SetValue(RunValue, $"\"{exePath}\" --minimized", RegistryValueKind.String);
        }
        else if (key.GetValueNames().Any(name => string.Equals(name, RunValue, StringComparison.OrdinalIgnoreCase)))
        {
            key.DeleteValue(RunValue);
        }
    }

    public static string InstalledDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "Mic Volume Lock");

    public static string InstalledExecutablePath => Path.Combine(InstalledDirectory, "MicVolumeLock.exe");

    public static void InstallSelf()
    {
        var currentPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Could not resolve current executable path.");

        Directory.CreateDirectory(InstalledDirectory);
        var currentDirectory = Path.GetDirectoryName(currentPath)
            ?? throw new InvalidOperationException("Could not resolve current executable directory.");

        if (!string.Equals(currentDirectory, InstalledDirectory, StringComparison.OrdinalIgnoreCase))
        {
            CopyApplicationDirectory(currentDirectory, InstalledDirectory);
        }

        SetAutoStart(enabled: true, exePath: InstalledExecutablePath);
    }

    public static void Uninstall(string? ignoreCurrentProcessPath = null)
    {
        SetAutoStart(enabled: false, exePath: InstalledExecutablePath);
        if (!string.IsNullOrWhiteSpace(ignoreCurrentProcessPath) &&
            string.Equals(ignoreCurrentProcessPath, InstalledExecutablePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(InstalledExecutablePath))
        {
            File.Delete(InstalledExecutablePath);
        }
    }

    private static void CopyApplicationDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            if (relativePath.StartsWith("ref\\", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith("refint\\", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(sourceFile, targetFile, overwrite: true);
        }
    }
}
