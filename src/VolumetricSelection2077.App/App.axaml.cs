using System;
using System.IO;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VolumetricSelection2077.App.Services;
using VolumetricSelection2077.App.TestingStuff;
using VolumetricSelection2077.App.Views;
using VolumetricSelection2077.Core.Converters.Complex;
using VolumetricSelection2077.Core.Parsers;
using VolumetricSelection2077.Core.Services;
using WolvenKit;
using WolvenKit.Common;
using WolvenKit.Common.Services;
using WolvenKit.Core.Interfaces;
using WolvenKit.Core.Services;
using WolvenKit.RED4.CR2W;
using WolvenKit.RED4.CR2W.Archive;

namespace VolumetricSelection2077.App;

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
        hostBuilder.Services.AddSingleton<HttpClient>();
        hostBuilder.Services.AddSingleton<SettingsService>(new SettingsService(settingsPath));
        hostBuilder.Services.AddSingleton<IProgressService, AppProgressService>();
        hostBuilder.Services.AddSingleton<ICacheService, AppCacheService>();
        hostBuilder.Services.AddSingleton<ILoggerService>(new SerilogWrapper());
        hostBuilder.Services.AddSingleton<IProgressService<double>, ProgressService<double>>();
        hostBuilder.Services.AddSingleton<IHashService, HashService>();
        hostBuilder.Services.AddSingleton<IHookService, HookService>();
        hostBuilder.Services.AddSingleton<Red4ParserService>();
        hostBuilder.Services.AddSingleton<IArchiveManager, ArchiveManager>();
        hostBuilder.Services.AddSingleton<GeometryCacheService>();
        hostBuilder.Services.AddSingleton<IVSSectorParser, VSSectorParser>();
        hostBuilder.Services.AddSingleton<IGameFileService, GameFileService>();
        hostBuilder.Services.AddSingleton<IAxlRemovalToWorldBuilderConverter, AxlRemovalToWorldBuilderConverter>();
        hostBuilder.Services.AddSingleton<ValidationService>();
        hostBuilder.Services.AddSingleton<SettingsViewPersistentCache>();
        hostBuilder.Services.AddSingleton<UpdateService>(sp => new UpdateService(
            sp.GetRequiredService<SettingsService>(),
            "notaspirit",
            "VolumetricSelection2077"));
        hostBuilder.Services.AddSingleton<DialogService>();
        
        #endregion
        
        #region Transient Services
        
        hostBuilder.Services.AddTransient<ISectorInfoBuildingService, SectorInfoBuildingService>();
        hostBuilder.Services.AddTransient<PostProcessingService>();
        hostBuilder.Services.AddTransient<IWorldQueryService, WorldQueryService>();
        hostBuilder.Services.AddTransient<ProcessDispatcher>();
        
        #endregion
        
        #region Debug Tools
        
        hostBuilder.Services.AddTransient<TestCache>();
        hostBuilder.Services.AddTransient<TestCacheResizing>();
        hostBuilder.Services.AddTransient<TestEulerQuatConversion>();
        hostBuilder.Services.AddTransient<TestGettingEmbeddedSectorFiles>();
        hostBuilder.Services.AddTransient<TestLoggerRefactor>();
        hostBuilder.Services.AddTransient<TestPathValidator>();
        hostBuilder.Services.AddTransient<TestSectorAABBTime>();
        hostBuilder.Services.AddTransient<TestShpereCheck>();
        hostBuilder.Services.AddTransient<TestWheezeKitSerialization>();
        hostBuilder.Services.AddTransient<Benchmarking>();
        
        #endregion
        
        AppHost = hostBuilder.Build();
        AppHost.Start();
        
        var desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        desktop.MainWindow = new MainWindow();
        
        AppHost.Services.GetRequiredService<DialogService>().Owner = desktop.MainWindow;
        
        base.OnFrameworkInitializationCompleted();
    }
}