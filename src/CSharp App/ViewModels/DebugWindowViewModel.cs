using System.Collections.Generic;
using Avalonia.Controls;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.TestingStuff;

namespace VolumetricSelection2077.ViewModels;

public class DebugWindowViewModel
{
    public MainWindowViewModel? ParentViewModel { get; }
    public bool IsProcessing { get; set; }
    
    public Dictionary<string, IDebugTool> DebugServices { get; } = new()
    {
        {nameof(CompareNewSectorBoundsWithStreamingBlock), new CompareNewSectorBoundsWithStreamingBlock()},
        {nameof(ManualTaskPlayground), new ManualTaskPlayground()},
        {nameof(TestCache), new TestCache()},
        {nameof(TestCacheResizing), new TestCacheResizing()},
        {nameof(TestEulerQuatConversion), new TestEulerQuatConversion()},
        {nameof(TestGettingEmbeddedSectorFiles), new TestGettingEmbeddedSectorFiles()},
        {nameof(TestLoggerRefactor), new TestLoggerRefactor()},
        {nameof(TestPathValidator), new TestPathValidator()},
        {nameof(TestSectorAABBTime), new TestSectorAABBTime()},
        {nameof(TestShpereCheck), new TestShpereCheck()},
        {nameof(TestWheezeKitSerialization), new TestWheezeKitSerialization()},
        {nameof(BuildKnownBadSectorVerbose), new BuildKnownBadSectorVerbose()}
    };
    
    public DebugWindowViewModel(Window mainWindow)
    {
        ParentViewModel = (MainWindowViewModel?)mainWindow.DataContext;
    }
}