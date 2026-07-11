using System.Collections.Generic;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.TestingStuff;

namespace VolumetricSelection2077.ViewModels;

public class DebugWindowViewModel
{
    public MainWindowViewModel? ParentViewModel { get; }
    public bool IsProcessing { get; set; }
    
    public Dictionary<string, IDebugTool> DebugServices { get; }
    
    public DebugWindowViewModel(Window mainWindow)
    {
        ParentViewModel = (MainWindowViewModel?)mainWindow.DataContext;
        var sp = App.AppHost!.Services;
        DebugServices = new()
        {
            {nameof(CompareNewSectorBoundsWithStreamingBlock), sp.GetRequiredService<CompareNewSectorBoundsWithStreamingBlock>()},
            {nameof(ManualTaskPlayground), sp.GetRequiredService<ManualTaskPlayground>()},
            {nameof(TestCache), sp.GetRequiredService<TestCache>()},
            {nameof(TestCacheResizing), sp.GetRequiredService<TestCacheResizing>()},
            {nameof(TestEulerQuatConversion), sp.GetRequiredService<TestEulerQuatConversion>()},
            {nameof(TestGettingEmbeddedSectorFiles), sp.GetRequiredService<TestGettingEmbeddedSectorFiles>()},
            {nameof(TestLoggerRefactor), sp.GetRequiredService<TestLoggerRefactor>()},
            {nameof(TestPathValidator), sp.GetRequiredService<TestPathValidator>()},
            {nameof(TestSectorAABBTime), sp.GetRequiredService<TestSectorAABBTime>()},
            {nameof(TestShpereCheck), sp.GetRequiredService<TestShpereCheck>()},
            {nameof(TestWheezeKitSerialization), sp.GetRequiredService<TestWheezeKitSerialization>()},
            {nameof(BuildKnownBadSectorVerbose), sp.GetRequiredService<BuildKnownBadSectorVerbose>()},
            {nameof(AbbrMeshToBlender), sp.GetRequiredService<AbbrMeshToBlender>()}
        };
    }
}