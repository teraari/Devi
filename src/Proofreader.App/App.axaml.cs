using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Proofreader.App.ViewModels;
using Proofreader.App.Views;
using Proofreader.Core.Interfaces;
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

            // Регистрируем наш словарь (он будет один на все приложение)
            services.AddSingleton<ISpellchecker, HunspellSpellchecker>();
            
            // Регистрируем ViewModel
            services.AddTransient<MainViewModel>();

            var serviceProvider = services.BuildServiceProvider();

            // Создаем окно и передаем ему ViewModel
            desktop.MainWindow = new MainWindow
            {
                DataContext = serviceProvider.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}