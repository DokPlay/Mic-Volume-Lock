using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using MicVolumeLock.Models;
using MicVolumeLock.Services;
using Forms = System.Windows.Forms;

namespace MicVolumeLock;

public partial class MainWindow : Window
{
    private readonly MicrophoneLockService _service;
    private AppConfig _config;
    private ServiceStatus? _lastStatus;
    private bool _suspendUi;
    private bool _isUiReady;
    private bool _exitRequested;
    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ToolStripMenuItem? _trayOpenItem;
    private Forms.ToolStripMenuItem? _trayPauseItem;
    private Forms.ToolStripMenuItem? _trayExitItem;
    private HotkeyManager? _hotkeys;
    private string _lastDiagnosticsText = string.Empty;
    private DateTime _lastVolumeNotificationUtc = DateTime.MinValue;

    public MainWindow()
    {
        _config = ConfigService.Load();
        LocalizationService.SetLanguage(_config.AppLanguage);
        ThemeService.Apply(_config.UseDarkTheme);

        InitializeComponent();
        SourceInitialized += (_, _) => ThemeService.ApplyWindowTheme(this, _config.UseDarkTheme);

        _service = new MicrophoneLockService(_config);
        _service.StatusChanged += OnServiceStatusChanged;
        _service.LogAdded += OnLogAdded;
        _service.HardwareSupportChanged += OnHardwareSupportChanged;
        _service.TargetVolumeAdopted += OnTargetVolumeAdopted;
        _service.VolumeRestored += OnVolumeRestored;
        _service.VolumeChangedObserved += OnVolumeChangedObserved;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
        StateChanged += MainWindow_StateChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _config.StartWithWindows = StartupService.IsAutoStartEnabled();
        SetupLanguageCombo();
        ApplyLanguage();
        InitializeTrayIcon();
        LoadUiFromConfig();
        SetupProfiles();
        RefreshProcesses();
        RefreshDevices();
        _isUiReady = true;
        ConfigureHotkeys();
        _service.Start();
        _ = WindowsAudioPolicyService.EnsureNoCommunicationsDucking();

        if (Environment.GetCommandLineArgs().Any(arg => string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase)))
        {
            HideToTray();
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _hotkeys?.Dispose();
        DisposeTrayIcon();
        _service.Stop();
        _service.Dispose();
        ConfigService.Save(_config);
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_exitRequested)
        {
            return;
        }

        e.Cancel = true;
        HideToTray();
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            HideToTray();
        }
    }

    private void InitializeTrayIcon()
    {
        if (_notifyIcon is not null)
        {
            UpdateTrayText();
            return;
        }

        _trayOpenItem = new Forms.ToolStripMenuItem(LocalizationService.T("TrayOpen"), null, (_, _) => ShowFromTray());
        _trayPauseItem = new Forms.ToolStripMenuItem(_config.IsPaused ? LocalizationService.T("Resume") : LocalizationService.T("Pause"), null, (_, _) => TogglePauseFromTray());
        _trayExitItem = new Forms.ToolStripMenuItem(LocalizationService.T("TrayExit"), null, (_, _) => ExitFromTray());

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(_trayOpenItem);
        menu.Items.Add(_trayPauseItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_trayExitItem);

        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = LoadTrayIcon(),
            Text = LocalizationService.T("AppTitle"),
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
        UpdateTrayText();
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "MicVolumeLock.ico");
            if (File.Exists(iconPath))
            {
                return new System.Drawing.Icon(iconPath);
            }

            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(exePath))
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                if (icon is not null)
                {
                    return icon;
                }
            }
        }
        catch
        {
        }

        return (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
    }

    private void UpdateTrayText()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Text = LocalizationService.T("AppTitle");

        if (_trayOpenItem is not null)
        {
            _trayOpenItem.Text = LocalizationService.T("TrayOpen");
        }

        if (_trayPauseItem is not null)
        {
            _trayPauseItem.Text = _config.IsPaused ? LocalizationService.T("Resume") : LocalizationService.T("Pause");
        }

        if (_trayExitItem is not null)
        {
            _trayExitItem.Text = LocalizationService.T("TrayExit");
        }
    }

    private void ShowFromTray()
    {
        Dispatcher.Invoke(() =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        });
    }

    private void HideToTray()
    {
        InitializeTrayIcon();
        Hide();
    }

    private void TogglePauseFromTray()
    {
        Dispatcher.Invoke(() =>
        {
            _config.IsPaused = !_config.IsPaused;
            _service.UpdateConfig(_config);
            SaveConfig();
            PauseButton.Content = _config.IsPaused ? LocalizationService.T("Resume") : LocalizationService.T("Pause");
            RenderStatus();
            UpdateTrayText();
        });
    }

    private void ExitFromTray()
    {
        Dispatcher.Invoke(() =>
        {
            _exitRequested = true;
            if (_notifyIcon is not null)
            {
                _notifyIcon.Visible = false;
            }

            Close();
        });
    }

    private void DisposeTrayIcon()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _notifyIcon = null;
    }

    private void SetupLanguageCombo()
    {
        _suspendUi = true;
        LanguageCombo.ItemsSource = LocalizationService.LanguageOptions;
        LanguageCombo.SelectedItem = LocalizationService.LanguageOptions.First(option =>
            option.Code.Equals(LocalizationService.CurrentLanguage, StringComparison.OrdinalIgnoreCase));
        _suspendUi = false;
    }

    private void ApplyLanguage()
    {
        Title = LocalizationService.T("AppTitle");
        TitleText.Text = LocalizationService.T("AppTitle");
        SubtitleText.Text = LocalizationService.T("AppSubtitle");

        MicrophoneTab.Header = LocalizationService.T("TabMicrophone");
        SettingsTab.Header = LocalizationService.T("TabSettings");
        LogTab.Header = LocalizationService.T("TabLog");
        LanguageTab.Header = LocalizationService.T("TabLanguage");
        AboutTab.Header = LocalizationService.T("TabAbout");
        ProfilesTab.Header = LocalizationService.T("TabProfiles");
        HotkeysTab.Header = LocalizationService.T("TabHotkeys");
        DiagnosticsTab.Header = LocalizationService.T("TabDiagnostics");
        HelpTab.Header = LocalizationService.T("TabHelp");
        UpdatesTab.Header = LocalizationService.T("TabUpdates");
        ThemeTab.Header = LocalizationService.T("TabTheme");

        DeviceSectionTitle.Text = LocalizationService.T("DeviceLabel");
        RefreshButton.Content = LocalizationService.T("Refresh");
        CopyEndpointIdButton.Content = LocalizationService.T("CopyEndpoint");
        VolumeSectionTitle.Text = LocalizationService.T("TargetVolume");
        ApplyButton.Content = LocalizationService.T("Apply");
        LockCheck.Content = LocalizationService.T("LockVolume");
        StatusSectionTitle.Text = LocalizationService.T("LiveStatus");

        SettingsSectionTitle.Text = LocalizationService.T("TabSettings");
        AutoStartCheck.Content = LocalizationService.T("Startup");
        FollowDefaultCheck.Content = LocalizationService.T("FollowDefault");
        TryAgcCheck.Content = LocalizationService.T("TryAgc");
        ShowNotificationsCheck.Content = LocalizationService.T("ShowNotifications");
        DarkThemeCheck.Content = LocalizationService.T("DarkTheme");
        PauseButton.Content = _config.IsPaused ? LocalizationService.T("Resume") : LocalizationService.T("Pause");
        ResetSettingsButton.Content = LocalizationService.T("ResetSettings");

        OpenLogButton.Content = LocalizationService.T("OpenLog");
        ClearLogViewButton.Content = LocalizationService.T("ClearView");
        ProfileSectionTitle.Text = LocalizationService.T("ProfileTitle");
        ProfileHintText.Text = LocalizationService.T("ProfileHint");
        ActiveProfileLabel.Text = LocalizationService.T("ActiveProfile");
        ProfileNameText.ToolTip = LocalizationService.T("ProfileName");
        ProfileVolumeText.ToolTip = LocalizationService.T("ProfileVolume");
        ProfileLockCheck.Content = LocalizationService.T("ProfileLock");
        ApplyProfileButton.Content = LocalizationService.T("ApplyProfile");
        SaveCurrentProfileButton.Content = LocalizationService.T("SaveCurrentProfile");
        DeleteProfileButton.Content = LocalizationService.T("DeleteProfile");
        HotkeysSectionTitle.Text = LocalizationService.T("HotkeysTitle");
        HotkeysEnabledCheck.Content = LocalizationService.T("HotkeysEnabled");
        HotkeyHintText.Text = LocalizationService.T("HotkeyHint");
        HotkeyToggleText.Text = LocalizationService.T("HotkeyToggle");
        HotkeyUpText.Text = LocalizationService.T("HotkeyUp");
        HotkeyDownText.Text = LocalizationService.T("HotkeyDown");
        DiagnosticsSectionTitle.Text = LocalizationService.T("DiagnosticsTitle");
        DiagnosticsHintText.Text = LocalizationService.T("DiagnosticsHint");
        ProcessLabelText.Text = LocalizationService.T("ProcessLabel");
        RefreshProcessesButton.Content = LocalizationService.T("RefreshProcesses");
        IgnoreProcessButton.Content = LocalizationService.T("IgnoreProcess");
        StopIgnoringProcessButton.Content = LocalizationService.T("StopIgnoringProcess");
        SuspectsTitleText.Text = LocalizationService.T("SuspectsTitle");
        DiscordHintText.Text = LocalizationService.T("DiscordHint");
        ZoomHintText.Text = LocalizationService.T("ZoomHint");
        SteamHintText.Text = LocalizationService.T("SteamHint");
        WindowsExclusiveHintText.Text = LocalizationService.T("WindowsExclusiveHint");
        NvidiaHintText.Text = LocalizationService.T("NvidiaHint");
        AmdHintText.Text = LocalizationService.T("AmdHint");
        RenderProcessExceptionStatus();
        HelpSectionTitle.Text = LocalizationService.T("HelpTitle");
        CopyLogButton.Content = LocalizationService.T("CopyLog");
        OpenLogsFolderButton.Content = LocalizationService.T("OpenLogsFolder");
        ExportSupportLogButton.Content = LocalizationService.T("ExportSupportLog");
        UpdatesSectionTitle.Text = LocalizationService.T("UpdatesTitle");
        UpdatesHintText.Text = LocalizationService.T("UpdatesHint");
        CheckUpdatesButton.Content = LocalizationService.T("CheckUpdates");
        ThemeSectionTitle.Text = LocalizationService.T("ThemeTitle");
        ThemeHintText.Text = LocalizationService.T("ThemeHint");
        LightThemeRadio.Content = LocalizationService.T("LightTheme");
        DarkThemeRadio.Content = LocalizationService.T("DarkThemeOption");
        LanguageSectionTitle.Text = LocalizationService.T("LanguageTitle");
        LanguageHintText.Text = LocalizationService.T("LanguageHint");
        AboutTitleText.Text = LocalizationService.T("AboutTitle");
        AboutTextBlock.Text = $"{LocalizationService.T("Version")}: 1.0.3{Environment.NewLine}{Environment.NewLine}{LocalizationService.T("AboutText")}";

        RenderStatus();
        UpdateEndpointHint();
        UpdateTrayText();
    }

    private void LoadUiFromConfig()
    {
        _suspendUi = true;
        AutoStartCheck.IsChecked = _config.StartWithWindows;
        FollowDefaultCheck.IsChecked = _config.FollowDefaultCommunicationsDevice;
        TryAgcCheck.IsChecked = _config.TryDisableHardwareAgc;
        ShowNotificationsCheck.IsChecked = _config.ShowNotifications;
        DarkThemeCheck.IsChecked = _config.UseDarkTheme;
        LightThemeRadio.IsChecked = !_config.UseDarkTheme;
        DarkThemeRadio.IsChecked = _config.UseDarkTheme;
        HotkeysEnabledCheck.IsChecked = _config.HotkeysEnabled;
        PauseButton.Content = _config.IsPaused ? LocalizationService.T("Resume") : LocalizationService.T("Pause");
        _suspendUi = false;
    }

    private void RefreshDevices()
    {
        var devices = _service.GetCaptureDevices();
        DeviceCombo.ItemsSource = devices;

        _suspendUi = true;
        var selected = devices.FirstOrDefault(d => string.Equals(d.Id, _config.SelectedEndpointId, StringComparison.OrdinalIgnoreCase));
        if (!_config.FollowDefaultCommunicationsDevice && selected != null)
        {
            DeviceCombo.SelectedItem = selected;
            OnDeviceSelected(selected);
        }
        else if (_config.FollowDefaultCommunicationsDevice)
        {
            var defaultDevice = devices.FirstOrDefault(d => d.IsDefaultCommunicationDevice);
            DeviceCombo.SelectedItem = defaultDevice;
            if (defaultDevice is not null)
            {
                LoadEndpointProfileIntoUi(defaultDevice.Id);
            }

            SetNoExplicitDevice();
            FollowDefaultCheck.IsChecked = true;
        }
        else if (devices.Count > 0)
        {
            DeviceCombo.SelectedItem = devices[0];
            OnDeviceSelected(devices[0]);
        }
        else
        {
            DeviceCombo.Text = LocalizationService.T("DevicePlaceholder");
            EndpointIdText.Text = $"{LocalizationService.T("EndpointLabel")}: -";
        }
        _suspendUi = false;

        UpdateEndpointHint();
    }

    private void OnDeviceSelected(MicDeviceInfo device)
    {
        var wasSuspended = _suspendUi;
        _suspendUi = true;
        _config.SelectedEndpointId = device.Id;
        LoadEndpointProfileIntoUi(device.Id);
        _suspendUi = wasSuspended;

        EndpointIdText.Text = $"{LocalizationService.T("EndpointLabel")}: {device.Id}";
        UpdateDbHint();
        _service.UpdateConfig(_config);
    }

    private void LoadEndpointProfileIntoUi(string endpointId)
    {
        var profile = _config.GetProfile(endpointId);
        VolumeSlider.Value = Math.Clamp(profile.TargetVolumePercent, 0, 100);
        VolumeText.Text = profile.TargetVolumePercent.ToString(CultureInfo.InvariantCulture);
        LockCheck.IsChecked = profile.IsLockEnabled;
        UpdateDbHint();
    }

    private void SetNoExplicitDevice()
    {
        EndpointIdText.Text = LocalizationService.T("DefaultEndpoint");
        _service.UpdateConfig(_config);
    }

    private void UpdateEndpointHint()
    {
        if (_config.FollowDefaultCommunicationsDevice)
        {
            var defaultDevice = _service.GetCaptureDevices().FirstOrDefault(d => d.IsDefaultCommunicationDevice);
            EndpointIdText.Text = defaultDevice != null
                ? $"{LocalizationService.T("EndpointLabel")}: {defaultDevice.Id}"
                : $"{LocalizationService.T("EndpointLabel")}: {LocalizationService.T("EndpointWaiting")}";
        }
        else if (DeviceCombo.SelectedItem is MicDeviceInfo selected)
        {
            EndpointIdText.Text = $"{LocalizationService.T("EndpointLabel")}: {selected.Id}";
        }
    }

    private void UpdateDbHint()
    {
        if (DbHint is null)
        {
            return;
        }

        var percent = Math.Round(VolumeSlider.Value);
        var scalar = percent / 100d;
        var db = scalar > 0 ? 20 * Math.Log10(scalar) : -100;
        DbHint.Text = $"dB: {db:F1}";
    }

    private void DeviceCombo_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        if (DeviceCombo.SelectedItem is MicDeviceInfo selected)
        {
            _config.FollowDefaultCommunicationsDevice = false;
            FollowDefaultCheck.IsChecked = false;
            OnDeviceSelected(selected);
            SaveConfig();
        }
    }

    private void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        var value = (int)Math.Round(VolumeSlider.Value);
        VolumeText.Text = value.ToString(CultureInfo.InvariantCulture);
        UpdateDbHint();
        SaveCurrentProfileFromInputs();
    }

    private void VolumeText_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        if (!int.TryParse(VolumeText.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return;
        }

        VolumeSlider.Value = Math.Clamp(parsed, 0, 100);
        UpdateDbHint();
    }

    private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        SaveCurrentProfileFromInputs();
        if (ResolveSelectedEndpoint(out var endpointId))
        {
            _service.ApplyNow(endpointId);
            AddLogManual("Apply", endpointId);
            StatusText.Text = $"{LocalizationService.T("StatusApplied")}: {Math.Round(VolumeSlider.Value)}%";
            HeaderStatusText.Text = StatusText.Text;
        }
    }

    private void LockCheck_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_isUiReady || _suspendUi || !ResolveSelectedEndpoint(out var endpointId))
        {
            return;
        }

        var profile = _config.GetProfile(endpointId);
        profile.IsLockEnabled = LockCheck.IsChecked == true;
        _config.Profiles[endpointId] = profile;
        _config.DefaultTargetVolumePercent = profile.TargetVolumePercent;
        _config.DefaultLockEnabled = profile.IsLockEnabled;
        SaveConfig();
        _service.UpdateConfig(_config);
        if (profile.IsLockEnabled)
        {
            _service.ApplyNow(endpointId);
        }

        RefreshCurrentProfileStatus(endpointId);
    }

    private void AutoStartCheck_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        var requested = AutoStartCheck.IsChecked == true;
        var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? StartupService.InstalledExecutablePath;

        try
        {
            StartupService.SetAutoStart(requested, exePath);
            _config.StartWithWindows = StartupService.IsAutoStartEnabled();
            SaveConfig();
        }
        catch (UnauthorizedAccessException)
        {
            ShowAutostartAdminMessage();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, LocalizationService.T("AutostartAdminTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            SyncAutostartCheckbox();
        }
    }

    private void FollowDefaultCheck_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        _config.FollowDefaultCommunicationsDevice = FollowDefaultCheck.IsChecked == true;
        DeviceCombo.IsEnabled = !_config.FollowDefaultCommunicationsDevice;

        if (_config.FollowDefaultCommunicationsDevice)
        {
            var defaultDevice = _service.GetCaptureDevices().FirstOrDefault(d => d.IsDefaultCommunicationDevice);
            if (defaultDevice is not null)
            {
                var wasSuspended = _suspendUi;
                _suspendUi = true;
                DeviceCombo.SelectedItem = defaultDevice;
                LoadEndpointProfileIntoUi(defaultDevice.Id);
                _suspendUi = wasSuspended;

                if (_config.GetProfile(defaultDevice.Id).IsLockEnabled)
                {
                    _service.ApplyNow(defaultDevice.Id);
                }
            }

            SetNoExplicitDevice();
        }
        else if (DeviceCombo.SelectedItem is MicDeviceInfo selected)
        {
            _config.SelectedEndpointId = selected.Id;
            OnDeviceSelected(selected);
        }

        SaveConfig();
        _service.UpdateConfig(_config);
        UpdateEndpointHint();
    }

    private async void TryAgcCheck_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        _config.TryDisableHardwareAgc = TryAgcCheck.IsChecked == true;
        SaveConfig();
        _service.UpdateConfig(_config);

        if (!_config.TryDisableHardwareAgc)
        {
            RenderStatus();
            return;
        }

        if (ResolveSelectedEndpoint(out var endpointId))
        {
            AgcText.Text = $"{LocalizationService.T("Agc")}: {LocalizationService.T("AgcChecking")}";
            await _service.TryDisableHardwareAgcAsync(endpointId);
            AgcText.Text = $"{LocalizationService.T("Agc")}: {LocalizationService.LocalizeTechnicalText(_service.AgcStatus)}";
        }
        else
        {
            AgcText.Text = $"{LocalizationService.T("Agc")}: {LocalizationService.T("StatusNoDevice")}";
        }
    }

    private void ShowNotificationsCheck_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        _config.ShowNotifications = ShowNotificationsCheck.IsChecked == true;
        SaveConfig();
        _service.UpdateConfig(_config);
    }

    private void DarkThemeCheck_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        SetTheme(DarkThemeCheck.IsChecked == true);
    }

    private void HotkeysEnabledCheck_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        _config.HotkeysEnabled = HotkeysEnabledCheck.IsChecked == true;
        SaveConfig();
        ConfigureHotkeys();
    }

    private void ResetSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var answer = System.Windows.MessageBox.Show(
            LocalizationService.T("ResetSettingsQuestion"),
            LocalizationService.T("ResetSettingsTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        var language = _config.AppLanguage;
        _config = new AppConfig
        {
            AppLanguage = language,
            StartWithWindows = StartupService.IsAutoStartEnabled()
        };
        ConfigService.Save(_config);
        LocalizationService.SetLanguage(_config.AppLanguage);
        ThemeService.Apply(_config.UseDarkTheme);
        _service.UpdateConfig(_config);
        LoadUiFromConfig();
        SetupProfiles();
        RefreshDevices();
        ConfigureHotkeys();
        ApplyLanguage();
        StatusText.Text = LocalizationService.T("SettingsReset");
        HeaderStatusText.Text = StatusText.Text;
    }

    private void PauseButton_OnClick(object sender, RoutedEventArgs e)
    {
        _config.IsPaused = !_config.IsPaused;
        _service.UpdateConfig(_config);
        SaveConfig();

        PauseButton.Content = _config.IsPaused ? LocalizationService.T("Resume") : LocalizationService.T("Pause");
        RenderStatus();
        UpdateTrayText();
    }

    private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshDevices();
    }

    private void CopyEndpointIdButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ResolveSelectedEndpoint(out var endpointId))
        {
            return;
        }

        System.Windows.Clipboard.SetText(endpointId);
        StatusText.Text = LocalizationService.T("StatusCopied");
        HeaderStatusText.Text = StatusText.Text;
    }

    private void OpenLogButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(ConfigService.ConfigDirectory);
            if (!File.Exists(ConfigService.LogFile))
            {
                File.WriteAllText(ConfigService.LogFile, string.Empty);
            }

            Process.Start(new ProcessStartInfo(ConfigService.LogFile) { UseShellExecute = true });
            StatusText.Text = LocalizationService.T("LogOpened");
        }
        catch
        {
            System.Windows.MessageBox.Show(LocalizationService.T("LogOpenFailed"), LocalizationService.T("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearLogViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        LogList.Items.Clear();
        LogList.Items.Add(LocalizationService.T("LogEmpty"));
    }

    private void LanguageCombo_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_isUiReady || _suspendUi || LanguageCombo.SelectedItem is not LanguageOption selected)
        {
            return;
        }

        LocalizationService.SetLanguage(selected.Code);
        _config.AppLanguage = LocalizationService.CurrentLanguage;
        SaveConfig();
        ApplyLanguage();
        RepaintLogView();
    }

    private void SetupProfiles()
    {
        _suspendUi = true;
        ProfileCombo.ItemsSource = null;
        ProfileCombo.ItemsSource = _config.VolumeProfiles;
        var selected = _config.VolumeProfiles.FirstOrDefault(p => string.Equals(p.Id, _config.ActiveVolumeProfileId, StringComparison.OrdinalIgnoreCase))
            ?? _config.VolumeProfiles.FirstOrDefault();
        ProfileCombo.SelectedItem = selected;
        FillProfileFields(selected);
        _suspendUi = false;
    }

    private void ProfileCombo_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suspendUi)
        {
            return;
        }

        FillProfileFields(ProfileCombo.SelectedItem as VolumeProfile);
    }

    private void FillProfileFields(VolumeProfile? profile)
    {
        ProfileNameText.Text = profile?.Name ?? string.Empty;
        ProfileVolumeText.Text = (profile?.TargetVolumePercent ?? (int)Math.Round(VolumeSlider.Value)).ToString(CultureInfo.InvariantCulture);
        ProfileLockCheck.IsChecked = profile?.IsLockEnabled ?? LockCheck.IsChecked == true;
    }

    private void ApplyProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var profile = ReadProfileFromInputs();
        if (profile is null)
        {
            return;
        }

        _config.ActiveVolumeProfileId = profile.Id;
        _config.DefaultTargetVolumePercent = profile.TargetVolumePercent;
        _config.DefaultLockEnabled = profile.IsLockEnabled;

        if (ResolveSelectedEndpoint(out var endpointId))
        {
            var deviceProfile = _config.GetProfile(endpointId);
            deviceProfile.TargetVolumePercent = profile.TargetVolumePercent;
            deviceProfile.IsLockEnabled = profile.IsLockEnabled;
            _config.Profiles[endpointId] = deviceProfile;

            _suspendUi = true;
            VolumeSlider.Value = profile.TargetVolumePercent;
            VolumeText.Text = profile.TargetVolumePercent.ToString(CultureInfo.InvariantCulture);
            LockCheck.IsChecked = profile.IsLockEnabled;
            _suspendUi = false;
            UpdateDbHint();

            if (profile.IsLockEnabled)
            {
                _service.ApplyNow(endpointId);
            }
        }

        SaveConfig();
        _service.UpdateConfig(_config);
        StatusText.Text = LocalizationService.T("ProfileApplied");
        HeaderStatusText.Text = StatusText.Text;
    }

    private void SaveCurrentProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var name = string.IsNullOrWhiteSpace(ProfileNameText.Text)
            ? $"{LocalizationService.T("TabProfiles")} {_config.VolumeProfiles.Count + 1}"
            : ProfileNameText.Text.Trim();
        var volume = int.TryParse(ProfileVolumeText.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Clamp(parsed, 0, 100)
            : (int)Math.Round(Math.Clamp(VolumeSlider.Value, 0, 100));

        var profile = ProfileCombo.SelectedItem as VolumeProfile;
        if (profile is null)
        {
            profile = new VolumeProfile();
            _config.VolumeProfiles.Add(profile);
        }

        profile.Name = name;
        profile.TargetVolumePercent = volume;
        profile.IsLockEnabled = ProfileLockCheck.IsChecked == true;
        _config.ActiveVolumeProfileId = profile.Id;
        SaveConfig();
        SetupProfiles();
        ProfileCombo.SelectedItem = profile;
        StatusText.Text = LocalizationService.T("ProfileSaved");
        HeaderStatusText.Text = StatusText.Text;
    }

    private void DeleteProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is not VolumeProfile profile)
        {
            return;
        }

        _config.VolumeProfiles.Remove(profile);
        if (string.Equals(_config.ActiveVolumeProfileId, profile.Id, StringComparison.OrdinalIgnoreCase))
        {
            _config.ActiveVolumeProfileId = null;
        }

        SaveConfig();
        SetupProfiles();
        StatusText.Text = LocalizationService.T("ProfileDeleted");
        HeaderStatusText.Text = StatusText.Text;
    }

    private VolumeProfile? ReadProfileFromInputs()
    {
        var selected = ProfileCombo.SelectedItem as VolumeProfile;
        if (selected is null)
        {
            return null;
        }

        if (int.TryParse(ProfileVolumeText.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            selected.TargetVolumePercent = Math.Clamp(parsed, 0, 100);
        }

        if (!string.IsNullOrWhiteSpace(ProfileNameText.Text))
        {
            selected.Name = ProfileNameText.Text.Trim();
        }

        selected.IsLockEnabled = ProfileLockCheck.IsChecked == true;
        return selected;
    }

    private void ConfigureHotkeys()
    {
        _hotkeys ??= new HotkeyManager(this);
        _hotkeys.HotkeyPressed -= OnHotkeyPressed;
        _hotkeys.HotkeyPressed += OnHotkeyPressed;

        if (!_config.HotkeysEnabled)
        {
            _hotkeys.Unregister();
            return;
        }

        if (!_hotkeys.Register())
        {
            _config.HotkeysEnabled = false;
            _suspendUi = true;
            HotkeysEnabledCheck.IsChecked = false;
            _suspendUi = false;
            SaveConfig();
            StatusText.Text = LocalizationService.T("HotkeyConflict");
            HeaderStatusText.Text = StatusText.Text;
        }
    }

    private void OnHotkeyPressed(HotkeyAction action)
    {
        Dispatcher.Invoke(() =>
        {
            switch (action)
            {
                case HotkeyAction.ToggleProtection:
                    LockCheck.IsChecked = LockCheck.IsChecked != true;
                    LockCheck_OnChecked(LockCheck, new RoutedEventArgs());
                    break;
                case HotkeyAction.VolumeUp:
                    SetVolumeFromHotkey(5);
                    break;
                case HotkeyAction.VolumeDown:
                    SetVolumeFromHotkey(-5);
                    break;
            }
        });
    }

    private void SetVolumeFromHotkey(int delta)
    {
        var next = Math.Clamp((int)Math.Round(VolumeSlider.Value) + delta, 0, 100);
        VolumeSlider.Value = next;
        SaveCurrentProfileFromInputs();
        if (LockCheck.IsChecked == true && ResolveSelectedEndpoint(out var endpointId))
        {
            _service.ApplyNow(endpointId);
        }
    }

    private void RefreshProcesses()
    {
        try
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(process.ProcessName))
                    {
                        var normalized = ProcessExclusionService.Normalize(process.ProcessName);
                        if (ProcessExclusionService.CanExclude(normalized))
                        {
                            names.Add(normalized);
                        }
                    }
                }
                catch
                {
                    // A process can exit while the list is being built.
                }
                finally
                {
                    process.Dispose();
                }
            }

            var processes = names
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            ProcessCombo.ItemsSource = processes;
            ProcessExclusionService.Clean(_config);
            var selectedName = ProcessExclusionService.Normalize(_config.DiagnosticSelectedProcessName ?? _config.DiagnosticIgnoredProcesses.FirstOrDefault());
            ProcessCombo.SelectedItem = processes.FirstOrDefault(p => string.Equals(p, selectedName, StringComparison.OrdinalIgnoreCase));
            RenderProcessExceptionStatus();
        }
        catch
        {
            ProcessCombo.ItemsSource = Array.Empty<string>();
        }
    }

    private void RefreshProcessesButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshProcesses();
    }

    private void IgnoreProcessButton_OnClick(object sender, RoutedEventArgs e)
    {
        var processName = ProcessExclusionService.Normalize(ProcessCombo.SelectedItem as string);
        if (!ProcessExclusionService.CanExclude(processName))
        {
            return;
        }

        _config.DiagnosticSelectedProcessName = processName;
        _config.DiagnosticIgnoredProcesses.Clear();
        _config.DiagnosticIgnoredProcesses.Add(processName);
        SaveConfig();
        _service.UpdateConfig(_config);
        RenderProcessExceptionStatus();
    }

    private void StopIgnoringProcessButton_OnClick(object sender, RoutedEventArgs e)
    {
        _config.DiagnosticIgnoredProcesses.Clear();
        _config.DiagnosticSelectedProcessName = null;
        SaveConfig();
        _service.UpdateConfig(_config);
        ProcessCombo.SelectedItem = null;
        _lastDiagnosticsText = string.Empty;
        DiagnosticsLastEventText.Text = string.Empty;
    }

    private void RenderProcessExceptionStatus()
    {
        ProcessExclusionService.Clean(_config);
        var processName = ProcessExclusionService.Normalize(_config.DiagnosticIgnoredProcesses.FirstOrDefault());
        if (string.IsNullOrWhiteSpace(processName))
        {
            DiagnosticsLastEventText.Text = _lastDiagnosticsText;
            return;
        }

        _lastDiagnosticsText = LocalizationService.Format("DiagnosticsIgnoredActive", processName);
        DiagnosticsLastEventText.Text = _lastDiagnosticsText;
    }

    private void CopyLogButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = SupportExportService.BuildClipboardReport(_config, _service.GetCaptureDevices(), _service.Log);
            System.Windows.Clipboard.SetText(text);
            StatusText.Text = LocalizationService.T("LogCopied");
            HeaderStatusText.Text = StatusText.Text;
        }
        catch
        {
            System.Windows.MessageBox.Show(LocalizationService.T("LogOpenFailed"), LocalizationService.T("AppTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenLogsFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(ConfigService.ConfigDirectory);
        Process.Start(new ProcessStartInfo(ConfigService.ConfigDirectory) { UseShellExecute = true });
        StatusText.Text = LocalizationService.T("LogsFolderOpened");
        HeaderStatusText.Text = StatusText.Text;
    }

    private void ExportSupportLogButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = SupportExportService.Export(_config, _service.GetCaptureDevices(), _service.Log);
            StatusText.Text = LocalizationService.Format("SupportExported", path);
            HeaderStatusText.Text = LocalizationService.T("SupportExported").Split(':')[0];
            Process.Start(new ProcessStartInfo(ConfigService.ConfigDirectory) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusText.Text = LocalizationService.Format("SupportExportFailed", ex.Message);
            HeaderStatusText.Text = StatusText.Text;
        }
    }

    private void CheckUpdatesButton_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/DokPlay/Mic-Volume-Lock/releases") { UseShellExecute = true });
    }

    private bool ResolveSelectedEndpoint(out string endpointId)
    {
        endpointId = string.Empty;
        if (_config.FollowDefaultCommunicationsDevice)
        {
            endpointId = _service.GetCaptureDevices().FirstOrDefault(d => d.IsDefaultCommunicationDevice)?.Id ?? string.Empty;
            return !string.IsNullOrWhiteSpace(endpointId);
        }

        if (DeviceCombo.SelectedItem is MicDeviceInfo selected)
        {
            endpointId = selected.Id;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_config.SelectedEndpointId))
        {
            endpointId = _config.SelectedEndpointId;
            return true;
        }

        return false;
    }

    private void SaveCurrentProfileFromInputs()
    {
        if (!_isUiReady)
        {
            return;
        }

        if (!ResolveSelectedEndpoint(out var endpointId))
        {
            return;
        }

        var profile = _config.GetProfile(endpointId);
        var target = (int)Math.Round(Math.Clamp(VolumeSlider.Value, 0, 100));
        profile.TargetVolumePercent = target;
        _config.Profiles[endpointId] = profile;
        _config.DefaultTargetVolumePercent = target;
        _config.DefaultLockEnabled = LockCheck.IsChecked == true;
        SaveConfig();
        _service.UpdateConfig(_config);
    }

    private void RefreshCurrentProfileStatus(string endpointId)
    {
        var profile = _config.GetProfile(endpointId);
        _suspendUi = true;
        LockCheck.IsChecked = profile.IsLockEnabled;
        VolumeSlider.Value = profile.TargetVolumePercent;
        VolumeText.Text = profile.TargetVolumePercent.ToString(CultureInfo.InvariantCulture);
        _suspendUi = false;
        UpdateDbHint();
        RenderStatus();
    }

    private void OnServiceStatusChanged(ServiceStatus status)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _lastStatus = status;
            RenderStatus();
        }));
    }

    private void RenderStatus()
    {
        if (_lastStatus is null)
        {
            StatusText.Text = LocalizationService.T("StatusNoDevice");
            HeaderStatusText.Text = StatusText.Text;
            ControlText.Text = $"{LocalizationService.T("Control")}: {LocalizationService.T("Unknown")}";
            AgcText.Text = $"{LocalizationService.T("Agc")}: {LocalizationService.T("NotChecked")}";
            return;
        }

        var status = _lastStatus;
        if (status.HasActiveDevice)
        {
            var state = status.IsLocked ? LocalizationService.T("StatusLocked") : LocalizationService.T("StatusUnlocked");
            StatusText.Text = $"{state} | {status.DeviceName}";
            if (status.CurrentPercent >= 0 && status.TargetPercent >= 0)
            {
                StatusText.Text += $" | {LocalizationService.T("Current")}: {status.CurrentPercent}% / {LocalizationService.T("Target")}: {status.TargetPercent}%";
            }
        }
        else
        {
            StatusText.Text = LocalizationService.T("StatusNoDevice");
        }

        if (status.IsPaused)
        {
            StatusText.Text += $" | {LocalizationService.T("StatusPaused")}";
        }

        HeaderStatusText.Text = status.IsLocked ? LocalizationService.T("StatusLocked") : LocalizationService.T("StatusUnlocked");
        ControlText.Text = $"{LocalizationService.T("Control")}: {LocalizationService.LocalizeTechnicalText(status.HardwareSupportText)}";
        AgcText.Text = $"{LocalizationService.T("Agc")}: {LocalizationService.LocalizeTechnicalText(status.AgcStatus)}";
        DeviceCombo.IsEnabled = !_config.FollowDefaultCommunicationsDevice;
    }

    private void OnLogAdded(LogEntry entry)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (LogList.Items.Count == 1 && string.Equals(LogList.Items[0]?.ToString(), LocalizationService.T("LogEmpty"), StringComparison.Ordinal))
            {
                LogList.Items.Clear();
            }

            LogList.Items.Insert(0, FormatLogEntry(entry));
            while (LogList.Items.Count > 40)
            {
                LogList.Items.RemoveAt(LogList.Items.Count - 1);
            }
        }));
    }

    private void OnHardwareSupportChanged(string value)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ControlText.Text = $"{LocalizationService.T("Control")}: {LocalizationService.LocalizeTechnicalText(value)}";
        }));
    }

    private void OnTargetVolumeAdopted(string endpointId, int percent)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!ResolveSelectedEndpoint(out var selectedEndpointId) ||
                !string.Equals(endpointId, selectedEndpointId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _suspendUi = true;
            VolumeSlider.Value = percent;
            VolumeText.Text = percent.ToString(CultureInfo.InvariantCulture);
            _suspendUi = false;
            UpdateDbHint();
            StatusText.Text = $"{LocalizationService.T("StatusAdopted")}: {percent}%";
            HeaderStatusText.Text = StatusText.Text;
        }));
    }

    private void OnVolumeRestored(string endpointId, int previousPercent, int targetPercent)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _lastDiagnosticsText = LocalizationService.Format("DiagnosticsEventLast", previousPercent, targetPercent);
            DiagnosticsLastEventText.Text = _lastDiagnosticsText;
        }));
    }

    private void OnVolumeChangedObserved(string endpointId, int previousPercent, int currentPercent)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _lastDiagnosticsText = LocalizationService.Format("DiagnosticsEventLast", previousPercent, currentPercent);
            DiagnosticsLastEventText.Text = _lastDiagnosticsText;

            if (_config.ShowNotifications && DateTime.UtcNow - _lastVolumeNotificationUtc >= TimeSpan.FromMinutes(2))
            {
                _lastVolumeNotificationUtc = DateTime.UtcNow;
                InitializeTrayIcon();
                _notifyIcon?.ShowBalloonTip(
                    3500,
                    LocalizationService.T("AppTitle"),
                    LocalizationService.Format("NotificationVolumeChanged", previousPercent, currentPercent),
                    Forms.ToolTipIcon.Info);
            }
        }));
    }

    private void LightThemeRadio_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        SetTheme(useDarkTheme: false);
    }

    private void DarkThemeRadio_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_isUiReady || _suspendUi)
        {
            return;
        }

        SetTheme(useDarkTheme: true);
    }

    private void SetTheme(bool useDarkTheme)
    {
        _config.UseDarkTheme = useDarkTheme;
        ThemeService.Apply(_config.UseDarkTheme);
        ThemeService.ApplyWindowTheme(this, _config.UseDarkTheme);

        _suspendUi = true;
        DarkThemeCheck.IsChecked = _config.UseDarkTheme;
        LightThemeRadio.IsChecked = !_config.UseDarkTheme;
        DarkThemeRadio.IsChecked = _config.UseDarkTheme;
        _suspendUi = false;

        SaveConfig();
    }

    private void AddLogManual(string source, string endpointId)
    {
        OnLogAdded(new LogEntry
        {
            Source = source,
            EndpointId = endpointId,
            NewPercent = (int)Math.Round(Math.Clamp(VolumeSlider.Value, 0, 100)),
            Message = "User action"
        });
    }

    private string FormatLogEntry(LogEntry entry)
    {
        var source = LocalizeLogToken(entry.Source);
        var message = LocalizeLogToken(entry.Message);
        var volume = entry.PreviousPercent.HasValue || entry.NewPercent.HasValue
            ? $" | {entry.PreviousPercent?.ToString(CultureInfo.InvariantCulture) ?? "-"}% -> {entry.NewPercent?.ToString(CultureInfo.InvariantCulture) ?? "-"}%"
            : string.Empty;

        return $"[{entry.Timestamp:HH:mm:ss}] {source}{volume} | {message}";
    }

    private static string LocalizeLogToken(string? value)
    {
        return LocalizationService.LocalizeLogToken(value);
    }

    private void RepaintLogView()
    {
        LogList.Items.Clear();
        if (_service.Log.Count == 0)
        {
            LogList.Items.Add(LocalizationService.T("LogEmpty"));
            return;
        }

        foreach (var entry in _service.Log.Take(40))
        {
            LogList.Items.Add(FormatLogEntry(entry));
        }
    }

    private void SyncAutostartCheckbox()
    {
        _config.StartWithWindows = StartupService.IsAutoStartEnabled();
        _suspendUi = true;
        AutoStartCheck.IsChecked = _config.StartWithWindows;
        _suspendUi = false;
        SaveConfig();
    }

    private void ShowAutostartAdminMessage()
    {
        System.Windows.MessageBox.Show(
            LocalizationService.T("AutostartAdminMessage"),
            LocalizationService.T("AutostartAdminTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void SaveConfig()
    {
        ConfigService.Save(_config);
    }
}








