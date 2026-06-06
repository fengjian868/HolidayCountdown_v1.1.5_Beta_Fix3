using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.customholiday", "自定义节日", "Gift", "Gift")]
public class CustomHolidaySettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public CustomHolidaySettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(H("🎂 自定义节日设置"));
        s.Children.Add(C("组件显示", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(R("显示数量", "", Combo(new[]{"1","2","3","5"}, _svc.Settings.CustomHolidayDisplayCount == 1 ? 0 : _svc.Settings.CustomHolidayDisplayCount == 2 ? 1 : _svc.Settings.CustomHolidayDisplayCount == 3 ? 2 : 3, v => _svc.Settings.CustomHolidayDisplayCount = v == 0 ? 1 : v == 1 ? 2 : v == 2 ? 3 : 5)));
            p.Children.Add(R("显示图标", "", T(_svc.Settings.CustomHolidayShowIcon, v => _svc.Settings.CustomHolidayShowIcon = v)));
            p.Children.Add(R("显示天数", "", T(_svc.Settings.CustomHolidayShowDays, v => _svc.Settings.CustomHolidayShowDays = v)));
        })));
        s.Children.Add(C("节日列表", BuildList()));
        s.Children.Add(Sv());
        return new ScrollViewer { Content = s };
    }

    Control BuildList()
    {
        var p = new StackPanel { Spacing = 8 };
        foreach (var h in _svc.Settings.CustomHolidays.ToList())
            p.Children.Add(MakeItem(h, p));
        var btn = new Button { Content = "➕ 添加", Padding = new Thickness(12, 6) };
        btn.Click += (a, e) => { var h = new CustomHoliday { Name = "新节日", Date = DateTime.Now.AddDays(1) }; _svc.Settings.CustomHolidays.Add(h); p.Children.Insert(p.Children.Count - 1, MakeItem(h, p)); };
        p.Children.Add(btn);
        return p;
    }

    Control MakeItem(CustomHoliday h, StackPanel parent)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("120 130 Auto Auto") };
        var n = new TextBox { Text = h.Name, Margin = new Thickness(0, 0, 8, 0) }; n.LostFocus += (a, b) => h.Name = n.Text ?? ""; Grid.SetColumn(n, 0);
        var d = new DatePicker { SelectedDate = h.Date, Margin = new Thickness(0, 0, 8, 0) }; d.SelectedDateChanged += (a, b) => { if (d.SelectedDate.HasValue) h.Date = d.SelectedDate.Value.DateTime; }; Grid.SetColumn(d, 1);
        var r = new CheckBox { Content = "每年", IsChecked = h.RepeatYearly, VerticalAlignment = VerticalAlignment.Center }; r.IsCheckedChanged += (a, b) => h.RepeatYearly = r.IsChecked == true; Grid.SetColumn(r, 2);
        var del = new Button { Content = "删除", Width = 50 }; del.Click += (a, b) => { _svc.Settings.CustomHolidays.Remove(h); parent.Children.Remove(g); }; Grid.SetColumn(del, 3);
        g.Children.Add(n); g.Children.Add(d); g.Children.Add(r); g.Children.Add(del);
        return g;
    }

    static TextBlock H(string t) => new() { Text = t, FontSize = 22, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) };
    static Border C(string title, Control c) => new() { Child = new StackPanel { Spacing = 10 }.Also(s => { s.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#2196F3")) }); s.Children.Add(c); }), Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")), CornerRadius = new CornerRadius(12), Padding = new Thickness(16), BorderBrush = new SolidColorBrush(Color.Parse("#1AFFFFFF")), BorderThickness = new Thickness(1), Margin = new Thickness(0, 4) };
    static Control R(string l, string d, Control c) { var g = new Grid { ColumnDefinitions = new ColumnDefinitions("120 *") }; var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; left.Children.Add(new TextBlock { Text = l, FontWeight = FontWeight.SemiBold }); if (!string.IsNullOrEmpty(d)) left.Children.Add(new TextBlock { Text = d, Opacity = 0.5, FontSize = 11 }); Grid.SetColumn(left, 0); Grid.SetColumn(c, 1); c.VerticalAlignment = VerticalAlignment.Center; g.Children.Add(left); g.Children.Add(c); return g; }
    static ToggleSwitch T(bool v, Action<bool> cb) { var t = new ToggleSwitch { IsChecked = v, OnContent = "开", OffContent = "关" }; t.IsCheckedChanged += (a, b) => cb(t.IsChecked == true); return t; }
    static ComboBox Combo(string[] items, int sel, Action<int> cb) { var c = new ComboBox { Width = 80, SelectedIndex = sel }; foreach (var i in items) c.Items.Add(i); c.SelectionChanged += (a, b) => cb(c.SelectedIndex); return c; }
    Button Sv() { var b = new Button { Content = "💾 保存", Padding = new Thickness(20, 8) }; b.Click += (a, e) => { _svc.SaveSettings(); b.Content = "✅ 已保存"; }; return b; }
}
