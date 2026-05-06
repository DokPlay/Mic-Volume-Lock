using System.Diagnostics;
using System.Windows;

namespace MicVolumeLock.Services;

public static class InstallerService
{
    public static void InstallCurrentBuild()
    {
        StartupService.InstallSelf();
        var installedPath = StartupService.InstalledExecutablePath;

        System.Windows.MessageBox.Show(
            $"Установка выполнена.{Environment.NewLine}{Environment.NewLine}" +
            $"Установленный exe:{Environment.NewLine}{installedPath}{Environment.NewLine}{Environment.NewLine}" +
            "Автозапуск включён.",
            "Mic Volume Lock",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        var currentPath = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.Equals(currentPath, installedPath, StringComparison.OrdinalIgnoreCase))
        {
            var startInfo = new ProcessStartInfo(installedPath, "--minimized")
            {
                UseShellExecute = true
            };
            _ = Process.Start(startInfo);
        }

        Environment.Exit(0);
    }

    public static void Uninstall()
    {
        var currentPath = Process.GetCurrentProcess().MainModule?.FileName;
        StartupService.Uninstall(ignoreCurrentProcessPath: currentPath);

        System.Windows.MessageBox.Show(
            "Удаление выполнено: автозапуск отключён. Для полной очистки удалите папку приложения при необходимости.",
            "Mic Volume Lock",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        Environment.Exit(0);
    }
}
