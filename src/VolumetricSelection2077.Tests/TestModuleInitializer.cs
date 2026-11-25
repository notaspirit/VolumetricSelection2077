using System.Runtime.CompilerServices;
using VolumetricSelection2077.Core.MessagePack.Helpers;

namespace VolumetricSelection2077.Tests;

/// <summary>
/// Configures the MessagePack formatters before any test runs, matching production startup.
/// </summary>
internal static class TestModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        MessagePackConfig.ConfigureFormatters();
    }
}
