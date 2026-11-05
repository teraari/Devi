using System;
using Avalonia.Controls;
using Proofreader.App.ViewModels;
using Proofreader.Core.Models;

namespace Proofreader.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainViewModel vm)
        {
            vm.ErrorSelected += OnErrorSelected;
        }
    }

    private void OnErrorSelected(SpellingError error)
    {
        // В AvaloniaEdit выделение задается через длину, а не конечный индекс
        EditorTextBox.SelectionStart = error.StartIndex;
        EditorTextBox.SelectionLength = error.Length;
        
        // Устанавливаем курсор в начало ошибки
        EditorTextBox.CaretOffset = error.StartIndex;
        
        EditorTextBox.Focus();
    }
}