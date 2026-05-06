using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using MicVolumeLock.Models;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace MicVolumeLock.Services;

public sealed class MicrophoneLockService : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly System.Timers.Timer _pollTimer;
    private readonly object _sync = new();

    private AppConfig _config;
    private string? _activeEndpointId;
    private DateTime _lastApplyUtc = DateTime.MinValue;
    private string _agcStatus = "Not checked";
    private string _lastHardwareSupportText = "not initialized";
    private bool _disposed;
    private readonly Dictionary<string, int> _lastObservedVolume = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<LogEntry> Log { get; } = new();

    public event Action<ServiceStatus>? StatusChanged;
    public event Action<LogEntry>? LogAdded;
    public event Action<string>? HardwareSupportChanged;
    public event Action<string, int>? TargetVolumeAdopted;
    public event Action<string, int, int>? VolumeRestored;
    public event Action<string, int, int>? VolumeChangedObserved;

    public MicrophoneLockService(AppConfig config)
    {
        _config = config ?? new AppConfig();
        _pollTimer = new System.Timers.Timer(1000);
        _pollTimer.Elapsed += (_, _) => PollOnce();
    }

    public IReadOnlyList<MicDeviceInfo> GetCaptureDevices()
    {
        try
        {
            var defaultComms = TryGetDefaultCommunicationEndpointId();
            var devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            var list = new List<MicDeviceInfo>(devices.Count);

            foreach (var device in devices)
            {
                var isDefault = string.Equals(device.ID, defaultComms, StringComparison.OrdinalIgnoreCase);
                list.Add(new MicDeviceInfo(device.ID, device.FriendlyName ?? "Без имени", isDefault));
            }

            return list
                .OrderByDescending(d => d.IsDefaultCommunicationDevice)
                .ThenBy(d => d.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch
        {
            return Array.Empty<MicDeviceInfo>();
        }
    }

    public void UpdateConfig(AppConfig config)
    {
        lock (_sync)
        {
            _config = config ?? new AppConfig();
        }
    }

    public void Start()
    {
        if (_disposed)
        {
            return;
        }

        _pollTimer.Start();
        PollOnce();
    }

    public void Stop()
    {
        _pollTimer.Stop();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _pollTimer.Stop();
        _pollTimer.Dispose();
        _enumerator.Dispose();
        _disposed = true;
    }

    public void ApplyNow(string endpointId)
    {
        _ = ApplyTargetAsync(endpointId, force: true);
    }

    public Task<bool> TryDisableHardwareAgcAsync(string endpointId)
    {
        return Task.Run(() =>
        {
            var success = TryDisableHardwareAgc(endpointId, out var status);
            _agcStatus = status;
            return success;
        });
    }

    private bool TryDisableHardwareAgc(string endpointId, out string status)
    {
        status = "No hardware AGC control exposed by this device.";

        try
        {
            var device = _enumerator.GetDevice(endpointId);
            if (device == null)
            {
                status = "Device unavailable";
                return false;
            }

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var foundAgc = false;
            var disabledAgc = false;

            for (var i = 0; i < device.DeviceTopology.ConnectorCount; i++)
            {
                var connector = device.DeviceTopology.GetConnector((uint)i);
                if (connector?.Part is not null)
                {
                    WalkTopologyPart(connector.Part, visited, ref foundAgc, ref disabledAgc);
                }
            }

            if (disabledAgc)
            {
                status = "Hardware AGC disabled.";
                return true;
            }

            if (foundAgc)
            {
                status = "Hardware AGC was already disabled.";
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            status = $"AGC query failed: {ex.Message}";
            return false;
        }
    }

    private static void WalkTopologyPart(Part part, HashSet<string> visited, ref bool foundAgc, ref bool disabledAgc)
    {
        var id = SafePartId(part);
        if (!visited.Add(id))
        {
            return;
        }

        if (TryDisableAgcOnPart(part, out var agcWasFound, out var agcWasDisabled))
        {
            foundAgc |= agcWasFound;
            disabledAgc |= agcWasDisabled;
        }

        WalkParts(part.PartsIncoming, visited, ref foundAgc, ref disabledAgc);
        WalkParts(part.PartsOutgoing, visited, ref foundAgc, ref disabledAgc);
    }

    private static void WalkParts(PartsList? parts, HashSet<string> visited, ref bool foundAgc, ref bool disabledAgc)
    {
        if (parts is null)
        {
            return;
        }

        for (var i = 0; i < parts.Count; i++)
        {
            WalkTopologyPart(parts[(uint)i], visited, ref foundAgc, ref disabledAgc);
        }
    }

    private static bool TryDisableAgcOnPart(Part part, out bool foundAgc, out bool disabledAgc)
    {
        foundAgc = false;
        disabledAgc = false;

        object? activated = null;
        try
        {
            var field = typeof(Part).GetField("partInterface", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rawPart = field?.GetValue(part);
            if (rawPart is null)
            {
                return false;
            }

            var agcInterface = typeof(Part).Assembly.GetType("NAudio.CoreAudioApi.Interfaces.IAudioAutoGainControl");
            var activate = field!.FieldType.GetMethod(
                "Activate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (agcInterface is null || activate is null)
            {
                return false;
            }

            var parameters = activate.GetParameters();
            var clsCtxAll = Enum.ToObject(parameters[0].ParameterType, 23);
            var iid = agcInterface.GUID;
            var activateArgs = new object?[] { clsCtxAll, iid, null };
            var hrObject = activate.Invoke(rawPart, activateArgs);
            var hr = hrObject is int value ? value : 0;
            activated = activateArgs[2];
            if (hr != 0 || activated is null)
            {
                return false;
            }

            foundAgc = true;
            var getEnabled = agcInterface.GetMethod(
                "GetEnabled",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var setEnabled = agcInterface.GetMethod(
                "SetEnabled",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (getEnabled is null || setEnabled is null)
            {
                return true;
            }

            var getArgs = new object?[] { false };
            var enabledResult = getEnabled.Invoke(activated, getArgs) as int? ?? 0;
            var enabled = getArgs[0] is bool enabledValue && enabledValue;
            if (enabledResult == 0 && enabled)
            {
                var setResult = setEnabled.Invoke(activated, new object?[] { false }) as int? ?? 0;
                disabledAgc = setResult == 0;
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (activated is not null && Marshal.IsComObject(activated))
            {
                Marshal.ReleaseComObject(activated);
            }
        }
    }

    private static string SafePartId(Part part)
    {
        try
        {
            return part.GlobalId;
        }
        catch
        {
            try
            {
                return part.LocalId.ToString(CultureInfo.InvariantCulture);
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }
    }

    private string? ResolveActiveEndpointId()
    {
        lock (_sync)
        {
            if (_config.FollowDefaultCommunicationsDevice)
            {
                return TryGetDefaultCommunicationEndpointId();
            }

            return _config.SelectedEndpointId;
        }
    }

    private void PollOnce()
    {
        if (_disposed)
        {
            return;
        }

        string? endpointId;
        AppConfig snapshot;
        bool isPaused;
        lock (_sync)
        {
            snapshot = _config;
            isPaused = snapshot.IsPaused;
            endpointId = ResolveActiveEndpointId();
        }

        if (string.IsNullOrWhiteSpace(endpointId))
        {
            RaiseStatus(new ServiceStatus
            {
                HasActiveDevice = false,
                IsLocked = false,
                IsPaused = isPaused,
                Message = snapshot.FollowDefaultCommunicationsDevice
                    ? "Waiting for default communications microphone."
                    : "No microphone selected."
            });
            return;
        }

        if (!string.Equals(_activeEndpointId, endpointId, StringComparison.OrdinalIgnoreCase))
        {
            _activeEndpointId = endpointId;
            _ = AddLog(new LogEntry
            {
                Source = "Rebind",
                EndpointId = endpointId,
                Message = "Monitoring a new microphone endpoint"
            });
            _ = UpdateCurrentHardwareSupport(endpointId);
        }

        var profile = snapshot.GetProfile(endpointId);
        if (!snapshot.IsPaused && profile.IsLockEnabled)
        {
            _ = ApplyTargetAsync(endpointId, force: false);
            if (!isPaused)
            {
                _agcStatus = string.IsNullOrWhiteSpace(_agcStatus) ? "Not checked" : _agcStatus;
            }
        }

        UpdateCurrentStatus(endpointId, profile, snapshot.IsPaused);
    }

    private bool TryGetDefaultCommunicationEndpointIdSafe(out string? endpointId, out string? error)
    {
        endpointId = null;
        error = null;
        try
        {
            endpointId = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).ID;
            return !string.IsNullOrWhiteSpace(endpointId);
        }
        catch (COMException ex)
        {
            error = ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private string? TryGetDefaultCommunicationEndpointId()
    {
        return TryGetDefaultCommunicationEndpointIdSafe(out var endpointId, out _)
            ? endpointId
            : null;
    }

    private async Task UpdateCurrentHardwareSupport(string endpointId)
    {
        var supportText = "unable to determine";

        try
        {
            var device = _enumerator.GetDevice(endpointId);
            if (device == null)
            {
                return;
            }

            var endpointVolume = device.AudioEndpointVolume;
            var flags = endpointVolume.HardwareSupport;

            var parts = new List<string>();
            if ((flags & EEndpointHardwareSupport.Volume) != 0) { parts.Add("hardware volume control"); }
            if ((flags & EEndpointHardwareSupport.Mute) != 0) { parts.Add("hardware mute control"); }
            if ((flags & EEndpointHardwareSupport.Meter) != 0) { parts.Add("hardware peak meter"); }
            supportText = parts.Count == 0
                ? "software endpoint volume (no hardware flags)"
                : string.Join(", ", parts);
        }
        catch
        {
            supportText = "hardware flags unavailable";
        }

        _lastHardwareSupportText = supportText;
        HardwareSupportChanged?.Invoke(supportText);
        await AddLog(new LogEntry
        {
            Source = "QueryHardwareSupport",
            EndpointId = endpointId,
            Message = supportText
        });
    }

    private async Task ApplyTargetAsync(string endpointId, bool force)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
        {
            return;
        }

        AppConfig snapshot;
        lock (_sync)
        {
            snapshot = _config;
        }

        var profile = snapshot.GetProfile(endpointId);
        if (snapshot.IsPaused && !force)
        {
            return;
        }

        if (!force && !profile.IsLockEnabled)
        {
            return;
        }

        try
        {
            var device = _enumerator.GetDevice(endpointId);
            if (device == null || device.State != DeviceState.Active)
            {
                _ = AddLog(new LogEntry
                {
                    Source = "Restore",
                    EndpointId = endpointId,
                    Message = "Device unavailable"
                });
                return;
            }

            var endpointVolume = device.AudioEndpointVolume;
            var currentPercent = (int)Math.Round(endpointVolume.MasterVolumeLevelScalar * 100);
            var target = Math.Clamp(profile.TargetVolumePercent, 0, 100);
            var delta = Math.Abs(currentPercent - target);

            if (!force && snapshot.AdoptHigherExternalVolume && currentPercent > target + 1)
            {
                profile.TargetVolumePercent = currentPercent;
                snapshot.Profiles[endpointId] = profile;
                ConfigService.Save(snapshot);
                TargetVolumeAdopted?.Invoke(endpointId, currentPercent);

                await AddLog(new LogEntry
                {
                    Source = "External raise",
                    EndpointId = endpointId,
                    PreviousPercent = target,
                    NewPercent = currentPercent,
                    Message = "New level adopted as target"
                });
                return;
            }

            if (delta > 1)
            {
                if (!force && DateTime.UtcNow - _lastApplyUtc < TimeSpan.FromMilliseconds(750))
                {
                    return;
                }

                if (!force && HasIgnoredDiagnosticProcess(snapshot, out var ignoredProcess))
                {
                    await AddLog(new LogEntry
                    {
                        Source = "Diagnostics",
                        EndpointId = endpointId,
                        PreviousPercent = currentPercent,
                        NewPercent = target,
                        Message = $"Restore ignored while {ignoredProcess} is running"
                    });
                    return;
                }

                endpointVolume.MasterVolumeLevelScalar = target / 100f;
                _lastApplyUtc = DateTime.UtcNow;
                VolumeRestored?.Invoke(endpointId, currentPercent, target);

                await AddLog(new LogEntry
                {
                    Source = "Forced",
                    EndpointId = endpointId,
                    PreviousPercent = currentPercent,
                    NewPercent = target,
                    Message = "Level restored to target"
                });
            }
        }
        catch (COMException ex)
        {
            _ = AddLog(new LogEntry
            {
                Source = "Forced",
                EndpointId = endpointId,
                Message = $"COM error: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            _ = AddLog(new LogEntry
            {
                Source = "Forced",
                EndpointId = endpointId,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    private static bool HasIgnoredDiagnosticProcess(AppConfig snapshot, out string processName)
    {
        processName = string.Empty;
        var names = snapshot.DiagnosticIgnoredProcesses ?? new List<string>();
        foreach (var name in names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var normalized = Path.GetFileNameWithoutExtension(name.Trim());
                if (Process.GetProcessesByName(normalized).Length > 0)
                {
                    processName = normalized;
                    return true;
                }
            }
            catch
            {
                // Diagnostics must never break microphone protection.
            }
        }

        return false;
    }

    private async Task AddLog(LogEntry entry)
    {
        try
        {
            Directory.CreateDirectory(ConfigService.ConfigDirectory);
            await File.AppendAllTextAsync(ConfigService.LogFile, entry + Environment.NewLine);
        }
        catch
        {
            // Logging must never break microphone protection.
        }

        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            Log.Insert(0, entry);
            while (Log.Count > 250)
            {
                Log.RemoveAt(Log.Count - 1);
            }
        });

        LogAdded?.Invoke(entry);
    }

    private void UpdateCurrentStatus(string endpointId, DeviceProfile profile, bool isPaused)
    {
        int current = -1;
        try
        {
            var device = _enumerator.GetDevice(endpointId);
            if (device != null && device.State == DeviceState.Active)
            {
                current = (int)Math.Round(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            }
        }
        catch
        {
            // ignored
        }

        var name = "Not found";
        try
        {
            var dev = _enumerator.GetDevice(endpointId);
            if (dev != null)
            {
                name = dev.FriendlyName ?? endpointId;
            }
        }
        catch
        {
            name = endpointId;
        }

        RaiseStatus(new ServiceStatus
        {
            HasActiveDevice = !string.IsNullOrWhiteSpace(name),
            IsLocked = profile.IsLockEnabled,
            IsPaused = isPaused,
            DeviceName = name,
            DeviceId = endpointId,
            CurrentPercent = current,
            TargetPercent = profile.TargetVolumePercent,
            HardwareSupportText = _lastHardwareSupportText,
            AgcStatus = _agcStatus,
            Message = profile.IsLockEnabled
                ? "Protection enabled"
                : "Protection disabled"
        });

        if (current >= 0)
        {
            if (_lastObservedVolume.TryGetValue(endpointId, out var previous) &&
                Math.Abs(previous - current) > 1 &&
                !profile.IsLockEnabled &&
                !isPaused)
            {
                VolumeChangedObserved?.Invoke(endpointId, previous, current);
            }

            _lastObservedVolume[endpointId] = current;
        }
    }

    private async void RaiseStatus(ServiceStatus status)
    {
        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusChanged?.Invoke(status);
        });
    }
}

