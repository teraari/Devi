using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Proofreader.App.Controls;
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
            vm.RequestShowAbout += OnRequestShowAbout;
            vm.RequestShowDocumentation += OnRequestShowDocumentation;
            vm.RequestShowShortcuts += OnRequestShowShortcuts;
        }
    }

    private void OnErrorSelected(SpellingError error)
    {
        var editor = this.GetVisualDescendants().OfType<BindableTextEditor>().FirstOrDefault();
        
        if (editor != null)
        {
            editor.SelectionStart = error.StartIndex;
            editor.SelectionLength = error.Length;
            editor.CaretOffset = error.StartIndex;
            editor.Focus(); 
        }
    }

    private void OnRequestShowAbout()
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowDialog(this);
    }

    private void OnRequestShowDocumentation()
    {
        var docWindow = new DocumentationWindow();
        docWindow.ShowDialog(this);
    }

    private void OnRequestShowShortcuts()
    {
        var shortcutsWindow = new ShortcutsWindow();
        shortcutsWindow.ShowDialog(this);
    }

    // =========================================================
    // МЕТОДЫ ДЛЯ ПЕРЕИМЕНОВАНИЯ ДОКУМЕНТА
    // =========================================================

    // 1. Двойной клик по названию
    private void OnFileNameDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is DocumentViewModel vm)
        {
            vm.IsEditing = true;
        }
    }

    // 2. Клик по "Переименовать" в контекстном меню
    private void OnRenameMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && mi.DataContext is DocumentViewModel vm)
        {
            vm.IsEditing = true;
        }
    }

    // 3. Автоматический фокус на поле ввода при появлении
    private void OnEditTextBoxLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }

    // 4. Завершение редактирования (кликнули в другое место)
    private void OnFileNameEditLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is DocumentViewModel vm)
        {
            vm.IsEditing = false;
            
            if (string.IsNullOrWhiteSpace(vm.FileName)) 
                vm.FileName = "Новый документ";
        }
    }

    // 5. Завершение редактирования (нажали Enter или Escape)
    private void OnFileNameEditKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Escape)
        {
            if (sender is TextBox tb && tb.DataContext is DocumentViewModel vm)
            {
                vm.IsEditing = false;
                
                if (string.IsNullOrWhiteSpace(vm.FileName)) 
                    vm.FileName = "Новый документ";
                    
                this.Focus(); 
                e.Handled = true;
            }
        }
    }
}