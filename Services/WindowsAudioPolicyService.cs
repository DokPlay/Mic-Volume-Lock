using Microsoft.Win32;

namespace MicVolumeLock.Services;

public static class WindowsAudioPolicyService
{
    private const string AudioPolicyKeyPath = @"Software\Microsoft\Multimedia\Audio";
    private const string UserAudioConsoleValue = "UserAudioConsole";
    private const int DoNothingForCommunications = 3;

    public static bool EnsureNoCommunicationsDucking()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(AudioPolicyKeyPath, writable: true);
            if (key is null)
            {
                return false;
            }

            var current = key.GetValue(UserAudioConsoleValue);
            if (current is int currentInt && currentInt == DoNothingForCommunications)
            {
                return false;
            }

            key.SetValue(UserAudioConsoleValue, DoNothingForCommunications, RegistryValueKind.DWord);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
