using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using MicVolumeLock.Models;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace MicVolumeLock.Services;

public sealed class MicrophoneLockService : IDisposable
{
    private const double WatchdogIntervalMilliseconds = 10_000;

    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly System.Timers.Timer _watchdogTimer;
    private readonly EndpointNotificationClient _endpointNotifications;
    private readonly Guid _volumeEventContext = Guid.NewGuid();
    private readonly object _sync = new();

    private AppConfig _config;
    private string? _activeEndpointId;
    private string? _subscribedEndpointId;
    private MMDevice? _subscribedDevice;
    private AudioEndpointVolume? _subscribedEndpointVolume;
    private ServiceStatus? _lastRaisedStatus;
    private DateTime _lastApplyUtc = DateTime.MinValue;
    private string _agcStatus = "Not checked";
    private string _lastHardwareSupportText = "not initialized";
    private bool _endpointNotificationsRegistered;
    private bool _disposed;
    private int _pollInProgress;
    private readonly Dictionary<string, int> _lastObservedVolume = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _agcCheckedEndpoints = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _startupAppliedEndpoints = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<LogEntry> Log { get; } = new();

    public event Action<ServiceStatus>? StatusChanged;
    public event Action<LogEntry>? LogAdded;
    public event Action<string>? HardwareSupportChanged;
    public event Action<string, int>? TargetVolumeAdopted;
    public event Action<string, int, int>? VolumeRestored;
    public event Action<string, int, int>? VolumeChangedObserved;

    public string AgcStatus
    {
        get
        {
            lock (_sync)
            {
                return _agcStatus;
            }
        }
    }

    public MicrophoneLockService(AppConfig config)
    {
        _config = config ?? new AppConfig();
        _endpointNotifications = new EndpointNotificationClient(this);
        _watchdogTimer = new System.Timers.Timer(WatchdogIntervalMilliseconds)
        {
            AutoReset = true
        };
        _watchdogTimer.Elapsed += (_, _) => PollOnce();
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
            var wasAgcEnabled = _config.TryDisableHardwareAgc;
            _config = config ?? new AppConfig();
            if (!_config.TryDisableHardwareAgc)
            {
                _agcCheckedEndpoints.Clear();
                _agcStatus = "Not checked";
            }
            else if (!wasAgcEnabled)
            {
                _agcCheckedEndpoints.Clear();
            }
        }

        RequestPoll();
    }

    public void Start()
    {
        if (_disposed)
        {
            return;
        }

        RegisterEndpointNotifications();
        _watchdogTimer.Start();
        PollOnce();
    }

    public void Stop()
    {
        _watchdogTimer.Stop();
        UnregisterEndpointNotifications();
        ClearVolumeSubscription();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _watchdogTimer.Stop();
        _watchdogTimer.Dispose();
        UnregisterEndpointNotifications();
        ClearVolumeSubscription();
        _enumerator.Dispose();
    }

    public void ApplyNow(string endpointId)
    {
        _ = ApplyTargetAsync(endpointId, force: true);
    }

    private void RegisterEndpointNotifications()
    {
        lock (_sync)
        {
            if (_endpointNotificationsRegistered || _disposed)
            {
                return;
            }
        }

        try
        {
            _enumerator.RegisterEndpointNotificationCallback(_endpointNotifications);
            lock (_sync)
            {
                _endpointNotificationsRegistered = true;
            }
        }
        catch
        {
            // Device notifications are an optimization. The watchdog still keeps protection alive.
        }
    }

    private void UnregisterEndpointNotifications()
    {
        lock (_sync)
        {
            if (!_endpointNotificationsRegistered)
            {
                return;
            }

            _endpointNotificationsRegistered = false;
        }

        try
        {
            _enumerator.UnregisterEndpointNotificationCallback(_endpointNotifications);
        }
        catch
        {
            // Best-effort cleanup; the process is shutting down or the enumerator is already unavailable.
        }
    }

    private void RequestPoll()
    {
        if (_disposed)
        {
            return;
        }

        _ = Task.Run(PollOnce);
    }

    private void EnsureVolumeSubscription(string endpointId)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
        {
            ClearVolumeSubscription();
            return;
        }

        lock (_sync)
        {
            if (string.Equals(_subscribedEndpointId, endpointId, StringComparison.OrdinalIgnoreCase) &&
                _subscribedEndpointVolume is not null)
            {
                return;
            }
        }

        ClearVolumeSubscription();

        MMDevice? device = null;
        AudioEndpointVolume? endpointVolume = null;
        try
        {
            device = _enumerator.GetDevice(endpointId);
            if (device is null || device.State != DeviceState.Active)
            {
                device?.Dispose();
                return;
            }

            endpointVolume = device.AudioEndpointVolume;
            endpointVolume.NotificationGuid = _volumeEventContext;
            endpointVolume.OnVolumeNotification += OnVolumeNotification;

            lock (_sync)
            {
                _subscribedEndpointId = endpointId;
                _subscribedDevice = device;
                _subscribedEndpointVolume = endpointVolume;
            }
        }
        catch
        {
            try
            {
                if (endpointVolume is not null)
                {
                    endpointVolume.OnVolumeNotification -= OnVolumeNotification;
                    endpointVolume.Dispose();
                }
            }
            catch
            {
                // ignored
            }

            device?.Dispose();
        }
    }

    private void ClearVolumeSubscription()
    {
        AudioEndpointVolume? endpointVolume;
        MMDevice? device;

        lock (_sync)
        {
            endpointVolume = _subscribedEndpointVolume;
            device = _subscribedDevice;
            _subscribedEndpointVolume = null;
            _subscribedDevice = null;
            _subscribedEndpointId = null;
        }

        try
        {
            if (endpointVolume is not null)
            {
                endpointVolume.OnVolumeNotification -= OnVolumeNotification;
                endpointVolume.Dispose();
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            device?.Dispose();
        }
        catch
        {
            // ignored
        }
    }

    private void OnVolumeNotification(AudioVolumeNotificationData notification)
    {
        if (_disposed)
        {
            return;
        }

        string? endpointId;
        lock (_sync)
        {
            endpointId = _subscribedEndpointId;
        }

        if (string.IsNullOrWhiteSpace(endpointId))
        {
            return;
        }

        var currentPercent = (int)Math.Round(Math.Clamp(notification.MasterVolume, 0f, 1f) * 100);
        _ = Task.Run(() => HandleVolumeNotification(endpointId, currentPercent));
    }

    private void HandleVolumeNotification(string endpointId, int currentPercent)
    {
        if (_disposed)
        {
            return;
        }

        AppConfig snapshot;
        lock (_sync)
        {
            snapshot = _config;
        }

        var profile = snapshot.GetProfile(endpointId);
        if (!snapshot.IsPaused && profile.IsLockEnabled)
        {
            var target = Math.Clamp(profile.TargetVolumePercent, 0, 100);
            if (Math.Abs(currentPercent - target) > 1)
            {
                _ = ApplyTargetAsync(endpointId, force: false);
            }
        }

        UpdateCurrentStatus(endpointId, profile, snapshot.IsPaused, currentPercent);
    }

    public Task<bool> TryDisableHardwareAgcAsync(string endpointId)
    {
        lock (_sync)
        {
            _agcCheckedEndpoints.Add(endpointId);
            _agcStatus = "Checking hardware AGC...";
        }

        return Task.Run(() =>
        {
            var success = TryDisableHardwareAgc(endpointId, out var status);
            lock (_sync)
            {
                _agcStatus = status;
            }

            return success;
        });
    }

    private bool TryDisableHardwareAgc(string endpointId, out string status)
    {
        status = "No hardware AGC control exposed by this device.";

        try
        {
            using var device = _enumerator.GetDevice(endpointId);
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

    private void PollOnce()
    {
        if (_disposed || Interlocked.Exchange(ref _pollInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            PollOnceCore();
        }
        catch
        {
            // The watchdog must never terminate the process or timer thread.
        }
        finally
        {
            Interlocked.Exchange(ref _pollInProgress, 0);
        }
    }

    private void PollOnceCore()
    {
        string? endpointId;
        AppConfig snapshot;
        bool isPaused;
        bool followDefaultCommunicationsDevice;
        string? selectedEndpointId;
        lock (_sync)
        {
            snapshot = _config;
            isPaused = snapshot.IsPaused;
            followDefaultCommunicationsDevice = snapshot.FollowDefaultCommunicationsDevice;
            selectedEndpointId = snapshot.SelectedEndpointId;
        }
        endpointId = followDefaultCommunicationsDevice
            ? TryGetDefaultCommunicationEndpointId()
            : selectedEndpointId;

        if (string.IsNullOrWhiteSpace(endpointId))
        {
            _activeEndpointId = null;
            ClearVolumeSubscription();
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

        var endpointChanged = !string.Equals(_activeEndpointId, endpointId, StringComparison.OrdinalIgnoreCase);
        if (endpointChanged)
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

        EnsureVolumeSubscription(endpointId);

        var profile = snapshot.GetProfile(endpointId);
        QueueHardwareAgcCheckIfNeeded(snapshot, endpointId);
        if (!snapshot.IsPaused && profile.IsLockEnabled)
        {
            var forceInitialApply = endpointChanged || !_startupAppliedEndpoints.Contains(endpointId);
            _ = ApplyTargetAsync(endpointId, force: forceInitialApply, markStartupApplied: forceInitialApply);
        }

        UpdateCurrentStatus(endpointId, profile, snapshot.IsPaused);
    }

    private void QueueHardwareAgcCheckIfNeeded(AppConfig snapshot, string endpointId)
    {
        if (!snapshot.TryDisableHardwareAgc)
        {
            return;
        }

        lock (_sync)
        {
            if (_agcCheckedEndpoints.Contains(endpointId))
            {
                return;
            }
        }

        _ = TryDisableHardwareAgcAsync(endpointId);
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
            using var device = _enumerator.GetDevice(endpointId);
            if (device == null)
            {
                return;
            }

            using var endpointVolume = device.AudioEndpointVolume;
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

    private async Task ApplyTargetAsync(string endpointId, bool force, bool markStartupApplied = false)
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
            using var device = _enumerator.GetDevice(endpointId);
            if (device == null || device.State != DeviceState.Active)
            {
                if (markStartupApplied)
                {
                    _startupAppliedEndpoints.Remove(endpointId);
                }

                _ = AddLog(new LogEntry
                {
                    Source = "Restore",
                    EndpointId = endpointId,
                    Message = "Device unavailable"
                });
                return;
            }

            using var endpointVolume = device.AudioEndpointVolume;
            endpointVolume.NotificationGuid = _volumeEventContext;
            var currentPercent = (int)Math.Round(endpointVolume.MasterVolumeLevelScalar * 100);
            var target = Math.Clamp(profile.TargetVolumePercent, 0, 100);
            var delta = Math.Abs(currentPercent - target);

            if (delta <= 1)
            {
                if (markStartupApplied)
                {
                    _startupAppliedEndpoints.Add(endpointId);
                }

                return;
            }

            if (!force && snapshot.AdoptHigherExternalVolume && currentPercent > target + 1)
            {
                profile.TargetVolumePercent = currentPercent;
                snapshot.Profiles[endpointId] = profile;
                snapshot.DefaultTargetVolumePercent = currentPercent;
                snapshot.DefaultLockEnabled = profile.IsLockEnabled;
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

            if (!force && DateTime.UtcNow - _lastApplyUtc < TimeSpan.FromMilliseconds(750))
            {
                return;
            }

            if (!force && HasIgnoredDiagnosticProcess(snapshot, out var ignoredProcess))
            {
                await AddLog(new LogEntry
                {
                    Source = "Exclusion",
                    EndpointId = endpointId,
                    PreviousPercent = currentPercent,
                    NewPercent = target,
                    Message = $"Restore skipped while process is running: {ignoredProcess}"
                });
                return;
            }

            endpointVolume.MasterVolumeLevelScalar = target / 100f;
            _lastApplyUtc = DateTime.UtcNow;
            if (markStartupApplied)
            {
                _startupAppliedEndpoints.Add(endpointId);
            }

            VolumeRestored?.Invoke(endpointId, currentPercent, target);

            await AddLog(new LogEntry
            {
                Source = markStartupApplied ? "Startup apply" : "Forced",
                EndpointId = endpointId,
                PreviousPercent = currentPercent,
                NewPercent = target,
                Message = markStartupApplied ? "Saved target applied" : "Level restored to target"
            });
        }
        catch (COMException ex)
        {
            if (markStartupApplied)
            {
                _startupAppliedEndpoints.Remove(endpointId);
            }

            _ = AddLog(new LogEntry
            {
                Source = "Forced",
                EndpointId = endpointId,
                Message = $"COM error: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            if (markStartupApplied)
            {
                _startupAppliedEndpoints.Remove(endpointId);
            }

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
                var normalized = ProcessExclusionService.Normalize(name);
                if (!ProcessExclusionService.CanExclude(normalized))
                {
                    continue;
                }

                if (Process.GetProcessesByName(normalized).Length > 0)
                {
                    processName = normalized;
                    return true;
                }
            }
            catch
            {
                // Process exclusions must never break microphone protection.
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

    private void UpdateCurrentStatus(string endpointId, DeviceProfile profile, bool isPaused, int? knownCurrentPercent = null)
    {
        var current = knownCurrentPercent ?? -1;
        if (!knownCurrentPercent.HasValue)
        {
            try
            {
                using var device = _enumerator.GetDevice(endpointId);
                if (device != null && device.State == DeviceState.Active)
                {
                    using var endpointVolume = device.AudioEndpointVolume;
                    current = (int)Math.Round(endpointVolume.MasterVolumeLevelScalar * 100);
                }
            }
            catch
            {
                // ignored
            }
        }

        var name = "Not found";
        try
        {
            MMDevice? subscribedDevice;
            lock (_sync)
            {
                subscribedDevice = string.Equals(_subscribedEndpointId, endpointId, StringComparison.OrdinalIgnoreCase)
                    ? _subscribedDevice
                    : null;
            }

            if (subscribedDevice != null)
            {
                name = subscribedDevice.FriendlyName ?? endpointId;
            }
            else
            {
                using var dev = _enumerator.GetDevice(endpointId);
                if (dev != null)
                {
                    name = dev.FriendlyName ?? endpointId;
                }
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
            AgcStatus = AgcStatus,
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
        if (!ShouldRaiseStatus(status))
        {
            return;
        }

        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusChanged?.Invoke(status);
        });
    }

    private bool ShouldRaiseStatus(ServiceStatus status)
    {
        lock (_sync)
        {
            if (IsSameStatus(_lastRaisedStatus, status))
            {
                return false;
            }

            _lastRaisedStatus = status;
            return true;
        }
    }

    private static bool IsSameStatus(ServiceStatus? left, ServiceStatus right)
    {
        return left is not null &&
            left.HasActiveDevice == right.HasActiveDevice &&
            left.IsLocked == right.IsLocked &&
            left.IsPaused == right.IsPaused &&
            left.CurrentPercent == right.CurrentPercent &&
            left.TargetPercent == right.TargetPercent &&
            string.Equals(left.DeviceName, right.DeviceName, StringComparison.Ordinal) &&
            string.Equals(left.DeviceId, right.DeviceId, StringComparison.Ordinal) &&
            string.Equals(left.HardwareSupportText, right.HardwareSupportText, StringComparison.Ordinal) &&
            string.Equals(left.AgcStatus, right.AgcStatus, StringComparison.Ordinal) &&
            string.Equals(left.Message, right.Message, StringComparison.Ordinal);
    }

    private sealed class EndpointNotificationClient : IMMNotificationClient
    {
        private readonly MicrophoneLockService _owner;

        public EndpointNotificationClient(MicrophoneLockService owner)
        {
            _owner = owner;
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            _owner.RequestPoll();
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            _owner.RequestPoll();
        }

        public void OnDeviceRemoved(string deviceId)
        {
            _owner.RequestPoll();
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Capture && role == Role.Communications)
            {
                _owner.RequestPoll();
            }
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
        }
    }
}

