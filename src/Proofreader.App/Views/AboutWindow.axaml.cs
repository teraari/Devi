using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Proofreader.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnGitHubClick(object? sender, RoutedEventArgs e)
    {
        // Замените ссылку, если ваш никнейм на GitHub другой
        OpenUrl("https://github.com/Belannandere/Devi");
    }

    private void OnLicenseClick(object? sender, RoutedEventArgs e)
    {
        // Ссылка на файл лицензии в вашем репозитории
        OpenUrl("https://github.com/Belannandere/Devi/blob/main/LICENSE");
    }

    // Вспомогательный метод для открытия ссылок в браузере по умолчанию
    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось открыть ссылку: {ex.Message}");
        }
    }
}