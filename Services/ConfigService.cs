using System.IO;
using System.Text.Json;
using MicVolumeLock.Models;
using Microsoft.Win32;

namespace MicVolumeLock.Services;

public static class ConfigService
{
    private const string InstallerSettingsKeyPath = @"Software\MicVolumeLock";
    private const string InstallerLanguageValue = "DefaultLanguage";

    public static readonly string ConfigDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicVolumeLock");

    public static readonly string LogFile = Path.Combine(ConfigDirectory, "events.log");

    public static readonly string ConfigFile = Path.Combine(ConfigDirectory, "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigFile))
            {
                return CreateDefaultConfig();
            }

            var json = File.ReadAllText(ConfigFile);
            var cfg = JsonSerializer.Deserialize<AppConfig>(json) ?? CreateDefaultConfig();
            EnsureConfigShape(cfg);

            if (string.IsNullOrWhiteSpace(cfg.AppLanguage))
            {
                cfg.AppLanguage = ReadInstallerDefaultLanguage() ?? LocalizationService.DefaultLanguage;
            }
            else
            {
                cfg.AppLanguage = LocalizationService.NormalizeLanguage(cfg.AppLanguage) ?? LocalizationService.DefaultLanguage;
            }

            return cfg;
        }
        catch
        {
            return CreateDefaultConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDirectory);
        EnsureConfigShape(config);
        config.AppLanguage = LocalizationService.NormalizeLanguage(config.AppLanguage) ?? LocalizationService.DefaultLanguage;
        var json = JsonSerializer.Serialize(config, SerializerOptions);
        File.WriteAllText(ConfigFile, json);
    }

    private static AppConfig CreateDefaultConfig()
    {
        var config = new AppConfig
        {
            AppLanguage = ReadInstallerDefaultLanguage() ?? LocalizationService.DefaultLanguage
        };
        EnsureConfigShape(config);
        return config;
    }

    private static void EnsureConfigShape(AppConfig config)
    {
        config.Profiles ??= new Dictionary<string, DeviceProfile>();
        config.VolumeProfiles ??= new List<VolumeProfile>();
        config.DiagnosticIgnoredProcesses ??= new List<string>();

        if (config.VolumeProfiles.Count == 0)
        {
            config.VolumeProfiles.AddRange(new[]
            {
                new VolumeProfile { Id = "games", Name = "Games", TargetVolumePercent = 90, IsLockEnabled = true },
                new VolumeProfile { Id = "work", Name = "Work", TargetVolumePercent = 75, IsLockEnabled = true },
                new VolumeProfile { Id = "stream", Name = "Stream", TargetVolumePercent = 85, IsLockEnabled = true },
                new VolumeProfile { Id = "quiet-room", Name = "Quiet room", TargetVolumePercent = 60, IsLockEnabled = true }
            });
        }

        config.DefaultTargetVolumePercent = Math.Clamp(config.DefaultTargetVolumePercent, 0, 100);
        foreach (var profile in config.VolumeProfiles)
        {
            profile.TargetVolumePercent = Math.Clamp(profile.TargetVolumePercent, 0, 100);
        }
    }

    private static string? ReadInstallerDefaultLanguage()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(InstallerSettingsKeyPath, writable: false);
            var value = key?.GetValue(InstallerLanguageValue) as string;
            return LocalizationService.NormalizeLanguage(value);
        }
        catch
        {
            return null;
        }
    }
}
