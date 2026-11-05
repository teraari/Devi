using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Proofreader.App.Services;
using Proofreader.App.ViewModels;
using Proofreader.App.Views;
using Proofreader.Core.Interfaces;
using Proofreader.Infrastructure.Files;
using Proofreader.Language;

namespace Proofreader.App;

public partial class App : global::Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Настраиваем DI-контейнер
            var services = new ServiceCollection();

            // Регистрируем сервисы
            services.AddSingleton<ISpellchecker, HunspellSpellchecker>();
            services.AddSingleton<ITextFileService, TextFileService>();
            services.AddTransient<MainViewModel>();

            // Создаем главное окно
            var mainWindow = new MainWindow();
            
            // Регистрируем окно и сервис диалогов (зависит от окна)
            services.AddSingleton<Window>(mainWindow);
            services.AddSingleton<IFileDialogService, FileDialogService>();

            var serviceProvider = services.BuildServiceProvider();

            // Передаем ViewModel в окно и показываем его
            mainWindow.DataContext = serviceProvider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}