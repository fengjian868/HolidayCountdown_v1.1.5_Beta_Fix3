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
        Dispatcher.UIThread.Post(() =>
        {
            _svc = new HolidayService();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _timer.Tick += (s, e) => Update();
            _timer.Start();
            // 订阅设置变更事件，保存后立即刷新
            HolidayService.SettingsChanged += OnSettingsChanged;
            Update();
        });
    }

    void OnSettingsChanged()
    {
        // 重新加载设置并刷新显示
        _svc?.LoadSettings();
        Dispatcher.UIThread.Post(Update);
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
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            for (int i = 0; i < hs.Count; i++)
            {
                var h = hs[i]; var days = (int)(h.Date.Date - DateTime.Now.Date).TotalDays;
                var color = _svc.Settings.AutoHolidayColor ? _svc.GetHolidayColor(h.Name) : Color.Parse("#2196F3");

                // 每个节日项：横向排列，进度环/图标和文字都在同一行
                var item = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

                if (_svc.Settings.ShowProgressRing && i == 0)
                {
                    var prev = _svc.GetPrevHoliday();
                    item.Children.Add(CreateArc(days, prev, h, color));
                }
                else item.Children.Add(new TextBlock { Text = h.IsCustom ? "🎂" : "📅", VerticalAlignment = VerticalAlignment.Center, FontSize = 13 });

                // 节日名称和天数在同一行
                var nameDaysRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
                nameDaysRow.Children.Add(new TextBlock { Text = h.Name, Foreground = new SolidColorBrush(color), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
                var daysText = days == 0 ? "就是今天！" : $"还有 {days} 天";
                if (_svc.Settings.ShowDaysOff && h.DaysOff > 1 && days >= 0) daysText += $"（放{h.DaysOff}天）";
                nameDaysRow.Children.Add(new TextBlock { Text = daysText, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8, FontSize = 11 });
                item.Children.Add(nameDaysRow);

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
        // 固定大小的进度环容器，避免穿模
        var container = new Border
        {
            Width = 40,
            Height = 40,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Brushes.Transparent
        };

        var inner = new Grid { Width = 36, Height = 36, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        inner.Children.Add(new Arc { Width = 36, Height = 36, StartAngle = -90, SweepAngle = 360, Stroke = new SolidColorBrush(Color.Parse("#20FFFFFF")), StrokeThickness = 3, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        double p = 0;
        if (prev != null) { var t = (next.Date - prev.Date).TotalDays; var pass = (DateTime.Now - prev.Date).TotalDays; p = Math.Max(0, Math.Min(1, pass / t)); }
        else p = Math.Max(0, Math.Min(1, 1 - days / 30.0));
        inner.Children.Add(new Arc { Width = 36, Height = 36, StartAngle = -90, SweepAngle = p * 360, Stroke = new SolidColorBrush(color), StrokeThickness = 3, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        // 圈内显示节假日的日期（几号）
        inner.Children.Add(new TextBlock { Text = next.Date.Day.ToString(), FontSize = 10, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        container.Child = inner;
        return container;
    }
}
