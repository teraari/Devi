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

        // Подписываемся на событие выбора ошибки из ViewModel
        if (DataContext is MainViewModel vm)
        {
            vm.ErrorSelected += OnErrorSelected;
        }
    }

    private void OnErrorSelected(SpellingError error)
    {
        // Выделяем слово в текстовом поле
        EditorTextBox.SelectionStart = error.StartIndex;
        EditorTextBox.SelectionEnd = error.StartIndex + error.Length;
        
        // Переводим фокус на поле, чтобы пользователь сразу мог начать печатать
        EditorTextBox.Focus();
    }
}