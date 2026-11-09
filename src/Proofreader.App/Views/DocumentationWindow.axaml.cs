using Avalonia.Controls;
using Proofreader.App.ViewModels;

namespace Proofreader.App.Views;

public partial class DocumentationWindow : Window
{
    public DocumentationWindow()
    {
        InitializeComponent();
        DataContext = new DocumentationViewModel();
    }
}