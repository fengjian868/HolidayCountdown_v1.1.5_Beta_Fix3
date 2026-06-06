using System.Linq;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.solarterm", "24节气设置", "\uE9CA", "\uE9CA")]
public class SolarTermSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public SolarTermSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(H("🌿 24节气设置"));
        s.Children.Add(C("颜色", new StackPanel { Spacing = 8 }.Also(p =>
        {
            foreach (var kv in _svc.Settings.TermColors.OrderBy(x => x.Key).ToList())
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                row.Children.Add(new TextBlock { Text = kv.Key, Width = 60, VerticalAlignment = VerticalAlignment.Center });
                var picker = new ColorPicker 
                { 
                    Width = 40, 
                    Height = 28,
                    Color = TryParseColor(kv.Value)
                };
                var key = kv.Key;
                picker.ColorChanged += (a, b) => { _svc.Settings.TermColors[key] = picker.Color.ToString(); };
                row.Children.Add(picker);
                p.Children.Add(row);
            }
        })));
        s.Children.Add(Sv());
        return new ScrollViewer { Content = s };
    }

    static TextBlock H(string t) => new() { Text = t, FontSize = 22, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) };
    static Border C(string title, Control c) => new() { Child = new StackPanel { Spacing = 10 }.Also(s => { s.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#2196F3")) }); s.Children.Add(c); }), Background = new SolidColorBrush(Color.Parse("#0DFFFFFF")), CornerRadius = new CornerRadius(12), Padding = new Thickness(16), BorderBrush = new SolidColorBrush(Color.Parse("#1AFFFFFF")), BorderThickness = new Thickness(1), Margin = new Thickness(0, 4) };
    static Avalonia.Media.Color TryParseColor(string hex)
    {
        try { return Avalonia.Media.Color.Parse(hex); }
        catch { return Avalonia.Media.Color.Parse("#2196F3"); }
    }
    Button Sv() { var b = new Button { Content = "💾 保存", Padding = new Thickness(20, 8) }; b.Click += (a, e) => { _svc.SaveSettings(); b.Content = "✅ 已保存"; }; return b; }
}
