﻿﻿using Avalonia;
using Serilog;
using System;
using System.IO;
using VolumetricSelection2077.Converters;

namespace VolumetricSelection2077;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            MessagePackConfig.ConfigureFormatters();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}