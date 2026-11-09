using Avalonia.Controls;
using Avalonia.Interactivity;
using Proofreader.App.ViewModels;

namespace Proofreader.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = new AboutViewModel();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}