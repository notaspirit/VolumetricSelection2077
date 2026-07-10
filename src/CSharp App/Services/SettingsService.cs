using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VolumetricSelection2077.Enums;
using VolumetricSelection2077.Json.Helpers;
using VolumetricSelection2077.Models;
using VolumetricSelection2077.Models.SettingsDTO;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace VolumetricSelection2077.Services;
public partial class SettingsService : ObservableObject
{
    private static SettingsService? _instance;
    private static readonly object Lock = new object();
    private readonly string _settingsFilePath;

    private SettingsService()
    {
        ProgramVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split("+")[0] ?? "Version not found.";
        LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "Logs");
        _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VolumetricSelection2077", "settings.json");
        Data = new SettingsDTOV0();
    }

    public static SettingsService Instance
    {
        get
        {
            lock (Lock)
            {
                if (_instance != null) 
                    return _instance;
                _instance = new SettingsService();
                _instance.LoadSettings();
                return _instance;
            }
        }
    }

    [ObservableProperty] private SettingsDTOV0 _data;
    public static string MinimumCacheVersion => "1000.0.0-beta11";
    public readonly string LogDirectory;
    public string ProgramVersion { get; }
    
    /// <summary>
    /// Loads the settings or creates a new settings file if it doesn't exist
    /// </summary>
    public void LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            SaveSettings();
        }
        else
        {
            try
            {
                Data.Deserialize(File.ReadAllText(_settingsFilePath));
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Failed to load settings.");
            }
        }
        
        Directory.CreateDirectory(Data.CacheDirectory);
        Directory.CreateDirectory(Data.BackupDirectory);
        Directory.CreateDirectory(Data.OutputDirectory);
        Directory.CreateDirectory(LogDirectory);
    }

    /// <summary>
    /// Saves the settings
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            var json = Data.Serialize();
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Failed to save settings.");
        }
    }
}