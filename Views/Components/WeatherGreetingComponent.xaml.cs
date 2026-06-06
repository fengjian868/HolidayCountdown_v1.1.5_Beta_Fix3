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
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); Update(); });
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.WeatherGreetingEnabled) { _txt.Text = ""; return; }
        _txt.Text = _svc.Settings.WeatherGreetingEnabled ? "☀️ 天气问候已启用" : "";
    }
}
