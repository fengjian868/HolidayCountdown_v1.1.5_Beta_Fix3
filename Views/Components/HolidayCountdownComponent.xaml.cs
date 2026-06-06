using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.Components;

[ComponentInfo(
    "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
    "节假日倒计时",
    "\uE8F5",
    "显示距离最近节假日的倒计时，横向排列，带弧形进度环"
)]
public class HolidayCountdownComponent : ComponentBase
{
    private HolidayService _svc = null!;
    private DispatcherTimer _timer = null!;
    private StackPanel _main = null!;

    public HolidayCountdownComponent()
    {
        _main = new StackPanel { Orientation = Orientation.Vertical, Spacing = 3, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        Content = _main;
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) }; _timer.Tick += (s, e) => Update(); _timer.Start(); Update(); });
    }

    void Update()
    {
        _main.Children.Clear();
        if (_svc == null) return;

        var wr = _svc.GetNextWorkdayReminder();
        if (wr != null)
        {
            var rd = (int)(wr.Date.Date - DateTime.Now.Date).TotalDays;
            if (rd <= _svc.Settings.WorkdayReminderDays)
                _main.Children.Add(new TextBlock { Text = rd == 0 ? "⚠️ 明天调休上课" : $"⚠️ {rd}天后调休上课", Foreground = new SolidColorBrush(Colors.Orange), FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 11 });
        }

        var hs = _svc.GetNextHolidays(_svc.Settings.DisplayCount);
        if (hs.Count > 0)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
            for (int i = 0; i < hs.Count; i++)
            {
                var h = hs[i]; var days = (int)(h.Date.Date - DateTime.Now.Date).TotalDays;
                var color = _svc.Settings.AutoHolidayColor ? _svc.GetHolidayColor(h.Name) : Color.Parse("#2196F3");
                var item = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3, VerticalAlignment = VerticalAlignment.Center };

                if (_svc.Settings.ShowProgressRing && i == 0)
                {
                    var prev = _svc.GetPrevHoliday();
                    item.Children.Add(CreateArc(days, prev, h, color));
                }
                else item.Children.Add(new TextBlock { Text = h.IsCustom ? "🎂" : "📅", VerticalAlignment = VerticalAlignment.Center });

                item.Children.Add(new TextBlock { Text = h.Name, Foreground = new SolidColorBrush(color), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });
                item.Children.Add(new TextBlock { Text = days == 0 ? "就是今天！" : $"还有 {days} 天", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8 });
                if (_svc.Settings.ShowDaysOff && h.DaysOff > 1 && days >= 0) ((TextBlock)item.Children.Last()).Text += $"（放{h.DaysOff}天）";
                row.Children.Add(item);
            }
            _main.Children.Add(row);
        }
        else _main.Children.Add(new TextBlock { Text = "暂无节假日", HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.5 });

        if (_svc.Settings.ShowYearRatio)
        {
            var ratio = _svc.GetYearRatio();
            _main.Children.Add(new TextBlock { Text = $"当年假期剩余 {ratio:P0}", HorizontalAlignment = HorizontalAlignment.Center, FontSize = 10, Opacity = 0.6 });
        }
    }

    Control CreateArc(int days, Holiday? prev, Holiday next, Color color)
    {
        var grid = new Grid { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        // 使用 Viewbox 让弧形进度环随容器自动缩放
        var vb = new Viewbox { Stretch = Stretch.Uniform, Width = 36, Height = 36 };
        var inner = new Grid { Width = 36, Height = 36 };
        inner.Children.Add(new Arc { Width = 36, Height = 36, StartAngle = -90, SweepAngle = 360, Stroke = new SolidColorBrush(Color.Parse("#20FFFFFF")), StrokeThickness = 3, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        double p = 0;
        if (prev != null) { var t = (next.Date - prev.Date).TotalDays; var pass = (DateTime.Now - prev.Date).TotalDays; p = Math.Max(0, Math.Min(1, pass / t)); }
        else p = Math.Max(0, Math.Min(1, 1 - days / 30.0));
        inner.Children.Add(new Arc { Width = 36, Height = 36, StartAngle = -90, SweepAngle = p * 360, Stroke = new SolidColorBrush(color), StrokeThickness = 3, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        inner.Children.Add(new TextBlock { Text = days > 0 ? days.ToString() : "!", FontSize = 10, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        vb.Child = inner;
        grid.Children.Add(vb);
        return grid;
    }
}
