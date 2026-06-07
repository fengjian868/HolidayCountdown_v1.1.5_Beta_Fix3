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
    "根据ClassIsland天气温度显示穿衣提醒，支持预警提示"
)]
public class WeatherGreetingComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;
    private string _lastWeatherKey = "";

    public WeatherGreetingComponent()
    {
        var panel = new Grid { ColumnDefinitions = new ColumnDefinitions("*"), VerticalAlignment = VerticalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 };
        Grid.SetColumn(_txt, 0); panel.Children.Add(_txt); Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); HolidayService.SettingsChanged += OnSettingsChanged; Update(); });
    }

    void OnSettingsChanged()
    {
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.WeatherGreetingEnabled) { _txt.Text = ""; return; }

        var (temp, weatherCode, warnings) = GetWeatherData();

        // 用温度+天气代码+预警拼接成key，判断天气是否有变化
        var currentKey = $"{temp}|{weatherCode}|{string.Join(",", warnings)}";
        // 即使key相同也更新（因为定时器就是用来刷新的），但保留key用于调试
        _lastWeatherKey = currentKey;

        // 优先根据温度给出穿衣提醒
        var greet = GetTempGreeting(temp);

        // 如果温度获取失败，回退到天气关键词匹配
        if (string.IsNullOrEmpty(greet) && !string.IsNullOrEmpty(weatherCode))
        {
            var weatherText = GetWeatherTextByCode(weatherCode);
            greet = GetWeatherGreeting(weatherText);
        }

        // 预警提醒（优先级最高）
        var warningText = GetWarningText(warnings);
        if (!string.IsNullOrEmpty(warningText))
            greet = warningText + " " + greet;

        _txt.Text = greet;
    }

    /// <summary>
    /// 根据温度给出穿衣提醒
    /// </summary>
    string GetTempGreeting(double? temp)
    {
        if (temp == null) return "";
        var t = temp.Value;
        return t switch
        {
            >= 35 => "高温预警，注意防暑 \uD83C\uDF21️",
            >= 30 => "很热，穿短袖注意防晒 ☀️",
            >= 25 => "较热，短袖即可 \uD83D\uDC55",
            >= 20 => "舒适，薄长袖或短袖 \uD83C\uDF43",
            >= 15 => "微凉，建议穿外套 \uD83E\uDDE5",
            >= 10 => "较冷，穿厚外套 \uD83E\uDDE3",
            >= 5 => "冷，穿羽绒服或棉衣 ❄️",
            >= 0 => "很冷，注意保暖 \uD83E\uDD76",
            _ => "严寒，多穿点别冻着 \uD83E\uDDCA"
        };
    }

    /// <summary>
    /// 根据天气文本匹配问候语（备用）
    /// </summary>
    string GetWeatherGreeting(string weatherText)
    {
        if (string.IsNullOrEmpty(weatherText)) return "";
        var match = _svc!.Settings.WeatherGreetings
            .Where(kv => kv.Key != "默认" && weatherText.Contains(kv.Key))
            .OrderByDescending(kv => kv.Key.Length)
            .FirstOrDefault();
        var greet = match.Value ?? "";
        if (string.IsNullOrEmpty(greet) && _svc.Settings.WeatherGreetings.TryGetValue("默认", out var def))
            greet = def.Replace("{weather}", weatherText);
        return greet;
    }

    /// <summary>
    /// 获取预警提醒文本
    /// </summary>
    string GetWarningText(string[] warnings)
    {
        if (warnings.Length == 0) return "";
        // 根据预警级别返回不同前缀
        var w = warnings[0]; // 取第一个预警
        if (w.Contains("红")) return $"\u26A0️红色预警{w.Replace("红色预警", "").Replace("红色", "")}";
        if (w.Contains("橙")) return $"\u26A0️橙色预警{w.Replace("橙色预警", "").Replace("橙色", "")}";
        if (w.Contains("黄")) return $"\u26A0️黄色预警{w.Replace("黄色预警", "").Replace("黄色", "")}";
        if (w.Contains("蓝")) return $"\u26A0️蓝色预警{w.Replace("蓝色预警", "").Replace("蓝色", "")}";
        return $"\u26A0️{w}";
    }

    /// <summary>
    /// 获取天气数据：温度、天气代码、预警列表
    /// </summary>
    (double? temp, string? weatherCode, string[] warnings) GetWeatherData()
    {
        try
        {
            var settings = GetSettingsServiceSettings();
            if (settings == null) return (null, null, Array.Empty<string>());

            var lastWeatherInfo = GetPropertyValue(settings, "LastWeatherInfo");
            if (lastWeatherInfo == null) return (null, null, Array.Empty<string>());

            // 获取 Current 中的温度
            var current = GetPropertyValue(lastWeatherInfo, "Current");
            double? temp = null;
            string? weatherCode = null;
            if (current != null)
            {
                var temperature = GetPropertyValue(current, "Temperature");
                if (temperature != null)
                {
                    var tempValue = GetPropertyValue(temperature, "Value")?.ToString();
                    if (double.TryParse(tempValue, out var t)) temp = t;
                }
                weatherCode = GetPropertyValue(current, "Weather")?.ToString();
            }

            // 获取所有预警
            var warnings = GetAllAlertTitles(lastWeatherInfo);

            return (temp, weatherCode, warnings);
        }
        catch { return (null, null, Array.Empty<string>()); }
    }

    object? GetSettingsServiceSettings()
    {
        try
        {
            var appHostType = Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Shared")
                ?? Type.GetType("ClassIsland.Shared.IAppHost, ClassIsland.Core")
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "IAppHost");

            if (appHostType == null) return null;

            var tryGetService = appHostType.GetMethod("TryGetService", BindingFlags.Public | BindingFlags.Static);
            if (tryGetService == null || !tryGetService.IsGenericMethodDefinition) return null;

            var settingsServiceType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "SettingsService");

            if (settingsServiceType == null) return null;

            var genericMethod = tryGetService.MakeGenericMethod(settingsServiceType);
            var settingsService = genericMethod.Invoke(null, null);
            if (settingsService == null) return null;

            var settingsProp = settingsServiceType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Instance);
            return settingsProp?.GetValue(settingsService);
        }
        catch { return null; }
    }

    object? GetPropertyValue(object obj, string propName)
    {
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(obj);
        }
        catch { return null; }
    }

    /// <summary>
    /// 获取所有预警标题
    /// </summary>
    string[] GetAllAlertTitles(object lastWeatherInfo)
    {
        try
        {
            var alerts = GetPropertyValue(lastWeatherInfo, "Alerts");
            if (alerts == null) return Array.Empty<string>();

            var countProp = alerts.GetType().GetProperty("Count");
            var count = (int?)countProp?.GetValue(alerts) ?? 0;
            if (count == 0) return Array.Empty<string>();

            var result = new System.Collections.Generic.List<string>();
            // 尝试用索引器获取
            var indexer = alerts.GetType().GetProperties()
                .FirstOrDefault(p => p.GetIndexParameters().Length == 1);
            if (indexer != null)
            {
                for (int i = 0; i < count; i++)
                {
                    var alert = indexer.GetValue(alerts, new object[] { i });
                    if (alert != null)
                    {
                        var titleProp = alert.GetType().GetProperty("Title", BindingFlags.Public | BindingFlags.Instance);
                        var title = titleProp?.GetValue(alert)?.ToString();
                        if (!string.IsNullOrEmpty(title)) result.Add(title);
                    }
                }
            }
            return result.ToArray();
        }
        catch { return Array.Empty<string>(); }
    }

    string GetWeatherTextByCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return "";
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
}
