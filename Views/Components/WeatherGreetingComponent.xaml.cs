using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "A7B8C9D0-E1F2-3456-0123-123456789016",
    "天气问候",
    "\uE753",
    "根据ClassIsland天气显示问候语和预警提醒"
)]
public class WeatherGreetingComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;

    public WeatherGreetingComponent()
    {
        var panel = new Grid { ColumnDefinitions = new ColumnDefinitions("*"), VerticalAlignment = VerticalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 };
        Grid.SetColumn(_txt, 0); panel.Children.Add(_txt); Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); Update(); });
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.WeatherGreetingEnabled) { _txt.Text = ""; return; }

        var weatherCode = GetCurrentWeatherCode();
        var warning = GetWeatherWarning();

        if (string.IsNullOrEmpty(weatherCode) && string.IsNullOrEmpty(warning))
        {
            _txt.Text = "";
            return;
        }

        // 通过 IWeatherService.GetWeatherTextByCode 获取天气文本
        var weatherText = GetWeatherTextByCode(weatherCode);

        // 使用设置中的自定义问候语匹配
        var greet = "";
        if (!string.IsNullOrEmpty(weatherText))
        {
            // 按关键词匹配，优先匹配最长的关键词
            var match = _svc.Settings.WeatherGreetings
                .Where(kv => kv.Key != "默认" && weatherText.Contains(kv.Key))
                .OrderByDescending(kv => kv.Key.Length)
                .FirstOrDefault();
            greet = match.Value ?? "";
            if (string.IsNullOrEmpty(greet) && _svc.Settings.WeatherGreetings.TryGetValue("默认", out var def))
                greet = def.Replace("{weather}", weatherText);
        }

        // 预警简短显示
        if (!string.IsNullOrEmpty(warning))
        {
            // 只保留预警类型（如"高温"、"暴雨"），去掉"发布"、"预警"等字
            var shortWarning = warning.Replace("发布", "").Replace("预警", "").Replace("信号", "").Trim();
            if (shortWarning.Length > 6) shortWarning = shortWarning.Substring(0, 6);
            greet = $"⚠️{shortWarning} " + greet;
        }

        _txt.Text = greet;
    }

    string GetWeatherGreeting(string weatherText)
    {
        if (string.IsNullOrEmpty(weatherText)) return "";

        var greetings = _svc?.Settings.WeatherGreetings;
        if (greetings == null || greetings.Count == 0) return weatherText;

        // 查找匹配的关键词（排除"默认"）
        foreach (var kv in greetings)
        {
            if (kv.Key == "默认") continue;
            if (weatherText.Contains(kv.Key))
                return kv.Value.Replace("{weather}", weatherText);
        }

        // 使用默认
        if (greetings.TryGetValue("默认", out var def))
            return def.Replace("{weather}", weatherText);

        return weatherText;
    }

    /// <summary>
    /// 通过反射获取当前天气代码
    /// </summary>
    string GetCurrentWeatherCode()
    {
        try
        {
            // 方式1：尝试通过 IAppHost.TryGetService<IWeatherService>() 获取
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");

            if (appHostType != null)
            {
                // 尝试调用 TryGetService<IWeatherService>()
                var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
                if (tryGetService != null && tryGetService.IsGenericMethodDefinition)
                {
                    var weatherServiceType = Type.GetType("ClassIsland.Core.Abstractions.Services.IWeatherService, ClassIsland.Core")
                        ?? AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == "IWeatherService");

                    if (weatherServiceType != null)
                    {
                        var genericMethod = tryGetService.MakeGenericMethod(weatherServiceType);
                        var weatherService = genericMethod.Invoke(null, null);
                        if (weatherService != null)
                        {
                            // 获取 WeatherStatusList 和 IsWeatherRefreshed
                            var wsType = weatherService.GetType();
                            var isRefreshedProp = wsType.GetProperty("IsWeatherRefreshed");
                            if (isRefreshedProp != null)
                            {
                                var isRefreshed = (bool)(isRefreshedProp.GetValue(weatherService) ?? false);
                                if (!isRefreshed) return "";
                            }

                            // 通过 SettingsService.Settings.LastWeatherInfo.Current.Weather 获取天气代码
                            return GetWeatherCodeViaSettings();
                        }
                    }
                }
            }

            // 方式2：直接通过 SettingsService 获取
            return GetWeatherCodeViaSettings();
        }
        catch { return ""; }
    }

    string GetWeatherCodeViaSettings()
    {
        try
        {
            // 获取 SettingsService
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");

            if (appHostType == null) return "";

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return "";

            // 查找 SettingsService 类型
            var settingsServiceType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "SettingsService");

            if (settingsServiceType == null) return "";

            var genericMethod = tryGetService.MakeGenericMethod(settingsServiceType);
            var settingsService = genericMethod.Invoke(null, null);
            if (settingsService == null) return "";

            // 获取 Settings 属性
            var settingsProp = settingsServiceType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Instance);
            if (settingsProp == null) return "";

            var settings = settingsProp.GetValue(settingsService);
            if (settings == null) return "";

            // 获取 LastWeatherInfo 属性
            var lastWeatherInfoProp = settings.GetType().GetProperty("LastWeatherInfo", BindingFlags.Public | BindingFlags.Instance);
            if (lastWeatherInfoProp == null) return "";

            var lastWeatherInfo = lastWeatherInfoProp.GetValue(settings);
            if (lastWeatherInfo == null) return "";

            // 获取 Current 属性
            var currentProp = lastWeatherInfo.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance);
            if (currentProp == null) return "";

            var current = currentProp.GetValue(lastWeatherInfo);
            if (current == null) return "";

            // 获取 Weather 属性（天气代码）
            var weatherProp = current.GetType().GetProperty("Weather", BindingFlags.Public | BindingFlags.Instance);
            if (weatherProp == null) return "";

            return weatherProp.GetValue(current)?.ToString() ?? "";
        }
        catch { return ""; }
    }

    string GetWeatherTextByCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return "";
        try
        {
            // 通过 IWeatherService.GetWeatherTextByCode 获取天气文本
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");

            if (appHostType == null) return "";

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return "";

            var weatherServiceType = Type.GetType("ClassIsland.Core.Abstractions.Services.IWeatherService, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IWeatherService");

            if (weatherServiceType == null) return "";

            var genericMethod = tryGetService.MakeGenericMethod(weatherServiceType);
            var weatherService = genericMethod.Invoke(null, null);
            if (weatherService == null) return "";

            var getWeatherText = weatherServiceType.GetMethod("GetWeatherTextByCode", BindingFlags.Public | BindingFlags.Instance);
            if (getWeatherText == null) return "";

            return getWeatherText.Invoke(weatherService, new object[] { code })?.ToString() ?? "";
        }
        catch { return ""; }
    }

    string GetWeatherWarning()
    {
        try
        {
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");

            if (appHostType == null) return "";

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return "";

            var settingsServiceType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "SettingsService");

            if (settingsServiceType == null) return "";

            var genericMethod = tryGetService.MakeGenericMethod(settingsServiceType);
            var settingsService = genericMethod.Invoke(null, null);
            if (settingsService == null) return "";

            var settingsProp = settingsServiceType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Instance);
            if (settingsProp == null) return "";

            var settings = settingsProp.GetValue(settingsService);
            if (settings == null) return "";

            var lastWeatherInfoProp = settings.GetType().GetProperty("LastWeatherInfo", BindingFlags.Public | BindingFlags.Instance);
            if (lastWeatherInfoProp == null) return "";

            var lastWeatherInfo = lastWeatherInfoProp.GetValue(settings);
            if (lastWeatherInfo == null) return "";

            // 获取 Alerts 属性
            var alertsProp = lastWeatherInfo.GetType().GetProperty("Alerts", BindingFlags.Public | BindingFlags.Instance);
            if (alertsProp == null) return "";

            var alerts = alertsProp.GetValue(lastWeatherInfo);
            if (alerts == null) return "";

            // Alerts 是 List<WeatherAlert>
            var countProp = alerts.GetType().GetProperty("Count");
            if (countProp == null) return "";

            var count = (int)(countProp.GetValue(alerts) ?? 0);
            if (count == 0) return "";

            // 获取第一个预警的 Title
            var indexer = alerts.GetType().GetProperty("Item");
            if (indexer == null)
            {
                // 尝试通过 LINQ FirstOrDefault
                var firstMethod = alerts.GetType().GetMethods()
                    .FirstOrDefault(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 0);
                if (firstMethod != null)
                {
                    var firstAlert = firstMethod.Invoke(alerts, null);
                    if (firstAlert != null)
                    {
                        var titleProp = firstAlert.GetType().GetProperty("Title", BindingFlags.Public | BindingFlags.Instance);
                        return titleProp?.GetValue(firstAlert)?.ToString() ?? "";
                    }
                }
                return "";
            }

            // 使用索引器获取第一个元素
            var first = alerts.GetType().GetMethod("get_Item")?.Invoke(alerts, new object[] { 0 });
            if (first == null) return "";

            var title = first.GetType().GetProperty("Title", BindingFlags.Public | BindingFlags.Instance);
            return title?.GetValue(first)?.ToString() ?? "";
        }
        catch { return ""; }
    }
}
