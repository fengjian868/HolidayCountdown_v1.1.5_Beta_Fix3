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
    "F6A7B8C9-D0E1-2345-F012-123456789015",
    "寒暑假倒计时",
    "\uE8F5",
    "显示距离寒暑假的剩余周数和天数"
)]
public class VacationCountdownComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private StackPanel _main = null!;
    private HolidayService? _svc;

    public VacationCountdownComponent()
    {
        _main = new StackPanel { Orientation = Orientation.Vertical, Spacing = 3, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        Content = _main;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); Update(); });
    }

    void Update()
    {
        _main.Children.Clear();
        if (_svc == null || !_svc.Settings.ShowVacationCountdown) return;
        var now = DateTime.Now; var s = _svc.Settings;
        var targets = new[] { ("暑假", s.SummerStart, s.SummerEnd), ("寒假", s.WinterStart, s.WinterEnd) };
        foreach (var (name, start, end) in targets)
        {
            if (now.Date < start.Date)
            {
                var span = start.Date - now.Date;
                var weeks = span.Days / 7; var days = span.Days % 7;
                _main.Children.Add(new TextBlock { Text = $"距离{name}还有 {weeks} 周 {days} 天", HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Color.Parse("#FF9800")) });
            }
            else if (now.Date >= start.Date && now.Date <= end.Date)
            {
                var span = end.Date - now.Date; var weeks = span.Days / 7; var days = span.Days % 7;
                _main.Children.Add(new TextBlock { Text = $"{name}进行中，剩余 {weeks} 周 {days} 天", HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Color.Parse("#4CAF50")) });
            }
        }
        if (_main.Children.Count == 0) _main.Children.Add(new TextBlock { Text = "暂无寒暑假安排", HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.5 });
    }
}
