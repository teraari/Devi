using System;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Proofreader.App.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty] private string _appVersion;
    [ObservableProperty] private string _dotnetVersion;
    [ObservableProperty] private string _osVersion;
    [ObservableProperty] private string _frameworkVersion;

    public AboutViewModel()
    {
        AppVersion = "1.0.0 (MVP)";
        DotnetVersion = Environment.Version.ToString();
        OsVersion = RuntimeInformation.OSDescription;
        FrameworkVersion = "Avalonia UI 12.0+";
    }
}