namespace MicVolumeLock.Services;

public sealed class LanguageOption
{
    public LanguageOption(string code, string displayName)
    {
        Code = code;
        DisplayName = displayName;
    }

    public string Code { get; }

    public string DisplayName { get; }

    public override string ToString() => DisplayName;
}

public static class LocalizationService
{
    public const string DefaultLanguage = "en-US";

    private static readonly string[] Keys =
    [
        "AppSubtitle", "TabMicrophone", "TabSettings", "TabLog", "TabLanguage", "TabAbout", "TabProfiles", "TabHotkeys", "TabDiagnostics", "TabHelp", "TabUpdates",
        "TrayOpen", "TrayExit", "DeviceLabel", "DevicePlaceholder", "Refresh", "CopyEndpoint", "EndpointLabel", "TargetVolume", "Apply", "LockVolume", "LiveStatus",
        "Current", "Target", "Control", "Agc", "Startup", "FollowDefault", "Pause", "Resume", "TryAgc", "DarkTheme", "ShowNotifications", "ResetSettings",
        "OpenLog", "ClearView", "LanguageTitle", "LanguageHint", "AboutTitle", "Version", "AboutText", "StatusLocked", "StatusUnlocked", "StatusNoDevice",
        "StatusPaused", "StatusApplied", "StatusCopied", "StatusAdopted", "AutostartAdminTitle", "AutostartAdminMessage", "AgcUnsupported", "LogEmpty",
        "DefaultEndpoint", "EndpointWaiting", "LogOpened", "LogOpenFailed", "Unknown", "NotChecked", "Yes", "No", "ResetSettingsTitle", "ResetSettingsQuestion",
        "SettingsReset", "ProfileTitle", "ProfileHint", "ActiveProfile", "ProfileName", "ProfileVolume", "ProfileLock", "ApplyProfile", "SaveCurrentProfile",
        "DeleteProfile", "ProfileApplied", "ProfileSaved", "ProfileDeleted", "HotkeysTitle", "HotkeysEnabled", "HotkeyToggle", "HotkeyUp", "HotkeyDown", "HotkeyHint",
        "HotkeyConflict", "DiagnosticsTitle", "DiagnosticsHint", "ProcessLabel", "RefreshProcesses", "IgnoreProcess", "StopIgnoringProcess", "SuspectsTitle",
        "DiscordHint", "ZoomHint", "SteamHint", "WindowsExclusiveHint", "DiagnosticsEventLast", "DiagnosticsIgnoredActive", "HelpTitle", "CopyLog", "OpenLogsFolder",
        "ExportSupportLog", "SupportExported", "SupportExportFailed", "LogCopied", "LogsFolderOpened", "UpdatesTitle", "UpdatesHint", "CheckUpdates", "OpenReleases",
        "NotificationVolumeChanged", "LogApply", "LogUserAction", "LogRebind", "LogMonitoring", "LogExternalRaise", "LogAdoptedTarget", "LogForced", "LogRestored",
        "LogDeviceUnavailable", "LogComError", "LogError"
    ];

    private const string MitLicense =
        "MIT License\n\nCopyright (c) 2026 Mic Volume Lock contributors\n\n" +
        "Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the \"Software\"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:\n\n" +
        "The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.\n\n" +
        "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "en-US", ["english"] = "en-US",
        ["ru"] = "ru-RU", ["russian"] = "ru-RU",
        ["es"] = "es-ES", ["spanish"] = "es-ES",
        ["pt"] = "pt-BR", ["pt-br"] = "pt-BR", ["portuguese"] = "pt-BR", ["brazilianportuguese"] = "pt-BR",
        ["de"] = "de-DE", ["german"] = "de-DE",
        ["fr"] = "fr-FR", ["french"] = "fr-FR",
        ["it"] = "it-IT", ["italian"] = "it-IT",
        ["pl"] = "pl-PL", ["polish"] = "pl-PL",
        ["tr"] = "tr-TR", ["turkish"] = "tr-TR",
        ["zh"] = "zh-CN", ["zh-cn"] = "zh-CN", ["chinese"] = "zh-CN", ["chinesesimplified"] = "zh-CN",
        ["ja"] = "ja-JP", ["japanese"] = "ja-JP",
        ["ko"] = "ko-KR", ["korean"] = "ko-KR",
        ["nl"] = "nl-NL", ["dutch"] = "nl-NL",
        ["id"] = "id-ID", ["id-id"] = "id-ID", ["indonesian"] = "id-ID",
        ["vi"] = "vi-VN", ["vi-vn"] = "vi-VN", ["vietnamese"] = "vi-VN",
        ["cs"] = "cs-CZ", ["czech"] = "cs-CZ",
        ["ar"] = "ar-SA", ["arabic"] = "ar-SA",
        ["hi"] = "hi-IN", ["hi-in"] = "hi-IN", ["hindi"] = "hi-IN",
        ["ro"] = "ro-RO", ["romanian"] = "ro-RO"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = BuildStrings();

    public static string CurrentLanguage { get; private set; } = DefaultLanguage;

    public static IReadOnlyList<LanguageOption> LanguageOptions { get; } =
    [
        new("en-US", "English"), new("ru-RU", "Русский"), new("es-ES", "Español"), new("pt-BR", "Português (Brasil)"),
        new("de-DE", "Deutsch"), new("fr-FR", "Français"), new("it-IT", "Italiano"), new("pl-PL", "Polski"),
        new("tr-TR", "Türkçe"), new("zh-CN", "简体中文"), new("ja-JP", "日本語"), new("ko-KR", "한국어"),
        new("nl-NL", "Nederlands"), new("id-ID", "Bahasa Indonesia"), new("vi-VN", "Tiếng Việt"), new("cs-CZ", "Čeština"),
        new("ar-SA", "العربية"), new("hi-IN", "हिन्दी"), new("ro-RO", "Română")
    ];

    public static string? NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var value = language.Trim();
        if (Strings.ContainsKey(value))
        {
            return value;
        }

        if (Aliases.TryGetValue(value, out var mapped))
        {
            return mapped;
        }

        var neutral = value.Split('-', '_')[0];
        return Aliases.TryGetValue(neutral, out mapped) ? mapped : null;
    }

    public static void SetLanguage(string? language)
    {
        CurrentLanguage = NormalizeLanguage(language) ?? DefaultLanguage;
    }

    public static string T(string key)
    {
        if (Strings.TryGetValue(CurrentLanguage, out var current) && current.TryGetValue(key, out var value))
        {
            return value;
        }

        return Strings[DefaultLanguage].TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string Format(string key, params object[] args) => string.Format(T(key), args);

    public static string LocalizeTechnicalText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return T("Unknown");
        }

        return text
            .Replace("Not checked", T("NotChecked"), StringComparison.OrdinalIgnoreCase)
            .Replace("not initialized", T("Unknown"), StringComparison.OrdinalIgnoreCase)
            .Replace("unable to determine", T("Unknown"), StringComparison.OrdinalIgnoreCase);
    }

    public static string LocalizeLogToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace("Monitoring a new microphone endpoint", T("LogMonitoring"), StringComparison.OrdinalIgnoreCase)
            .Replace("New level adopted as target", T("LogAdoptedTarget"), StringComparison.OrdinalIgnoreCase)
            .Replace("Level restored to target", T("LogRestored"), StringComparison.OrdinalIgnoreCase)
            .Replace("Device unavailable", T("LogDeviceUnavailable"), StringComparison.OrdinalIgnoreCase)
            .Replace("External raise", T("LogExternalRaise"), StringComparison.OrdinalIgnoreCase)
            .Replace("User action", T("LogUserAction"), StringComparison.OrdinalIgnoreCase)
            .Replace("COM error", T("LogComError"), StringComparison.OrdinalIgnoreCase)
            .Replace("Rebind", T("LogRebind"), StringComparison.OrdinalIgnoreCase)
            .Replace("Forced", T("LogForced"), StringComparison.OrdinalIgnoreCase)
            .Replace("Apply", T("LogApply"), StringComparison.OrdinalIgnoreCase)
            .Replace("Error", T("LogError"), StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, Dictionary<string, string>> BuildStrings()
    {
        var data = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["en-US"] = En(),
            ["ru-RU"] = Ru(),
            ["es-ES"] = Es(),
            ["pt-BR"] = Pt(),
            ["de-DE"] = De(),
            ["fr-FR"] = Fr(),
            ["it-IT"] = It(),
            ["pl-PL"] = Pl(),
            ["tr-TR"] = Tr(),
            ["zh-CN"] = Zh(),
            ["ja-JP"] = Ja(),
            ["ko-KR"] = Ko(),
            ["nl-NL"] = Nl(),
            ["id-ID"] = Id(),
            ["vi-VN"] = Vi(),
            ["cs-CZ"] = Cs(),
            ["ar-SA"] = Ar(),
            ["hi-IN"] = Hi(),
            ["ro-RO"] = Ro()
        };

        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (language, values) in data)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["AppTitle"] = "Mic Volume Lock"
            };

            for (var i = 0; i < Keys.Length && i < values.Length; i++)
            {
                dict[Keys[i]] = values[i].Replace("\\n", Environment.NewLine, StringComparison.Ordinal);
            }

            result[language] = dict;
        }

        ApplyCriticalTranslations(result);
        return result;
    }

    private static void ApplyCriticalTranslations(Dictionary<string, Dictionary<string, string>> result)
    {
        void Set(string language, string key, string value)
        {
            if (result.TryGetValue(language, out var dict))
            {
                dict[key] = value.Replace("\\n", Environment.NewLine, StringComparison.Ordinal);
            }
        }

        Set("en-US", "NotificationVolumeChanged", "Your system changed the microphone volume: {0}% -> {1}%. Enable volume lock in Mic Volume Lock to keep the level stable.");
        Set("ru-RU", "NotificationVolumeChanged", "Система изменила громкость микрофона: {0}% -> {1}%. Включите фиксацию громкости в Mic Volume Lock, чтобы удерживать уровень стабильным.");
        Set("es-ES", "NotificationVolumeChanged", "El sistema cambió el volumen del micrófono: {0}% -> {1}%. Activa el bloqueo de volumen en Mic Volume Lock para mantener el nivel estable.");
        Set("pt-BR", "NotificationVolumeChanged", "O sistema alterou o volume do microfone: {0}% -> {1}%. Ative o bloqueio de volume no Mic Volume Lock para manter o nível estável.");
        Set("de-DE", "NotificationVolumeChanged", "Das System hat die Mikrofonlautstärke geändert: {0}% -> {1}%. Aktivieren Sie die Lautstärkesperre in Mic Volume Lock, um den Pegel stabil zu halten.");
        Set("fr-FR", "NotificationVolumeChanged", "Le système a modifié le volume du microphone : {0}% -> {1}%. Activez le verrouillage du volume dans Mic Volume Lock pour stabiliser le niveau.");
        Set("it-IT", "NotificationVolumeChanged", "Il sistema ha modificato il volume del microfono: {0}% -> {1}%. Attiva il blocco del volume in Mic Volume Lock per mantenere stabile il livello.");
        Set("pl-PL", "NotificationVolumeChanged", "System zmienił głośność mikrofonu: {0}% -> {1}%. Włącz blokadę głośności w Mic Volume Lock, aby utrzymać stabilny poziom.");
        Set("tr-TR", "NotificationVolumeChanged", "Sistem mikrofon ses düzeyini değiştirdi: {0}% -> {1}%. Seviyeyi sabit tutmak için Mic Volume Lock içinde ses kilidini etkinleştirin.");
        Set("zh-CN", "NotificationVolumeChanged", "系统更改了麦克风音量：{0}% -> {1}%。请在 Mic Volume Lock 中启用音量锁定以保持稳定。");
        Set("ja-JP", "NotificationVolumeChanged", "システムがマイク音量を変更しました: {0}% -> {1}%。安定させるには Mic Volume Lock で音量固定を有効にしてください。");
        Set("ko-KR", "NotificationVolumeChanged", "시스템이 마이크 볼륨을 변경했습니다: {0}% -> {1}%. 안정적으로 유지하려면 Mic Volume Lock에서 볼륨 잠금을 켜세요.");
        Set("nl-NL", "NotificationVolumeChanged", "Het systeem heeft het microfoonvolume gewijzigd: {0}% -> {1}%. Schakel volumevergrendeling in Mic Volume Lock in om het niveau stabiel te houden.");
        Set("id-ID", "NotificationVolumeChanged", "Sistem mengubah volume mikrofon: {0}% -> {1}%. Aktifkan kunci volume di Mic Volume Lock agar level tetap stabil.");
        Set("vi-VN", "NotificationVolumeChanged", "Hệ thống đã thay đổi âm lượng micro: {0}% -> {1}%. Hãy bật khóa âm lượng trong Mic Volume Lock để giữ mức ổn định.");
        Set("cs-CZ", "NotificationVolumeChanged", "Systém změnil hlasitost mikrofonu: {0}% -> {1}%. Zapněte zámek hlasitosti v Mic Volume Lock, aby úroveň zůstala stabilní.");
        Set("ar-SA", "NotificationVolumeChanged", "غيّر النظام مستوى صوت الميكروفون: {0}% -> {1}%. فعّل قفل الصوت في Mic Volume Lock للحفاظ على المستوى ثابتاً.");
        Set("hi-IN", "NotificationVolumeChanged", "सिस्टम ने माइक्रोफोन वॉल्यूम बदला: {0}% -> {1}%. स्तर स्थिर रखने के लिए Mic Volume Lock में वॉल्यूम लॉक चालू करें.");
        Set("ro-RO", "NotificationVolumeChanged", "Sistemul a schimbat volumul microfonului: {0}% -> {1}%. Activează blocarea volumului în Mic Volume Lock pentru a menține nivelul stabil.");

        Set("en-US", "TabTheme", "Theme");
        Set("ru-RU", "TabTheme", "Тема");
        Set("es-ES", "TabTheme", "Tema");
        Set("pt-BR", "TabTheme", "Tema");
        Set("de-DE", "TabTheme", "Design");
        Set("fr-FR", "TabTheme", "Thème");
        Set("it-IT", "TabTheme", "Tema");
        Set("pl-PL", "TabTheme", "Motyw");
        Set("tr-TR", "TabTheme", "Tema");
        Set("zh-CN", "TabTheme", "主题");
        Set("ja-JP", "TabTheme", "テーマ");
        Set("ko-KR", "TabTheme", "테마");
        Set("nl-NL", "TabTheme", "Thema");
        Set("id-ID", "TabTheme", "Tema");
        Set("vi-VN", "TabTheme", "Giao diện");
        Set("cs-CZ", "TabTheme", "Motiv");
        Set("ar-SA", "TabTheme", "السمة");
        Set("hi-IN", "TabTheme", "थीम");
        Set("ro-RO", "TabTheme", "Temă");

        Set("en-US", "ThemeTitle", "Appearance");
        Set("ru-RU", "ThemeTitle", "Оформление");
        Set("es-ES", "ThemeTitle", "Apariencia");
        Set("pt-BR", "ThemeTitle", "Aparência");
        Set("de-DE", "ThemeTitle", "Darstellung");
        Set("fr-FR", "ThemeTitle", "Apparence");
        Set("it-IT", "ThemeTitle", "Aspetto");
        Set("pl-PL", "ThemeTitle", "Wygląd");
        Set("tr-TR", "ThemeTitle", "Görünüm");
        Set("zh-CN", "ThemeTitle", "外观");
        Set("ja-JP", "ThemeTitle", "外観");
        Set("ko-KR", "ThemeTitle", "모양");
        Set("nl-NL", "ThemeTitle", "Uiterlijk");
        Set("id-ID", "ThemeTitle", "Tampilan");
        Set("vi-VN", "ThemeTitle", "Giao diện");
        Set("cs-CZ", "ThemeTitle", "Vzhled");
        Set("ar-SA", "ThemeTitle", "المظهر");
        Set("hi-IN", "ThemeTitle", "रूप");
        Set("ro-RO", "ThemeTitle", "Aspect");

        Set("en-US", "ThemeHint", "Choose the app theme. The change is applied immediately and saved for the next launch.");
        Set("ru-RU", "ThemeHint", "Выберите тему приложения. Изменение применяется сразу и сохраняется для следующего запуска.");
        Set("es-ES", "ThemeHint", "Elige el tema de la aplicación. El cambio se aplica de inmediato y se guarda para el próximo inicio.");
        Set("pt-BR", "ThemeHint", "Escolha o tema do aplicativo. A alteração é aplicada imediatamente e salva para a próxima inicialização.");
        Set("de-DE", "ThemeHint", "Wählen Sie das App-Design. Die Änderung wird sofort angewendet und für den nächsten Start gespeichert.");
        Set("fr-FR", "ThemeHint", "Choisissez le thème de l’application. Le changement est appliqué immédiatement et enregistré pour le prochain lancement.");
        Set("it-IT", "ThemeHint", "Scegli il tema dell'app. La modifica viene applicata subito e salvata per il prossimo avvio.");
        Set("pl-PL", "ThemeHint", "Wybierz motyw aplikacji. Zmiana działa od razu i zostaje zapisana na następne uruchomienie.");
        Set("tr-TR", "ThemeHint", "Uygulama temasını seçin. Değişiklik hemen uygulanır ve sonraki açılış için kaydedilir.");
        Set("zh-CN", "ThemeHint", "选择应用主题。更改会立即应用，并保存到下次启动。");
        Set("ja-JP", "ThemeHint", "アプリのテーマを選択します。変更はすぐに適用され、次回起動時にも保存されます。");
        Set("ko-KR", "ThemeHint", "앱 테마를 선택하세요. 변경 사항은 즉시 적용되며 다음 실행에도 저장됩니다.");
        Set("nl-NL", "ThemeHint", "Kies het app-thema. De wijziging wordt direct toegepast en opgeslagen voor de volgende start.");
        Set("id-ID", "ThemeHint", "Pilih tema aplikasi. Perubahan diterapkan langsung dan disimpan untuk peluncuran berikutnya.");
        Set("vi-VN", "ThemeHint", "Chọn giao diện ứng dụng. Thay đổi được áp dụng ngay và lưu cho lần mở tiếp theo.");
        Set("cs-CZ", "ThemeHint", "Vyberte motiv aplikace. Změna se použije okamžitě a uloží pro další spuštění.");
        Set("ar-SA", "ThemeHint", "اختر سمة التطبيق. يتم تطبيق التغيير فوراً وحفظه للتشغيل التالي.");
        Set("hi-IN", "ThemeHint", "ऐप थीम चुनें। बदलाव तुरंत लागू होगा और अगली बार के लिए सहेजा जाएगा.");
        Set("ro-RO", "ThemeHint", "Alege tema aplicației. Modificarea se aplică imediat și se salvează pentru următoarea pornire.");

        Set("en-US", "LightTheme", "Light theme");
        Set("ru-RU", "LightTheme", "Светлая тема");
        Set("es-ES", "LightTheme", "Tema claro");
        Set("pt-BR", "LightTheme", "Tema claro");
        Set("de-DE", "LightTheme", "Helles Design");
        Set("fr-FR", "LightTheme", "Thème clair");
        Set("it-IT", "LightTheme", "Tema chiaro");
        Set("pl-PL", "LightTheme", "Jasny motyw");
        Set("tr-TR", "LightTheme", "Açık tema");
        Set("zh-CN", "LightTheme", "浅色主题");
        Set("ja-JP", "LightTheme", "ライトテーマ");
        Set("ko-KR", "LightTheme", "밝은 테마");
        Set("nl-NL", "LightTheme", "Licht thema");
        Set("id-ID", "LightTheme", "Tema terang");
        Set("vi-VN", "LightTheme", "Giao diện sáng");
        Set("cs-CZ", "LightTheme", "Světlý motiv");
        Set("ar-SA", "LightTheme", "السمة الفاتحة");
        Set("hi-IN", "LightTheme", "लाइट थीम");
        Set("ro-RO", "LightTheme", "Temă luminoasă");

        Set("en-US", "DarkThemeOption", "Dark theme");
        Set("ru-RU", "DarkThemeOption", "Тёмная тема");
        Set("es-ES", "DarkThemeOption", "Tema oscuro");
        Set("pt-BR", "DarkThemeOption", "Tema escuro");
        Set("de-DE", "DarkThemeOption", "Dunkles Design");
        Set("fr-FR", "DarkThemeOption", "Thème sombre");
        Set("it-IT", "DarkThemeOption", "Tema scuro");
        Set("pl-PL", "DarkThemeOption", "Ciemny motyw");
        Set("tr-TR", "DarkThemeOption", "Koyu tema");
        Set("zh-CN", "DarkThemeOption", "深色主题");
        Set("ja-JP", "DarkThemeOption", "ダークテーマ");
        Set("ko-KR", "DarkThemeOption", "어두운 테마");
        Set("nl-NL", "DarkThemeOption", "Donker thema");
        Set("id-ID", "DarkThemeOption", "Tema gelap");
        Set("vi-VN", "DarkThemeOption", "Giao diện tối");
        Set("cs-CZ", "DarkThemeOption", "Tmavý motiv");
        Set("ar-SA", "DarkThemeOption", "السمة الداكنة");
        Set("hi-IN", "DarkThemeOption", "डार्क थीम");
        Set("ro-RO", "DarkThemeOption", "Temă întunecată");

        Set("en-US", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: check microphone effects, noise removal, and the selected input device.");
        Set("ru-RU", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: проверьте эффекты микрофона, шумоподавление и выбранное устройство ввода.");
        Set("es-ES", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: revisa los efectos del micrófono, la reducción de ruido y el dispositivo de entrada seleccionado.");
        Set("pt-BR", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: verifique os efeitos do microfone, a redução de ruído e o dispositivo de entrada selecionado.");
        Set("de-DE", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: prüfen Sie Mikrofoneffekte, Rauschunterdrückung und das ausgewählte Eingabegerät.");
        Set("fr-FR", "NvidiaHint", "NVIDIA Broadcast / RTX Voice : vérifiez les effets du microphone, la réduction du bruit et le périphérique d’entrée sélectionné.");
        Set("it-IT", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: controlla gli effetti del microfono, la riduzione del rumore e il dispositivo di input selezionato.");
        Set("pl-PL", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: sprawdź efekty mikrofonu, redukcję szumów i wybrane urządzenie wejściowe.");
        Set("tr-TR", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: mikrofon efektlerini, gürültü azaltmayı ve seçili giriş aygıtını kontrol edin.");
        Set("zh-CN", "NvidiaHint", "NVIDIA Broadcast / RTX Voice：检查麦克风效果、降噪和所选输入设备。");
        Set("ja-JP", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: マイク効果、ノイズ除去、選択中の入力デバイスを確認してください。");
        Set("ko-KR", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: 마이크 효과, 노이즈 제거, 선택된 입력 장치를 확인하세요.");
        Set("nl-NL", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: controleer microfooneffecten, ruisonderdrukking en het geselecteerde invoerapparaat.");
        Set("id-ID", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: periksa efek mikrofon, pengurangan noise, dan perangkat input yang dipilih.");
        Set("vi-VN", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: kiểm tra hiệu ứng micro, khử nhiễu và thiết bị đầu vào đã chọn.");
        Set("cs-CZ", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: zkontrolujte efekty mikrofonu, potlačení šumu a vybrané vstupní zařízení.");
        Set("ar-SA", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: تحقق من تأثيرات الميكروفون، وإزالة الضوضاء، وجهاز الإدخال المحدد.");
        Set("hi-IN", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: माइक्रोफोन प्रभाव, शोर हटाने और चुने गए इनपुट डिवाइस की जाँच करें.");
        Set("ro-RO", "NvidiaHint", "NVIDIA Broadcast / RTX Voice: verifică efectele microfonului, reducerea zgomotului și dispozitivul de intrare selectat.");

        Set("en-US", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("ru-RU", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("es-ES", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("pt-BR", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("de-DE", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("fr-FR", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("it-IT", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("pl-PL", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("tr-TR", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("zh-CN", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("ja-JP", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("ko-KR", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("nl-NL", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("id-ID", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("vi-VN", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("cs-CZ", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("ar-SA", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("hi-IN", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
        Set("ro-RO", "AmdHint", "AMD Software: Adrenalin Edition -> Audio & Video -> AMD Noise Suppression / AMD Streaming Audio Device.");
    }

    private static string About(string purpose, string privacy, string limitation) =>
        purpose + "\\n\\n" + privacy + "\\n\\nLicense: MIT License.\\n\\n" + limitation + "\\n\\n" + MitLicense.Replace("\n", "\\n", StringComparison.Ordinal);

    private static string[] En() =>
    [
        "Keep your selected microphone level steady in Windows 10/11.", "Microphone", "Settings", "Log", "Language", "About", "Profiles", "Hotkeys", "Diagnostics", "Help", "Updates",
        "Open", "Exit", "Microphone", "No active microphone found", "Refresh", "Copy ID", "Endpoint ID", "Target volume", "Apply", "Lock volume", "Live status",
        "current", "target", "Control", "AGC", "Run Mic Volume Lock at Windows startup", "Follow default communications microphone", "Pause protection", "Resume protection", "Try to disable hardware AGC", "Use dark theme", "Show notifications", "Reset settings",
        "Open log file", "Clear view", "Interface language", "The language changes immediately and is saved for the next launch.", "About Mic Volume Lock", "Version", About("Mic Volume Lock is a free Windows 10/11 utility that keeps a chosen microphone endpoint at the level you set.", "It does not record, store, or transmit microphone audio. It only reads and writes the Windows Core Audio endpoint volume for the selected capture device.", "Some drivers, vendor APOs, app-side auto-gain, hardware AGC, and exclusive-mode audio paths can still affect the real microphone gain outside normal endpoint volume control."), "Protection enabled", "Protection disabled", "No microphone selected",
        "Paused", "Applied", "Endpoint ID copied to clipboard", "New external level adopted", "Administrator rights required", "This installation uses all-users startup. Run Mic Volume Lock as administrator, or enable startup during installation.", "AGC: hardware AGC control is not available in this build.", "No events yet.",
        "Default communications microphone will be selected automatically.", "Waiting for default communications microphone", "Log file opened", "Could not open log file", "unknown", "not checked", "Yes", "No", "Reset settings", "Reset all Mic Volume Lock settings to defaults? The current language will be kept.",
        "Settings were reset.", "Volume profiles", "Create quick presets for games, work, streaming, and quiet rooms.", "Active profile", "Profile name", "Volume", "Lock after applying", "Apply profile", "Save current as profile", "Delete profile", "Profile applied.", "Profile saved.", "Profile deleted.", "Hotkeys", "Enable global hotkeys", "Ctrl + Alt + M toggles protection", "Ctrl + Alt + Up raises volume", "Ctrl + Alt + Down lowers volume", "Hotkeys work while the app is in the tray.", "Could not register hotkeys. Another app may already use them.", "Who changed volume?", "Windows does not expose the exact app that changed microphone level. Use these diagnostics to check likely causes and temporarily ignore a selected process while testing.", "Process", "Refresh processes", "Ignore selected process", "Stop ignoring process", "Likely places to check", "Discord: Voice & Video -> Automatic Gain Control", "Zoom: Audio -> Automatically adjust microphone volume", "Steam or game voice input settings", "Windows microphone exclusive mode and communication settings", "Last change: {0}% -> {1}%", "Ignoring restore while {0} is running.", "Support", "Copy log", "Open logs folder", "Export support log", "Support log exported: {0}", "Could not export support log: {0}", "Log copied to clipboard.", "Logs folder opened.", "Updates", "Auto-update is not built in yet. You can check GitHub Releases for new versions.", "Check for updates", "Open releases page", "Your system changed the microphone volume: {0}% -> {1}%. Enable volume lock in Mic Volume Lock to keep it stable.", "Apply", "User action", "Rebind", "Monitoring a new microphone endpoint", "External raise", "New level adopted as target", "Forced", "Level restored to target", "Device unavailable", "COM error", "Error"
    ];

    private static string[] Ru() =>
    [
        "Фиксирует выбранный уровень микрофона в Windows 10/11.", "Микрофон", "Настройки", "Журнал", "Язык", "О программе", "Профили", "Горячие клавиши", "Диагностика", "Помощь", "Обновления",
        "Открыть", "Выход", "Микрофон", "Активный микрофон не найден", "Обновить", "Копировать ID", "Endpoint ID", "Целевая громкость", "Применить", "Фиксировать уровень", "Текущий статус",
        "текущий", "целевой", "Контроль", "AGC", "Запускать Mic Volume Lock вместе с Windows", "Следовать за устройством связи по умолчанию", "Поставить защиту на паузу", "Возобновить защиту", "Пытаться отключить аппаратный AGC", "Использовать тёмную тему", "Показывать уведомления", "Сбросить настройки",
        "Открыть журнал", "Очистить вид", "Язык интерфейса", "Язык меняется сразу и сохраняется для следующего запуска.", "О Mic Volume Lock", "Версия", About("Mic Volume Lock — бесплатная утилита для Windows 10/11, которая удерживает выбранный микрофон на заданном уровне громкости.", "Программа не записывает, не хранит и не отправляет звук с микрофона. Она только читает и меняет системный уровень Windows Core Audio для выбранного устройства записи.", "Некоторые драйверы, vendor APO, автоусиление в приложениях, аппаратный AGC и exclusive-mode аудиопути могут менять фактическое усиление вне обычного системного ползунка."), "Фиксация включена", "Фиксация отключена", "Микрофон не выбран",
        "Пауза", "Применено", "Endpoint ID скопирован в буфер обмена", "Новый внешний уровень принят", "Нужны права администратора", "Эта установка использует автозапуск для всех пользователей. Запустите Mic Volume Lock от администратора или включите автозапуск во время установки.", "AGC: управление аппаратным AGC в этой сборке недоступно.", "Событий пока нет.",
        "Устройство связи по умолчанию будет выбрано автоматически.", "Ожидание устройства связи по умолчанию", "Журнал открыт", "Не удалось открыть журнал", "неизвестно", "не проверено", "Да", "Нет", "Сбросить настройки", "Сбросить все настройки Mic Volume Lock по умолчанию? Текущий язык будет сохранён.",
        "Настройки сброшены.", "Профили громкости", "Быстрые пресеты для игр, работы, стрима и тихой комнаты.", "Активный профиль", "Название профиля", "Громкость", "Фиксировать после применения", "Применить профиль", "Сохранить текущий как профиль", "Удалить профиль", "Профиль применён.", "Профиль сохранён.", "Профиль удалён.", "Горячие клавиши", "Включить глобальные горячие клавиши", "Ctrl + Alt + M включает/выключает защиту", "Ctrl + Alt + ↑ повышает громкость", "Ctrl + Alt + ↓ снижает громкость", "Горячие клавиши работают, даже когда приложение в трее.", "Не удалось зарегистрировать горячие клавиши. Возможно, их уже использует другая программа.", "Кто меняет громкость?", "Windows не сообщает точное приложение, которое изменило уровень микрофона. Эта диагностика помогает проверить вероятные причины и временно игнорировать выбранный процесс при тесте.", "Процесс", "Обновить процессы", "Игнорировать выбранный процесс", "Больше не игнорировать", "Что проверить", "Discord: Voice & Video -> Automatic Gain Control", "Zoom: Audio -> Automatically adjust microphone volume", "Steam или голосовые настройки игры", "Windows: эксклюзивный режим микрофона и параметры связи", "Последнее изменение: {0}% -> {1}%", "Восстановление отключено, пока запущен {0}.", "Помощь", "Скопировать лог", "Открыть папку с логами", "Экспорт лога для поддержки", "Лог поддержки экспортирован: {0}", "Не удалось экспортировать лог поддержки: {0}", "Лог скопирован в буфер обмена.", "Папка с логами открыта.", "Обновления", "Автообновление пока не встроено. Новые версии можно проверять на странице GitHub Releases.", "Проверить обновления", "Открыть страницу релизов", "Система изменила громкость микрофона: {0}% -> {1}%. Включите фиксацию в Mic Volume Lock, чтобы удерживать уровень стабильным.", "Применить", "Действие пользователя", "Переподключение", "Отслеживание нового микрофона", "Внешнее повышение", "Новый уровень принят как целевой", "Принудительно", "Уровень восстановлен до целевого", "Устройство недоступно", "COM ошибка", "Ошибка"
    ];

    private static string[] Es() => Pack("Mantiene estable el nivel del micrófono seleccionado en Windows 10/11.", "Micrófono", "Ajustes", "Registro", "Idioma", "Acerca de", "Perfiles", "Atajos", "Diagnóstico", "Ayuda", "Actualizaciones", "Abrir", "Salir", "No se encontró ningún micrófono activo", "Actualizar", "Copiar ID", "Volumen objetivo", "Aplicar", "Bloquear volumen", "Ejecutar al iniciar Windows", "Usar tema oscuro", "Mostrar notificaciones", "Restablecer ajustes", "Sí", "No", "¿Quién cambia el volumen?", "Soporte", "Buscar actualizaciones", "El sistema cambió el volumen del micrófono: {0}% -> {1}%. Activa el bloqueo de volumen en Mic Volume Lock para mantenerlo estable.");
    private static string[] Pt() => Pack("Mantém estável o nível do microfone selecionado no Windows 10/11.", "Microfone", "Configurações", "Registro", "Idioma", "Sobre", "Perfis", "Atalhos", "Diagnóstico", "Ajuda", "Atualizações", "Abrir", "Sair", "Nenhum microfone ativo encontrado", "Atualizar", "Copiar ID", "Volume alvo", "Aplicar", "Bloquear volume", "Executar ao iniciar o Windows", "Usar tema escuro", "Mostrar notificações", "Redefinir configurações", "Sim", "Não", "Quem altera o volume?", "Suporte", "Verificar atualizações", "O sistema alterou o volume do microfone: {0}% -> {1}%. Ative o bloqueio de volume no Mic Volume Lock para manter o nível estável.");
    private static string[] De() => Pack("Hält den gewählten Mikrofonpegel unter Windows 10/11 stabil.", "Mikrofon", "Einstellungen", "Protokoll", "Sprache", "Über", "Profile", "Hotkeys", "Diagnose", "Hilfe", "Updates", "Öffnen", "Beenden", "Kein aktives Mikrofon gefunden", "Aktualisieren", "ID kopieren", "Ziel-Lautstärke", "Anwenden", "Lautstärke sperren", "Mit Windows starten", "Dunkles Design verwenden", "Benachrichtigungen anzeigen", "Einstellungen zurücksetzen", "Ja", "Nein", "Wer ändert die Lautstärke?", "Support", "Nach Updates suchen", "Das System hat die Mikrofonlautstärke geändert: {0}% -> {1}%. Aktivieren Sie die Lautstärkesperre in Mic Volume Lock, um den Pegel stabil zu halten.");
    private static string[] Fr() => Pack("Maintient stable le niveau du microphone sélectionné sous Windows 10/11.", "Microphone", "Paramètres", "Journal", "Langue", "À propos", "Profils", "Raccourcis", "Diagnostic", "Aide", "Mises à jour", "Ouvrir", "Quitter", "Aucun microphone actif trouvé", "Actualiser", "Copier l’ID", "Volume cible", "Appliquer", "Verrouiller le volume", "Lancer avec Windows", "Utiliser le thème sombre", "Afficher les notifications", "Réinitialiser les paramètres", "Oui", "Non", "Qui change le volume ?", "Support", "Rechercher des mises à jour", "Le système a modifié le volume du microphone : {0}% -> {1}%. Activez le verrouillage du volume dans Mic Volume Lock pour le stabiliser.");
    private static string[] It() => Pack("Mantiene stabile il livello del microfono selezionato in Windows 10/11.", "Microfono", "Impostazioni", "Registro", "Lingua", "Informazioni", "Profili", "Scorciatoie", "Diagnostica", "Aiuto", "Aggiornamenti", "Apri", "Esci", "Nessun microfono attivo trovato", "Aggiorna", "Copia ID", "Volume target", "Applica", "Blocca volume", "Esegui all’avvio di Windows", "Usa tema scuro", "Mostra notifiche", "Ripristina impostazioni", "Sì", "No", "Chi cambia il volume?", "Supporto", "Controlla aggiornamenti", "Il sistema ha modificato il volume del microfono: {0}% -> {1}%. Attiva il blocco del volume in Mic Volume Lock per mantenerlo stabile.");
    private static string[] Pl() => Pack("Utrzymuje wybrany poziom mikrofonu w Windows 10/11.", "Mikrofon", "Ustawienia", "Dziennik", "Język", "O programie", "Profile", "Skróty", "Diagnostyka", "Pomoc", "Aktualizacje", "Otwórz", "Zakończ", "Nie znaleziono aktywnego mikrofonu", "Odśwież", "Kopiuj ID", "Docelowa głośność", "Zastosuj", "Zablokuj głośność", "Uruchamiaj z Windows", "Użyj ciemnego motywu", "Pokazuj powiadomienia", "Resetuj ustawienia", "Tak", "Nie", "Kto zmienia głośność?", "Wsparcie", "Sprawdź aktualizacje", "System zmienił głośność mikrofonu: {0}% -> {1}%. Włącz blokadę głośności w Mic Volume Lock, aby utrzymać stabilny poziom.");
    private static string[] Tr() => Pack("Windows 10/11’de seçili mikrofon seviyesini sabit tutar.", "Mikrofon", "Ayarlar", "Günlük", "Dil", "Hakkında", "Profiller", "Kısayollar", "Tanılama", "Yardım", "Güncellemeler", "Aç", "Çıkış", "Etkin mikrofon bulunamadı", "Yenile", "ID kopyala", "Hedef ses düzeyi", "Uygula", "Ses düzeyini kilitle", "Windows başlangıcında çalıştır", "Koyu temayı kullan", "Bildirimleri göster", "Ayarları sıfırla", "Evet", "Hayır", "Sesi kim değiştiriyor?", "Destek", "Güncellemeleri kontrol et", "Sistem mikrofon ses düzeyini değiştirdi: {0}% -> {1}%. Seviyeyi sabit tutmak için Mic Volume Lock içinde ses kilidini etkinleştirin.");
    private static string[] Zh() => Pack("在 Windows 10/11 中保持所选麦克风音量稳定。", "麦克风", "设置", "日志", "语言", "关于", "配置文件", "快捷键", "诊断", "帮助", "更新", "打开", "退出", "未找到活动麦克风", "刷新", "复制 ID", "目标音量", "应用", "锁定音量", "Windows 启动时运行", "使用深色主题", "显示通知", "重置设置", "是", "否", "谁更改了音量？", "支持", "检查更新", "系统更改了麦克风音量：{0}% -> {1}%。请在 Mic Volume Lock 中启用音量锁定以保持稳定。");
    private static string[] Ja() => Pack("Windows 10/11 で選択したマイク音量を安定させます。", "マイク", "設定", "ログ", "言語", "情報", "プロファイル", "ホットキー", "診断", "ヘルプ", "更新", "開く", "終了", "有効なマイクが見つかりません", "更新", "ID をコピー", "目標音量", "適用", "音量を固定", "Windows 起動時に実行", "ダークテーマを使用", "通知を表示", "設定をリセット", "はい", "いいえ", "誰が音量を変えた？", "サポート", "更新を確認", "システムがマイク音量を変更しました: {0}% -> {1}%。安定させるには Mic Volume Lock で音量固定を有効にしてください。");
    private static string[] Ko() => Pack("Windows 10/11에서 선택한 마이크 볼륨을 안정적으로 유지합니다.", "마이크", "설정", "로그", "언어", "정보", "프로필", "단축키", "진단", "도움말", "업데이트", "열기", "종료", "활성 마이크를 찾을 수 없음", "새로 고침", "ID 복사", "목표 볼륨", "적용", "볼륨 잠금", "Windows 시작 시 실행", "어두운 테마 사용", "알림 표시", "설정 초기화", "예", "아니요", "누가 볼륨을 변경하나요?", "지원", "업데이트 확인", "시스템이 마이크 볼륨을 변경했습니다: {0}% -> {1}%. 안정적으로 유지하려면 Mic Volume Lock에서 볼륨 잠금을 켜세요.");
    private static string[] Nl() => Pack("Houdt het gekozen microfoonniveau stabiel in Windows 10/11.", "Microfoon", "Instellingen", "Logboek", "Taal", "Over", "Profielen", "Sneltoetsen", "Diagnose", "Help", "Updates", "Openen", "Afsluiten", "Geen actieve microfoon gevonden", "Vernieuwen", "ID kopiëren", "Doelvolume", "Toepassen", "Volume vergrendelen", "Starten met Windows", "Donker thema gebruiken", "Meldingen tonen", "Instellingen resetten", "Ja", "Nee", "Wie verandert het volume?", "Support", "Controleren op updates", "Het systeem heeft het microfoonvolume gewijzigd: {0}% -> {1}%. Schakel volumevergrendeling in Mic Volume Lock in om het niveau stabiel te houden.");
    private static string[] Id() => Pack("Menjaga level mikrofon yang dipilih tetap stabil di Windows 10/11.", "Mikrofon", "Pengaturan", "Log", "Bahasa", "Tentang", "Profil", "Hotkey", "Diagnostik", "Bantuan", "Pembaruan", "Buka", "Keluar", "Tidak ada mikrofon aktif ditemukan", "Segarkan", "Salin ID", "Volume target", "Terapkan", "Kunci volume", "Jalankan saat Windows mulai", "Gunakan tema gelap", "Tampilkan notifikasi", "Atur ulang pengaturan", "Ya", "Tidak", "Siapa yang mengubah volume?", "Dukungan", "Periksa pembaruan", "Sistem mengubah volume mikrofon: {0}% -> {1}%. Aktifkan kunci volume di Mic Volume Lock agar level tetap stabil.");
    private static string[] Vi() => Pack("Giữ ổn định mức âm lượng micro đã chọn trên Windows 10/11.", "Micro", "Cài đặt", "Nhật ký", "Ngôn ngữ", "Giới thiệu", "Hồ sơ", "Phím nóng", "Chẩn đoán", "Trợ giúp", "Cập nhật", "Mở", "Thoát", "Không tìm thấy micro đang hoạt động", "Làm mới", "Sao chép ID", "Âm lượng mục tiêu", "Áp dụng", "Khóa âm lượng", "Chạy khi Windows khởi động", "Dùng giao diện tối", "Hiển thị thông báo", "Đặt lại cài đặt", "Có", "Không", "Ai thay đổi âm lượng?", "Hỗ trợ", "Kiểm tra cập nhật", "Hệ thống đã thay đổi âm lượng micro: {0}% -> {1}%. Hãy bật khóa âm lượng trong Mic Volume Lock để giữ mức ổn định.");
    private static string[] Cs() => Pack("Udržuje vybranou úroveň mikrofonu ve Windows 10/11 stabilní.", "Mikrofon", "Nastavení", "Protokol", "Jazyk", "O aplikaci", "Profily", "Klávesové zkratky", "Diagnostika", "Nápověda", "Aktualizace", "Otevřít", "Ukončit", "Nebyl nalezen žádný aktivní mikrofon", "Obnovit", "Kopírovat ID", "Cílová hlasitost", "Použít", "Uzamknout hlasitost", "Spouštět s Windows", "Použít tmavý motiv", "Zobrazovat oznámení", "Obnovit nastavení", "Ano", "Ne", "Kdo mění hlasitost?", "Podpora", "Zkontrolovat aktualizace", "Systém změnil hlasitost mikrofonu: {0}% -> {1}%. Zapněte zámek hlasitosti v Mic Volume Lock, aby úroveň zůstala stabilní.");
    private static string[] Ar() => Pack("يحافظ على مستوى الميكروفون المحدد ثابتاً في Windows 10/11.", "الميكروفون", "الإعدادات", "السجل", "اللغة", "حول", "الملفات", "الاختصارات", "التشخيص", "المساعدة", "التحديثات", "فتح", "خروج", "لم يتم العثور على ميكروفون نشط", "تحديث", "نسخ المعرّف", "مستوى الصوت الهدف", "تطبيق", "قفل مستوى الصوت", "تشغيل عند بدء Windows", "استخدام السمة الداكنة", "إظهار الإشعارات", "إعادة ضبط الإعدادات", "نعم", "لا", "من يغير مستوى الصوت؟", "الدعم", "التحقق من التحديثات", "غيّر النظام مستوى صوت الميكروفون: {0}% -> {1}%. فعّل قفل الصوت في Mic Volume Lock للحفاظ على المستوى ثابتاً.");
    private static string[] Hi() => Pack("Windows 10/11 में चुने गए माइक्रोफोन स्तर को स्थिर रखता है।", "माइक्रोफोन", "सेटिंग्स", "लॉग", "भाषा", "परिचय", "प्रोफ़ाइल", "हॉटकी", "डायग्नोस्टिक्स", "सहायता", "अपडेट", "खोलें", "बाहर निकलें", "कोई सक्रिय माइक्रोफोन नहीं मिला", "रीफ्रेश", "ID कॉपी करें", "लक्षित वॉल्यूम", "लागू करें", "वॉल्यूम लॉक करें", "Windows शुरू होने पर चलाएँ", "डार्क थीम उपयोग करें", "सूचनाएँ दिखाएँ", "सेटिंग्स रीसेट करें", "हाँ", "नहीं", "वॉल्यूम कौन बदल रहा है?", "समर्थन", "अपडेट जाँचें", "सिस्टम ने माइक्रोफोन वॉल्यूम बदला: {0}% -> {1}%. स्तर स्थिर रखने के लिए Mic Volume Lock में वॉल्यूम लॉक चालू करें.");
    private static string[] Ro() => Pack("Menține stabil nivelul microfonului selectat în Windows 10/11.", "Microfon", "Setări", "Jurnal", "Limbă", "Despre", "Profiluri", "Taste rapide", "Diagnostic", "Ajutor", "Actualizări", "Deschide", "Ieșire", "Nu s-a găsit niciun microfon activ", "Reîmprospătează", "Copiază ID", "Volum țintă", "Aplică", "Blochează volumul", "Rulează la pornirea Windows", "Folosește tema întunecată", "Afișează notificări", "Resetează setările", "Da", "Nu", "Cine schimbă volumul?", "Suport", "Caută actualizări", "Sistemul a schimbat volumul microfonului: {0}% -> {1}%. Activează blocarea volumului în Mic Volume Lock pentru a menține nivelul stabil.");

    private static string[] Pack(
        string subtitle, string mic, string settings, string log, string language, string about, string profiles, string hotkeys, string diagnostics, string help, string updates,
        string open, string exit, string noMic, string refresh, string copyId, string targetVolume, string apply, string lockVolume, string startup,
        string darkTheme, string notifications, string reset, string yes, string no, string whoChanged, string support, string checkUpdates, string notificationChanged)
    {
        var en = En();
        var values = (string[])en.Clone();
        values[0] = subtitle; values[1] = mic; values[2] = settings; values[3] = log; values[4] = language; values[5] = about; values[6] = profiles; values[7] = hotkeys; values[8] = diagnostics; values[9] = help; values[10] = updates;
        values[11] = open; values[12] = exit; values[13] = mic; values[14] = noMic; values[15] = refresh; values[16] = copyId; values[18] = targetVolume; values[19] = apply; values[20] = lockVolume;
        values[26] = startup; values[31] = darkTheme; values[32] = notifications; values[33] = reset; values[58] = yes; values[59] = no; values[83] = whoChanged; values[96] = support; values[106] = checkUpdates; values[108] = notificationChanged;
        return values;
    }
}


