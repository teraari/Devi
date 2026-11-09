using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Proofreader.App.ViewModels;

namespace Proofreader.App.Views;

public partial class ShortcutsWindow : Window
{
    public ShortcutsWindow()
    {
        InitializeComponent(); // Теперь это должно работать
        DataContext = new ShortcutsViewModel();
    }
}