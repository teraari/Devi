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
using Proofreader.Language.Grammar;
using System;
using System.Threading.Tasks;

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
            var services = new ServiceCollection();

            services.AddSingleton<IDictionaryService, JsonDictionaryService>();
            services.AddSingleton<ISpellchecker, HunspellSpellchecker>();
            services.AddSingleton<IGrammarChecker, LocalGrammarChecker>();
            services.AddSingleton<IFileService, FileService>();
            
            services.AddSingleton<IProofreaderEngine, ProofreaderEngine>();

            services.AddTransient<MainViewModel>();

            var mainWindow = new MainWindow();
            services.AddSingleton<Window>(mainWindow);
            services.AddSingleton<IFileDialogService, FileDialogService>();

            var serviceProvider = services.BuildServiceProvider();

            // НОВОЕ: "Прогрев" словаря в фоновом потоке при старте.
            // Это загрузит 3.5 МБ словарь Hunspell заранее, чтобы первая проверка была мгновенной.
            _ = Task.Run(() => 
            {
                try
                {
                    serviceProvider.GetRequiredService<ISpellchecker>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка предзагрузки словаря: {ex.Message}");
                }
            });

            mainWindow.DataContext = serviceProvider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}