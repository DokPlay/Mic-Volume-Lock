using MicVolumeLock.Services;
using System.Windows;

namespace MicVolumeLock;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Contains("--install", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                InstallerService.InstallCurrentBuild();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Не удалось выполнить установку: {ex.Message}",
                    "Mic Volume Lock",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            Shutdown();
            return;
        }

        if (e.Args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                InstallerService.Uninstall();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Не удалось выполнить удаление: {ex.Message}",
                    "Mic Volume Lock",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            Shutdown();
            return;
        }

        base.OnStartup(e);
    }
}
