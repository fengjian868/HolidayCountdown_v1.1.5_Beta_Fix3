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
    private object? _weatherService;
    private PropertyInfo? _currentWeatherProp;
    private PropertyInfo? _weatherStatusProp;
    private PropertyInfo? _warningProp;

    public WeatherGreetingComponent()
    {
        var panel = new Grid { ColumnDefinitions = new ColumnDefinitions("*"), VerticalAlignment = VerticalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 };
        Grid.SetColumn(_txt, 0); panel.Children.Add(_txt); Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); InitWeatherService(); Update(); });
    }

    void InitWeatherService()
    {
        try
        {
            // 通过反射获取 ClassIsland 主程序的天气服务
            var appBaseType = Type.GetType("ClassIsland.Core.AppBase, ClassIsland.Core");
            if (appBaseType == null) return;
            var currentProp = appBaseType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
            if (currentProp == null) return;
            var app = currentProp.GetValue(null);
            if (app == null) return;

            // 尝试获取 IServiceProvider
            var spProp = appBaseType.GetProperty("Services", BindingFlags.Public | BindingFlags.Instance);
            if (spProp == null) spProp = appBaseType.GetProperty("ServiceProvider", BindingFlags.Public | BindingFlags.Instance);
            if (spProp == null)
            {
                // 尝试从 Host 获取
                var hostProp = appBaseType.GetProperty("Host", BindingFlags.Public | BindingFlags.Static);
                if (hostProp != null)
                {
                    var host = hostProp.GetValue(null);
                    if (host != null)
                    {
                        var servicesProp = host.GetType().GetProperty("Services", BindingFlags.Public | BindingFlags.Instance);
                        if (servicesProp != null)
                        {
                            var sp = servicesProp.GetValue(host);
                            if (sp != null) ResolveWeatherService(sp);
                        }
                    }
                }
                return;
            }
            var serviceProvider = spProp.GetValue(app);
            if (serviceProvider != null) ResolveWeatherService(serviceProvider);
        }
        catch { }
    }

    void ResolveWeatherService(object serviceProvider)
    {
        try
        {
            var getService = serviceProvider.GetType().GetMethod("GetService", BindingFlags.Public | BindingFlags.Instance);
            if (getService == null) return;

            // 尝试获取 IWeatherService
            var weatherServiceType = Type.GetType("ClassIsland.Core.Abstractions.Services.IWeatherService, ClassIsland.Core");
            if (weatherServiceType == null)
            {
                // 尝试从所有程序集中查找
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    weatherServiceType = asm.GetTypes().FirstOrDefault(t => t.Name == "IWeatherService");
                    if (weatherServiceType != null) break;
                }
            }
            if (weatherServiceType == null) return;

            _weatherService = getService.Invoke(serviceProvider, new[] { weatherServiceType });
            if (_weatherService == null) return;

            var wsType = _weatherService.GetType();
            _currentWeatherProp = wsType.GetProperty("CurrentWeather", BindingFlags.Public | BindingFlags.Instance);
            _weatherStatusProp = wsType.GetProperty("WeatherStatus", BindingFlags.Public | BindingFlags.Instance);
            _warningProp = wsType.GetProperty("WeatherWarning", BindingFlags.Public | BindingFlags.Instance);

            // 如果找不到 CurrentWeather，尝试其他属性名
            if (_currentWeatherProp == null)
                _currentWeatherProp = wsType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => p.Name.Contains("Weather") && p.Name.Contains("Current"));
            if (_weatherStatusProp == null)
                _weatherStatusProp = wsType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => p.Name.Contains("Status") || p.Name.Contains("Condition"));
        }
        catch { }
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.WeatherGreetingEnabled) { _txt.Text = ""; return; }

        // 如果还没获取到天气服务，尝试初始化
        if (_weatherService == null) { InitWeatherService(); }

        var weather = GetWeatherText();
        var warning = GetWeatherWarning();

        var greet = weather switch
        {
            var w when string.IsNullOrEmpty(w) => "",
            var w when w.Contains("雨") => "下雨记得带伞 ☔",
            var w when w.Contains("雪") => "下雪了，注意保暖 ❄️",
            var w when w.Contains("晴") => "天气不错，保持好心情 ☀️",
            var w when w.Contains("阴") => "阴天适合专注学习 📖",
            var w when w.Contains("雾") => "雾大注意安全 🌫️",
            var w when w.Contains("霾") => "霾天减少户外活动 😷",
            var w when w.Contains("风") => "大风天注意安全 🍃",
            var w when w.Contains("雷") => "雷电天气注意安全 ⚡",
            var w when w.Contains("云") => "多云天气，舒适宜人 ⛅",
            _ => $"今日天气：{weather}"
        };

        if (!string.IsNullOrEmpty(warning))
            greet = $"⚠️ {warning} " + greet;

        _txt.Text = greet;
    }

    string GetWeatherText()
    {
        try
        {
            if (_weatherService == null || _currentWeatherProp == null) return "";
            var currentWeather = _currentWeatherProp.GetValue(_weatherService);
            if (currentWeather == null) return "";

            // 尝试获取天气文本属性
            var cwType = currentWeather.GetType();
            var textProp = cwType.GetProperty("Weather", BindingFlags.Public | BindingFlags.Instance)
                ?? cwType.GetProperty("WeatherText", BindingFlags.Public | BindingFlags.Instance)
                ?? cwType.GetProperty("Condition", BindingFlags.Public | BindingFlags.Instance)
                ?? cwType.GetProperty("Status", BindingFlags.Public | BindingFlags.Instance)
                ?? cwType.GetProperty("Description", BindingFlags.Public | BindingFlags.Instance);

            if (textProp != null)
                return textProp.GetValue(currentWeather)?.ToString() ?? "";

            // 尝试 ToString
            return currentWeather.ToString() ?? "";
        }
        catch { return ""; }
    }

    string GetWeatherWarning()
    {
        try
        {
            if (_weatherService == null) return "";
            if (_warningProp != null)
            {
                var warning = _warningProp.GetValue(_weatherService);
                if (warning != null) return warning.ToString() ?? "";
            }
            return "";
        }
        catch { return ""; }
    }
}
