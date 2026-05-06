using System.Text.Json.Serialization;

namespace MicVolumeLock.Models;

public sealed class AppConfig
{
    public string AppLanguage { get; set; } = "en-US";

    public bool StartWithWindows { get; set; } = true;

    public bool FollowDefaultCommunicationsDevice { get; set; } = true;

    public bool TryDisableHardwareAgc { get; set; } = false;

    public bool IsPaused { get; set; } = false;

    public bool AdoptHigherExternalVolume { get; set; } = true;

    public bool ShowNotifications { get; set; } = true;

    public bool UseDarkTheme { get; set; } = false;

    public bool HotkeysEnabled { get; set; } = true;

    public int DefaultTargetVolumePercent { get; set; } = 85;

    public bool DefaultLockEnabled { get; set; } = false;

    public string? ActiveVolumeProfileId { get; set; }

    public List<VolumeProfile> VolumeProfiles { get; set; } = new();

    public string? DiagnosticSelectedProcessName { get; set; }

    public List<string> DiagnosticIgnoredProcesses { get; set; } = new();

    public string? SelectedEndpointId { get; set; }

    public Dictionary<string, DeviceProfile> Profiles { get; set; } = new();

    public DeviceProfile GetProfile(string endpointId)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
        {
            return new DeviceProfile
            {
                TargetVolumePercent = DefaultTargetVolumePercent,
                IsLockEnabled = DefaultLockEnabled
            };
        }

        if (!Profiles.TryGetValue(endpointId, out var profile))
        {
            profile = new DeviceProfile
            {
                TargetVolumePercent = DefaultTargetVolumePercent,
                IsLockEnabled = DefaultLockEnabled
            };
            Profiles[endpointId] = profile;
        }

        return profile;
    }
}

public sealed class VolumeProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public int TargetVolumePercent { get; set; } = 85;

    public bool IsLockEnabled { get; set; } = true;

    public override string ToString() => Name;
}

public sealed class DeviceProfile
{
    public int TargetVolumePercent { get; set; } = 85;

    public bool IsLockEnabled { get; set; } = false;
}

public sealed class MicDeviceInfo
{
    [JsonConstructor]
    public MicDeviceInfo(string id, string displayName, bool isDefaultCommunicationDevice)
    {
        Id = id;
        DisplayName = displayName;
        IsDefaultCommunicationDevice = isDefaultCommunicationDevice;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public bool IsDefaultCommunicationDevice { get; }

    public override string ToString() => DisplayName;
}
