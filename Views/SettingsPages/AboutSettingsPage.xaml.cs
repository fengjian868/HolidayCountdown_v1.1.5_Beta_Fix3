using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.about", "关于", "\uE946", "\uE946")]
public class AboutSettingsPage : SettingsPageBase
{
    public AboutSettingsPage() { Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 14, Margin = new Thickness(24, 16) };
        s.Children.Add(new TextBlock { Text = "节假日倒计时", FontSize = 28, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#2196F3")) });
        s.Children.Add(new TextBlock { Text = "版本: v1.1.5 (Beta测试版)", FontSize = 14, Opacity = 0.7 });
        s.Children.Add(new TextBlock { Text = "作者: fengjian868", FontSize = 14, Opacity = 0.7 });
        s.Children.Add(new TextBlock { Text = "GitHub: https://github.com/fengjian868/HolidayCountdown", FontSize = 12, Opacity = 0.5 });
        s.Children.Add(new TextBlock { Text = "功能模块:", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
        s.Children.Add(new TextBlock { Text = "- 节假日倒计时（调休提醒、进度环、假期占比）", FontSize = 12, Opacity = 0.8 });
        s.Children.Add(new TextBlock { Text = "- 24节气倒计时（网络自动刷新）", FontSize = 12, Opacity = 0.8 });
        s.Children.Add(new TextBlock { Text = "- 农历日期显示（自定义模板）", FontSize = 12, Opacity = 0.8 });
        s.Children.Add(new TextBlock { Text = "- 自定义节日倒计时", FontSize = 12, Opacity = 0.8 });
        s.Children.Add(new TextBlock { Text = "- 寒暑假倒计时（周+天）", FontSize = 12, Opacity = 0.8 });
        s.Children.Add(new TextBlock { Text = "- 时段问候语（早中晚+放学+晚修）", FontSize = 12, Opacity = 0.8 });
        s.Children.Add(new TextBlock { Text = "- 天气问候（根据天气自动匹配）", FontSize = 12, Opacity = 0.8 });
        s.Children.Add(new TextBlock { Text = "- 合并/分开显示模式", FontSize = 12, Opacity = 0.8 });
        s.Children.Add(new TextBlock { Text = "Made with love for ClassIsland", FontSize = 12, Opacity = 0.5, Margin = new Thickness(0, 8, 0, 0) });
        return new ScrollViewer { Content = s };
    }
}
