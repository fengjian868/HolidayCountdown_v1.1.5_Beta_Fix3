using System;
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
    "\uE9D2",
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
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); Update(); });
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.WeatherGreetingEnabled) { _txt.Text = ""; return; }
        // 尝试通过AppHost获取天气（如果ClassIsland暴露）
        // 由于API不确定，先显示占位并尝试读取全局存储
        try
        {
            var weather = ""; // TODO: 通过IWeatherService获取
            var warning = "";
            var greet = weather switch
            {
                var w when w.Contains("雨") => "下雨记得带伞 ☔",
                var w when w.Contains("雪") => "下雪了，注意保暖 ❄️",
                var w when w.Contains("晴") => "天气不错，保持好心情 ☀️",
                var w when w.Contains("阴") => "阴天适合专注学习 📖",
                var w when w.Contains("雾") => "雾大注意安全 🌫️",
                var w when w.Contains("霾") => "霾天减少户外活动 😷",
                _ => ""
            };
            if (!string.IsNullOrEmpty(warning)) greet = $"⚠️ {warning} " + greet;
            _txt.Text = greet;
        }
        catch { _txt.Text = ""; }
    }
}
