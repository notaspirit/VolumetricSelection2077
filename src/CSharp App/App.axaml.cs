using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VolumetricSelection2077.Parsers;
using VolumetricSelection2077.Services;
using VolumetricSelection2077.TestingStuff;
using VolumetricSelection2077.Views;

namespace VolumetricSelection2077;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var hostBuilder = Host.CreateApplicationBuilder();
        
        #region Singleton Services
        
        var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VolumetricSelection2077", "settings.json");
        hostBuilder.Services.AddSingleton<SettingsService>(new SettingsService(settingsPath));
        hostBuilder.Services.AddSingleton<Models.Progress>();
        hostBuilder.Services.AddSingleton<CacheService>();
        hostBuilder.Services.AddSingleton<GameFileService>();
        hostBuilder.Services.AddSingleton<ValidationService>();
        hostBuilder.Services.AddSingleton<SettingsViewPersistentCache>();
        hostBuilder.Services.AddSingleton<UpdateService>(sp => new UpdateService(
            sp.GetRequiredService<SettingsService>(),
            "notaspirit",
            "VolumetricSelection2077"));
        
        #endregion
        
        #region Transient Services
        
        hostBuilder.Services.AddTransient<BoundingBoxBuilderService>();
        hostBuilder.Services.AddTransient<PostProcessingService>();
        hostBuilder.Services.AddTransient<ProcessService>();
        hostBuilder.Services.AddTransient<ProcessDispatcher>();
        hostBuilder.Services.AddTransient<Benchmarking>();
        
        #endregion
        
        #region Debug Tools
        
        hostBuilder.Services.AddTransient<CompareNewSectorBoundsWithStreamingBlock>();
        hostBuilder.Services.AddTransient<ManualTaskPlayground>();
        hostBuilder.Services.AddTransient<TestCache>();
        hostBuilder.Services.AddTransient<TestCacheResizing>();
        hostBuilder.Services.AddTransient<TestEulerQuatConversion>();
        hostBuilder.Services.AddTransient<TestGettingEmbeddedSectorFiles>();
        hostBuilder.Services.AddTransient<TestLoggerRefactor>();
        hostBuilder.Services.AddTransient<TestPathValidator>();
        hostBuilder.Services.AddTransient<TestSectorAABBTime>();
        hostBuilder.Services.AddTransient<TestShpereCheck>();
        hostBuilder.Services.AddTransient<TestWheezeKitSerialization>();
        hostBuilder.Services.AddTransient<BuildKnownBadSectorVerbose>();
        hostBuilder.Services.AddTransient<AbbrMeshToBlender>();
        
        #endregion

        var desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        hostBuilder.Services.AddSingleton<DialogService>(new DialogService(desktop.MainWindow!));
        
        AppHost = hostBuilder.Build();
        AppHost.Start();
        
        desktop.MainWindow = new MainWindow();
        
        base.OnFrameworkInitializationCompleted();
    }
}