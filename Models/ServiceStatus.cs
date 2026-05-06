namespace MicVolumeLock.Models;

public sealed class ServiceStatus
{
    public bool HasActiveDevice { get; init; }

    public bool IsLocked { get; init; }

    public bool IsPaused { get; init; }

    public string? DeviceName { get; init; }

    public string? DeviceId { get; init; }

    public int CurrentPercent { get; init; } = -1;

    public int TargetPercent { get; init; } = -1;

    public string HardwareSupportText { get; init; } = "неизвестно";

    public string AgcStatus { get; init; } = "не проверено";

    public string Message { get; init; } = "Ожидание...";
}
