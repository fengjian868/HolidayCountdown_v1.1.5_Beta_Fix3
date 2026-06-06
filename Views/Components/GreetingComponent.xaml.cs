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
    "B2C3D4E5-F6A7-8901-BCDE-F12345678901",
    "时段问候语",
    "\uE9D2",
    "根据时间显示早中晚问候、放学提醒和周日晚修提示"
)]
public class GreetingComponent : ComponentBase
{
    private DispatcherTimer _timer = null!;
    private TextBlock _txt = null!;
    private HolidayService? _svc;

    public GreetingComponent()
    {
        var panel = new Grid { ColumnDefinitions = new ColumnDefinitions("*"), VerticalAlignment = VerticalAlignment.Center };
        _txt = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 };
        Grid.SetColumn(_txt, 0); panel.Children.Add(_txt); Content = panel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) }; _timer.Tick += (s, e) => Update(); _timer.Start();
        Dispatcher.UIThread.Post(() => { _svc = new HolidayService(); Update(); });
    }

    void Update()
    {
        if (_svc == null || !_svc.Settings.ShowGreeting) { _txt.Text = ""; return; }
        var now = DateTime.Now; var s = _svc.Settings;
        if (s.ShowSundayEveningStudy && now.DayOfWeek == DayOfWeek.Sunday && now.Hour >= 17 && now.Hour <= 21) { _txt.Text = s.SundayEveningStudyText; return; }
        var se = new TimeSpan(s.SchoolEndHour, s.SchoolEndMinute, 0); var ct = now.TimeOfDay; var rb = se - TimeSpan.FromMinutes(s.SchoolEndReminderMinutes);
        if (ct >= se) { _txt.Text = s.AfterSchoolEndText; return; }
        if (ct >= rb) { _txt.Text = $"{s.BeforeSchoolEndText}（还有{(int)(se - ct).TotalMinutes}分钟）"; return; }
        var key = now.Hour switch { >= 5 and < 8 => 5, >= 8 and < 12 => 8, >= 12 and < 14 => 12, >= 14 and < 17 => 14, >= 17 and < 19 => 17, _ => 19 };
        if (s.HourlyGreetings.TryGetValue(key, out var g)) { _txt.Text = g; return; }
        if (now.DayOfWeek == DayOfWeek.Monday && now.Hour < 12) { _txt.Text = s.SpecialGreetings.TryGetValue("MondayMorning", out var mm) ? mm : ""; return; }
        if (now.DayOfWeek == DayOfWeek.Wednesday) { _txt.Text = s.SpecialGreetings.TryGetValue("Wednesday", out var wd) ? wd : ""; return; }
        if (now.DayOfWeek == DayOfWeek.Friday && now.Hour >= 12) { _txt.Text = s.SpecialGreetings.TryGetValue("FridayAfternoon", out var fa) ? fa : ""; return; }
        if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) { _txt.Text = s.SpecialGreetings.TryGetValue("Weekend", out var we) ? we : ""; return; }
        _txt.Text = "";
    }
}
