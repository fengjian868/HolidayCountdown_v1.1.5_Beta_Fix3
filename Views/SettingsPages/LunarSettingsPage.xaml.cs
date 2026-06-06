using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.lunar", "农历设置", "\uE787", "\uE787")]
public class LunarSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public LunarSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(H("🌙 农历日期设置"));
        s.Children.Add(C("显示", new StackPanel { Spacing = 10 }.Also(p =>
        {
            p.Children.Add(R("显示农历", "", T(_svc.Settings.ShowLunarDate, v => _svc.Settings.ShowLunarDate = v)));
            p.Children.Add(R("自动网络刷新", "有网络时自动获取最新农历", T(_svc.Settings.LunarAutoRefresh, v => _svc.Settings.LunarAutoRefresh = v)));
            p.Children.Add(R("显示模板", "可用变量: {gzYear} {IMonthCn} {IDayCn} {Animal} {Term} {lunarDate}", Tx(_svc.Settings.LunarDateTemplate, 300, v => _svc.Settings.LunarDateTemplate = v)));
        })));
        s.Children.Add(new TextBlock { Text = "示例: 癸卯年 九月初八 兔", Opacity = 0.5, FontSize = 11 });
        s.Children.Add(Sv());
        return new ScrollViewer { Content = s };
    }

    static TextBlock H(string t) => new() { Text = t, FontSize = 22, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) };
    static Border C(string title, Control c) => new() { Child = new StackPanel { Spacing = 10 }.Also(s => { s.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#2196F3")) }); s.Children.Add(c); }), Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")), CornerRadius = new CornerRadius(12), Padding = new Thickness(16), BorderBrush = new SolidColorBrush(Color.Parse("#1AFFFFFF")), BorderThickness = new Thickness(1), Margin = new Thickness(0, 4) };
    static Control R(string l, string d, Control c) { var g = new Grid { ColumnDefinitions = new ColumnDefinitions("120 *") }; var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; left.Children.Add(new TextBlock { Text = l, FontWeight = FontWeight.SemiBold }); if (!string.IsNullOrEmpty(d)) left.Children.Add(new TextBlock { Text = d, Opacity = 0.5, FontSize = 11 }); Grid.SetColumn(left, 0); Grid.SetColumn(c, 1); c.VerticalAlignment = VerticalAlignment.Center; g.Children.Add(left); g.Children.Add(c); return g; }
    static ToggleSwitch T(bool v, Action<bool> cb) { var t = new ToggleSwitch { IsChecked = v, OnContent = "开", OffContent = "关" }; t.IsCheckedChanged += (a, b) => cb(t.IsChecked == true); return t; }
    static TextBox Tx(string v, int w, Action<string> cb) { var t = new TextBox { Text = v, Width = w }; t.LostFocus += (a, b) => cb(t.Text ?? ""); return t; }
    Button Sv() { var b = new Button { Content = "💾 保存", Padding = new Thickness(20, 8) }; b.Click += (a, e) => { _svc.SaveSettings(); b.Content = "✅ 已保存"; }; return b; }
}
